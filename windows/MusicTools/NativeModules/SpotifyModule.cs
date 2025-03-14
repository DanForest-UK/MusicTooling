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
        int delayTime = 500; // Delay in API requests to prevent too many requests error

        // React context for emitting events
        ReactContext reactContext;

        // Constants for event names
        private const string SPOTIFY_OPERATION_PROGRESS = "spotifyOperationProgress";
        private const string SPOTIFY_OPERATION_COMPLETE = "spotifyOperationComplete";
        private const string SPOTIFY_OPERATION_ERROR = "spotifyOperationError";

        // Cancellation token source for cancellable operations
        private CancellationTokenSource cancelSource;

        /// <summary>
        /// Initializes a new instance of the SpotifyModule class
        /// </summary>
        public SpotifyModule()
        {
            var settings = LoadSettings();
            spotifyApi = Runtime.GetSpotifyAPI(settings.ClientId, settings.ClientSecret, redirectUrl);
            delayTime = settings.ApiWait;
            isInitialised = true;
        }

        /// <summary>
        /// Initialize method called by React Native runtime
        /// </summary>
        [ReactInitializer]
        public void Initialize(ReactContext reactContext)
        {
            this.reactContext = reactContext;
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
        /// Cancels any ongoing Spotify operations
        /// </summary>
        [ReactMethod("CancelSpotifyOperation")]
        public void CancelSpotifyOperation()
        {
            try
            {
                cancelSource?.Cancel();
                EmitEvent(SPOTIFY_OPERATION_COMPLETE, new { success = false, cancelled = true });
                Runtime.Warning("Spotify operation cancelled by user");
            }
            catch (Exception ex)
            {
                Runtime.Error("Error cancelling Spotify operation", ex);
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

                // Create a new cancellation token source for this operation
                var cts = new CancellationTokenSource();

                return Task.Run(async () => {
                    try
                    {
                        var errors = new ConcurrentBag<SpotifyError>();
                        var foundArtists = new ConcurrentDictionary<SpotifyArtistId, string>();// lookup of spotify artist id to name

                        int processedCount = 0;
                        int totalCount = distinctArtists.Count();

                        foreach (var artist in distinctArtists.ToArray())
                        {
                            // Check for cancellation
                            if (cts.Token.IsCancellationRequested)
                            {
                                Runtime.Warning("Artist search operation was cancelled");
                                break;
                            }

                            var result = await SearchForArtist(artist, cts.Token);
                            processedCount++;

                            // Periodically update status
                            if (processedCount % 5 == 0 || processedCount == totalCount)
                            {
                                Runtime.Info($"Searching for artists on Spotify ({processedCount}/{totalCount})...");
                            }

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

                            // Add delay with cancellation token
                            try
                            {
                                await Task.Delay(delayTime, cts.Token);
                            }
                            catch (TaskCanceledException)
                            {
                                Runtime.Warning("Artist search was cancelled during delay");
                                break;
                            }
                        }

                        // Periodically update status with progress
                        Runtime.Info($"Found {foundArtists.Count} of {distinctArtists.Count()} artists on Spotify");

                        // Second phase: Like all found artists in a single batch operation
                        if (foundArtists.Any() && !cts.Token.IsCancellationRequested)
                        {
                            Runtime.Info($"Following {foundArtists.Count} artists on Spotify...");

                            var result = await spotifyApi.FollowArtistsAsync(foundArtists.Keys.ToArray(), cts.Token); // sending spotify song ID to api
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
                            if (cts.Token.IsCancellationRequested)
                            {
                                Runtime.Warning("Operation was cancelled before following artists");
                            }
                            else if (errors.Any())
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
                    catch (TaskCanceledException)
                    {
                        Runtime.Warning("Artist follow operation was cancelled");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            cancelled = true,
                            error = "Operation was cancelled"
                        });
                    }
                    catch (Exception ex)
                    {
                        Runtime.Error($"Error following artists", ex);
                        return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    }
                    finally
                    {
                        // Dispose of the cancellation token source
                        cts.Dispose();
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
        /// Starts the process to like songs on Spotify - event-based approach
        /// </summary>
        [ReactMethod("LikeSongs")]
        public void LikeSongs()
        {
            try
            {
                EnsureInitialized();

                // Dispose of any existing cancellation token source
                cancelSource?.Dispose();

                // Create a new cancellation token for this operation
                cancelSource = new CancellationTokenSource();
                var token = cancelSource.Token;

                // Start the operation in a background task
                Task.Run(async () => await LikeSongsProcessAsync(token), token);

                // Return immediately, status will be sent via events
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error starting LikeSongs operation", ex);
                EmitEvent(SPOTIFY_OPERATION_ERROR, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Processes the like songs operation and sends progress events
        /// </summary>
        private async Task LikeSongsProcessAsync(CancellationToken cancellationToken)
        {
            try
            {
                var filteredSongs = ObservableState.Current.FilteredSongs();

                if (!filteredSongs.Any())
                {
                    Runtime.Warning("No songs provided to like");
                    EmitEvent(SPOTIFY_OPERATION_ERROR, new { error = "No songs provided" });
                    return;
                }

                Runtime.Info($"Preparing to like {filteredSongs.Count()} songs on Spotify...");
                EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                {
                    phase = "initializing",
                    totalSongs = filteredSongs.Count(),
                    processed = 0,
                    found = 0,
                    liked = 0,
                    message = $"Preparing to process {filteredSongs.Count()} songs"
                });

                var errors = new List<SpotifyError>();
                var foundSongs = new Dictionary<SpotifySongId, int>(); // lookup from spotify song id to our song id
                var songStatusUpdates = new List<(int SongId, SpotifyStatus SpotifyStatus)>();

                int processedCount = 0;
                int totalCount = filteredSongs.Count();

                // Phase 1: Search for songs
                EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                {
                    phase = "searching",
                    totalSongs = totalCount,
                    processed = processedCount,
                    found = 0,
                    message = "Searching for songs on Spotify..."
                });

                foreach (var song in filteredSongs.ToArray())
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Runtime.Warning("Song liking operation was cancelled");
                        EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                        {
                            success = false,
                            cancelled = true,
                            message = "Operation cancelled by user"
                        });
                        return;
                    }

                    // Pass the cancellation token to SearchForSong
                    var result = await SearchForSong(song, cancellationToken);
                    processedCount++;

                    result.Match(
                       Right: id =>
                       {
                           foundSongs.TryAdd(id, song.Id);
                           songStatusUpdates.Add((song.Id, SpotifyStatus.Found));
                       },
                      Left: error =>
                      {
                          if (error is SongNotFound songNotFound)
                          {
                              songStatusUpdates.Add((song.Id, SpotifyStatus.NotFound));
                          }
                          else
                          {
                              errors.Add(error);
                          }
                      });

                    // Update status in batches to reduce UI updates
                    if (songStatusUpdates.Count >= 10 || processedCount == totalCount)
                    {
                        ObservableState.UpdateSongStatus(songStatusUpdates.ToArray());
                        songStatusUpdates.Clear();
                    }

                    // Send progress event
                    if (processedCount % 5 == 0 || processedCount == totalCount)
                    {
                        EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                        {
                            phase = "searching",
                            totalSongs = totalCount,
                            processed = processedCount,
                            found = foundSongs.Count,
                            message = $"Searching for songs on Spotify ({processedCount}/{totalCount})..."
                        });

                        Runtime.Info($"Searching for songs on Spotify ({processedCount}/{totalCount})...");
                    }

                    // Use try-catch specifically for Task.Delay with cancellation token
                    try
                    {
                        await Task.Delay(delayTime, cancellationToken); // Prevent too many requests from spotify
                    }
                    catch (TaskCanceledException)
                    {
                        Runtime.Warning("Operation was cancelled during delay");
                        EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                        {
                            success = false,
                            cancelled = true,
                            message = "Operation cancelled by user"
                        });
                        return;
                    }
                }

                // Check for cancellation before moving to phase 2
                if (cancellationToken.IsCancellationRequested)
                {
                    Runtime.Warning("Song liking operation was cancelled");
                    EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                    {
                        success = false,
                        cancelled = true,
                        message = "Operation cancelled by user"
                    });
                    return;
                }

                Runtime.Info($"Found {foundSongs.Count} of {filteredSongs.Count()} songs on Spotify");
                EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                {
                    phase = "searchComplete",
                    totalSongs = totalCount,
                    processed = processedCount,
                    found = foundSongs.Count,
                    message = $"Found {foundSongs.Count} of {filteredSongs.Count()} songs"
                });

                // Phase 2: Like the found songs
                if (foundSongs.Any())
                {
                    Runtime.Info($"Liking {foundSongs.Count} songs on Spotify...");
                    EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                    {
                        phase = "liking",
                        totalSongs = totalCount,
                        processed = processedCount,
                        found = foundSongs.Count,
                        message = $"Liking {foundSongs.Count} songs on Spotify..."
                    });

                    var result = await spotifyApi.LikeSongsAsync(foundSongs.Keys.ToArray(), cancellationToken); // sending spotify song ID to api
                    result.Errors.Iter(errors.Add);

                    // Convert spotify liked song ID to our song ID
                    var likedSongIds = result.LikedSongs.Select(id => foundSongs.ValueOrNone(id)).Somes();

                    if (likedSongIds.Count() != result.LikedSongs.Count())
                    {
                        Runtime.Warning("Some song mappings were lost during processing");
                    }

                    ObservableState.UpdateSongStatus(likedSongIds.Select(id => (id, SpotifyStatus.Liked)).ToArray());

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

                        try
                        {
                            // Final success event
                            EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                            {
                                success = true,
                                partialSuccess = likedSongIds.Count() < foundSongs.Count,
                                totalSongs = totalCount,
                                processed = processedCount,
                                found = foundSongs.Count,
                                liked = likedSongIds.Count(),
                                message = $"Successfully liked {likedSongIds.Count()} songs on Spotify",
                                errors = errors.Any() ? errors.ToArray() : null
                            });
                        }
                        catch (Exception finalEx)
                        {
                            Debug.WriteLine($"Error emitting final success event: {finalEx.Message}");
                            Runtime.Error($"Error emitting final event", finalEx);
                        }
                    }
                    else if (errors.Any())
                    {
                        Runtime.Error("Failed to like any songs on Spotify", None);
                        try
                        {
                            EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                            {
                                success = false,
                                totalSongs = totalCount,
                                processed = processedCount,
                                found = foundSongs.Count,
                                liked = 0,
                                message = "Failed to like any songs on Spotify",
                                errors = errors.ToArray()
                            });
                        }
                        catch (Exception finalEx)
                        {
                            Debug.WriteLine($"Error emitting final error event: {finalEx.Message}");
                            Runtime.Error($"Error emitting final event", finalEx);
                        }
                    }
                }
                else
                {
                    if (errors.Any())
                    {
                        Runtime.Error("Failed to find any songs on Spotify", None);
                        try
                        {
                            EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                            {
                                success = false,
                                totalSongs = totalCount,
                                processed = processedCount,
                                found = 0,
                                message = "Failed to find any songs on Spotify",
                                errors = errors.ToArray()
                            });
                        }
                        catch (Exception finalEx)
                        {
                            Debug.WriteLine($"Error emitting final event (no songs found): {finalEx.Message}");
                            Runtime.Error($"Error emitting final event", finalEx);
                        }
                    }
                    else
                    {
                        Runtime.Warning("No songs found on Spotify");
                        try
                        {
                            EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                            {
                                success = false,
                                totalSongs = totalCount,
                                processed = processedCount,
                                found = 0,
                                message = "No songs found on Spotify"
                            });
                        }
                        catch (Exception finalEx)
                        {
                            Debug.WriteLine($"Error emitting final event (no songs): {finalEx.Message}");
                            Runtime.Error($"Error emitting final event", finalEx);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Runtime.Warning("Song liking operation was cancelled");
                EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                {
                    success = false,
                    cancelled = true,
                    message = "Operation cancelled by user"
                });
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error liking songs", ex);
                EmitEvent(SPOTIFY_OPERATION_ERROR, new { error = ex.Message });
            }
            finally
            {
                // Clean up the cancellation token source
                cancelSource?.Dispose();
                cancelSource = null;
            }
        }

        /// <summary>
        /// Emits an event to the React Native JavaScript side
        /// </summary>
        private void EmitEvent(string eventName, object data)
        {
            try
            {
                // Make a local copy of reactContext to prevent race conditions
                var context = reactContext;
                if ((object)context != null)
                {
                    // Serialize data first to identify any serialization issues
                    string jsonData;
                    try
                    {
                        jsonData = JsonConvert.SerializeObject(data);
                    }
                    catch (Exception serEx)
                    {
                        Debug.WriteLine($"Error serializing event data for {eventName}: {serEx.Message}");
                        Runtime.Error($"Error serializing event data", serEx);

                        // Use a simplified fallback object if serialization fails
                        jsonData = JsonConvert.SerializeObject(new
                        {
                            error = $"Error serializing event data: {serEx.Message}",
                            simplifiedData = true
                        });
                    }

                    context.EmitJSEvent(
                        "RCTDeviceEventEmitter",
                        eventName,
                        jsonData);
                }
                else
                {
                    Debug.WriteLine($"Cannot emit event {eventName}: reactContext is null");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error emitting event {eventName}: {ex.Message}");
                Runtime.Error($"Error emitting event {eventName}", ex);
            }
        }

        /// <summary>
        /// Search for single song with improved status reporting
        /// </summary>
        async Task<Either<SpotifyError, SpotifySongId>> SearchForSong(SongInfo song, CancellationToken cancellationToken)
        {
            Runtime.Info($"Searching for song: '{song.Name}' by {string.Join(", ", song.Artist)}");

            // Log cancellation token status at start of search
            Debug.WriteLine($"SearchForSong: CancellationToken.IsCancellationRequested = {cancellationToken.IsCancellationRequested}");

            try
            {
                // Pass the cancellation token to the SpotifyAPI
                var searchResult = await spotifyApi.SearchSongAsync(song.Id, song.Name, song.Artist, cancellationToken);

                // Log cancellation token status after search
                Debug.WriteLine($"SearchForSong (after API call): CancellationToken.IsCancellationRequested = {cancellationToken.IsCancellationRequested}");

                searchResult.Match(
                    Right: track =>
                    {
                        Runtime.Success($"Found song: '{song.Name}' by {string.Join(", ", song.Artist)}");
                    },
                    Left: error =>
                    {
                        if (error is SongNotFound)
                        {
                            Runtime.Warning($"Song not found: '{song.Name}' by {string.Join(", ", song.Artist)}");
                        }
                        else
                        {
                            Runtime.Error($"Error searching for song: '{song.Name}'", None);
                        }
                    }
                );

                return searchResult.Map(v => v.Id);
            }
            catch (TaskCanceledException)
            {
                Runtime.Warning($"Song search was cancelled: '{song.Name}'");
                return new SpotifyErrors.ApiError("search", 0, "Operation was canceled");
            }
            catch (Exception ex)
            {
                Runtime.Error($"Exception in SearchForSong: {ex.Message}", ex);
                return new SpotifyErrors.ApiError("search", 500, ex.Message);
            }
        }

        /// <summary>
        /// Search for single artist with improved status reporting
        /// </summary>
        async Task<Either<SpotifyError, SpotifyArtistId>> SearchForArtist(string artistName, CancellationToken cancellationToken)
        {
            Runtime.Info($"Searching for artist: '{artistName}'");

            try
            {
                // Pass the cancellation token to the SpotifyAPI
                var searchResult = await spotifyApi.SearchArtistAsync(artistName, cancellationToken);

                searchResult.Match(
                    Right: artist =>
                    {
                        Runtime.Success($"Found artist: '{artistName}'");
                    },
                    Left: error =>
                    {
                        if (error is ArtistNotFound)
                        {
                            Runtime.Warning($"Artist not found: '{artistName}'");
                        }
                        else
                        {
                            Runtime.Error($"Error searching for artist: '{artistName}'", None);
                        }
                    }
                );

                return searchResult.Map(v => v.Id);
            }
            catch (TaskCanceledException)
            {
                Runtime.Warning($"Artist search was cancelled: '{artistName}'");
                return new SpotifyErrors.ApiError("search", 0, "Operation was canceled");
            }
            catch (Exception ex)
            {
                Runtime.Error($"Exception in SearchForArtist: {ex.Message}", ex);
                return new SpotifyErrors.ApiError("search", 500, ex.Message);
            }
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