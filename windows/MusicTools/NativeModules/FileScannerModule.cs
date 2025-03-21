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
using Windows.Storage.Pickers;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace MusicTools.NativeModules
{
    [ReactModule("FileScannerModule")]
    public sealed class FileScannerModule
    {
        // Default music folder path - used if no path is specified
        private static string musicFolderPath = @"C:\Dan\Dropbox\[Music]\[Folk]";

        /// <summary>
        /// React method that returns the current music folder path
        /// </summary>
        [ReactMethod("GetMusicFolderPath")]
        public Task<string> GetMusicFolderPathAsync()
        {
            return Task.FromResult(musicFolderPath);
        }

        /// <summary>
        /// React method that opens a folder picker for the user to select a music folder
        /// </summary>
        [ReactMethod("BrowseFolders")]
        public void BrowseFoldersAsync(IReactPromise<string> promise)
        {
            try
            {
                var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher == null)
                {
                    Runtime.Error("Could not get dispatcher", None);
                    promise.Reject(new ReactError { Message = "Could not get UI dispatcher" });
                    return;
                }

                // Run the picker on the UI thread
                var ignoreResult = dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        var picker = new FolderPicker
                        {
                            SuggestedStartLocation = PickerLocationId.MusicLibrary
                        };
                        picker.FileTypeFilter.Add("*");

                        var folder = await picker.PickSingleFolderAsync();

                        if (folder != null)
                        {
                            musicFolderPath = folder.Path;
                            Runtime.Info($"Selected folder: {musicFolderPath}");
                            promise.Resolve(musicFolderPath);
                        }
                        else
                        {
                            Runtime.Info("Folder selection canceled by user");
                            promise.Resolve(musicFolderPath); // Return existing path if selection was canceled
                        }
                    }
                    catch (Exception ex)
                    {
                        Runtime.Error("Error in folder picker", ex);
                        promise.Reject(new ReactError { Message = $"Error selecting folder: {ex.Message}" });
                    }
                });
            }
            catch (Exception ex)
            {
                Runtime.Error("Error launching folder picker", ex);
                promise.Reject(new ReactError { Message = $"Error launching folder picker: {ex.Message}" });
            }
        }

        /// <summary>
        /// React method that scans for music files and updates application state with filtered songs
        /// </summary>
        [ReactMethod("ScanFiles")]
        public async Task ScanFilesAsync(string path = null)
        {
            React.AssertFileAccess();

            // Use the provided path if available, otherwise use the stored path
            var folderPath = string.IsNullOrWhiteSpace(path) ? musicFolderPath : path;

            // Update the stored path for future use
            musicFolderPath = folderPath;

            var results = await ScanFiles.ScanFilesAsync(folderPath);

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