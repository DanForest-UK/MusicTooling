using LanguageExt;
using static MusicTools.Core.Types;
using System;

namespace MusicTools.Core
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

        // Update entire state
        public static void Update(AppModel newState)
        {
            lock (sync)
            {
                if (!state.Equals(newState))
                {
                    state = newState;
                    StateChanged?.Invoke(null, state);
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