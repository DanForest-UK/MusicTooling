using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.ReactNative;
using Microsoft.ReactNative.Managed;

namespace MusicTooling
{
    public class FileInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
    }

    [ReactModule("FileScannerModule")]
    public sealed class FileScanner
    {
        [ReactMethod("ScanFiles")]
        public async Task<IList<FileInfo>> ScanFilesAsync()
        {
            try
            {
                List<FileInfo> files = new List<FileInfo>
                {
                    new FileInfo { Name = "File1.mp3", Size = 123456, Path = "C:/Music/File1.mp3" },
                    new FileInfo { Name = "File2.mp3", Size = 234567, Path = "C:/Music/File2.mp3" }
                };

                return files;
            }
            catch (Exception ex)
            {
                throw new ReactException("ScanFilesError", ex);
            }
        }
    }
}
