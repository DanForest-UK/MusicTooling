using LanguageExt;
using static MusicTools.Core.Types;
using System;
using static LanguageExt.Prelude;

namespace MusicTools.Logic
{
    /// <summary>
    /// Manages application state using an immutable Atom container
    /// </summary>
    public static class ObservableState
    {
        // Thread safe and atomic management of state
        static readonly Atom<AppModel> stateAtom = Atom(new AppModel(new SongInfo[0], 0));

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
            // Compare-and-swap the atom value, then fire event if changed
            stateAtom.Swap(oldState =>
            {
                // Only update if values are different
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
                return newState;
            });
        }

        /// <summary>
        /// Sets the minimum rating filter
        /// </summary>
        public static void SetMinimumRating(int rating) =>
            stateAtom.Swap(state => state with { MinimumRating = rating });

        /// <summary>
        /// Sets the song collection
        /// </summary>
        public static void SetSongs(Seq<SongInfo> songs) =>
            stateAtom.Swap(state => state with { Songs = songs.ToArray() });
    }
}