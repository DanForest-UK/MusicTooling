﻿using LanguageExt;
using System;
using System.Linq;
using static LanguageExt.Prelude;
using System.Collections.Generic;
using MusicTools.Domain;

namespace MusicTools.Logic
{
    /// <summary>
    /// Manages application state using an immutable Atom container
    /// </summary>
    public static class ObservableState
    {
        // Thread safe and atomic management of state
        static readonly Atom<AppModel> stateAtom = Atom(new AppModel(
            Songs: new Map<SongId, SongInfo>(), // chosen mutable type for efficiency on updates
            ChosenSongs: new SongId[0],
            MinimumRating: new SongRating(0)
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
        public static void SetMinimumRating(SongRating rating) =>
            Update(stateAtom.Value with { MinimumRating = rating });

        /// <summary>
        /// Updates song statuses
        /// </summary>
        public static void UpdateSongStatus((SongId SongId, SpotifyStatus Status)[] updates) =>
            Update(stateAtom.Value.UpdateSongsStatus(updates));

        /// <summary>
        /// Updates artist statuses
        /// </summary>
        public static void UpdateArtistStatus((Artist Artist, SpotifyStatus status)[] updates) =>
            Update(stateAtom.Value.UpdateArtistsStatus(updates));

        /// <summary>
        /// Sets the song collection and initializes chosen songs 
        /// </summary>
        public static void SetSongs(Seq<SongInfo> songs) =>
            Update(stateAtom.Value.SetSongs(songs));

        /// <summary>
        /// Sets the chosen songs
        /// </summary>
        public static void SetChosenSongs(Seq<SongId> songIds) =>
            Update(stateAtom.Value with { ChosenSongs = songIds.ToArray() });

        /// <summary>
        /// Toggles the selection state of a song
        /// </summary>
        public static void ToggleSongSelection(SongId songId) =>
            Update(stateAtom.Value.ToggleSongSelection(songId));


        // <summary>
        // Sets the application state from persisted data without triggering the state change event
        // This is used when loading state from disk to avoid a circular save
        // </summary>
        public static void SetPersistedState(Dictionary<int, SongInfo> songs, SongId[] chosenSongs, SongRating minimumRating)
        {
            var newModel = new AppModel(
                Songs: toMap(songs.AsEnumerable().Select(i => (new SongId(i.Key), i.Value))),
                ChosenSongs: chosenSongs,
                MinimumRating: minimumRating
            );

            stateAtom.Swap(_ => newModel);
            // Then manually trigger the state changed event
            StateChanged?.Invoke(null, newModel);
        }
    }
}