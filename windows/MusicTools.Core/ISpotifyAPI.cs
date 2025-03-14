using LanguageExt;
using LanguageExt.Common;
using MusicTools.Core;
using System;
using System.Threading;
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
        Task<Either<SpotifyErrors.SpotifyError, SpotifyTrack>> SearchSongAsync(
            int id,
            string title,
            string[] artists,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for an artist on Spotify
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, SpotifyArtist>> SearchArtistAsync(
            string artistName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Likes multiple songs on Spotify, processing in batches and handling errors
        /// </summary>
        /// <param name="spotifyTrackIds">Array of Spotify track IDs to like</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <returns>Tuple containing any errors and array of successfully liked track IDs</returns>
        Task<(SpotifyErrors.SpotifyError[] Errors, SpotifySongId[] LikedSongs)> LikeSongsAsync(
            SpotifySongId[] spotifyTrackIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Follows multiple artists on Spotify, processing in batches and handling errors
        /// </summary>
        /// <param name="spotifyArtistIds">Array of Spotify artist IDs to follow</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <returns>Tuple containing any errors and array of successfully followed artist IDs</returns>
        Task<(SpotifyErrors.SpotifyError[] Errors, SpotifyArtistId[] FollowedArtists)> FollowArtistsAsync(
            SpotifyArtistId[] spotifyArtistIds,
            CancellationToken cancellationToken = default);
    }
}