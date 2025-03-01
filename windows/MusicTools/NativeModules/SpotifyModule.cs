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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MusicTools.NativeModules
{
    [ReactModule("SpotifyModule")]
    public sealed class SpotifyModule
    {
        private readonly SpotifyApi _spotifyApi;
        private bool _isInitialized = false;
        private HttpListener _httpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<string> _authCodeCompletionSource;
        private bool _isAuthenticated = false;
        private static bool _isListening = false;

        /// <summary>
        /// Initializes a new instance of the SpotifyModule class
        /// </summary>
        public SpotifyModule()
        {
            // These values should be stored securely in configuration
            string clientId = "a53ac9883ecd4a4da3f3b40c7588585c";
            string clientSecret = "9aac6c7555934655b601e4598f4b715b";

            // Use localhost redirect URI 
            string redirectUri = "http://localhost:8888/callback";

            _spotifyApi = new SpotifyApi(clientId, clientSecret, redirectUri);
            _isInitialized = true;
        }

        /// <summary>
        /// Gets the Spotify authorization URL for the authentication flow
        /// </summary>
        [ReactMethod("GetAuthUrl")]
        public void GetAuthUrl(IReactPromise<string> promise)
        {
            try
            {
                EnsureInitialized();
                promise.Resolve(_spotifyApi.GetAuthorizationUrl());
            }
            catch (Exception ex)
            {
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Starts listening for the OAuth callback
        /// </summary>
        [ReactMethod("StartAuthListener")]
        public void StartAuthListener(IReactPromise<string> promise)
        {
            try
            {
                EnsureInitialized();

                // If already listening, return success
                if (_isListening)
                {
                    promise.Resolve(JsonConvert.SerializeObject(new { success = true, message = "Auth listener already started" }));
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _authCodeCompletionSource = new TaskCompletionSource<string>();

                // Start HTTP listener
                Task.Run(async () =>
                {
                    try
                    {
                        await StartHttpListenerAsync(_cancellationTokenSource.Token);
                        _isListening = true;
                        promise.Resolve(JsonConvert.SerializeObject(new { success = true, message = "Auth listener started" }));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error starting HTTP listener: {ex.Message}");
                        promise.Reject(new ReactError { Message = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in StartAuthListener: {ex.Message}");
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Waits for authentication to complete
        /// </summary>
        [ReactMethod("WaitForAuthentication")]
        public void WaitForAuthentication(IReactPromise<string> promise)
        {
            try
            {
                if (!_isListening)
                {
                    promise.Resolve(JsonConvert.SerializeObject(new { success = false, error = "Auth listener not started" }));
                    return;
                }

                Task.Run(async () => {
                    try
                    {
                        // Wait for auth code or timeout after 3 minutes
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(3));
                        var completedTask = await Task.WhenAny(_authCodeCompletionSource.Task, timeoutTask);

                        if (completedTask == timeoutTask)
                        {
                            promise.Resolve(JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Authentication timed out. Please try again."
                            }));
                            return;
                        }

                        // Get the auth code
                        var authCode = await _authCodeCompletionSource.Task;

                        // Exchange code for token
                        var result = await _spotifyApi.GetAccessTokenAsync(authCode);

                        _isListening = false;

                        result.Match(
                            Right: success => {
                                _isAuthenticated = true;
                                promise.Resolve(JsonConvert.SerializeObject(new { success = true }));
                            },
                            Left: error => {
                                promise.Resolve(JsonConvert.SerializeObject(new { success = false, error = error }));
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        promise.Reject(new ReactError { Message = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in WaitForAuthentication: {ex.Message}");
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Checks if authentication has completed
        /// </summary>
        [ReactMethod("CheckAuthStatus")]
        public void CheckAuthStatus(IReactPromise<string> promise)
        {
            try
            {
                promise.Resolve(JsonConvert.SerializeObject(new { isAuthenticated = _isAuthenticated }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CheckAuthStatus: {ex.Message}");
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Stops the authentication listener
        /// </summary>
        [ReactMethod("StopAuthListener")]
        public void StopAuthListener(IReactPromise<string> promise)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _isListening = false;
                StopHttpListener();
                promise.Resolve(JsonConvert.SerializeObject(new { success = true }));
            }
            catch (Exception ex)
            {
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Starts an HTTP listener for Spotify OAuth callback
        /// </summary>
        private async Task StartHttpListenerAsync(CancellationToken cancellationToken)
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add("http://localhost:8888/");
                _httpListener.Start();

                Debug.WriteLine("HTTP Listener started on http://localhost:8888/");

                // Handle incoming requests asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var context = await _httpListener.GetContextAsync();
                            ProcessRequest(context);
                        }
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        Debug.WriteLine($"HTTP Listener error: {ex.Message}");
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start HTTP listener: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Processes incoming HTTP requests
        /// </summary>
        private async void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                Debug.WriteLine($"Received request: {request.Url.PathAndQuery}");

                // Check if this is the callback URL
                if (request.Url.AbsolutePath.Contains("/callback"))
                {
                    // Extract code parameter
                    string code = request.QueryString["code"];

                    if (!string.IsNullOrEmpty(code))
                    {
                        Debug.WriteLine($"Got auth code: {code}");
                        _authCodeCompletionSource.TrySetResult(code);

                        // Return a success page to the user
                        string successHtml = "<html><body><h1>Authentication Successful!</h1><p>You can now close this window and return to the app.</p></body></html>";
                        byte[] buffer = Encoding.UTF8.GetBytes(successHtml);
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        // Handle error
                        string error = request.QueryString["error"];
                        Debug.WriteLine($"Auth error: {error}");
                        _authCodeCompletionSource.TrySetException(new Exception($"Authentication failed: {error}"));

                        // Return an error page
                        string errorHtml = $"<html><body><h1>Authentication Failed</h1><p>Error: {error}</p><p>Please close this window and try again.</p></body></html>";
                        byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                }
                else
                {
                    // Not a callback, return 404
                    response.StatusCode = 404;
                    string notFoundHtml = "<html><body><h1>404 Not Found</h1></body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(notFoundHtml);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }

                response.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing HTTP request: {ex.Message}");
                try
                {
                    // Try to return an error response
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch
                {
                    // Ignore any errors in closing the response
                }
            }
        }

        /// <summary>
        /// Stops the HTTP listener
        /// </summary>
        private void StopHttpListener()
        {
            try
            {
                if (_httpListener != null && _httpListener.IsListening)
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                    Debug.WriteLine("HTTP Listener stopped");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping HTTP listener: {ex.Message}");
            }
        }

        /// <summary>
        /// Searches for and likes songs on Spotify
        /// </summary>
        [ReactMethod("LikeSongs")]
        public void LikeSongs(string chosenSongsJson, IReactPromise<string> promise)
        {
            try
            {
                EnsureInitialized();

                var chosenSongs = JsonConvert.DeserializeObject<SongInfo[]>(chosenSongsJson);
                if (chosenSongs == null || !chosenSongs.Any())
                {
                    promise.Resolve(JsonConvert.SerializeObject(new { success = false, error = "No songs provided" }));
                    return;
                }

                Task.Run(async () => {
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

                        if (errors.Any())
                        {
                            promise.Resolve(JsonConvert.SerializeObject(new
                            {
                                success = false,
                                partialSuccess = errors.Count < chosenSongs.Length,
                                errors = errors
                            }));
                        }
                        else
                        {
                            promise.Resolve(JsonConvert.SerializeObject(new { success = true }));
                        }
                    }
                    catch (Exception ex)
                    {
                        promise.Reject(new ReactError { Message = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LikeSongs: {ex.Message}");
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Follows artists from chosen songs on Spotify
        /// </summary>
        [ReactMethod("FollowArtists")]
        public void FollowArtists(string chosenSongsJson, IReactPromise<string> promise)
        {
            try
            {
                EnsureInitialized();

                var chosenSongs = JsonConvert.DeserializeObject<SongInfo[]>(chosenSongsJson);
                if (chosenSongs == null || !chosenSongs.Any())
                {
                    promise.Resolve(JsonConvert.SerializeObject(new { success = false, error = "No songs provided" }));
                    return;
                }

                Task.Run(async () => {
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

                        if (errors.Any())
                        {
                            promise.Resolve(JsonConvert.SerializeObject(new
                            {
                                success = false,
                                partialSuccess = errors.Count < distinctArtists.Count,
                                errors = errors
                            }));
                        }
                        else
                        {
                            promise.Resolve(JsonConvert.SerializeObject(new { success = true }));
                        }
                    }
                    catch (Exception ex)
                    {
                        promise.Reject(new ReactError { Message = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FollowArtists: {ex.Message}");
                promise.Reject(new ReactError { Message = ex.Message });
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