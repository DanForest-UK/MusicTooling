using LanguageExt;
using LanguageExt.Common;
using MusicTools.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using static LanguageExt.Prelude;
using static MusicTools.Core.Types;
using System.Diagnostics;
using SpotifyAPI.Web.Http;

namespace MusicTools.Logic
{
    /// <summary>
    /// Spotify API implementation using SpotifyAPI.Web package
    /// </summary>
    public class SpotifyApi : ISpotifyApi
    {
        readonly string clientId;
        readonly string clientSecret;
        readonly string redirectUri;
        Option<SpotifyClient> spotifyClient;
        DateTime tokenExpiry = DateTime.MinValue;
        const int TooManyRequests = 429;

        // Maximum number of IDs per batch for Spotify API limits
        private const int MAX_BATCH_SIZE = 50;

        /// <summary>
        /// Initializes a new instance of the SpotifyApi class
        /// </summary>
        public SpotifyApi(string clientId, string clientSecret, string redirectUri)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.redirectUri = redirectUri;
        }

        SpotifyClient SpotifyClient => spotifyClient.IfNoneThrow("Spotify client not initialized");

        /// <summary>
        /// Gets the login URL for the Spotify authorization flow
        /// </summary>
        public string GetAuthorizationUrl()
        {
            var loginRequest = new LoginRequest(
                new Uri(redirectUri),
                clientId,
                LoginRequest.ResponseType.Code)
            {
                Scope = new List<string>
                {
                    Scopes.UserLibraryRead,
                    Scopes.UserLibraryModify,
                    Scopes.UserFollowModify,
                    Scopes.UserFollowRead
                }
            };

            // Get the URI and log it for debugging
            return loginRequest.ToUri().ToString();
        }

