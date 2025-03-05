using LanguageExt;
using LanguageExt.Common;
using MusicTools.Core;
using System;
using System.Threading.Tasks;
using static MusicTools.Core.Types;

namespace MusicTools.Core
{
    /// <summary>
    /// Interface for Spotify API operations
    /// </summary>
    public interface ISpotifyApi
    {
        /// <summary>
        /// Gets the login URL for the Spotify authorization flow
        /// </summary>
        string GetAuthorizationUrl();

        /// <summary>
        /// Exchanges the authorization code for an access token
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, bool>> GetAccessTokenAsync(string code);

        /// <summary>
        /// Searches for a song on Spotify
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, SpotifyTrack>> SearchSongAsync(int id, string title, string[] artists);

        /// <summary>
        /// Searches for an artist on Spotify
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, SpotifyArtist>> SearchArtistAsync(string artistName);

        /// <summary>
        /// Likes a song on Spotify
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, bool>> LikeSongAsync(string spotifyTrackId);

        /// <summary>
        /// Likes multiple songs on Spotify in a single API call
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, bool>> LikeSongsAsync(string[] spotifyTrackIds);

        /// <summary>
        /// Follows an artist on Spotify
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, bool>> FollowArtistAsync(string spotifyArtistId);

        /// <summary>
        /// Follows multiple artists on Spotify in a single API call
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, bool>> FollowArtistsAsync(string[] spotifyArtistIds);
    }
}