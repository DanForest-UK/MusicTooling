using System;
using System.Collections.Generic;
using System.Text;

namespace MusicTools.Domain
{
    /// <summary>
    /// Defines if the item can be found on Spotify
    /// </summary>
    public enum SpotifyStatus
    {
        NotSearched = 0,
        Found = 1,
        NotFound = 2,
        Liked = 3
    }

    /// <summary>
    /// Main type for a song
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="Path"></param>
    /// <param name="Artist"></param>
    /// <param name="Album"></param>
    /// <param name="Rating"></param>
    /// <param name="ArtistStatus"></param>
    /// <param name="SongStatus"></param>
    public record SongInfo(SongId Id, SongName Name, SongPath Path, Artist[] Artist, Album Album, SongRating Rating, SpotifyStatus ArtistStatus, SpotifyStatus SongStatus);

}
