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
using System.Diagnostics;

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
            React.AssertFileAccess();

            var results = await ScanFiles.ScanFilesAsync();

            // Error handling is done in the ScanFiles      
            results.Match(
                Right: list =>
                {
                    var filteredList = from song in list
                                       where song.Rating >= ObservableState.Current.MinimumRating
                                       select song;

                    ObservableState.SetSongs(filteredList);
                    return unit;
                },
                Left: error => unit);
        }

        /// <summary>
        /// React method that cancels any ongoing file scan operation
        /// </summary>
        [ReactMethod("CancelScan")]
        public void CancelScan()
        {
            try
            {
                ScanFiles.CancelScan();
                Runtime.Info("Scan cancelled by user");
            }
            catch (Exception ex)
            {
                Runtime.Error("Error cancelling scan", ex);
            }
        }
    }
}