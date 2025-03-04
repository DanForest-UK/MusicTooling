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
using LanguageExt.Common;
using System.IO;
using static MusicTools.Core.Extensions;
using System.Net;

namespace MusicTools.NativeModules
{
    [ReactModule("SpotifyModule")]
    public sealed class SpotifyModule
    {
        readonly SpotifyApi spotifyApi;
        bool isInitialised = false;
        bool isAuthenticated = false;
        public const string spotifyAuthUriKey = "spotifyAuthUri";
        public const string redirectUrl = "musictools://auth/callback";

        /// <summary>
        /// Initializes a new instance of the SpotifyModule class
        /// </summary>
        public SpotifyModule()
        {
            try
            {
                var settings = LoadSettings();
                spotifyApi = new SpotifyApi(settings.ClientId, settings.ClientSecret, redirectUrl);
                isInitialised = true;
                Debug.WriteLine("SpotifyModule initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing SpotifyModule: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Load settings from file, includes client ID and secret
        /// </summary>
        private SpotifySettings LoadSettings()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spotifySettings.json");
                Debug.WriteLine($"Loading Spotify settings from: {path}");

                if (!File.Exists(path))
                {
                    Debug.WriteLine("Warning: spotifySettings.json file does not exist!");
                    return new SpotifySettings("", "");
                }

                var json = File.ReadAllText(path);
                Debug.WriteLine("Spotify settings loaded");

                var settings = JsonConvert.DeserializeObject<SpotifySettings>(json);
                if (settings == null)
                {
                    Debug.WriteLine("Warning: Failed to deserialize Spotify settings");
                    return new SpotifySettings("", "");
                }

                // Partially mask credentials in logs for security
                var maskedClientId = settings.ClientId.Length > 4
                    ? settings.ClientId.Substring(0, 4) + "..."
                    : "Invalid ClientId";

                Debug.WriteLine($"Loaded Spotify Client ID: {maskedClientId}");
                return settings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                return new SpotifySettings("", ""); // Return empty settings
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
                var url = spotifyApi.GetAuthorizationUrl();
                Debug.WriteLine($"Generated Spotify auth URL (truncated): {url.Substring(0, Math.Min(50, url.Length))}...");
                return Task.FromResult(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting auth URL: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// Checks if authentication has completed
        /// </summary>
        [ReactMethod("CheckAuthStatus")]
        public Task<bool> CheckAuthStatus()
        {
            Debug.WriteLine($"CheckAuthStatus called, isAuthenticated: {isAuthenticated}");
            return Task.FromResult(isAuthenticated);
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

                if (!code.HasValue())
                {
                    Debug.WriteLine("Error: No authorization code provided");
                    return Task.FromResult(JsonConvert.SerializeObject(
                        new { success = false, error = "No authorization code provided" }));
                }

                Debug.WriteLine($"Exchanging code (truncated): {code.Substring(0, Math.Min(10, code.Length))}...");

                // Clean the code to prevent any URL encoding issues
                // The code might have been already decoded or might need decoding
                string trimmedCode = code.Trim();

                return Task.Run(async () => {
                    try
                    {
                        // Now we have a clean code, we can exchange it for a token without delay
                        Debug.WriteLine("Calling GetAccessTokenAsync");
                        var result = await spotifyApi.GetAccessTokenAsync(trimmedCode);

                        var response = result.Match(
                            Right: success =>
                            {
                                isAuthenticated = true;
                                Debug.WriteLine("Authorization successful, user is authenticated!");
                                return new { success = true, error = SpotifyErrors.Empty };
                            },
                            Left: error => {
                                Debug.WriteLine($"Authorization failed: {error.Message}");
                                return new { success = false, error };
                            });

                        return JsonConvert.SerializeObject(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error exchanging code: {ex.Message}");
                        Debug.WriteLine(ex.StackTrace);
                        var errorResult = new { success = false, error = ex.Message };
                        return JsonConvert.SerializeObject(errorResult);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExchangeCodeForToken: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
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
                if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue(spotifyAuthUriKey, out object uriString) && uriString is string uri)
                {
                    Debug.WriteLine($"Found stored auth URI: {uri}");
                    return Task.FromResult(uri);
                }

                Debug.WriteLine("No stored auth URI found");
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
                if (localSettings.Values.ContainsKey(spotifyAuthUriKey))
                {
                    localSettings.Values.Remove(spotifyAuthUriKey);
                    Debug.WriteLine("Cleared stored auth URI");
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
        /// Searches for and likes songs on Spotify - optimized with batch processing
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

                Debug.WriteLine($"LikeSongs called with {chosenSongs.Length} songs");

                return Task.Run(async () => {
                    try
                    {
                        var errors = new List<SpotifyErrors.SpotifyError>();
                        var trackIds = new List<string>();

                        // First phase: Search for all songs to get their Spotify IDs
                        foreach (var song in chosenSongs)
                        {
                            Debug.WriteLine($"Searching for song: {song.Name} by {string.Join(", ", song.Artist)}");

                            // Search for the song
                            var searchResult = await spotifyApi.SearchSongAsync(song.Name, song.Artist);

                            searchResult.Match(
                                Right: track => {
                                    Debug.WriteLine($"Song found: {track.Name} (ID: {track.Id})");
                                    trackIds.Add(track.Id);
                                },
                                Left: error => {
                                    Debug.WriteLine($"Error finding song: {error.Message}");
                                    errors.Add(error);
                                }
                            );
                        }

                        // Second phase: Like all found songs in a single batch operation
                        if (trackIds.Any())
                        {
                            Debug.WriteLine($"Liking {trackIds.Count} songs in batch operation");
                            var likeResult = await spotifyApi.LikeSongsAsync(trackIds.ToArray());

                            likeResult.Match(
                                Right: _ => {
                                    Debug.WriteLine("Batch like operation successful");
                                },
                                Left: error => {
                                    Debug.WriteLine($"Error in batch like operation: {error.Message}");
                                    errors.Add(error);
                                }
                            );
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

                            Debug.WriteLine($"Operation completed with {errors.Count} errors");
                        }
                        else
                        {
                            response = new { success = true };
                            Debug.WriteLine("All songs liked successfully");
                        }

                        return JsonConvert.SerializeObject(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in LikeSongs task: {ex.Message}");
                        Debug.WriteLine(ex.StackTrace);
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
        /// Follows artists from chosen songs on Spotify - optimized with batch processing
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

                Debug.WriteLine($"FollowArtists called with {chosenSongs.Length} songs");

                return Task.Run(async () => {
                    try
                    {
                        // Extract distinct artist names from the songs
                        var distinctArtists = chosenSongs
                            .SelectMany(s => s.Artist)
                            .Where(a => a.HasValue())
                            .Distinct()
                            .ToList();

                        Debug.WriteLine($"Found {distinctArtists.Count} distinct artists to follow");

                        var errors = new List<SpotifyErrors.SpotifyError>();
                        var artistIds = new List<string>();

                        // First phase: Search for all artists to get their Spotify IDs
                        foreach (var artistName in distinctArtists)
                        {
                            Debug.WriteLine($"Searching for artist: {artistName}");

                            // Search for the artist
                            var searchResult = await spotifyApi.SearchArtistAsync(artistName);

                            searchResult.Match(
                                Right: artist => {
                                    Debug.WriteLine($"Artist found: {artist.Name} (ID: {artist.Id})");
                                    artistIds.Add(artist.Id);
                                },
                                Left: error => {
                                    Debug.WriteLine($"Error finding artist: {error.Message}");
                                    errors.Add(error);
                                }
                            );
                        }

                        // Second phase: Follow all found artists in a single batch operation
                        if (artistIds.Any())
                        {
                            Debug.WriteLine($"Following {artistIds.Count} artists in batch operation");
                            var followResult = await spotifyApi.FollowArtistsAsync(artistIds.ToArray());

                            followResult.Match(
                                Right: _ => {
                                    Debug.WriteLine("Batch follow operation successful");
                                },
                                Left: error => {
                                    Debug.WriteLine($"Error in batch follow operation: {error.Message}");
                                    errors.Add(error);
                                }
                            );
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

                            Debug.WriteLine($"Operation completed with {errors.Count} errors");
                        }
                        else
                        {
                            response = new { success = true };
                            Debug.WriteLine("All artists followed successfully");
                        }

                        return JsonConvert.SerializeObject(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in FollowArtists task: {ex.Message}");
                        Debug.WriteLine(ex.StackTrace);
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
            if (!isInitialised)
            {
                Debug.WriteLine("SpotifyModule is not properly initialized");
                throw new InvalidOperationException("SpotifyModule is not properly initialized");
            }
        }
    }
}