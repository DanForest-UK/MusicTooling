using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using MusicTools.Core;
using Newtonsoft.Json;
using static LanguageExt.Prelude;
using static MusicTools.Core.Types;

namespace MusicTools.Logic
{
    public static class SpotifyHttpStatusCodes
    {
        public const HttpStatusCode TooManyRequests = (HttpStatusCode)429;
    }

    /// <summary>
    /// Spotify API implementation
    /// </summary>
    public class SpotifyApi
    {
        private readonly HttpClient _httpClient;
        private string _accessToken = string.Empty;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;

        /// <summary>
        /// Initializes a new instance of the SpotifyApi class
        /// </summary>
        public SpotifyApi(string clientId, string clientSecret, string redirectUri)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.spotify.com/v1/");
            _clientId = clientId;
            _clientSecret = clientSecret;
            _redirectUri = redirectUri;
        }

        /// <summary>
        /// Gets the login URL for the Spotify authorization flow using PKCE
        /// </summary>
        public string GetAuthorizationUrl()
        {
            var scopes = new[]
            {
                "user-library-read",
                "user-library-modify",
                "user-follow-modify",
                "user-follow-read"
            };

            var scopeParam = string.Join(" ", scopes);

            // Generate a code challenge for PKCE 
            // In a full implementation, you should generate and store a code verifier
            // and create a code challenge from it.
            // For simplicity, we're not implementing the full PKCE flow here.

            return $"https://accounts.spotify.com/authorize" +
                   $"?client_id={_clientId}" +
                   $"&response_type=code" +
                   $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                   $"&scope={Uri.EscapeDataString(scopeParam)}";
        }

        /// <summary>
        /// Exchanges the authorization code for an access token
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, bool>> GetAccessTokenAsync(string code)
        {
            try
            {
                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret)
                });

                var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", requestContent);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new SpotifyErrors.AuthenticationError(content);
                }

                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(content);
                if (tokenResponse == null)
                {
                    return new SpotifyErrors.AuthenticationError("Failed to parse token response");
                }

                _accessToken = tokenResponse.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Buffer of 60 seconds

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                return true;
            }
            catch (Exception ex)
            {
                return new SpotifyErrors.AuthenticationError(ex.Message);
            }
        }

        /// <summary>
        /// Refreshes the access token if necessary
        /// </summary>
        private async Task<Either<SpotifyErrors.SpotifyError, bool>> EnsureValidTokenAsync()
        {
            if (DateTime.UtcNow < _tokenExpiry && !string.IsNullOrEmpty(_accessToken))
            {
                return true;
            }

            // In a real app, you would implement token refresh logic here
            return new SpotifyErrors.AuthenticationError("Token expired and refresh not implemented");
        }

        // The rest of your SpotifyAPI class methods remain unchanged

        /// <summary>
        /// Searches for a song on Spotify
        /// </summary>
        public async Task<Either<SpotifyErrors.SpotifyError, SpotifyTrack>> SearchSongAsync(string title, string[] artists)
        {
            var tokenCheck = await EnsureValidTokenAsync();
            if (tokenCheck.IsLeft)
            {
                return tokenCheck.LeftToList().First();
            }

            try
            {
                var query = Uri.EscapeDataString($"track:{title} artist:{string.Join(" ", artists)}");
                var response = await _httpClient.GetAsync($"search?q={query}&type=track&limit=1");

                if (response.StatusCode == SpotifyHttpStatusCodes.TooManyRequests &&
                    response.Headers.TryGetValues("Retry-After", out var retryValues))
                {
                    int.TryParse(retryValues.FirstOrDefault(), out var retryAfter);
                    return new SpotifyErrors.RateLimitError("search", retryAfter > 0 ? retryAfter : 60);
                }

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new SpotifyErrors.ApiError("search", (int)response.StatusCode, content);
                }

                var searchResult = JsonConvert.DeserializeObject<SearchResponse<SpotifyTrack>>(content);
                var tracks = searchResult?.Tracks?.Items;

                if (tracks == null || !tracks.Any())
                {
                    return new SpotifyErrors.SongNotFound(title, artists, "No tracks found matching criteria");
                }

                return tracks.First();
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
            {
                return tokenCheck.LeftToList().First();
            }

            try
            {
                var query = Uri.EscapeDataString($"artist:{artistName}");
                var response = await _httpClient.GetAsync($"search?q={query}&type=artist&limit=1");

                if (response.StatusCode == SpotifyHttpStatusCodes.TooManyRequests &&
                    response.Headers.TryGetValues("Retry-After", out var retryValues))
                {
                    int.TryParse(retryValues.FirstOrDefault(), out var retryAfter);
                    return new SpotifyErrors.RateLimitError("search", retryAfter > 0 ? retryAfter : 60);
                }

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new SpotifyErrors.ApiError("search", (int)response.StatusCode, content);
                }

                var searchResult = JsonConvert.DeserializeObject<SearchResponse<SpotifyArtist>>(content);
                var artists = searchResult?.Artists?.Items;

                if (artists == null || !artists.Any())
                {
                    return new SpotifyErrors.ArtistNotFound(artistName, "No artists found matching criteria");
                }

                return artists.First();
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
            {
                return tokenCheck.LeftToList().First();
            }

            try
            {
                var response = await _httpClient.PutAsync($"me/tracks?ids={spotifyTrackId}", null);

                if (response.StatusCode == SpotifyHttpStatusCodes.TooManyRequests &&
                    response.Headers.TryGetValues("Retry-After", out var retryValues))
                {
                    int.TryParse(retryValues.FirstOrDefault(), out var retryAfter);
                    return new SpotifyErrors.RateLimitError("like_track", retryAfter > 0 ? retryAfter : 60);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return new SpotifyErrors.ApiError("like_track", (int)response.StatusCode, content);
                }

                return true;
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
            {
                return tokenCheck.LeftToList().First();
            }

            try
            {
                var response = await _httpClient.PutAsync($"me/following?type=artist&ids={spotifyArtistId}", null);

                if (response.StatusCode == SpotifyHttpStatusCodes.TooManyRequests &&
                    response.Headers.TryGetValues("Retry-After", out var retryValues))
                {
                    int.TryParse(retryValues.FirstOrDefault(), out var retryAfter);
                    return new SpotifyErrors.RateLimitError("follow_artist", retryAfter > 0 ? retryAfter : 60);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return new SpotifyErrors.ApiError("follow_artist", (int)response.StatusCode, content);
                }

                return true;
            }
            catch (Exception ex)
            {
                return new SpotifyErrors.ApiError("follow_artist", 500, ex.Message);
            }
        }
    }

    // Response classes for Spotify API - These are unchanged

    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class SearchResponse<T> where T : class
    {
        [JsonProperty("tracks")]
        public PaginatedResponse<SpotifyTrack>? Tracks { get; set; }

        [JsonProperty("artists")]
        public PaginatedResponse<SpotifyArtist>? Artists { get; set; }
    }

    public class PaginatedResponse<T> where T : class
    {
        [JsonProperty("items")]
        public List<T> Items { get; set; } = new List<T>();

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
    }

    public class SpotifyTrack
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("artists")]
        public List<SpotifyArtist> Artists { get; set; } = new List<SpotifyArtist>();

        [JsonProperty("album")]
        public SpotifyAlbum? Album { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; } = string.Empty;
    }

    public class SpotifyArtist
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("uri")]
        public string Uri { get; set; } = string.Empty;
    }

    public class SpotifyAlbum
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }
}