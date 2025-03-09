using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MusicTools.Core.Types;
using G = System.Collections.Generic;

namespace MusicTools.Core
{
    public static class Logic
    {
        /// <summary>
        /// Sets the list of songs
        /// </summary>
        /// <param name="current"></param>
        /// <param name="songs"></param>
        /// <returns></returns>
        public static AppModel SetSongs(this AppModel current, Seq<SongInfo> songs)
        {
            // Ensure sequential ordering
            if (songs.Select(s => s.Id).Distinct().Count() != songs.Count())
            {
                songs = songs.Select((s, i) => s with { Id = i + 1 }).ToSeq();
            }

            // Initialize ChosenSongs with all song IDs if it's empty
            var chosenSongs = current.ChosenSongs.Length == 0
                ? songs.Select(s => s.Id).ToArray()
                : current.ChosenSongs;

            return current with
            {
                Songs = songs.ToConcurrentDictionary(s => s.Id),
                ChosenSongs = chosenSongs
            };
        }

        // todo move logic to core
        public static AppModel UpdateSongsStatus(this AppModel current, int[] songId, SpotifyStatus status)
        {
            var songs = current.Songs;
            songId.Iter(songId =>
            {
                if (songs.ContainsKey(songId))
                    songs[songId] = songs[songId] with { SongStatus = status };
            });
            return current with { Songs = songs };
        }

        public static AppModel UpdateArtistsStatus(this AppModel current, string[] artists, SpotifyStatus status)
        {
            var songs = current.Songs;

            var songsWithArtist = (from s in songs.Values
                                   from songInfoArtists in s.Artist
                                   from artist in artists
                                   where artist.ToLower() == songInfoArtists.ToLower() // todo check if we can make search case insensitive
                                   select s.Id).Distinct();

            songsWithArtist.Iter(songId =>
            {
                if (songs.ContainsKey(songId))
                    songs[songId] = songs[songId] with { ArtistStatus = status };
            });
            return current with { Songs = songs };
        }

        /// <summary>
        /// Toggle if song is selected for submission 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="songId"></param>
        /// <returns></returns>
        public static AppModel ToggleSongSelection(this AppModel current, int songId)
        {
            // Mutable hash set for fast add/remove
            var currentChosen = new G.HashSet<int>(current.ChosenSongs);

            if (currentChosen.Contains(songId))
                currentChosen.Remove(songId);
            else
                currentChosen.Add(songId);

            return current with { ChosenSongs = currentChosen.ToArray() };
        }
    }
}
