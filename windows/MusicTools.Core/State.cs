using LanguageExt;
using System;
using System.Collections.Generic;
using System.Text;
using static MusicTools.Core.Types;

namespace MusicTools.Core
{
    public static class ObservableState
    {
        private static readonly object _lock = new object();
        private static AppStateData _appState = new AppStateData(new SongInfo[0], 0);

        // Event that fires when state changes
        public static event EventHandler<AppStateData> StateChanged;

        // Current state (read-only from outside)
        public static AppStateData Current
        {
            get { return _appState; }
        }

        // Update entire state
        public static void Update(AppStateData newState)
        {
            lock (_lock)
            {
                if (!_appState.Equals(newState))
                {
                    _appState = newState;
                    StateChanged?.Invoke(null, _appState);
                }
            }
        }

        // Helper methods for specific state updates
        public static void SetMinimumRating(int rating)
        {
            Update(_appState with { MinimumRating = rating });
        }

        public static void SetSongs(Seq<SongInfo> songs)
        {
            Update(_appState with { Songs = songs.ToArray() });
        }
    }
}
