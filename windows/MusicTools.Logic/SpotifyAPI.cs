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
using System.Threading;

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

        // Backoff system constants
        private const int MAX_RETRY_ATTEMPTS = 10;
        private const int BACKOFF_MS = 60000;  // Backoff for 1 minute

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
                Runtime.Error($"API Error during authentication: {ex.Message}", ex as Exception);
                return new SpotifyErrors.AuthenticationError(
                    $"API Error: {ex.Message}, Status: {ex.Response?.StatusCode}, Body: {ex.Response?.Body}");
            }
            catch (Exception ex)
            {
                Runtime.Error($"Authentication error: {ex.Message}", Some(ex));
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
        /// Executes an API operation with exponential backoff retry for rate limiting
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="resourceName">Name of the resource being accessed (for error reporting)</param>
        /// <param name="itemName">Optional item name for detailed status reporting</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <returns>The result of the operation wrapped in Either</returns>
        public async Task<Either<SpotifyErrors.SpotifyError, T>> WithBackoff<T>(
            Func<Task<Either<SpotifyErrors.SpotifyError, T>>> operation,
            string resourceName,
            string itemName = null,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;

            while (attempt < MAX_RETRY_ATTEMPTS && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await operation();

                    if (result.IsLeft)
                    {
                        // Check if it's a rate limit error
                        var error = result.LeftToList().First();
                        if (error is SpotifyErrors.RateLimitError)
                        {
                            attempt = attempt++;

                            string itemInfo = string.IsNullOrEmpty(itemName) ? "" : $" for {itemName}";
                            StatusHelper.Warning($"Rate limit reached{itemInfo}. Waiting {BACKOFF_MS / 1000} seconds before retrying...");

                            // Use the cancellation token with Task.Delay
                            try
                            {
                                await Task.Delay(BACKOFF_MS, cancellationToken);
                            }
                            catch (TaskCanceledException)
                            {
                                return new SpotifyErrors.ApiError(resourceName, 0, "Operation was canceled");
                            }

                            StatusHelper.Info($"Retrying {resourceName}{itemInfo} (attempt {attempt + 1}/{MAX_RETRY_ATTEMPTS})...");
                            continue;
                        }
                    }

                    return result;
                }
                catch (TaskCanceledException)
                {
                    // Handle cancellation explicitly
                    return new SpotifyErrors.ApiError(resourceName, 0, "Operation was canceled");
                }
                catch (Exception ex)
                {
                    // If it's not a rate limit exception, don't retry
                    if (!(ex is APIException apiEx &&
                          apiEx.Response != null &&
                          (int)apiEx.Response.StatusCode == TooManyRequests))
                    {
                        return HandleApiException<T>(ex, resourceName);
                    }
                }
            }

            // Check if we're here because of cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return new SpotifyErrors.ApiError(resourceName, 0, "Operation was canceled");
            }

            // If we reach here, all attempts failed due to rate limiting
            return new SpotifyErrors.RateLimitError(resourceName);
        }

        /// <summary>
        /// Searches for a song on Spotify with backoff retry for rate limits
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, SpotifyTrack>> SearchSongAsync(
            int id,
            string title,
            string[] artists,
            CancellationToken cancellationToken = default)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            StatusHelper.Info($"Searching for song: {title} by {string.Join(", ", artists)}...");

            string itemName = $"{title} by {string.Join(", ", artists)}";

            return await WithBackoff(async () =>
            {
                try
                {
                    var query = $"track:{title} artist:{string.Join(" ", artists)}";
                    var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
                    var searchResponse = await SpotifyClient.Search.Item(searchRequest, cancellationToken);

                    if (searchResponse.Tracks.Items == null || !searchResponse.Tracks.Items.Any())
                        return new SpotifyErrors.SongNotFound(id, title, artists, "No tracks found matching criteria");

                    // Convert to our domain model
                    var spotifyTrack = searchResponse.Tracks.Items.First();
                    return ToSpotifyTrack(spotifyTrack);
                }
                catch (TaskCanceledException)
                {
                    return new SpotifyErrors.ApiError("search", 0, "Operation was canceled");
                }
                catch (Exception ex)
                {
                    return HandleApiException<SpotifyTrack>(ex, "search");
                }
            }, "song search", itemName, cancellationToken);
        }

        /// <summary>
        /// Searches for an artist on Spotify with backoff retry for rate limits
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, SpotifyArtist>> SearchArtistAsync(
            string artistName,
            CancellationToken cancellationToken = default)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            StatusHelper.Info($"Searching for artist: {artistName}...");

            // Use the backoff system with the captured operation
            return await WithBackoff(async () =>
            {
                try
                {
                    var query = $"artist:{artistName}";
                    var searchRequest = new SearchRequest(SearchRequest.Types.Artist, query);
                    var searchResponse = await SpotifyClient.Search.Item(searchRequest, cancellationToken);

                    if (searchResponse.Artists.Items == null || !searchResponse.Artists.Items.Any())
                        return new SpotifyErrors.ArtistNotFound(artistName, "No artists found matching criteria");

                    // Convert to our domain model
                    var spotifyArtist = searchResponse.Artists.Items.First();
                    return ToSpotifyArtist(spotifyArtist);
                }
                catch (TaskCanceledException)
                {
                    return new SpotifyErrors.ApiError("search", 0, "Operation was canceled");
                }
                catch (Exception ex)
                {
                    return HandleApiException<SpotifyArtist>(ex, "search");
                }
            }, "artist search", artistName, cancellationToken);
        }

        /// <summary>
        /// Likes multiple songs on Spotify, processing in batches and handling errors with backoff
        /// </summary>
        public async Task<(SpotifyErrors.SpotifyError[] Errors, SpotifySongId[] LikedSongs)> LikeSongsAsync(
            SpotifySongId[] spotifyTrackIds,
            CancellationToken cancellationToken = default)
        {
            var likedSongs = new List<SpotifySongId>();
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return (Errors: tokenCheck.LeftToArray().ToArray(), new SpotifySongId[0]);

            if (spotifyTrackIds == null || !spotifyTrackIds.Any())
                return (new SpotifyErrors.SpotifyError[0], new SpotifySongId[0]); // Nothing to do

            var errors = new List<SpotifyErrors.SpotifyError>();

            try
            {
                // Handle Spotify API limit of 50 IDs per request
                int batchCount = 1;
                int totalBatches = (int)Math.Ceiling((double)spotifyTrackIds.Length / MAX_BATCH_SIZE);

                foreach (var batch in spotifyTrackIds.ToBatchArray(MAX_BATCH_SIZE))
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        errors.Add(new SpotifyErrors.ApiError("like_songs", 0, "Operation was canceled"));
                        break;
                    }

                    StatusHelper.Info($"Liking songs batch {batchCount}/{totalBatches} ({batch.Length} songs)...");

                    var result = await WithBackoff(async () =>
                    {
                        try
                        {
                            await SpotifyClient.Library.SaveTracks(new LibrarySaveTracksRequest(batch.Select(v => v.Value).ToArray()), cancellationToken);
                            return true;
                        }
                        catch (TaskCanceledException)
                        {
                            return new SpotifyErrors.ApiError("like_songs_batch", 0, "Operation was canceled");
                        }
                        catch (Exception ex)
                        {
                            return HandleApiException<bool>(ex, "like_songs_batch");
                        }
                    }, "like songs", $"batch {batchCount}/{totalBatches}", cancellationToken);

                    result.Match(
                        Right: success =>
                        {
                            likedSongs.AddRange(batch);
                        },
                        Left: error =>
                        {
                            errors.Add(error);
                        });

                    batchCount++;
                }
            }
            catch (TaskCanceledException)
            {
                errors.Add(new SpotifyErrors.ApiError("like_songs", 0, "Operation was canceled"));
            }
            catch (Exception ex)
            {
                var error = HandleApiException<Unit>(ex, "like_songs");
                error.Match(
                    Left: err =>
                    {
                        errors.Add(err);
                        return unit;
                    },
                    Right: _ => unit);
            }

            return (errors.ToArray(), likedSongs.ToArray());
        }

        /// <summary>
        /// Follows multiple artists on Spotify, processing in batches and handling errors with backoff
        /// </summary>
        public async Task<(SpotifyErrors.SpotifyError[] Errors, SpotifyArtistId[] FollowedArtists)> FollowArtistsAsync(
            SpotifyArtistId[] spotifyArtistIds,
            CancellationToken cancellationToken = default)
        {
            var followedArtists = new List<SpotifyArtistId>();
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return (Errors: tokenCheck.LeftToArray().ToArray(), new SpotifyArtistId[0]);

            if (spotifyArtistIds == null || !spotifyArtistIds.Any())
                return (new SpotifyErrors.SpotifyError[0], new SpotifyArtistId[0]); // Nothing to do

            var errors = new List<SpotifyErrors.SpotifyError>();

            try
            {
                // Handle Spotify API limit of 50 IDs per request
                int batchCount = 1;
                int totalBatches = (int)Math.Ceiling((double)spotifyArtistIds.Length / MAX_BATCH_SIZE);

                foreach (var batch in spotifyArtistIds.ToBatchArray(MAX_BATCH_SIZE))
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        errors.Add(new SpotifyErrors.ApiError("follow_artists", 0, "Operation was canceled"));
                        break;
                    }

                    StatusHelper.Info($"Following artists batch {batchCount}/{totalBatches} ({batch.Length} artists)...");

                    var result = await WithBackoff(async () =>
                    {
                        try
                        {
                            await SpotifyClient.Follow.Follow(new FollowRequest(FollowRequest.Type.Artist, batch.Select(v => v.Value).ToArray()), cancellationToken);
                            return true;
                        }
                        catch (TaskCanceledException)
                        {
                            return new SpotifyErrors.ApiError("follow_artists_batch", 0, "Operation was canceled");
                        }
                        catch (Exception ex)
                        {
                            return HandleApiException<bool>(ex, "follow_artists_batch");
                        }
                    }, "follow artists", $"batch {batchCount}/{totalBatches}", cancellationToken);

                    result.Match(
                        Right: success =>
                        {
                            followedArtists.AddRange(batch);
                        },
                        Left: error =>
                        {
                            errors.Add(error);
                        });

                    batchCount++;
                }
            }
            catch (TaskCanceledException)
            {
                errors.Add(new SpotifyErrors.ApiError("follow_artists", 0, "Operation was canceled"));
            }
            catch (Exception ex)
            {
                var error = HandleApiException<Unit>(ex, "follow_artists");
                error.Match(
                    Left: err =>
                    {
                        errors.Add(err);
                        return unit;
                    },
                    Right: _ => unit);
            }

            return (errors.ToArray(), followedArtists.ToArray());
        }

        /// <summary>
        /// Handles rate limit errors from Spotify API
        /// </summary>
        SpotifyErrors.RateLimitError HandleRateLimitError(APIException ex, string resource)
        {
            // Cast to Exception before wrapping in Option
            Runtime.Error($"Rate limit exceeded for {resource}", Some((Exception)ex));
            return new SpotifyErrors.RateLimitError(resource);
        }

        int GetStatusCode(IResponse response) => Optional(response).Map(r => (int)r.StatusCode).IfNone(500);

        Either<SpotifyErrors.SpotifyError, T> HandleApiException<T>(Exception ex, string resource)
        {
            if (ex is TaskCanceledException)
            {
                return new SpotifyErrors.ApiError(resource, 0, "Operation was canceled");
            }
            if (ex is APIException apiEx && Optional(apiEx.Response).Map(r => (int)r.StatusCode).IfNone(500) == TooManyRequests)
            {
                return HandleRateLimitError(apiEx, resource);
            }
            if (ex is APIException apiEx2)
            {
                var statusCode = GetStatusCode(apiEx2.Response!);
                // Cast to ensure type compatibility with Option<Exception>
                Runtime.Error($"API error ({statusCode}): {ex.Message}", Some((Exception)ex));
                return new SpotifyErrors.ApiError(resource, statusCode, ex.Message);
            }
            else
            {
                Runtime.Error($"General error for {resource}: {ex.Message}", Some(ex));
                return new SpotifyErrors.ApiError(resource, 500, ex.Message);
            }
        }

        /// <summary>
        /// Maps SpotifyAPI.Web FullTrack to our domain model
        /// </summary>
        SpotifyTrack ToSpotifyTrack(FullTrack track) =>
            new SpotifyTrack(
                SpotifySongId.New(track.Id),
                track.Name,
                track.Artists.Select(artist => ToSpotifyArtist(artist)).ToArray(),
                track.Uri
            );

        /// <summary>
        /// Maps SpotifyAPI.Web SimpleArtist to our domain model
        /// </summary>
        SpotifyArtist ToSpotifyArtist(SimpleArtist artist) =>
            new SpotifyArtist(
                SpotifyArtistId.New(artist.Id),
                artist.Name,
                artist.Uri
            );

        /// <summary>
        /// Maps SpotifyAPI.Web FullArtist to our domain model
        /// </summary>
        SpotifyArtist ToSpotifyArtist(FullArtist artist) =>
            new SpotifyArtist(
                SpotifyArtistId.New(artist.Id),
                artist.Name,
                artist.Uri);
    }
}