using LanguageExt;
using static MusicTools.Core.Types;
using System;
using System.Linq;
using static LanguageExt.Prelude;
using G = System.Collections.Generic;
using MusicTools.Core;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace MusicTools.Logic
{
    /// <summary>
    /// Manages application state using an immutable Atom container
    /// </summary>
    public static class ObservableState
    {
        // Thread safe and atomic management of state
        static readonly Atom<AppModel> stateAtom = Atom(new AppModel(
            Songs: new ConcurrentDictionary<int, SongInfo>(), // chosen mutable type for efficiency on updates
            ChosenSongs: new int[0],
            MinimumRating: 0
        ));

        // Event that fires when state changes
        public static event EventHandler<AppModel>? StateChanged;

        /// <summary>
        /// Current application state (read-only access)
        /// </summary>
        public static AppModel Current => stateAtom.Value;

        /// <summary>
        /// Updates the entire application state atomically
        /// </summary>
        public static void Update(AppModel newState)
        {
            var oldState = stateAtom.Value;
            stateAtom.Swap(_ => newState);

            if (!oldState.Equals(newState))
            {
                try
                {
                    StateChanged?.Invoke(null, newState);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in state change notification: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sets the minimum rating filter
        /// </summary>
        public static void SetMinimumRating(int rating) =>
            stateAtom.Swap(state => state with { MinimumRating = rating });

        public static void UpdateSongStatus(int[] songIds, SpotifyStatus status) =>
            stateAtom.Swap(state => state.UpdateSongsStatus(songIds, status));

        public static void UpdateArtistStatus(string[] artists, SpotifyStatus status) =>
            stateAtom.Swap(state => state.UpdateArtistsStatus(artists, status));
          
        /// <summary>
        /// Sets the song collection and initializes chosen songs if not set
        /// </summary>
        public static void SetSongs(Seq<SongInfo> songs) =>
            stateAtom.Swap(state => state.SetSongs(songs));

        /// <summary>
        /// Sets the chosen songs
        /// </summary>
        public static void SetChosenSongs(int[] songIds) =>
            stateAtom.Swap(state => state with { ChosenSongs = songIds });

        /// <summary>
        /// Toggles the selection state of a song
        /// </summary>
        public static void ToggleSongSelection(int songId) =>       
            stateAtom.Swap(state => state.ToggleSongSelection(songId));
    }
}