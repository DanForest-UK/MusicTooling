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
using static LanguageExt.Prelude;
using Windows.Storage;
using LanguageExt.ClassInstances;
using LanguageExt;
using System.Threading;

namespace MusicTools.NativeModules
{
    [ReactModule("SpotifyModule")]
    public sealed class SpotifyModule
    {
        readonly ISpotifyApi spotifyApi;
        bool isInitialised = false;
        bool isAuthenticated = false;
        public const string spotifyAuthUriKey = "spotifyAuthUri";
        public const string redirectUrl = "musictools://auth/callback";
        const int delayTime = 200; // Delay in API requests to prevent too many requests error

        /// <summary>
        /// Initializes a new instance of the SpotifyModule class
        /// </summary>
        public SpotifyModule()
        {
            var settings = LoadSettings();
            spotifyApi = Runtime.GetSpotifyAPI(settings.ClientId, settings.ClientSecret, redirectUrl);
            isInitialised = true;
        }

        /// <summary>
        /// Load settings from file, includes client ID and secret
        /// </summary>
        SpotifySettings LoadSettings()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spotifySettings.json");

                if (!File.Exists(path))
                {
                    Debug.WriteLine("Warning: spotifySettings.json file does not exist!");
                    return new SpotifySettings("", "");
                }

                var json = File.ReadAllText(path);
               
                var settings = JsonConvert.DeserializeObject<SpotifySettings>(json);
                if (settings == null)
                {
                    Debug.WriteLine("Warning: Failed to deserialize Spotify settings");
                    return new SpotifySettings("", "");
                }
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
                {
                    Debug.WriteLine("Error: No authorization code provided");
                    return Task.FromResult(JsonConvert.SerializeObject(
                        new { success = false, error = "No authorization code provided" }));
                }

                Debug.WriteLine($"Exchanging code (truncated): {code.Substring(0, Math.Min(10, code.Length))}...");

                // Clean the code to prevent any URL encoding issues
                var trimmedCode = code.Trim();

                return Task.Run(async () => {
                    try
                    {
                        var result = await spotifyApi.GetAccessTokenAsync(trimmedCode);

                        var response = result.Match(
                            Right: success => {
                                isAuthenticated = true;
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
                        return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExchangeCodeForToken: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
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
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(spotifyAuthUriKey, out object uriString) && uriString is string uri)              
                    return Task.FromResult(uri);
                
                Debug.WriteLine("No stored auth URI found");
                return Task.FromResult("");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting stored URI: {ex.Message}");
                return Task.FromResult("");
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
                var localSettings = ApplicationData.Current.LocalSettings;
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

        /// <summary>
        /// Searches for and likes songs on Spotify - optimized with batch processing
        /// </summary>
        [ReactMethod("LikeSongs")]
        public Task<string> LikeSongs()
        {
            try
            {
                EnsureInitialized();

                var filteredSongs = ObservableState.Current.FilteredSongs();

                if (!filteredSongs.Any())
                    return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = "No songs provided" }));

                return Task.Run(async () => {
                    try
                    {
                        var errors = new List<SpotifyErrors.SpotifyError>();
                        var trackIds = new List<string>();
                        
                        filteredSongs.ToArray().Iter(async song =>
                        {                            
                            var result = await SearchForSong(song);
                            result.Match(
                               Right: id => trackIds.Add(id),
                               Left: error =>
                               {
                                   if (error is SpotifyErrors.SongNotFound songNotFound)
                                   {
                                       ObservableState.SongNotFound(songNotFound.TrackId);
                                   }
                                   else
                                   {
                                       errors.Add(error);
                                   }                                   
                               });
                               await Task.Delay(delayTime); // Prevent too many requests from spotify
                        });                            

                        // Second phase: Like all found songs in a single batch operation
                        if (trackIds.Any())
                            await spotifyApi.LikeSongsAsync(trackIds.ToArray());

                        return JsonConvert.SerializeObject(errors.Any()
                            ? new
                            {
                                success = false,
                                partialSuccess = errors.Count < filteredSongs.Length,
                                errors
                            }
                            : new { success = true });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in LikeSongs task: {ex.Message}");
                        Debug.WriteLine(ex.StackTrace);
                        return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LikeSongs: {ex.Message}");
                return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
            }
        }

        /// <summary>
        /// Search for single song
        /// </summary>
        async Task<Either<SpotifyErrors.SpotifyError, string>> SearchForSong(SongInfo song)
        {
            var searchResult = await spotifyApi.SearchSongAsync(song.Id, song.Name, song.Artist);
            return searchResult.Map(v => v.Id);              
        }

        async Task<Either<SpotifyErrors.SpotifyError, string>> SearchForArtist(string artistName)
        {
            var searchResult = await spotifyApi.SearchArtistAsync(artistName);
            return searchResult.Map(v => v.Id);
        }

        /// <summary>
        /// Follows artists from chosen songs on Spotify - optimized with batch processing
        /// </summary>
        [ReactMethod("FollowArtists")]
        public Task<string> FollowArtists()
        {
            try
            {
                EnsureInitialized();

                var distinctArtists = ObservableState.Current.DistinctArtists();
                if (!distinctArtists.Any())
                    return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = "No songs provided" }));

                return Task.Run(async () => {
                    try
                    {
                        
                        var errors = new List<SpotifyErrors.SpotifyError>();
                        var artistIds = new List<string>();

                        // First phase: Search for all artists to get their Spotify IDs
                        distinctArtists.Iter(async artist => // todo correct use of anync?
                        {
                            var result = await SearchForArtist(artist);
                            result.Match(
                               Right: id => artistIds.Add(id),
                               Left: error => errors.Add(error));
                            await Task.Delay(delayTime); // Prevent too many requests from spotify

                        });

                        // Second phase: Follow all found artists in a single batch operation
                        if (artistIds.Any())
                        {
                            var followResult = await spotifyApi.FollowArtistsAsync(artistIds.ToArray());

                            followResult.Match(
                                Right: _ => Debug.WriteLine("Batch follow operation successful"),
                                Left: error => {
                                    Debug.WriteLine($"Error in batch follow operation: {error.Message}");
                                    errors.Add(error);
                                });
                        }

                        return JsonConvert.SerializeObject(errors.Any()
                            ? new
                            {
                                success = false,
                                partialSuccess = errors.Count < distinctArtists.Count,
                                errors
                            }
                            : new { success = true });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in FollowArtists task: {ex.Message}");
                        Debug.WriteLine(ex.StackTrace);
                        return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FollowArtists: {ex.Message}");
                return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
            }
        }

        void EnsureInitialized()
        {
            if (!isInitialised)
            {
                Debug.WriteLine("SpotifyModule is not properly initialized");
                throw new InvalidOperationException("SpotifyModule is not properly initialized");
            }
        }
    }
}