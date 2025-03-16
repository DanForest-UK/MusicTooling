using Microsoft.ReactNative.Managed;
using MusicTools.Core;
using static MusicTools.Core.Types;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.ReactNative;
using System.Diagnostics;
using MusicTools.Logic;
using MusicTools.NativeModules;

namespace MusicTools.NativeModules
{
    [ReactModule("StateModule")]
    public sealed class StateModule : IDisposable
    {
        // Constants for event names
        public const string STATE_UPDATED_EVENT = "appStateUpdated";

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
        /// Emits the updated state to JavaScript
        /// </summary>
        private void EmitStateUpdated(AppModel state)
        {
            if ((object)reactContext != null)
            {
                try
                {
                    // Debug log state contents
                    Debug.WriteLine($"Emitting state with {state.Songs?.Count} songs, {state.ChosenSongs?.Length} chosen songs");

                    // Pass the state object directly to EmitEvent - don't serialize here
                    JsEmitterHelper.EmitEvent(reactContext, STATE_UPDATED_EVENT, state);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error emitting state update: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Returns the current application state as JSON
        /// This method is kept for backward compatibility
        /// </summary>
        [ReactMethod("GetCurrentState")]
        public Task<string> GetCurrentState()
        {
            try
            {
                return Task.FromResult(JsonConvert.SerializeObject(ObservableState.Current));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting state: {ex.Message}");
                return Task.FromResult("{}"); // todo this breaks the UI need something better
            }
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
        /// Updates the minimum rating filter
        /// </summary>
        [ReactMethod("SetMinimumRating")]
        public void SetMinimumRating(int rating) =>
            ObservableState.SetMinimumRating(rating);

        /// <summary>
        /// Toggles the selection of a song
        /// </summary>
        [ReactMethod("ToggleSongSelection")]
        public void ToggleSongSelection(string songId) =>
            ObservableState.ToggleSongSelection(int.Parse(songId));

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
                    ObservableState.SetChosenSongs(songIds);
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
        }
    }
}