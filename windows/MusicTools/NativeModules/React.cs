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
using System.Threading;

namespace MusicTools.NativeModules
{
    public static class React
    {
        /// <summary>
        /// Gets files with specific extension from a folder
        /// </summary>
        public static async Task<Seq<string>> GetFilesWithExtensionAsync(string folderPath, string extension, CancellationToken cancellationToken = default)
        {
            var files = new List<string>();
            var rootFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            await GetFilesRecursively(rootFolder, extension, files, cancellationToken);
            return files.ToSeq();
        }

        /// <summary>
        /// Recursively gets files with the specified extension from folders and subfolders
        /// </summary>
        static async Task GetFilesRecursively(StorageFolder folder, string extension, List<string> files, CancellationToken cancellationToken)
        {
            // Check for cancellation before processing folder
            CancellationHelper.CheckForCancel(cancellationToken, "File scanning");

            Runtime.Info($"Scanning folder {folder.Path}");

            var foundFiles = await folder.GetFilesAsync();

            // Check for cancellation after getting files
            CancellationHelper.CheckForCancel(cancellationToken, "File scanning");

            var matchingFiles = from file in foundFiles
                                where file.FileType.Equals(extension, StringComparison.OrdinalIgnoreCase)
                                select file.Path;

            files.AddRange(matchingFiles);

            var subfolders = await folder.GetFoldersAsync();

            // Check for cancellation after getting subfolders
            CancellationHelper.CheckForCancel(cancellationToken, "File scanning");

            foreach (var subfolder in subfolders)
            {
                // Check for cancellation before processing each subfolder
                CancellationHelper.CheckForCancel(cancellationToken, "File scanning");

                await GetFilesRecursively(subfolder, extension, files, cancellationToken);
            }
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