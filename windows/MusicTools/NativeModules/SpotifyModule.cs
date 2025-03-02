using Microsoft.ReactNative.Managed;
using MusicTools.Core;
using MusicTools.Logic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using static MusicTools.Core.Types;

namespace MusicTools.NativeModules
{
    [ReactModule("SpotifyModule")]
    public sealed class SpotifyModule
    {
        private readonly SpotifyApi _spotifyApi;
        private bool _isInitialized = false;
        private bool _isAuthenticated = false;

        /// <summary>
        /// Initializes a new instance of the SpotifyModule class
        /// </summary>
        public SpotifyModule()
        {
            try
            {
                // These values should be stored securely in configuration
                string clientId = "a53ac9883ecd4a4da3f3b40c7588585c";
                string clientSecret = "9aac6c7555934655b601e4598f4b715b";

                // Use a custom protocol scheme for redirect to our app
                string redirectUri = "musictools://auth/callback";

                _spotifyApi = new SpotifyApi(clientId, clientSecret, redirectUri);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing SpotifyModule: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the Spotify authorization URL for the authentication flow
        /// </summary>
        [ReactMethod("GetAuthUrl")]
        public Task<string> GetAuthUrl()
        {
            try
            {
                EnsureInitialized();
                var authUrl = _spotifyApi.GetAuthorizationUrl();
                return Task.FromResult(authUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting auth URL: {ex.Message}");
                return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// Checks if authentication has completed
        /// </summary>
        [ReactMethod("CheckAuthStatus")]
        public Task<string> CheckAuthStatus()
        {
            try
            {
                var result = new { isAuthenticated = _isAuthenticated };
                return Task.FromResult(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CheckAuthStatus: {ex.Message}");
                return Task.FromResult("{}");
            }
        }

        /// <summary>
        /// Exchanges authorization code for an access token
        /// </summary>
        [ReactMethod("ExchangeCodeForToken")]
        public Task<string> ExchangeCodeForToken(string code)
        {
            try
            {
                EnsureInitialized();

                if (string.IsNullOrEmpty(code))
                {
                    var errorResult = new { success = false, error = "No authorization code provided" };
                    return Task.FromResult(JsonConvert.SerializeObject(errorResult));
                }

                return Task.Run(async () => {
                    try
                    {
                        // Exchange code for token
                        var result = await _spotifyApi.GetAccessTokenAsync(code);

                        object response = null;
                        result.Match(
                            Right: success => {
                                _isAuthenticated = true;
                                response = new { success = true };
                            },
                            Left: error => {
                                response = new { success = false, error = error };
                            }
                        );

                        if (response == null)
                        {
                            throw new ReactException("Error in ExchangeCodeForToken: response object is null");
                        }
                        return JsonConvert.SerializeObject(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error exchanging code: {ex.Message}");
                        var errorResult = new { success = false, error = ex.Message };
                        return JsonConvert.SerializeObject(errorResult);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExchangeCodeForToken: {ex.Message}");
                var errorResult = new { success = false, error = ex.Message };
                return Task.FromResult(JsonConvert.SerializeObject(errorResult));
            }
        }

        /// <summary>
        /// Gets any stored auth URI from application settings
        /// </summary>
        [ReactMethod("GetStoredAuthUri")]
        public Task<string> GetStoredAuthUri()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue("spotifyAuthUri", out object uriString) && uriString is string uri)
                {
                    return Task.FromResult(uri);
                }

                return Task.FromResult(string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting stored URI: {ex.Message}");
                return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// Clears the stored auth URI from application settings
        /// </summary>
        [ReactMethod("ClearStoredAuthUri")]
        public Task<string> ClearStoredAuthUri()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values.ContainsKey("spotifyAuthUri"))
                {
                    localSettings.Values.Remove("spotifyAuthUri");
                }
                return Task.FromResult("success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing stored URI: {ex.Message}");
                return Task.FromResult("error");
            }
        }

        /// <summary>
        /// Searches for and likes songs on Spotify
        /// </summary>
        [ReactMethod("LikeSongs")]
        public Task<string> LikeSongs(string chosenSongsJson)
        {
            try
            {
                EnsureInitialized();

                var chosenSongs = JsonConvert.DeserializeObject<SongInfo[]>(chosenSongsJson);
                if (chosenSongs == null || !chosenSongs.Any())
                {
                    var errorResult = new { success = false, error = "No songs provided" };
                    return Task.FromResult(JsonConvert.SerializeObject(errorResult));
                }

                return Task.Run(async () => {
                    try
                    {
                        var errors = new List<SpotifyErrors.SpotifyError>();

                        foreach (var song in chosenSongs)
                        {
                            // Search for the song
                            var searchResult = await _spotifyApi.SearchSongAsync(song.Name, song.Artist);

                            await searchResult.Match(
                                Right: async track => {
                                    // Like the song if found
                                    var likeResult = await _spotifyApi.LikeSongAsync(track.Id);
                                    likeResult.Match(
                                        Right: _ => { },
                                        Left: error => errors.Add(error)
                                    );
                                },
                                Left: error => {
                                    errors.Add(error);
                                    return Task.CompletedTask;
                                }
                            );

                            // Add a small delay to avoid hitting rate limits
                            await Task.Delay(200);
                        }

                        object response;
                        if (errors.Any())
                        {
                            response = new
                            {
                                success = false,
                                partialSuccess = errors.Count < chosenSongs.Length,
                                errors = errors
                            };
                        }
                        else
                        {
                            response = new { success = true };
                        }

                        return JsonConvert.SerializeObject(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in LikeSongs task: {ex.Message}");
                        var errorResult = new { success = false, error = ex.Message };
                        return JsonConvert.SerializeObject(errorResult);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LikeSongs: {ex.Message}");
                var errorResult = new { success = false, error = ex.Message };
                return Task.FromResult(JsonConvert.SerializeObject(errorResult));
            }
        }

        /// <summary>
        /// Follows artists from chosen songs on Spotify
        /// </summary>
        [ReactMethod("FollowArtists")]
        public Task<string> FollowArtists(string chosenSongsJson)
        {
            try
            {
                EnsureInitialized();

                var chosenSongs = JsonConvert.DeserializeObject<SongInfo[]>(chosenSongsJson);
                if (chosenSongs == null || !chosenSongs.Any())
                {
                    var errorResult = new { success = false, error = "No songs provided" };
                    return Task.FromResult(JsonConvert.SerializeObject(errorResult));
                }

                return Task.Run(async () => {
                    try
                    {
                        // Extract distinct artist names from the songs
                        var distinctArtists = chosenSongs
                            .SelectMany(s => s.Artist)
                            .Where(a => !string.IsNullOrWhiteSpace(a))
                            .Distinct()
                            .ToList();

                        var errors = new List<SpotifyErrors.SpotifyError>();

                        foreach (var artistName in distinctArtists)
                        {
                            // Search for the artist
                            var searchResult = await _spotifyApi.SearchArtistAsync(artistName);

                            await searchResult.Match(
                                Right: async artist => {
                                    // Follow the artist if found
                                    var followResult = await _spotifyApi.FollowArtistAsync(artist.Id);
                                    followResult.Match(
                                        Right: _ => { },
                                        Left: error => errors.Add(error)
                                    );
                                },
                                Left: error => {
                                    errors.Add(error);
                                    return Task.CompletedTask;
                                }
                            );

                            // Add a small delay to avoid hitting rate limits
                            await Task.Delay(200);
                        }

                        object response;
                        if (errors.Any())
                        {
                            response = new
                            {
                                success = false,
                                partialSuccess = errors.Count < distinctArtists.Count,
                                errors = errors
                            };
                        }
                        else
                        {
                            response = new { success = true };
                        }

                        return JsonConvert.SerializeObject(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in FollowArtists task: {ex.Message}");
                        var errorResult = new { success = false, error = ex.Message };
                        return JsonConvert.SerializeObject(errorResult);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FollowArtists: {ex.Message}");
                var errorResult = new { success = false, error = ex.Message };
                return Task.FromResult(JsonConvert.SerializeObject(errorResult));
            }
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("SpotifyModule is not properly initialized");
            }
        }
    }
}