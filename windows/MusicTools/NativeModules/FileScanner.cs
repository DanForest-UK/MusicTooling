using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.ReactNative;
using Microsoft.ReactNative.Managed;
using System.IO;

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
            string filePath = @"C:\Dan\Dropbox\Dropbox\[NewMusic]\new 16";

            try
            {
                var list = new List<FileInfo>();
                foreach (var song in Directory.GetFiles(filePath, "*.mp3", SearchOption.AllDirectories))
                {
                    var file = TagLib.File.Create(song);
                    list.Add(new FileInfo
                    {
                        Name = file.Tag.Title,
                        Size = file.Length,
                        Path = song
                    });
                }
                return list;
            }
            catch (UnauthorizedAccessException exx)
            {
                throw new ReactException($"Access to: {filePath} is denied", exx);
            }
            catch (Exception ex)
            {
                throw new ReactException("ScanFilesError", ex);
            }
        }
    }
}
