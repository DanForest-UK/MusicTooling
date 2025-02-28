using LanguageExt;
using static MusicTools.Core.Types;
using System;

namespace MusicTools.Logic
{
    public static class ObservableState
    {
        static readonly object sync = new();
        static AppModel state = new AppModel(new SongInfo[0], 0);

        // Event that fires when state changes
        public static event EventHandler<AppModel>? StateChanged;

        // Current state (read-only from outside)
        public static AppModel Current =>
            state;

        /// <summary>
        /// Updates entire application state
        /// </summary>
        public static void Update(AppModel newState)
        {
            lock (sync)
            {
                if (!state.Equals(newState))
                {
                    var oldState = state;
                    state = newState;

                    // Ensure we're not triggering with null state
                    if (StateChanged != null)
                    {
                        try
                        {
                            StateChanged(null, state);
                        }
                        catch (Exception ex)
                        {
                            // Log exception but don't crash
                            System.Diagnostics.Debug.WriteLine($"Error in state change notification: {ex.Message}");
                        }
                    }
                }
            }
        }

        // Helper methods for specific state updates
        public static void SetMinimumRating(int rating) =>
            Update(state with { MinimumRating = rating });

        public static void SetSongs(Seq<SongInfo> songs) =>
            Update(state with { Songs = songs.ToArray() });
    }
}