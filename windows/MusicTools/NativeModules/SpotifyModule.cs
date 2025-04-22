using Microsoft.ReactNative.Managed;
using MusicTools.Logic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using static LanguageExt.Prelude;
using Windows.Storage;
using LanguageExt;
using System.Threading;
using MusicTools.Domain;

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
        int delayTime = 50; // Delay in API requests to prevent too many requests error

        // React context for emitting events
        ReactContext reactContext;

        // Constants for event names
        private const string SPOTIFY_OPERATION_PROGRESS = "spotifyOperationProgress";
        private const string SPOTIFY_OPERATION_COMPLETE = "spotifyOperationComplete";
        private const string SPOTIFY_OPERATION_ERROR = "spotifyOperationError";

        // Constants for batch processing
        private const int BATCH_SIZE = 50;

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
                                return new { success = true, error = new SpotifyError("", "", "") };
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
        /// Emits a cancellation event and logs a warning message
        /// </summary>
        /// <param name="warningMessage">The message to log as a warning</param>
        private void EmitCancellationEvent(string warningMessage = "Spotify operation cancelled by user")
        {
            Runtime.Warning(warningMessage);
            EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
            {
                success = false,
                cancelled = true,
                message = "Operation cancelled by user"
            });
        }

        /// <summary>
        /// Cancels any ongoing Spotify operations
        /// </summary>
        [ReactMethod("CancelSpotifyOperation")]
        public void CancelSpotifyOperation()
        {
            try
            {
                if (CancellationHelper.CancelOperation(cancelSource, "Spotify"))
                    EmitCancellationEvent();
            }
            catch (Exception ex)
            {
                Runtime.Error("Error cancelling Spotify operation", ex);
            }
        }

        /// <summary>
        /// Checks if the operation should be cancelled and emits event if needed
        /// </summary>
        void CheckForCancel(CancellationToken token)
        {
            try
            {
                CancellationHelper.CheckForCancel(token, "Spotify");
            }
            catch (TaskCanceledException)
            {
                EmitCancellationEvent();
                throw;
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

                var distinctArtists = ObservableState.Current.DistinctArtists(includeAlreadyProcessed: false);
                if (!distinctArtists.Any())
                {
                    return Task.FromResult(JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "No artists available to follow. All artists have been processed already.",
                        noArtistsToProcess = true
                    }));
                }

                Runtime.Info($"Searching for {distinctArtists.Count()} artists on Spotify...");

                // Create a new cancellation token source for this operation
                cancelSource = CancellationHelper.ResetCancellationToken(ref cancelSource);
                var token = cancelSource.Token;

                return Task.Run(async () => {
                    try
                    {
                        // Final results
                        var errors = new List<SpotifyError>();
                        var foundArtists = new Dictionary<SpotifyArtistId, Artist>();// lookup of spotify artist id to name

                        // Artist array for processing in batches
                        var totalArtists = distinctArtists.Length;
                        var processedCount = 0;
                        var artistUpdates = new List<(Artist ArtistName, SpotifyStatus Status)>();

                        // Process artists in batches
                        for (int index = 0; index < totalArtists; index += BATCH_SIZE)
                        {
                            // Check for cancellation between batches
                            CheckForCancel(token);

                            // Calculate the actual batch size for this iteration
                            var artistsBatch = distinctArtists.Skip(index).Take(BATCH_SIZE);

                            // Process one batch: Search for artists
                            var batchFoundArtists = new Dictionary<SpotifyArtistId, Artist>();

                            foreach (var artist in artistsBatch)
                            {
                                // Check for cancellation within batch
                                CheckForCancel(token);

                                // Update progress for UI
                                EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                                {
                                    phase = "searching",
                                    totalArtists,
                                    processed = processedCount,
                                    message = $"Searching for artist {processedCount} of {totalArtists}: {artist.Value}"
                                });

                                var result = await SearchForArtist(artist, token);
                                processedCount++;

                                result.Match(
                                   Right: foundArtistId =>
                                   {
                                       batchFoundArtists.TryAdd(foundArtistId, artist);
                                       artistUpdates.Add((artist, SpotifyStatus.Found));
                                   },
                                  Left: error =>
                                  {
                                      if (error is ArtistNotFound)
                                          artistUpdates.Add((artist, SpotifyStatus.NotFound));
                                      else
                                          errors.Add(error);
                                  });

                                // Update status in batches to reduce UI updates
                                if (artistUpdates.Count >= 100 || processedCount == totalArtists)
                                {
                                    ObservableState.UpdateArtistStatus(artistUpdates.ToArray());
                                    artistUpdates.Clear();
                                }

                                // Add delay with cancellation token
                                try
                                {
                                    await Task.Delay(delayTime, token);
                                }
                                catch (TaskCanceledException)
                                {
                                    EmitCancellationEvent("Artist search was cancelled during delay");
                                    break;
                                }
                            }

                            // Like the batch of found artists
                            if (batchFoundArtists.Any() && !token.IsCancellationRequested)
                            {
                                Runtime.Info($"Following {batchFoundArtists.Count} artists on Spotify...");

                                EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                                {
                                    phase = "following",
                                    totalArtists,
                                    processed = processedCount,
                                    message = $"Following {batchFoundArtists.Count} artists on Spotify..."
                                });

                                var result = await spotifyApi.FollowArtistsAsync(batchFoundArtists.Keys.ToArray(), token);
                                result.Errors.Iter(errors.Add);

                                // Convert spotify artist id to artist name
                                var followedArtistNames = result.FollowedArtists.Select(id => batchFoundArtists.ValueOrNone(id)).Somes().Distinct();

                                if (followedArtistNames.Count() != result.FollowedArtists.Count())
                                    Runtime.Warning("Some artist mappings were lost during processing");

                                // Update UI with followed status
                                ObservableState.UpdateArtistStatus(followedArtistNames.Select(a => (a, SpotifyStatus.Liked)).ToArray());

                                // Merge batch results with overall results
                                foreach (var artist in batchFoundArtists)
                                    foundArtists.TryAdd(artist.Key, artist.Value);
                            }

                            // Progress update after batch completion
                            EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                            {
                                phase = "searching",
                                totalArtists,
                                processed = processedCount,
                                message = $"Processed {processedCount} of {totalArtists} artists..."
                            });
                        }

                        // Final status update
                        Runtime.Info($"Found and processed {foundArtists.Count} of {totalArtists} artists on Spotify");
                                               
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
                        EmitCancellationEvent("Artist follow operation was cancelled");
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
        /// Refactored to process songs in batches, searching and liking each batch before moving to the next
        /// </summary>
        [ReactMethod("LikeSongs")]
        public void LikeSongs()
        {
            try
            {
                EnsureInitialized();

                // Check if there are any songs to process before starting
                var filteredSongs = ObservableState.Current.FilteredSongs(includeAlreadyProcessed: false);

                if (!filteredSongs.Any())
                {
                    EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                    {
                        success = false,
                        message = "No songs available to like. All songs have been processed already.",
                        noSongsToProcess = true
                    });
                    return;
                }

                // Create a new cancellation token for this operation
                cancelSource = CancellationHelper.ResetCancellationToken(ref cancelSource);
                var token = cancelSource.Token;

                // Start the operation in a background task
                Task.Run(async () => await LikeSongsProcessAsync(filteredSongs, token), token);

                // Return immediately, status will be sent via events
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error starting LikeSongs operation", ex);
                EmitEvent(SPOTIFY_OPERATION_ERROR, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Processes the like songs operation in batches of 50 songs
        /// For each batch: Search for songs -> Like found songs -> Move to next batch
        /// </summary>
        private async Task LikeSongsProcessAsync(Seq<SongInfo> songs, CancellationToken cancellationToken)
        {
            try
            {                            
                // Overall tracking of errors and processed songs
                var errors = new List<SpotifyError>();
                var totalProcessed = 0;
                var totalLiked = 0;
                var totalCount = songs.Count();

                var songStatusUpdates = new List<(SongId SongId, SpotifyStatus Status)>();

                // Process songs in batches
                for (int index = 0; index < totalCount; index += BATCH_SIZE)
                {
                    // Check for cancellation between batches
                    CheckForCancel(cancellationToken);

                    var songsBatch = songs.Skip(index).Take(BATCH_SIZE);

                    // Current batch tracking
                    var foundSongs = new Dictionary<SpotifySongId, SongId>(); // Spotify ID to song ID lookup
                    var batchNumber = index / BATCH_SIZE + 1;
                    var totalBatches = (totalCount + BATCH_SIZE - 1) / BATCH_SIZE;

                    // Search for all songs in this batch
                    foreach (var song in songsBatch)
                    {
                        // Check for cancellation within batch
                        CheckForCancel(cancellationToken);

                        EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                        {
                            phase = "searching",
                            totalSongs = totalCount,
                            processed = totalProcessed,
                            batchNumber,
                            totalBatches,
                            message = $"Searching for song {totalProcessed}/{totalCount}: {song.Name.Value}"
                        });

                        // Pass the cancellation token to SearchForSong
                        var result = await SearchForSong(song, cancellationToken);
                        totalProcessed++;

                        result.Match(
                           Right: id =>
                           {
                               foundSongs.TryAdd(id, song.Id);
                               songStatusUpdates.Add((song.Id, SpotifyStatus.Found));
                           },
                          Left: error =>
                          {
                              if (error is AuthenticationError authentication)
                                  throw new Exception("Authentication expired");
                              else if (error is SongNotFound)
                                  songStatusUpdates.Add((song.Id, SpotifyStatus.NotFound));
                              else
                                  errors.Add(error);
                          });

                        // Update status in batches to reduce UI updates
                        if (songStatusUpdates.Count >= 100 || totalProcessed == totalCount)
                        {
                            ObservableState.UpdateSongStatus(songStatusUpdates.ToArray());
                            songStatusUpdates.Clear();
                        }

                        try
                        {
                            await Task.Delay(delayTime, cancellationToken); // Prevent too many requests from Spotify
                        }
                        catch (TaskCanceledException)
                        {
                            EmitCancellationEvent("Operation was cancelled during delay");
                            return;
                        }
                    }

                    // Like the found songs in this batch
                    if (foundSongs.Any() && !cancellationToken.IsCancellationRequested)
                    {
                        EmitEvent(SPOTIFY_OPERATION_PROGRESS, new
                        {
                            phase = "liking",
                            totalSongs = totalCount,
                            processed = totalProcessed,
                            batchNumber,
                            totalBatches,
                            message = $"Liking {foundSongs.Count} songs from batch {batchNumber}/{totalBatches}..."
                        });

                        var result = await spotifyApi.LikeSongsAsync(foundSongs.Keys.ToArray(), cancellationToken);
                        result.Errors.Iter(errors.Add);

                        // Convert Spotify liked song ID to our song ID
                        var likedSongIds = result.LikedSongs.Select(id => foundSongs.ValueOrNone(id)).Somes();
                        totalLiked += likedSongIds.Count();

                        if (likedSongIds.Count() != result.LikedSongs.Count())
                            Runtime.Error("Some song mappings were lost during processing", None);

                        ObservableState.UpdateSongStatus(likedSongIds.Select(id => (id, SpotifyStatus.Liked)).ToArray());
                    }
                }

                // Final operation status
                if (totalLiked > 0)
                {
                    EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                    {
                        success = true,
                        partialSuccess = totalLiked < totalProcessed,
                        message = $"Successfully liked {totalLiked} songs on Spotify",
                        errors = errors.Any() ? errors.ToArray() : null
                    });
                }
                else if (errors.Any())
                {
                    Runtime.Error("Failed to like any songs on Spotify", None);
                    EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                    {
                        success = false,
                        message = "Failed to like any songs on Spotify",
                        errors = errors.ToArray()
                    });
                }
                else
                {
                    Runtime.Warning("No songs found on Spotify");
                    EmitEvent(SPOTIFY_OPERATION_COMPLETE, new
                    {
                        success = false,
                        message = "No songs found on Spotify"
                    });
                }
            }
            catch (TaskCanceledException)
            {
                EmitCancellationEvent("Song liking operation was cancelled");
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error liking songs", ex);
                EmitEvent(SPOTIFY_OPERATION_ERROR, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Emits an event to the React Native JavaScript side using the shared helper
        /// </summary>
        private void EmitEvent(string eventName, object data) =>
            JsEmitterHelper.EmitEvent(reactContext, eventName, data);

        /// <summary>
        /// Search for single song with improved status reporting
        /// </summary>
        async Task<Either<SpotifyError, SpotifySongId>> SearchForSong(SongInfo song, CancellationToken cancellationToken)
        {
            try
            {
                // Pass the cancellation token to the SpotifyAPI
                var searchResult = await spotifyApi.SearchSongAsync(song.Id, song.Name, song.Artist, cancellationToken);

                // Log cancellation token status after search
                Debug.WriteLine($"SearchForSong (after API call): CancellationToken.IsCancellationRequested = {cancellationToken.IsCancellationRequested}");

                searchResult.Match(
                    Right: _ => { },
                    Left: error =>
                    {
                        if (error is SongNotFound) { }
                        else
                            Runtime.Error($"Error searching for song: '{song.Name}'", None);
                    }
                );

                return searchResult.Map(v => v.Id);
            }
            catch (TaskCanceledException)
            {
                Runtime.Warning($"Song search was cancelled: '{song.Name}'");
                return new ApiError("search", 0, "Operation was canceled");
            }
            catch (Exception ex)
            {
                Runtime.Error($"Exception in SearchForSong: {ex.Message}", ex);
                return new ApiError("search", 500, ex.Message);
            }
        }

        /// <summary>
        /// Search for single artist with improved status reporting
        /// </summary>
        async Task<Either<SpotifyError, SpotifyArtistId>> SearchForArtist(Artist artistName, CancellationToken cancellationToken)
        {
            try
            {
                // Pass the cancellation token to the SpotifyAPI
                var searchResult = await spotifyApi.SearchArtistAsync(artistName, cancellationToken);

                searchResult.Match(
                    Right: artist => { },
                    Left: error =>
                    {
                        if (error is ArtistNotFound) { }
                        else if (error is AuthenticationError authentication)
                            throw new Exception("Authentication expired");
                        else
                            Runtime.Error($"Error searching for artist: '{artistName}'", None);
                    }
                );

                return searchResult.Map(v => v.Id);
            }
            catch (TaskCanceledException)
            {
                Runtime.Warning($"Artist search was cancelled: '{artistName}'");
                return new ApiError("search", 0, "Operation was canceled");
            }
            catch (Exception ex)
            {
                Runtime.Error($"There was a problem searching for artists", ex);
                return new ApiError("search", 500, ex.Message);
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