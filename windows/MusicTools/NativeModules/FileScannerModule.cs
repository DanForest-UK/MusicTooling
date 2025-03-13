using System;
using System.Threading.Tasks;
using Microsoft.ReactNative;
using Microsoft.ReactNative.Managed;
using LanguageExt;
using static MusicTools.Core.Types;
using MusicTools.Core;
using MusicTools.Logic;
using System.Linq;
using MusicTools.NativeModules;
using static LanguageExt.Prelude;

namespace MusicTools.NativeModules
{
    [ReactModule("FileScannerModule")]
    public sealed class FileScannerModule
    {
        /// <summary>
        /// React method that scans for music files and updates application state with filtered songs
        /// </summary>
        [ReactMethod("ScanFiles")]
        public async Task ScanFilesAsync()
        {
            var path = @"C:\Dan\Dropbox\Dropbox\[Music]\[Folk]\[Acapella]";
            React.AssertFileAccess();

            Runtime.Info("Starting music file scan...");

            var results = await ScanFiles.ScanFilesAsync();

            // Error handling is done in the ScanFiles      
            results.Match(
                Right: list =>
                {
                    var filteredList = from song in list
                                       where song.Rating >= ObservableState.Current.MinimumRating
                                       select song;

                    Runtime.Info($"Found {list.Count()} music files, filtering...");
                    ObservableState.SetSongs(filteredList);

                    Runtime.Success($"Scan complete: {filteredList.Count()} files match your criteria");
                    return unit;
                },
                Left: error => unit);
        }
    }
}