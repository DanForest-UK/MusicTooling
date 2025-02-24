using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.ReactNative;
using Microsoft.ReactNative.Managed;
using System.IO;
using System.Diagnostics;
using Windows.Security.Authorization.AppCapabilityAccess;
using TagLib.Jpeg;
using Windows.Storage.Streams;
using MusicTools;
using System.Linq;

namespace MusicTooling
{
    public class FileInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
    }

    public class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        private readonly Stream _stream;

        public StreamFileAbstraction(string name, Stream stream)
        {
            Name = name;
            _stream = stream;
        }

        public string Name { get; }

        public Stream ReadStream => _stream;

        public Stream WriteStream => throw new NotSupportedException("This file abstraction is read-only.");

        public void CloseStream(Stream stream)
        {
            stream?.Dispose();
        }
    }



    [ReactModule("FileScannerModule")]
    public sealed class FileScanner
    {
        [ReactMethod("ScanFiles")]
        public async Task<IList<FileInfo>> ScanFilesAsync()
        {
            string path = @"C:\Dan\Dropbox\Dropbox\[NewMusic]";

            var status = AppCapability.Create("broadFileSystemAccess").CheckAccess();
            if (status != AppCapabilityAccessStatus.Allowed)
            {
                throw new ReactException("File access needs to be granted for this app in Privacy & Security -> File system");
            }
   
            try
            {
                var list = new List<FileInfo>();

                var mp3Files = await GetFilesWithExtensionAsync(path, ".mp3");

                foreach (var mp3 in mp3Files)
                {
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(mp3.Path);

                        // Open the file as a stream
                        using (IRandomAccessStream randomStream = await file.OpenAsync(FileAccessMode.Read))
                        using (Stream stream = randomStream.AsStreamForRead()) // Convert IRandomAccessStream to .NET Stream
                        {
                            // Use TagLib to read metadata
                            var tagFile = TagLib.File.Create(new StreamFileAbstraction(file.Name, stream));
                            list.Add(new FileInfo
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = tagFile.Tag.Title.ValueOrNone().IfNone("[No title]"),
                                Size = 10,
                                Path = mp3.Path
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error reading metadata for {mp3.Path}: {ex.Message}");
                    }
                }
                return list;
            }
            catch (UnauthorizedAccessException exx)
            {
                throw new ReactException($"Access to: {path} is denied", exx);
            }
            catch (Exception ex)
            {
                throw new ReactException("ScanFilesError", ex);
            }
        }

        public async Task<List<StorageFile>> GetFilesWithExtensionAsync(string folderPath, string extension)
        {
            List<StorageFile> files = new List<StorageFile>();
            StorageFolder rootFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);

            await GetFilesRecursively(rootFolder, extension, files);
            return files;
        }

        private async Task GetFilesRecursively(StorageFolder folder, string extension, List<StorageFile> files)
        {
            // Get all files in the current folder with the specified extension
            var foundFiles = await folder.GetFilesAsync();
            files.AddRange(foundFiles.Where(file => file.FileType.Equals(extension, StringComparison.OrdinalIgnoreCase)));

            // Get all subfolders
            var subfolders = await folder.GetFoldersAsync();

            // Recursively process each subfolder
            foreach (var subfolder in subfolders)
            {
                await GetFilesRecursively(subfolder, extension, files);
            }
        }
    }
}