        /// <summary>
        /// Exchanges the authorization code for an access token
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, bool>> GetAccessTokenAsync(string code)
        {
            if (spotifyClient.IsSome)
                return new SpotifyErrors.AlreadyAuthenticated();

            try
            {
                var oauth = new OAuthClient();

                var decodedCode = WebUtility.UrlDecode(code);

                // Use the decoded code only if it's different, otherwise use original
                // This prevents double-decoding issues
                var codeToUse = decodedCode != code ? decodedCode : code;

                var tokenRequest = new AuthorizationCodeTokenRequest(
                    clientId,
                    clientSecret,
                    codeToUse,
                    new Uri(redirectUri)
                );

                // Exchange code for token
                var tokenResponse = await oauth.RequestToken(tokenRequest);


                // Create the Spotify client with the access token
                spotifyClient = new SpotifyClient(tokenResponse.AccessToken);

                // Calculate token expiry (subtract 60 seconds as buffer)
                tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);

                return true;
            }
            catch (APIException ex)
            {
                return new SpotifyErrors.AuthenticationError(
                    $"API Error: {ex.Message}, Status: {ex.Response?.StatusCode}, Body: {ex.Response?.Body}");
            }
            catch (Exception ex)
            {
                return new SpotifyErrors.AuthenticationError(ex.Message);
            }
        }

        /// <summary>
        /// Ensures the Spotify client has a valid token
        /// </summary>
        Task<Either<SpotifyErrors.SpotifyError, bool>> EnsureValidTokenAsync() =>
            spotifyClient.IsSome && DateTime.UtcNow < tokenExpiry
                ? Task.FromResult<Either<SpotifyErrors.SpotifyError, bool>>(true)
                : Task.FromResult<Either<SpotifyErrors.SpotifyError, bool>>(
                    new SpotifyErrors.AuthenticationError("Token expired or not initialized"));

        /// <summary>
        /// Searches for a song on Spotify
        /// todo - try individual artists if multiple artists search fails
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, SpotifyTrack>> SearchSongAsync(Guid id, string title, string[] artists)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            try
            {
                var query = $"track:{title} artist:{string.Join(" ", artists)}";
                var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
                var searchResponse = await SpotifyClient.Search.Item(searchRequest);

                if (searchResponse.Tracks.Items == null || !searchResponse.Tracks.Items.Any())
                    return new SpotifyErrors.SongNotFound(id, title, artists, "No tracks found matching criteria");

                // Convert to our domain model
                var spotifyTrack = searchResponse.Tracks.Items.First();
                return ToSpotifyTrack(spotifyTrack);
            }
            catch (Exception ex)
            {
                return HandleApiException<SpotifyTrack>(ex, "search");
            }
        }

        /// <summary>
        /// Searches for an artist on Spotify
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, SpotifyArtist>> SearchArtistAsync(string artistName)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            try
            {
                var query = $"artist:{artistName}";
                var searchRequest = new SearchRequest(SearchRequest.Types.Artist, query);
                var searchResponse = await SpotifyClient.Search.Item(searchRequest);

                if (searchResponse.Artists.Items == null || !searchResponse.Artists.Items.Any())
                    return new SpotifyErrors.ArtistNotFound(artistName, "No artists found matching criteria");

                // Convert to our domain model
                var spotifyArtist = searchResponse.Artists.Items.First();
                return ToSpotifyArtist(spotifyArtist);
            }
            catch (Exception ex)
            {
                return HandleApiException<SpotifyArtist>(ex, "search");
            }
        }

        /// <summary>
        /// Likes a song on Spotify
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, bool>> LikeSongAsync(string spotifyTrackId)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            try
            {
                await SpotifyClient.Library.SaveTracks(new LibrarySaveTracksRequest(new[] { spotifyTrackId }));
                return true;
            }
            catch (Exception ex)
            {
                return HandleApiException<bool>(ex, "like_track");
            }
        }

        /// <summary>
        /// Likes multiple songs on Spotify in a single API call
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, bool>> LikeSongsAsync(string[] spotifyTrackIds)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            if (spotifyTrackIds == null || !spotifyTrackIds.Any())
                return true; // Nothing to do

            try
            {
                // Handle Spotify API limit of 50 IDs per request
                foreach (var batch in spotifyTrackIds.ToBatchArray(MAX_BATCH_SIZE))
                {
                    await SpotifyClient.Library.SaveTracks(new LibrarySaveTracksRequest(batch));
                }
                return true;
            }
            catch (Exception ex)
            {
                return HandleApiException<bool>(ex, "like_tracks");
            }
        }

        /// <summary>
        /// Follows an artist on Spotify
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, bool>> FollowArtistAsync(string spotifyArtistId)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            try
            {
                await SpotifyClient.Follow.Follow(new FollowRequest(FollowRequest.Type.Artist, new[] { spotifyArtistId }));
                return true;
            }
            catch (Exception ex)
            {
                return HandleApiException<bool>(ex, "follow_artist");
            }
        }

        /// <summary>
        /// Follows multiple artists on Spotify in a single API call
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, bool>> FollowArtistsAsync(string[] spotifyArtistIds)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            if (spotifyArtistIds == null || !spotifyArtistIds.Any())
                return true; // Nothing to do

            try
            {
                // Handle Spotify API limit of 50 IDs per request
                foreach (var batch in spotifyArtistIds.ToBatchArray(MAX_BATCH_SIZE))
                {
                    await SpotifyClient.Follow.Follow(new FollowRequest(FollowRequest.Type.Artist, batch));
                }
                return true;
            }
            catch (Exception ex)
            {
                return HandleApiException<bool>(ex, "follow_artists");
            }
        }

        /// <summary>
        /// Handles rate limit errors from Spotify API
        /// </summary>
        SpotifyErrors.RateLimitError HandleRateLimitError(APIException ex, string resource)
        {
            var retryAfter = 60; // Default

            if (ex.Response?.Headers.TryGetValue("Retry-After", out var values) == true &&
                values.Any() && int.TryParse(values, out var seconds))
                retryAfter = seconds;

            return new SpotifyErrors.RateLimitError(resource, retryAfter);
        }

        int GetStatusCode(IResponse response) => Optional(response).Map(r => (int)r.StatusCode).IfNone(500);

        Either<SpotifyErrors.SpotifyError, T> HandleApiException<T>(Exception ex, string resource)
        {
            if (ex is APIException apiEx && Optional(apiEx.Response).Map(r => (int)r.StatusCode).IfNone(500) == TooManyRequests)
            {
                return HandleRateLimitError(apiEx, resource);
            }
            if (ex is APIException apiEx2)
            {
                return new SpotifyErrors.ApiError(resource, GetStatusCode(apiEx2.Response!), ex.Message);
            }
            else
            {
                return new SpotifyErrors.ApiError(resource, 500, ex.Message);
            }
        }

        /// <summary>
        /// Maps SpotifyAPI.Web FullTrack to our domain model
        /// </summary>
        SpotifyTrack ToSpotifyTrack(FullTrack track) =>
            new SpotifyTrack(
                track.Id,
                track.Name,
                track.Artists.Select(artist => ToSpotifyArtist(artist)).ToArray(),
                track.Album != null ? new SpotifyAlbum(track.Album.Id, track.Album.Name) : null,
                track.Uri
            );

        /// <summary>
        /// Maps SpotifyAPI.Web SimpleArtist to our domain model
        /// </summary>
        SpotifyArtist ToSpotifyArtist(SimpleArtist artist) =>
            new SpotifyArtist(
                artist.Id,
                artist.Name,
                artist.Uri
            );

        /// <summary>
        /// Maps SpotifyAPI.Web FullArtist to our domain model
        /// </summary>
        SpotifyArtist ToSpotifyArtist(FullArtist artist) =>
            new SpotifyArtist(
                artist.Id,
                artist.Name,
                artist.Uri);        
    }
}