using LanguageExt;
using static MusicTools.Core.Types;
using System;
using System.Linq;
using static LanguageExt.Prelude;

namespace MusicTools.Logic
{
    /// <summary>
    /// Manages application state using an immutable Atom container
    /// </summary>
    public static class ObservableState
    {
        // Thread safe and atomic management of state
        static readonly Atom<AppModel> stateAtom = Atom(new AppModel(
            Songs: new SongInfo[0],
            ChosenSongs: new string[0],
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

        /// <summary>
        /// Sets the song collection and initializes chosen songs if not set
        /// </summary>
        public static void SetSongs(Seq<SongInfo> songs)
        {
            stateAtom.Swap(state => {
                var songsArray = songs.ToArray();

                // Initialize ChosenSongs with all song IDs if it's empty
                var chosenSongs = state.ChosenSongs.Length == 0
                    ? songsArray.Select(s => s.Id).ToArray()
                    : state.ChosenSongs;

                return state with
                {
                    Songs = songsArray,
                    ChosenSongs = chosenSongs
                };
            });
        }

        /// <summary>
        /// Sets the chosen songs
        /// </summary>
        public static void SetChosenSongs(string[] songIds) =>
            stateAtom.Swap(state => state with { ChosenSongs = songIds });

        /// <summary>
        /// Toggles the selection state of a song
        /// </summary>
        public static void ToggleSongSelection(string songId)
        {
            stateAtom.Swap(state => {
                var currentChosen = toHashSet(state.ChosenSongs);

                if (currentChosen.Contains(songId))
                    currentChosen.Remove(songId);
                else
                    currentChosen.Add(songId);

                return state with { ChosenSongs = currentChosen.ToArray() };
            });
        }
    }
}