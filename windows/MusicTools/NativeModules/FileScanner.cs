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
using LanguageExt;

namespace MusicTooling
{
    [ReactModule("FileScannerModule")]
    public sealed class FileScanner
    {
        [ReactMethod("ScanFiles")]
        public async Task<IList<SongInfo>> ScanFilesAsync()
        {
            string path = @"C:\Dan\Dropbox\Dropbox\[Music]\[Folk]\[Acapella]";

            var status = AppCapability.Create("broadFileSystemAccess").CheckAccess();
            if (status != AppCapabilityAccessStatus.Allowed)
            {
                throw new ReactException("File access needs to be granted for this app in Privacy & Security -> File system");
            }
   
            try
            {
                var list = new List<SongInfo>();
                var mp3Files = await GetFilesWithExtensionAsync(path, ".mp3");
                mp3Files.Iter(async file => await AddFileInfo(file.Path, list));

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

        async Task AddFileInfo(string path, List<SongInfo> list)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);

                // Open the file as a stream
                using (IRandomAccessStream randomStream = await file.OpenAsync(FileAccessMode.Read))
                using (Stream stream = randomStream.AsStreamForRead()) // Convert IRandomAccessStream to .NET Stream
                {
                    // Use TagLib to read metadata
                    var tagFile = TagLib.File.Create(new StreamFileAbstraction(file.Name, stream));
                    list.Add(new SongInfo(
                        Id: Guid.NewGuid().ToString(),
                        Name: tagFile.Tag.Title.ValueOrNone().IfNone("[No title]"),
                        Path: path,
                        Artist: tagFile.Tag.AlbumArtists.Union(tagFile.Tag.Artists).ToArray(),
                        Album: tagFile.Tag.Album.ValueOrNone().IfNone("[No album]")));                   
                }
            }
            catch (Exception ex)
            {
                // todo we want to return a list of errors
                Debug.WriteLine($"Error reading metadata for {path}: {ex.Message}");
            }
        }

        async Task<List<StorageFile>> GetFilesWithExtensionAsync(string folderPath, string extension)
        {
            var files = new List<StorageFile>();
            var rootFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            await GetFilesRecursively(rootFolder, extension, files);
            return files;
        }

        async Task GetFilesRecursively(StorageFolder folder, string extension, List<StorageFile> files)
        {
            var foundFiles = await folder.GetFilesAsync();
            files.AddRange(foundFiles.Where(file => file.FileType.Equals(extension, StringComparison.OrdinalIgnoreCase)));

            var subfolders = await folder.GetFoldersAsync();

            foreach (var subfolder in subfolders)
            {
                await GetFilesRecursively(subfolder, extension, files);
            }
        }
    }
}
