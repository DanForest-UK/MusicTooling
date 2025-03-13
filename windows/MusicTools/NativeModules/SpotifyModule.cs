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
using System.Reflection.Metadata.Ecma335;
using static MusicTools.Core.SpotifyErrors;
using Windows.UI.Xaml.Shapes;
using System.Collections.Concurrent;

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
        int delayTime = 200; // Delay in API requests to prevent too many requests error

        /// <summary>
        /// Initializes a new instance of the SpotifyModule class
        /// </summary>
        public SpotifyModule()
        {
            var settings = LoadSettings();
            spotifyApi = Runtime.GetSpotifyAPI(settings.ClientId, settings.ClientSecret, redirectUrl, settings.ApiWait);
            delayTime = settings.ApiWait;
            isInitialised = true;
        }

        /// <summary>
        /// Load settings from file, includes client ID and secret
        /// </summary>
        SpotifySettings LoadSettings()
        {
            try
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spotifySettings.json");

                if (!File.Exists(path))
                {
                    Runtime.Warning("Spotify settings file not found");
                    return new SpotifySettings("", "", 0);
                }

                var json = File.ReadAllText(path);

                var settings = JsonConvert.DeserializeObject<SpotifySettings>(json);
                if (settings == null)
                {
                    Runtime.Warning("Failed to parse Spotify settings");
                    return new SpotifySettings("", "", 0);
                }
                return settings;
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error loading Spotify settings", ex);
                return new SpotifySettings("", "", 0); // Return empty settings
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
                Runtime.Info("Preparing Spotify authorization...");
                var url = spotifyApi.GetAuthorizationUrl();
                return Task.FromResult(url);
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error generating authorization URL", ex);
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

                Runtime.Info("Connecting to Spotify...");

                if (!code.HasValue())
                {
                    Runtime.Error("No authorization code provided", None);
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
                                Runtime.Success("Connected to Spotify successfully");
                                return new { success = true, error = SpotifyErrors.Empty };
                            },
                            Left: error => {
                                Runtime.Error($"Spotify authentication failed", None);
                                return new { success = false, error };
                            });

                        return JsonConvert.SerializeObject(response);
                    }
                    catch (Exception ex)
                    {
                        Runtime.Error($"Error exchanging code", ex);
                        return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error in ExchangeCodeForToken", ex);
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
                Runtime.Warning($"Error retrieving stored auth URI");
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
                Runtime.Warning($"Error clearing stored auth URI");
                return Task.FromResult("error");
            }
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
                {
                    Runtime.Warning("No artists to follow");
                    return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = "No songs provided" }));
                }

                Runtime.Info($"Searching for {distinctArtists.Count()} artists on Spotify...");

                return Task.Run(async () => {
                    try
                    {
                        var errors = new ConcurrentBag<SpotifyError>();
                        var foundArtists = new ConcurrentDictionary<SpotifyArtistId, string>();// lookup of spotify artist id to name

                        distinctArtists.ToArray().Iter(async artist =>
                        {
                            var result = await SearchForArtist(artist);
                            result.Match(
                               Right: foundArtistId =>
                               {
                                   foundArtists.TryAdd(foundArtistId, artist);
                                   ObservableState.Current.UpdateArtistsStatus(new string[] { artist }, SpotifyStatus.Found);
                               },
                              Left: error =>
                              {
                                  if (error is ArtistNotFound)
                                  {
                                      ObservableState.UpdateArtistStatus(new string[] { artist }, SpotifyStatus.NotFound);
                                  }
                                  else
                                  {
                                      errors.Add(error);
                                  }
                              });
                            await Task.Delay(delayTime); // Prevent too many requests from spotify
                        });

                        // Periodically update status with progress
                        Runtime.Info($"Found {foundArtists.Count} of {distinctArtists.Count()} artists on Spotify");

                        // Second phase: Like all found artists in a single batch operation
                        if (foundArtists.Any())
                        {
                            Runtime.Info($"Following {foundArtists.Count} artists on Spotify...");

                            var result = await spotifyApi.FollowArtistsAsync(foundArtists.Keys.ToArray()); // sending spotify song ID to api
                            result.Errors.Iter(errors.Add);

                            // Convert spotify artist id to artist name
                            var followedArtistNames = result.FollowedArtists.Select(id => foundArtists.ValueOrNone(id)).Somes().Distinct();

                            if (followedArtistNames.Count() != result.FollowedArtists.Count())
                            {
                                Runtime.Warning("Some artist mappings were lost during processing");
                            }

                            ObservableState.UpdateArtistStatus(followedArtistNames.ToArray(), SpotifyStatus.Liked);

                            if (followedArtistNames.Count() > 0)
                            {
                                Runtime.Success($"Successfully followed {followedArtistNames.Count()} artists on Spotify");
                            }
                            else if (errors.Any())
                            {
                                Runtime.Error("Failed to follow any artists on Spotify", None);
                            }
                        }
                        else
                        {
                            if (errors.Any())
                            {
                                Runtime.Error("Failed to find any artists on Spotify", None);
                            }
                            else
                            {
                                Runtime.Warning("No artists found on Spotify");
                            }
                        }

                        return JsonConvert.SerializeObject(errors.Any()
                            ? new
                            {
                                success = false,
                                partialSuccess = errors.Any() && foundArtists.Any(),
                                errors
                            }
                            : new { success = true });
                    }
                    catch (Exception ex)
                    {
                        Runtime.Error($"Error following artists", ex);
                        return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error in FollowArtists", ex);
                return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
            }
        }

        /// <summary>
        /// Searches for and likes songs on Spotify - optimized with batch processing
        /// </summary>
        [ReactMethod("LikeSongs")]
        public Task<string> LikeSongs()
        {
            EnsureInitialized();

            try
            {
                var filteredSongs = ObservableState.Current.FilteredSongs();

                if (!filteredSongs.Any())
                {
                    Runtime.Warning("No songs provided to like");
                    return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = "No songs provided" }));
                }

                Runtime.Info($"Preparing to like {filteredSongs.Count()} songs on Spotify...");

                return Task.Run(async () => {
                    try
                    {
                        var errors = new ConcurrentBag<SpotifyError>();

                        var foundSongs = new ConcurrentDictionary<SpotifySongId, int>(); // lookup from spotify song id to our song id

                        int processedCount = 0;
                        int totalCount = filteredSongs.Count();

                        filteredSongs.ToArray().Iter(async song =>
                        {
                            var result = await SearchForSong(song);
                            processedCount++;

                            // Periodically update status
                            if (processedCount % 10 == 0 || processedCount == totalCount)
                            {
                                Runtime.Info($"Searching for songs on Spotify ({processedCount}/{totalCount})...");
                            }

                            result.Match(
                               Right: id =>
                               {
                                   foundSongs.TryAdd(id, song.Id);
                                   ObservableState.Current.UpdateSongsStatus(new int[] { song.Id }, SpotifyStatus.Found);
                               },
                              Left: error =>
                              {
                                  if (error is SongNotFound songNotFound)
                                  {
                                      ObservableState.UpdateSongStatus(new int[] { song.Id }, SpotifyStatus.NotFound);
                                  }
                                  else
                                  {
                                      errors.Add(error);
                                  }
                              });
                            await Task.Delay(delayTime); // Prevent too many requests from spotify
                        });

                        Runtime.Info($"Found {foundSongs.Count} of {filteredSongs.Count()} songs on Spotify");

                        // Second phase: Like all found songs in a single batch operation
                        if (foundSongs.Any())
                        {
                            Runtime.Info($"Liking {foundSongs.Count} songs on Spotify...");
                            var result = await spotifyApi.LikeSongsAsync(foundSongs.Keys.ToArray()); // sending spotify song ID to api
                            result.Errors.Iter(errors.Add);

                            // Convert spotify liked song ID to our song ID
                            var likedSongIds = result.LikedSongs.Select(id => foundSongs.ValueOrNone(id)).Somes();

                            if (likedSongIds.Count() != result.LikedSongs.Count())
                            {
                                Runtime.Warning("Some song mappings were lost during processing");
                            }

                            ObservableState.UpdateSongStatus(likedSongIds.ToArray(), SpotifyStatus.Liked);

                            if (likedSongIds.Count() > 0)
                            {
                                if (likedSongIds.Count() < foundSongs.Count)
                                {
                                    Runtime.Warning($"Partial success: Liked {likedSongIds.Count()} of {foundSongs.Count} songs");
                                }
                                else
                                {
                                    Runtime.Success($"Successfully liked {likedSongIds.Count()} songs on Spotify");
                                }
                            }
                            else if (errors.Any())
                            {
                                Runtime.Error("Failed to like any songs on Spotify", None);
                            }
                        }
                        else
                        {
                            if (errors.Any())
                            {
                                Runtime.Error("Failed to find any songs on Spotify", None);
                            }
                            else
                            {
                                Runtime.Warning("No songs found on Spotify");
                            }
                        }

                        return JsonConvert.SerializeObject(errors.Any()
                            ? new
                            {
                                success = false,
                                partialSuccess = errors.Any() && foundSongs.Any(),
                                errors
                            }
                            : new { success = true });
                    }
                    catch (Exception ex)
                    {
                        Runtime.Error($"Error liking songs", ex);
                        return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error in LikeSongs", ex);
                return Task.FromResult(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
            }
        }

        /// <summary>
        /// Search for single song
        /// </summary>
        async Task<Either<SpotifyError, SpotifySongId>> SearchForSong(SongInfo song)
        {
            var searchResult = await spotifyApi.SearchSongAsync(song.Id, song.Name, song.Artist);
            return searchResult.Map(v => v.Id);
        }

        async Task<Either<SpotifyError, SpotifyArtistId>> SearchForArtist(string artistName)
        {
            var searchResult = await spotifyApi.SearchArtistAsync(artistName);
            return searchResult.Map(v => v.Id);
        }

        void EnsureInitialized()
        {
            if (!isInitialised)
            {
                Runtime.Error("SpotifyModule is not properly initialized", None);
                throw new InvalidOperationException("SpotifyModule is not properly initialized");
            }
        }
    }
}