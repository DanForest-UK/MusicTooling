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
using MusicTools.Core;
using static MusicTools.Core.Types;
using MusicTools.Logic;
using MusicTools.NativeModules;

namespace MusicTooling
{
    [ReactModule("FileScannerModule")]
    public sealed class FileScanner
    {
        [ReactMethod("ScanFiles")]
        public async Task<IList<SongInfo>> ScanFilesAsync(int minRating)
        {
            string path = @"C:\Dan\Dropbox\Dropbox\[Music]\[Folk]\[Acapella]";

            React.AssertFileAccess();

            var resutls = await ScanFiles.ScanFilesAsync();

            return resutls.Match(
                Right: list => list.Where(song => song.Rating >= minRating).ToList(),               
                Left: error => throw new ReactException(error.Message)
            );
        }
    }       
}
