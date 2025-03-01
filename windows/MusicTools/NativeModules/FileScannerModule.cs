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

            try
            {
                var results = await ScanFiles.ScanFilesAsync();

                results.Match(
                    Right: list =>
                    {
                        var filteredList = from song in list
                                           where song.Rating >= ObservableState.Current.MinimumRating
                                           select song;

                        ObservableState.SetSongs(filteredList);
                        return unit;
                    },
                    Left: error => throw new ReactException(error.Message)
                );
            }
            catch (Exception ex)
            {
                throw new ReactException(ex.Message);
            }
        }
    }
}