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

namespace MusicTooling
{
    [ReactModule("FileScannerModule")]
    public sealed class FileScanner
    {
        [ReactMethod("ScanFiles")]
        public async Task ScanFilesAsync()
        {
            string path = @"C:\Dan\Dropbox\Dropbox\[Music]\[Folk]\[Acapella]";
            React.AssertFileAccess();

            try
            {
                var results = await ScanFiles.ScanFilesAsync();

                results.Match(
                    Right: list => {
                        var filteredList = list.Where(song => song.Rating >= ObservableState.Current.MinimumRating);

                        // Update state with new songs - this will trigger UI update
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