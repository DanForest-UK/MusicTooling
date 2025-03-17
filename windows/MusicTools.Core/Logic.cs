using LanguageExt;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
            songs = songs.Select((s, i) => s with { Id = i + 1 }).ToSeq();

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


        public static AppModel UpdateSongsStatus(this AppModel current, (int SongId, SpotifyStatus Status)[] updates)
        {
            var songs = current.Songs;
            updates.Iter(update =>
            {
                if (songs.ContainsKey(update.SongId))
                    songs[update.SongId] = songs[update.SongId] with 
                    { SongStatus =songs[update.SongId].SongStatus == SpotifyStatus.Liked // Never downgrade to 'found'
                        ? SpotifyStatus.Liked
                        : update.Status };
            });
            return current with { Songs = songs };
        }

        public static AppModel UpdateArtistsStatus(this AppModel current, (string Artist, SpotifyStatus Status)[] updates)
        {
            var songs = current.Songs;

            var songsWithArtist = (from s in songs.Values
                                   from songInfoArtist in s.Artist
                                   from update in updates
                                   where update.Artist.ToLower() == songInfoArtist.ToLower() // todo check if we can make search case insensitive
                                   select (Id: s.Id, Status: update.Status)).Distinct();

            songsWithArtist.Iter(song =>
            {
                if (songs.ContainsKey(song.Id))
                    songs[song.Id] = songs[song.Id] 
                        with { ArtistStatus = songs[song.Id].ArtistStatus == SpotifyStatus.Liked // never downgrade to 'found'
                            ? SpotifyStatus.Liked
                            : song.Status };
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
