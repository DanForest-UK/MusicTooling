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
using static LanguageExt.Prelude;
using MusicTools.Logic;

namespace MusicTools.NativeModules
{
    public static class React
    {
        /// <summary>
        /// Gets files with specific extension from a folder
        /// </summary>
        public static async Task<Seq<string>> GetFilesWithExtensionAsync(string folderPath, string extension)
        {
            var files = new List<string>();
            var rootFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            await GetFilesRecursively(rootFolder, extension, files);
            return files.ToSeq();
        }

        /// <summary>
        /// Recursively gets files with the specified extension from folders and subfolders
        /// </summary>
        static async Task GetFilesRecursively(StorageFolder folder, string extension, List<string> files)
        {
            var foundFiles = await folder.GetFilesAsync();

            var matchingFiles = from file in foundFiles
                                where file.FileType.Equals(extension, StringComparison.OrdinalIgnoreCase)
                                select file.Path;

            files.AddRange(matchingFiles);

            var subfolders = await folder.GetFoldersAsync();

            foreach (var subfolder in subfolders)
                await GetFilesRecursively(subfolder, extension, files);
        }

        /// <summary>
        /// Verifies file system access permissions
        /// </summary>
        public static void AssertFileAccess()
        {
            var status = AppCapability.Create("broadFileSystemAccess").CheckAccess();

            if (status != AppCapabilityAccessStatus.Allowed)
                Runtime.Error("File access needs to be granted for this app in Privacy & Security -> File system", None);            
        }

        /// <summary>
        /// Performs an action with a file stream
        /// </summary>
        public static async Task WithStream(string path, Func<Stream, Task> action)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using (var randomStream = await file.OpenAsync(FileAccessMode.Read))
            using (var stream = randomStream.AsStreamForRead())
                await action(stream);
        }
    }
}