using System;
using LanguageExt;
using static LanguageExt.Prelude;
using System.Linq;
using G = System.Collections.Generic;

namespace MusicTools.Domain
{
    public record AppModel(Map<SongId, SongInfo> Songs, SongId[] ChosenSongs, SongRating MinimumRating)
    {
        public Seq<SongInfo> FilteredSongs(bool includeAlreadyProcessed = false)
        {
            var chosenSongsHash = toHashSet(ChosenSongs);

            return (from s in Songs.Values
                    where s.Rating >= MinimumRating &&
                          chosenSongsHash.Contains(s.Id) &&
                          (includeAlreadyProcessed || s.SongStatus == SpotifyStatus.NotSearched)
                    select s).ToSeq();
        }

        public Seq<Artist> DistinctArtists(bool includeAlreadyProcessed = false) =>
            (from s in FilteredSongs(includeAlreadyProcessed: true)
             from a in s.Artist
             where a.Value.HasValue() &&
                  (includeAlreadyProcessed || s.ArtistStatus == SpotifyStatus.NotSearched)
             select a).Distinct().ToSeq();

        public AppModel SetSongs(Seq<SongInfo> songs)
        {
            // Ensure sequential ordering
            songs = songs.Select((s, i) => s with { Id = new SongId(i + 1) }).ToSeq();

            // Initialize ChosenSongs with all song IDs
            var chosenSongs = songs.Select(s => s.Id).ToArray();

            return this with
            {
                Songs = toMap(songs.Select(s => (s.Id, s))),
                ChosenSongs = chosenSongs
            };
        }

        public AppModel UpdateSongsStatus((SongId SongId, SpotifyStatus Status)[] updates)
        {
            var songs = Songs;
            updates.Iter(update =>
            {
                songs.Find(update.SongId).IfSome(song =>
                    songs = songs.AddOrUpdate(update.SongId, song with
                    {
                        SongStatus = song.SongStatus == SpotifyStatus.Liked
                        ? SpotifyStatus.Liked // Never downgrade to found
                        : update.Status
                    }));
            });
            return this with { Songs = songs };
        }

        public AppModel UpdateArtistsStatus((Artist Artist, SpotifyStatus Status)[] updates)
        {
            var songs = Songs;

            var songsWithArtist = (from s in songs.Values
                                   from songInfoArtist in s.Artist
                                   from update in updates
                                   where update.Artist == songInfoArtist 
                                   select (update.Status, Song: s)).Distinct();

            songsWithArtist.Iter(update =>
                songs = songs.AddOrUpdate(update.Song.Id, update.Song with
                {
                    ArtistStatus = update.Song.ArtistStatus == SpotifyStatus.Liked
                    ? SpotifyStatus.Liked // Never downgrade to found
                    : update.Status
                }));

            return this with { Songs = songs };
        }

        /// <summary>
        /// Toggle if song is selected for submission 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="songId"></param>
        /// <returns></returns>
        public AppModel ToggleSongSelection(SongId songId)
        {
            // Mutable hash set for fast add/remove
            var currentChosen = new G.HashSet<SongId>(ChosenSongs);

            if (currentChosen.Contains(songId))
                currentChosen.Remove(songId);
            else
                currentChosen.Add(songId);

            return this with { ChosenSongs = currentChosen.ToArray() };
        }
    }
}
