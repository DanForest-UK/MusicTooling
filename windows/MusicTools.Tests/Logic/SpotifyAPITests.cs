using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTools.Logic;
using MusicTools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using Moq;
using static MusicTools.Core.Types;
using System.Net;
using System.Linq;
using static MusicTools.Core.SpotifyErrors;
using static LanguageExt.Prelude;
using LanguageExt;

namespace MusicTools.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="SpotifyApi"/> class.
    /// </summary>
    [TestClass]
    public class SpotifyApiTests
    {
        private SpotifyApi spotifyApi;
        private Mock<IOAuthClient> mockOAuthClient;
        private Mock<ISpotifyClient> mockSpotifyClient;
        private Mock<ISearchClient> mockSearchClient;
        private Mock<ILibraryClient> mockLibraryClient;
        private Mock<IFollowClient> mockFollowClient;
        private Mock<Func<IOAuthClient>> mockOAuthClientFactory;

        /// <summary>
        /// Initializes the test environment by setting up mocks and resetting the authentication state.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            mockOAuthClient = new Mock<IOAuthClient>();
            mockSpotifyClient = new Mock<ISpotifyClient>();
            mockSearchClient = new Mock<ISearchClient>();
            mockLibraryClient = new Mock<ILibraryClient>();
            mockFollowClient = new Mock<IFollowClient>();

            mockOAuthClientFactory = new Mock<Func<IOAuthClient>>();
            mockOAuthClientFactory.Setup(f => f()).Returns(mockOAuthClient.Object);

            mockSpotifyClient.Setup(c => c.Search).Returns(mockSearchClient.Object);
            mockSpotifyClient.Setup(c => c.Library).Returns(mockLibraryClient.Object);
            mockSpotifyClient.Setup(c => c.Follow).Returns(mockFollowClient.Object);

            spotifyApi = new SpotifyApi(
                "test-client-id",
                "test-client-secret",
                "https://test-redirect-uri",
                mockOAuthClientFactory.Object);

            ResetAuthenticationState();
            SetupRuntimeHelpers();
        }

        /// <summary>
        /// Resets the authentication state of the Spotify API.
        /// </summary>
        private void ResetAuthenticationState()
        {
            var spotifyClientField = typeof(SpotifyApi).GetField("spotifyClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            spotifyClientField.SetValue(spotifyApi, Option<ISpotifyClient>.None);

            var tokenExpiryField = typeof(SpotifyApi).GetField("tokenExpiry",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            tokenExpiryField.SetValue(spotifyApi, DateTime.MinValue);
        }

        /// <summary>
        /// Sets up runtime helpers to capture logs.
        /// </summary>
        private void SetupRuntimeHelpers()
        {
            Runtime.Info = message => { Console.WriteLine($"INFO: {message}"); return LanguageExt.Unit.Default; };
            Runtime.Warning = message => { Console.WriteLine($"WARNING: {message}"); return LanguageExt.Unit.Default; };
            Runtime.Error = (message, ex) => { Console.WriteLine($"ERROR: {message} - {ex.Map(e => e.Message).IfNone("")}"); return LanguageExt.Unit.Default; };
            Runtime.Status = (message, level) => { Console.WriteLine($"STATUS ({level}): {message}"); return LanguageExt.Unit.Default; };
        }

        /// <summary>
        /// Tests that SpotifyApi.GetAccessTokenAsync retrieves an access token.
        /// </summary>
        [TestMethod]
        public async Task GetAccessToken()
        {
            var authCode = "test-auth-code";
            var tokenResponse = new AuthorizationCodeTokenResponse
            {
                AccessToken = "test-access-token",
                ExpiresIn = 3600,
                RefreshToken = "test-refresh-token"
            };

            mockOAuthClient
                .Setup(o => o.RequestToken(
                    It.IsAny<AuthorizationCodeTokenRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenResponse);

            var result = await spotifyApi.GetAccessTokenAsync(authCode);

            mockOAuthClient.Verify(o => o.RequestToken(
                It.IsAny<AuthorizationCodeTokenRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

            Assert.IsTrue(result.IsRight, "Result should be successful");
            result.Match(
                Right: success =>
                {
                    Assert.IsTrue(success, "Success flag should be true");
                    return Unit.Default;
                },
                Left: error =>
                {
                    Assert.Fail($"Should not return error: {error.Message}");
                    return Unit.Default;
                }
            );
        }

        /// <summary>
        /// Tests that SpotifyApi.GetAccessTokenAsync returns an authentication error when the API fails.
        /// </summary>
        [TestMethod]
        public async Task GetTokenAuthenticationError()
        {
            var authCode = "test-auth-code";
            var exceptionMessage = "API Error during authentication";

            mockOAuthClient
                .Setup(o => o.RequestToken(
                    It.IsAny<AuthorizationCodeTokenRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new APIException(exceptionMessage, null));

            var result = await spotifyApi.GetAccessTokenAsync(authCode);

            mockOAuthClient.Verify(o => o.RequestToken(
                It.IsAny<AuthorizationCodeTokenRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

            Assert.IsTrue(result.IsLeft, "Result should be an error");
            result.Match(
                Right: _ =>
                {
                    Assert.Fail("Should not return success");
                    return Unit.Default;
                },
                Left: error =>
                {
                    Assert.IsTrue(error is AuthenticationError, "Error should be AuthenticationError");
                    var authError = error as AuthenticationError;
                    Assert.IsTrue(authError.Message.Contains("API Error"), "Error message should mention API error");
                    return Unit.Default;
                }
            );
        }

        /// <summary>
        /// Tests that SpotifyApi.GetAccessTokenAsync returns an error when already authenticated.
        /// </summary>
        [TestMethod]
        public async Task GetAccessTokenAlreadyAuthenticated()
        {
            var authCode = "test-auth-code";

            SetAuthenticatedState();

            var result = await spotifyApi.GetAccessTokenAsync(authCode);

            mockOAuthClient.Verify(o => o.RequestToken(
                It.IsAny<AuthorizationCodeTokenRequest>(),
                It.IsAny<CancellationToken>()), Times.Never);

            Assert.IsTrue(result.IsLeft, "Result should be an error");
            result.Match(
                Right: _ =>
                {
                    Assert.Fail("Should not return success");
                    return Unit.Default;
                },
                Left: error =>
                {
                    Assert.IsTrue(error is AlreadyAuthenticated, "Error should be AlreadyAuthenticated");
                    return Unit.Default;
                }
            );
        }

        /// <summary>
        /// Tests that SpotifyApi.SearchSongAsync successfully retrieves song information.
        /// </summary>
        [TestMethod]
        public async Task SearchSongAsync()
        {
            var searchRequest = new SearchRequest(SearchRequest.Types.Track, "track:Test Song artist:Test Artist");
            var searchResponse = new SearchResponse
            {
                Tracks = new Paging<FullTrack, SearchResponse>
                {
                    Items = new FullTrack[]
                    {
                            new FullTrack
                            {
                                Id = "spotify-track-id",
                                Name = "Test Song",
                                Artists = new SimpleArtist[]
                                {
                                    new SimpleArtist
                                    {
                                        Id = "spotify-artist-id",
                                        Name = "Test Artist",
                                        Uri = "spotify:artist:id"
                                    }
                                }.ToList(),
                                Uri = "spotify:track:id"
                            }
                    }.ToList()
                }
            };

            SetAuthenticatedState();

            mockSearchClient.Setup(c => c.Item(
                It.IsAny<SearchRequest>(),
                It.IsAny<CancellationToken>()
            )).ReturnsAsync(searchResponse);

            var result = await spotifyApi.SearchSongAsync(
                1,
                "Test Song",
                new[] { "Test Artist" },
                CancellationToken.None
            );

            Assert.IsTrue(result.IsRight, "Result should be successful");
            result.Match(
                Right: track =>
                {
                    Assert.AreEqual("spotify-track-id", track.Id.Value, "Track ID should match");
                    Assert.AreEqual("Test Song", track.Name, "Track name should match");
                    Assert.AreEqual(1, track.Artists.Length, "Should have one artist");
                    Assert.AreEqual("Test Artist", track.Artists[0].Name, "Artist name should match");
                    return LanguageExt.Unit.Default;
                },
                Left: error =>
                {
                    Assert.Fail($"Should not return error: {error.Message}");
                    return LanguageExt.Unit.Default;
                }
            );
        }

        /// <summary>
        /// Sets the Spotify API state to authenticated.
        /// </summary>
         void SetAuthenticatedState()
        {
            var spotifyClientField = typeof(SpotifyApi).GetField("spotifyClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var spotifyClientOption = Some(mockSpotifyClient.Object);
            spotifyClientField.SetValue(spotifyApi, spotifyClientOption);

            var tokenExpiryField = typeof(SpotifyApi).GetField("tokenExpiry",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            tokenExpiryField.SetValue(spotifyApi, DateTime.UtcNow.AddHours(1));
        }
    }
}