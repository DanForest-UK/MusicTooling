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

namespace MusicTools.Logic
{
    /// <summary>
    /// Spotify API implementation using SpotifyAPI.Web package
    /// </summary>
    public class SpotifyApi
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private SpotifyClient? _spotifyClient;
        private DateTime _tokenExpiry = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the SpotifyApi class
        /// </summary>
        public SpotifyApi(string clientId, string clientSecret, string redirectUri)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;

            // Ensure redirect URI is exactly as expected without any modifications
            _redirectUri = redirectUri;

            Debug.WriteLine($"SpotifyApi initialized with redirectUri: {_redirectUri}");
        }

        /// <summary>
        /// Gets the login URL for the Spotify authorization flow
        /// </summary>
        public string GetAuthorizationUrl()
        {
            // Create the login request with exact redirect URI
            var loginRequest = new LoginRequest(
                new Uri(_redirectUri),
                _clientId,
                LoginRequest.ResponseType.Code
            )
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
            var authUri = loginRequest.ToUri().ToString();
            Debug.WriteLine($"Generated auth URL: {authUri}");

            return authUri;
        }

        /// <summary>
        /// Exchanges the authorization code for an access token
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, bool>> GetAccessTokenAsync(string code)
        {
            try
            {
                // Log the code received (truncated for security)
                if (!string.IsNullOrEmpty(code) && code.Length > 4)
                {
                    Debug.WriteLine($"Code received for exchange (first 4 chars): {code.Substring(0, 4)}..., length: {code.Length}");
                }
                else
                {
                    Debug.WriteLine("Warning: Code received is null, empty, or too short");
                }

                // Ensure the code is not URL encoded again
                // If it came from a URL, it might already be decoded
                string decodedCode = WebUtility.UrlDecode(code);

                // Use the decoded code only if it's different, otherwise use original
                // This prevents double-decoding issues
                string codeToUse = decodedCode != code ? decodedCode : code;

                Debug.WriteLine($"Using redirect URI for token exchange: {_redirectUri}");

                // Create OAuth client and immediately exchange the code for a token
                var oauth = new OAuthClient();

                // Create the token request with exact parameters
                var tokenRequest = new AuthorizationCodeTokenRequest(
                    _clientId,
                    _clientSecret,
                    codeToUse,
                    new Uri(_redirectUri)
                );

                // Exchange code for token immediately
                var tokenResponse = await oauth.RequestToken(tokenRequest);

                // Log success
                Debug.WriteLine("Token exchange successful!");

                // Create the Spotify client with the access token
                _spotifyClient = new SpotifyClient(tokenResponse.AccessToken);

                // Calculate token expiry (subtract 60 seconds as buffer)
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);

                return true;
            }
            catch (APIException ex)
            {
                // Log detailed API exception information
                Debug.WriteLine($"Spotify API Exception: {ex.Message}");
                Debug.WriteLine($"Response status: {ex.Response?.StatusCode}");
                Debug.WriteLine($"Response body: {ex.Response?.Body}");

                return new SpotifyErrors.AuthenticationError(
                    $"API Error: {ex.Message}, Status: {ex.Response?.StatusCode}, Body: {ex.Response?.Body}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Generic exception during token exchange: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return new SpotifyErrors.AuthenticationError(ex.Message);
            }
        }

        /// <summary>
        /// Ensures the Spotify client has a valid token
        /// </summary>
        private Task<Either<SpotifyErrors.SpotifyError, bool>> EnsureValidTokenAsync()
        {
            if (_spotifyClient != null && DateTime.UtcNow < _tokenExpiry)
                return Task.FromResult<Either<SpotifyErrors.SpotifyError, bool>>(true);

            return Task.FromResult<Either<SpotifyErrors.SpotifyError, bool>>(
                new SpotifyErrors.AuthenticationError("Token expired or not initialized"));
        }

        /// <summary>
        /// Searches for a song on Spotify
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, SpotifyTrack>> SearchSongAsync(string title, string[] artists)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
                return tokenCheck.LeftToList().First();

            try
            {
                var query = $"track:{title} artist:{string.Join(" ", artists)}";
                var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
                var searchResponse = await _spotifyClient!.Search.Item(searchRequest);

                if (searchResponse.Tracks.Items == null || !searchResponse.Tracks.Items.Any())
                    return new SpotifyErrors.SongNotFound(title, artists, "No tracks found matching criteria");

                // Convert to our domain model
                var spotifyTrack = searchResponse.Tracks.Items.First();
                return ToSpotifyTrack(spotifyTrack);
            }
            catch (APIException ex) when (ex.Response?.StatusCode == (System.Net.HttpStatusCode)429)
            {
                // Handle rate limiting
                var retryAfter = 60; // Default
                if (ex.Response.Headers.TryGetValue("Retry-After", out var values) &&
                    values.Any() && int.TryParse(values, out var seconds))
                {
                    retryAfter = seconds;
                }

                return new SpotifyErrors.RateLimitError("search", retryAfter);
            }
            catch (APIException ex)
            {
                return new SpotifyErrors.ApiError("search", (int)ex.Response?.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return new SpotifyErrors.ApiError("search", 500, ex.Message);
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
                var searchResponse = await _spotifyClient!.Search.Item(searchRequest);

                if (searchResponse.Artists.Items == null || !searchResponse.Artists.Items.Any())
                    return new SpotifyErrors.ArtistNotFound(artistName, "No artists found matching criteria");

                // Convert to our domain model
                var spotifyArtist = searchResponse.Artists.Items.First();
                return ToSpotifyArtist(spotifyArtist);
            }
            catch (APIException ex) when (ex.Response?.StatusCode == (System.Net.HttpStatusCode)429)
            {
                // Handle rate limiting
                var retryAfter = 60; // Default
                if (ex.Response.Headers.TryGetValue("Retry-After", out var values) &&
                    values.Any() && int.TryParse(values, out var seconds))
                {
                    retryAfter = seconds;
                }

                return new SpotifyErrors.RateLimitError("search", retryAfter);
            }
            catch (APIException ex)
            {
                return new SpotifyErrors.ApiError("search", (int)ex.Response?.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return new SpotifyErrors.ApiError("search", 500, ex.Message);
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
                await _spotifyClient!.Library.SaveTracks(new LibrarySaveTracksRequest(new[] { spotifyTrackId }));
                return true;
            }
            catch (APIException ex) when (ex.Response?.StatusCode == (System.Net.HttpStatusCode)429)
            {
                // Handle rate limiting
                var retryAfter = 60; // Default
                if (ex.Response.Headers.TryGetValue("Retry-After", out var values) &&
                    values.Any() && int.TryParse(values, out var seconds))
                {
                    retryAfter = seconds;
                }

                return new SpotifyErrors.RateLimitError("like_track", retryAfter);
            }
            catch (APIException ex)
            {
                return new SpotifyErrors.ApiError("like_track", (int)ex.Response?.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return new SpotifyErrors.ApiError("like_track", 500, ex.Message);
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
                await _spotifyClient!.Follow.Follow(new FollowRequest(FollowRequest.Type.Artist, new[] { spotifyArtistId }));
                return true;
            }
            catch (APIException ex) when (ex.Response?.StatusCode == (System.Net.HttpStatusCode)429)
            {
                // Handle rate limiting
                var retryAfter = 60; // Default
                if (ex.Response.Headers.TryGetValue("Retry-After", out var values) &&
                    values.Any() && int.TryParse(values, out var seconds))
                {
                    retryAfter = seconds;
                }

                return new SpotifyErrors.RateLimitError("follow_artist", retryAfter);
            }
            catch (APIException ex)
            {
                return new SpotifyErrors.ApiError("follow_artist", (int)ex.Response?.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return new SpotifyErrors.ApiError("follow_artist", 500, ex.Message);
            }
        }

        /// <summary>
        /// Maps SpotifyAPI.Web FullTrack to our domain model
        /// </summary>
        private SpotifyTrack ToSpotifyTrack(FullTrack track) =>
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
        private SpotifyArtist ToSpotifyArtist(SimpleArtist artist) =>
            new SpotifyArtist(
                artist.Id,
                artist.Name,
                artist.Uri
            );

        /// <summary>
        /// Maps SpotifyAPI.Web FullArtist to our domain model
        /// </summary>
        private SpotifyArtist ToSpotifyArtist(FullArtist artist) =>
            new SpotifyArtist(
                artist.Id,
                artist.Name,
                artist.Uri
            );
    }
}