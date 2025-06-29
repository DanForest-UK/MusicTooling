﻿using Microsoft.ReactNative.Managed;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using MusicTools.Logic;
using System.Linq;
using MusicTools.Domain;
using static LanguageExt.Prelude;

namespace MusicTools.NativeModules
{
    [ReactModule("StateModule")]
    public sealed class StateModule : IDisposable
    {
        // Constants for event names
        public const string STATE_UPDATED_EVENT = "appStateUpdated";
        public const string SAVED_STATE_EVENT = "savedStateAvailable";

        // Field to hold the React context
        ReactContext reactContext;

        // Flag to track if we're subscribed to state changes
        private bool isSubscribed = false;

        /// <summary>
        /// Default constructor for React Native code generation
        /// </summary>
        public StateModule()
        { }

        /// <summary>
        /// Initialize method called by React Native runtime
        /// </summary>
        [ReactInitializer]
        public void Initialize(ReactContext reactContext)
        {
            this.reactContext = reactContext;

            // Subscribe to state changes if not already subscribed
            SubscribeToStateChanges();

            // Initialize the state persistence service
            PersistedStateService.Initialize();

            // Check for saved state on startup
            CheckForSavedState();
        }

        /// <summary>
        /// Subscribes to state change events from ObservableState
        /// </summary>
        private void SubscribeToStateChanges()
        {
            if (!isSubscribed)
            {
                ObservableState.StateChanged += OnStateChanged;
                isSubscribed = true;
                System.Diagnostics.Debug.WriteLine("Subscribed to ObservableState changes");
            }
        }

        /// <summary>
        /// Handler for state change events
        /// </summary>
        private void OnStateChanged(object sender, AppModel newState)
        {
            try
            {
                EmitStateUpdated(newState);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling state change: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for saved state and notifies React if found
        /// </summary>
        private async void CheckForSavedState()
        {
            try
            {
                var hasSavedState = await PersistedStateService.HasSavedStateAsync();
                if (hasSavedState)
                {
                    JsEmitterHelper.EmitEvent(reactContext, SAVED_STATE_EVENT, new { available = true });
                    System.Diagnostics.Debug.WriteLine("Notified React of saved state");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for saved state: {ex.Message}");
            }
        }

        /// <summary>
        /// Emits the updated state to JavaScript
        /// </summary>
        void EmitStateUpdated(AppModel state)
        {
            if ((object)reactContext != null)
            {
                try
                {
                    var frontendState = ConvertStateForFrontend(state);
                    JsEmitterHelper.EmitEvent(reactContext, STATE_UPDATED_EVENT, frontendState);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error emitting state update: {ex.Message}");
                }
            }
        }


        object ConvertStateForFrontend(AppModel state)
        {
            // Convert Map to array of objects with Index and Song properties
            var songs = state.Songs.ToArray().Select(kvp => new
            {
                Index = kvp.Key.Value, // Send the Value property instead of the SongId object
                Song = kvp.Value
            }).ToArray();

            // Send ChosenSongs as primitive values
            var chosenSongs = state.ChosenSongs.Select(id => id.Value).ToArray();

            return new
            {
                Songs = songs,
                ChosenSongs = chosenSongs, // Now sending just the values
                MinimumRating = state.MinimumRating // Send the Value property
            };
        }  
        
        /// <summary>
        /// Registers a listener for state updates - implementing IReactPromise
        /// </summary>
        [ReactMethod("RegisterStateListener")]
        public void RegisterStateListener(IReactPromise<string> promise)
        {
            try
            {
                // Ensure we're subscribed to state changes
                SubscribeToStateChanges();

                // Send the current state immediately
                EmitStateUpdated(ObservableState.Current);

                // Resolve the promise
                promise.Resolve("State listener registered successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering state listener: {ex.Message}");
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Loads state from disk - implementing IReactPromise
        /// </summary>
        [ReactMethod("LoadSavedState")]
        public async void LoadSavedState(IReactPromise<bool> promise)
        {
            try
            {
                var success = await PersistedStateService.LoadStateFromDiskAsync();
                if (success)
                {
                    promise.Resolve(true);
                }
                else
                {
                    Runtime.Warning("Failed to load previous session");
                    promise.Resolve(false);
                }
            }
            catch (Exception ex)
            {
                Runtime.Error("Error loading saved state", ex);
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes saved state from disk - implementing IReactPromise
        /// </summary>
        [ReactMethod("DeleteSavedState")]
        public async void DeleteSavedState(IReactPromise<bool> promise)
        {
            try
            {
                await PersistedStateService.DeleteSavedStateAsync();
                Runtime.Info("Previous session data deleted");
                promise.Resolve(true);
            }
            catch (Exception ex)
            {
                Runtime.Error("Error deleting saved state", ex);
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Check if saved state exists - implementing IReactPromise
        /// </summary>
        [ReactMethod("CheckForSavedState")]
        public async void CheckForSavedState(IReactPromise<bool> promise)
        {
            try
            {
                var hasState = await PersistedStateService.HasSavedStateAsync();
                promise.Resolve(hasState);
            }
            catch (Exception ex)
            {
                Runtime.Error("Error checking for saved state", ex);
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Updates the minimum rating filter
        /// </summary>
        [ReactMethod("SetMinimumRating")]
        public void SetMinimumRating(int rating) =>
            ObservableState.SetMinimumRating(new SongRating(rating));

        /// <summary>
        /// Toggles the selection of a song
        /// </summary>
        [ReactMethod("ToggleSongSelection")]
        public void ToggleSongSelection(string songId) =>
            ObservableState.ToggleSongSelection(new SongId(int.Parse(songId)));

        /// <summary>
        /// Sets all chosen songs
        /// </summary>
        [ReactMethod("SetChosenSongs")]
        public void SetChosenSongs(string chosenSongsJson)
        {
            try
            {
                var songIds = JsonConvert.DeserializeObject<int[]>(chosenSongsJson);
                if (songIds != null)
                    ObservableState.SetChosenSongs(toSeq(songIds.Select(s => new SongId(s))));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting chosen songs: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup resources when component is disposed
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from state change events
            if (isSubscribed)
            {
                ObservableState.StateChanged -= OnStateChanged;
                isSubscribed = false;
            }

            // Clean up the persistence service
            PersistedStateService.Cleanup();
        }
    }
}