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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing SpotifyModule: {ex.Message}");
            }
        }

        /// <summary>
        /// Load settings from file, includes client ID and secret so not included in repo
        /// Please see 'exampleSpotifySettings.json' - fill out and rename to 'spotifySettings.json'
        /// </summary>
        /// <returns></returns>
        private SpotifySettings LoadSettings()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spotifySettings.json");
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<SpotifySettings>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
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
                return Task.FromResult(spotifyApi.GetAuthorizationUrl());
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
        public Task<bool> CheckAuthStatus() =>
            Task.FromResult(isAuthenticated);
      
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
                     return Task.FromResult(JsonConvert.SerializeObject(
                        new { success = false, error = "No authorization code provided" }));                

                return Task.Run(async () => {
                    try
                    {
                        // Exchange code for token
                        var result = await spotifyApi.GetAccessTokenAsync(code);

                        var response = result.Match(
                            Right: success =>
                            {
                                isAuthenticated = true;
                                return new { success = true, error = SpotifyErrors.Empty };
                            },
                            Left: error => new { success = false, error });

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
                if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue(spotifyAuthUriKey, out object uriString) && uriString is string uri)                
                    return Task.FromResult(uri);                

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
                }
                return Task.FromResult("success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing stored URI: {ex.Message}");
                return Task.FromResult("error");
            }
        }


        // todo chosen songs hsould come from the state
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
                            var searchResult = await spotifyApi.SearchSongAsync(song.Name, song.Artist);

                            await searchResult.Match(
                                Right: async track => {
                                    // Like the song if found
                                    var likeResult = await spotifyApi.LikeSongAsync(track.Id);
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
                            var searchResult = await spotifyApi.SearchArtistAsync(artistName);

                            await searchResult.Match(
                                Right: async artist => {
                                    // Follow the artist if found
                                    var followResult = await spotifyApi.FollowArtistAsync(artist.Id);
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
            if (!isInitialised)
            {
                throw new InvalidOperationException("SpotifyModule is not properly initialized");
            }
        }
    }
}