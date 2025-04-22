using MusicTools.Logic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using MusicTools.Domain;

namespace MusicTools.NativeModules
{
    /// <summary>
    /// Service to handle saving and loading application state from disk
    /// </summary>
    public static class PersistedStateService
    {
        // File name for persisted state
        private const string STATE_FILE_NAME = "musictools_state.json";

        // Semaphore to prevent concurrent file access
        private static readonly SemaphoreSlim fileLock = new SemaphoreSlim(1, 1);

        // Rate limiting - don't save more than once per 2 seconds
        private static readonly TimeSpan SaveThrottleInterval = TimeSpan.FromSeconds(2);
        private static DateTime lastSaveTime = DateTime.MinValue;
        private static bool pendingSave = false;
        private static AppModel pendingState = null;

        // Initialize the service by subscribing to state changes
        public static void Initialize()
        {
            ObservableState.StateChanged += OnStateChanged;
            Debug.WriteLine("State persistence service initialized");
        }

        // Cleanup by unsubscribing from state changes
        public static void Cleanup()
        {
            ObservableState.StateChanged -= OnStateChanged;
            Debug.WriteLine("State persistence service cleaned up");
        }

        // Handler for state change events - throttles saves and prevents file contention
        private static void OnStateChanged(object sender, AppModel state)
        {
            pendingState = state;

            // If we already have a pending save, just update the state and return
            if (pendingSave)
            {
                return;
            }

            var timeSinceLastSave = DateTime.Now - lastSaveTime;

            // If it's been long enough since the last save, save immediately
            if (timeSinceLastSave > SaveThrottleInterval)
            {
                // Save state asynchronously
                Task.Run(() => SaveStateToDiskAsync(state));
                lastSaveTime = DateTime.Now;
            }
            else
            {
                // Otherwise, schedule a save after the throttle interval
                pendingSave = true;

                // Use a separate task to wait and then save
                Task.Run(async () => {
                    try
                    {
                        // Wait for the remaining time in the throttle interval
                        var delayTime = SaveThrottleInterval - timeSinceLastSave;
                        await Task.Delay(delayTime);

                        // Save the most recent pending state
                        await SaveStateToDiskAsync(pendingState);
                        lastSaveTime = DateTime.Now;
                    }
                    finally
                    {
                        pendingSave = false;
                        pendingState = null;
                    }
                });
            }
        }

        /// <summary>
        /// Saves the current state to disk asynchronously with file locking
        /// </summary>
        private static async Task SaveStateToDiskAsync(AppModel state)
        {
            // Skip empty states and clear state if there are no songs
            if (state == null || state.Songs == null || state.Songs.Count == 0)
            {
                await DeleteSavedStateAsync();
                return;
            }

            // Only allow one save operation at a time
            await fileLock.WaitAsync();

            try
            {
                // Create a copy of the state with clean references to prevent serialization issues
                var stateCopy = new StateDto
                {
                    Songs = state.Songs.Values.ToDictionary(s => s.Id.Value),
                    ChosenSongs = state.ChosenSongs,
                    MinimumRating = state.MinimumRating
                };

                // Serialize to JSON
                var json = JsonConvert.SerializeObject(stateCopy, Formatting.None);

                // Get the local app data folder
                var localFolder = ApplicationData.Current.LocalFolder;

                try
                {
                    // Use atomic write pattern - write to temp file first, then replace
                    var tempFileName = STATE_FILE_NAME + ".temp";
                    var tempFile = await localFolder.CreateFileAsync(tempFileName, CreationCollisionOption.ReplaceExisting);

                    // Write the file
                    await FileIO.WriteTextAsync(tempFile, json);

                    // Get the target file or create it if it doesn't exist
                    StorageFile targetFile;
                    try
                    {
                        targetFile = await localFolder.GetFileAsync(STATE_FILE_NAME);
                    }
                    catch
                    {
                        targetFile = await localFolder.CreateFileAsync(STATE_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                    }

                    // Replace the target file with the temp file
                    await tempFile.RenameAsync(STATE_FILE_NAME, NameCollisionOption.ReplaceExisting);

                    Debug.WriteLine($"State saved to disk: {targetFile.Path}");
                }
                catch (Exception ex)
                {
                    Runtime.Error($"Error during file write operation", ex);
                }
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error saving state to disk", ex);
            }
            finally
            {
                // Always release the lock
                fileLock.Release();
            }
        }

        /// <summary>
        /// Checks if a saved state file exists
        /// </summary>
        public static async Task<bool> HasSavedStateAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var item = await localFolder.TryGetItemAsync(STATE_FILE_NAME);
                return item != null;
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error checking for saved state", ex);
                return false;
            }
        }

        /// <summary>
        /// Loads the state from disk and updates the application state
        /// </summary>
        public static async Task<bool> LoadStateFromDiskAsync()
        {
            // Acquire the file lock to prevent concurrent access
            await fileLock.WaitAsync();

            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.TryGetItemAsync(STATE_FILE_NAME) as StorageFile;

                if (file == null)
                {
                    Debug.WriteLine("No saved state file found");
                    return false;
                }

                // Read the file content
                var json = await FileIO.ReadTextAsync(file);

                if (string.IsNullOrEmpty(json))
                {
                    Debug.WriteLine("State file is empty");
                    return false;
                }

                // Deserialize
                var stateDto = JsonConvert.DeserializeObject<StateDto>(json);

                if (stateDto == null || stateDto.Songs == null)
                {
                    Debug.WriteLine("Failed to deserialize state or state has no songs");
                    return false;
                }

                // Convert back to AppModel and update
                ObservableState.SetPersistedState(
                    songs: stateDto.Songs,
                    chosenSongs: stateDto.ChosenSongs,
                    minimumRating: stateDto.MinimumRating
                );

                Debug.WriteLine($"State loaded from disk with {stateDto.Songs.Count} songs");
                return true;
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error loading state from disk", ex);
                return false;
            }
            finally
            {
                // Always release the lock
                fileLock.Release();
            }
        }

        /// <summary>
        /// Deletes the saved state file
        /// </summary>
        public static async Task DeleteSavedStateAsync()
        {
            // Acquire the file lock to prevent concurrent access
            await fileLock.WaitAsync();

            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.TryGetItemAsync(STATE_FILE_NAME) as StorageFile;

                if (file != null)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    Debug.WriteLine("Saved state file deleted");
                }

                // Also try to clean up any temp files that might be left over
                try
                {
                    var tempFile = await localFolder.TryGetItemAsync(STATE_FILE_NAME + ".temp") as StorageFile;
                    if (tempFile != null)
                    {
                        await tempFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
                catch
                {
                    // Ignore errors cleaning up temp files
                }
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error deleting saved state", ex);
            }
            finally
            {
                // Always release the lock
                fileLock.Release();
            }
        }
    }

    /// <summary>
    /// Data transfer object for serialization
    /// </summary>
    public class StateDto
    {
        public Dictionary<int, SongInfo> Songs { get; set; }
        public SongId[] ChosenSongs { get; set; }
        public SongRating MinimumRating { get; set; }
    }
}