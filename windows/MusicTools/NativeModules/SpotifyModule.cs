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

namespace MusicTools.NativeModules
{
    [ReactModule("SpotifyModule")]
    public sealed class SpotifyModule
    {
        readonly SpotifyApi spotifyApi;
        bool isInitialised = false;
        bool isAuthenticated = false;

        /// <summary>
        /// Initializes a new instance of the SpotifyModule class
        /// </summary>
        public SpotifyModule()
        {
            try
            {
                // Todo, store securely in configuration
                string clientId = "a53ac9883ecd4a4da3f3b40c7588585c";
                string clientSecret = "9aac6c7555934655b601e4598f4b715b";

                // Use a custom protocol scheme for redirect to our app
                var redirectUri = "musictools://auth/callback";

                spotifyApi = new SpotifyApi(clientId, clientSecret, redirectUri);
                isInitialised = true;
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

        // todo ask for explanation
        /// <summary>
        /// Gets any stored auth URI from application settings
        /// </summary>
        [ReactMethod("GetStoredAuthUri")]
        public Task<string> GetStoredAuthUri()
        {
            try
            {
                if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue("spotifyAuthUri", out object uriString) && uriString is string uri)                
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