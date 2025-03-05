using LanguageExt;
using static MusicTools.Core.Types;
using System;
using System.Linq;
using static LanguageExt.Prelude;
using G = System.Collections.Generic;
using MusicTools.Core;
using System.Diagnostics;
using System.Collections.Generic;

namespace MusicTools.Logic
{
    /// <summary>
    /// Manages application state using an immutable Atom container
    /// </summary>
    public static class ObservableState
    {
        // Thread safe and atomic management of state
        static readonly Atom<AppModel> stateAtom = Atom(new AppModel(
            Songs: new Dictionary<int, SongInfo>(), // chosen mutable type for efficiency on updates
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

        public static void SongNotFound(int trackId) =>
            stateAtom.Swap(state =>
            {
                var songs = state.Songs;
                if (songs.ContainsKey(trackId))
                    songs[trackId] = songs[trackId] with { SongFoundOnSpotify = false };
                return state with { Songs = songs };
            });

        /// <summary>
        /// Sets the song collection and initializes chosen songs if not set
        /// </summary>
        public static void SetSongs(Seq<SongInfo> songs)
        {
            //todo these methods should be moved to .Core        
            stateAtom.Swap(state =>
            {
                // Ensure sequential ordering
                if (songs.Select(s => s.Id).Distinct().Count() != songs.Count())
                {
                    songs = songs.Select((s, i) => s with { Id = i + 1 }).ToSeq();
                }

                // Initialize ChosenSongs with all song IDs if it's empty
                var chosenSongs = state.ChosenSongs.Length == 0
                    ? songs.Select(s => s.Id).ToArray()
                    : state.ChosenSongs;

                return state with
                {
                    Songs = songs.ToDictionary(s => s.Id),
                    ChosenSongs = chosenSongs
                };
            });
        }

        /// <summary>
        /// Sets the chosen songs
        /// </summary>
        public static void SetChosenSongs(int[] songIds) =>
            stateAtom.Swap(state => state with { ChosenSongs = songIds });

        /// <summary>
        /// Toggles the selection state of a song
        /// </summary>
        public static void ToggleSongSelection(int songId)
        {
            stateAtom.Swap(state => {

                // Mutable hash set for fast add/remove
                var currentChosen = new G.HashSet<int>(state.ChosenSongs);

                if (currentChosen.Contains(songId))
                    currentChosen.Remove(songId);
                else
                    currentChosen.Add(songId);

                return state with { ChosenSongs = currentChosen.ToArray() };
            });
        }
    }
}