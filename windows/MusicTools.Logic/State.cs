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
            StateChanged?.Invoke(null, newState);
        }

        /// <summary>
        /// Sets the minimum rating filter
        /// </summary>
        public static void SetMinimumRating(int rating) =>
            Update(stateAtom.Value with { MinimumRating = rating });

        /// <summary>
        /// Updates song statuses
        /// </summary>
        public static void UpdateSongStatus((int SongId, SpotifyStatus Status)[] updates) =>
            Update(stateAtom.Value.UpdateSongsStatus(updates));

        /// <summary>
        /// Updates artist statuses
        /// </summary>
        public static void UpdateArtistStatus((string Artist, SpotifyStatus status)[] updates) =>
            Update(stateAtom.Value.UpdateArtistsStatus(updates));

        /// <summary>
        /// Sets the song collection and initializes chosen songs if not set
        /// </summary>
        public static void SetSongs(Seq<SongInfo> songs) =>
            Update(stateAtom.Value.SetSongs(songs));

        /// <summary>
        /// Sets the chosen songs
        /// </summary>
        public static void SetChosenSongs(int[] songIds) =>
            Update(stateAtom.Value with { ChosenSongs = songIds });

        /// <summary>
        /// Toggles the selection state of a song
        /// </summary>
        public static void ToggleSongSelection(int songId) =>
            Update(stateAtom.Value.ToggleSongSelection(songId));

        /// <summary>
        /// Sets the application state from persisted data without triggering the state change event
        /// This is used when loading state from disk to avoid a circular save
        /// </summary>
        public static void SetPersistedState(Dictionary<int, SongInfo> songs, int[] chosenSongs, int minimumRating)
        {
            var newModel = new AppModel(
                Songs: new ConcurrentDictionary<int, SongInfo>(songs),
                ChosenSongs: chosenSongs,
                MinimumRating: minimumRating
            );

            stateAtom.Swap(_ => newModel);
            // Then manually trigger the state changed event
            StateChanged?.Invoke(null, newModel);
        }
    }
}