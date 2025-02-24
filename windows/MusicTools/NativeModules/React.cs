using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using System.IO;
using Microsoft.ReactNative.Managed;
using Windows.Security.Authorization.AppCapabilityAccess;

namespace MusicTools.NativeModules
{
    public static class React
    {
        public static async Task<Seq<string>> GetFilesWithExtensionAsync(string folderPath, string extension)
        {
            var files = new List<string>();
            var rootFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            await GetFilesRecursively(rootFolder, extension, files);
            return files.ToSeq();
        }

        static async Task GetFilesRecursively(StorageFolder folder, string extension, List<string> files)
        {
            var foundFiles = await folder.GetFilesAsync();
            files.AddRange(foundFiles.Where(file => file.FileType.Equals(extension, StringComparison.OrdinalIgnoreCase))
                                     .Select(file => file.Path));

            var subfolders = await folder.GetFoldersAsync();

            foreach (var subfolder in subfolders)
            {
                await GetFilesRecursively(subfolder, extension, files);
            }
        }

        public static void AssertFileAccess()
        {
            var status = AppCapability.Create("broadFileSystemAccess").CheckAccess();
            if (status != AppCapabilityAccessStatus.Allowed)
            {
                throw new ReactException("File access needs to be granted for this app in Privacy & Security -> File system");
            }
        }

        public static async Task WithStream(string path, Func<Stream, Task> action)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using (IRandomAccessStream randomStream = await file.OpenAsync(FileAccessMode.Read))
            using (Stream stream = randomStream.AsStreamForRead())
            {
                await action(stream);
            }
        }
    }
}
