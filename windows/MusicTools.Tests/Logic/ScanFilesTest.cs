using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTools.Logic;
using MusicTools.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static MusicTools.Core.Types;
using static LanguageExt.Prelude;
using System.IO;
using System.Linq;
using Moq;

namespace MusicTools.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="ScanFiles"/> class.
    /// </summary>
    [TestClass]
    public class ScanFilesTests
    {
        private List<string> statusMessages;
        private List<string> errorMessages;
        private List<string> warningMessages;
        private List<string> infoMessages;

        /// <summary>
        /// Initializes the test environment by setting up message collections and resetting static state.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            statusMessages = new List<string>();
            errorMessages = new List<string>();
            warningMessages = new List<string>();
            infoMessages = new List<string>();

            Runtime.Info = message => { infoMessages.Add(message); return unit; };
            Runtime.Warning = message => { warningMessages.Add(message); return unit; };
            Runtime.Error = (message, ex) => { errorMessages.Add(message); return unit; };
            Runtime.Status = (message, level) => { statusMessages.Add(message); return unit; };

            ScanFiles.CancelScan();
        }

        /// <summary>
        /// Tests that <see cref="ScanFiles.ScanFilesAsync(string)"/> returns an error for null, empty, or whitespace paths.
        /// </summary>
        [TestMethod]
        public async Task NullOrEmptyPath()
        {
            var nullPathResult = await ScanFiles.ScanFilesAsync(null);
            var emptyPathResult = await ScanFiles.ScanFilesAsync("");
            var whitespacePathResult = await ScanFiles.ScanFilesAsync("   ");

            Assert.IsTrue(nullPathResult.IsLeft, "Result should be an error for null path");
            Assert.IsTrue(emptyPathResult.IsLeft, "Result should be an error for empty path");
            Assert.IsTrue(whitespacePathResult.IsLeft, "Result should be an error for whitespace path");

            Assert.IsTrue(errorMessages.Any(m => m.Contains("No folder path specified")),
                "Should log an error message about no folder path");
        }

        /// <summary>
        /// Tests that ScanFilesAsync calls Runtime.GetFilesWithExtensionAsync with the correct parameters.
        /// </summary>
        [TestMethod]
        public async Task ScanFilesAsync()
        {
            var testPath = @"C:\Test\Path";
            var testFiles = new[] { @"C:\Test\Path\file1.mp3", @"C:\Test\Path\file2.mp3" }.ToSeq();
            var getFilesCalled = false;

            Runtime.GetFilesWithExtensionAsync = (path, ext, token) =>
            {
                Assert.AreEqual(testPath, path, "Path should match input");
                Assert.AreEqual(".mp3", ext, "Extension should be .mp3");
                getFilesCalled = true;
                return Task.FromResult(testFiles);
            };

            Runtime.WithStream = (path, action) =>
            {
                action(null);
                return Task.CompletedTask;
            };

            Runtime.ReadSongInfo = (path, stream) =>            
                new SongInfo(
                    Id: 1,
                    Name: "Test Song",
                    Path: path,
                    Artist: new[] { "Test Artist" },
                    Album: "Test Album",
                    Rating: 5,
                    ArtistStatus: SpotifyStatus.NotSearched,
                    SongStatus: SpotifyStatus.NotSearched
                );
            

            var result = await ScanFiles.ScanFilesAsync(testPath);

            Assert.IsTrue(getFilesCalled, "GetFilesWithExtension should be called");
            Assert.IsTrue(result.IsRight, "Result should be successful");

            result.Match(
                Right: songs =>
                {
                    Assert.AreEqual(2, songs.Count(), "Should return 2 songs");
                    return unit;
                },
                Left: error =>
                {
                    Assert.Fail($"Should not return error: {error.Message}");
                    return unit;
                }
            );

            Assert.IsTrue(infoMessages.Any(m => m.Contains("Found 2 MP3 files")),
                "Should log number of files found");
        }

        /// <summary>
        /// Tests that <see cref="ScanFiles.ScanFilesAsync(string)"/> handles cancellation correctly.
        /// </summary>
        [TestMethod]
        public async Task ScanFilesWithCancellation()
        {
            var testPath = @"C:\Test\Path";
            var getFilesCalled = false;
            var delayTcs = new TaskCompletionSource<bool>();

            Runtime.GetFilesWithExtensionAsync = (path, ext, token) =>
            {
                getFilesCalled = true;
                return delayTcs.Task.ContinueWith(_ => new Seq<string>());
            };

            var scanTask = ScanFiles.ScanFilesAsync(testPath);

            while (!getFilesCalled)
            {
                await Task.Delay(10);
            }

            ScanFiles.CancelScan();

            delayTcs.SetResult(true);

            var result = await scanTask;

            Assert.IsTrue(result.IsLeft, "Result should be an error");
        }

        /// <summary>
        /// Tests that ScanFiles.ScanFilesAsync returns an error when an exception occurs during processing.
        /// </summary>
        [TestMethod]
        public async Task ScanFilesAsync_WithExceptionInProcess_ReturnsError()
        {
            var testPath = @"C:\Test\Path";

            Runtime.GetFilesWithExtensionAsync = (path, ext, token) =>
                 throw new UnauthorizedAccessException("Access denied");
            
            var result = await ScanFiles.ScanFilesAsync(testPath);

            Assert.IsTrue(result.IsLeft, "Result should be an error");
            result.Match(
                Right: _ =>
                {
                    Assert.Fail("Should not return success");
                    return unit;
                },
                Left: error =>
                {
                    Assert.AreEqual(AppErrors.AccessToPathDenied(testPath).Code, error.Code,
                        "Error should be access denied");
                    return unit;
                }
            );

            Assert.IsTrue(errorMessages.Any(m => m.Contains("Access to path denied")),
                "Should log access denied error");
        }

        /// <summary>
        /// Tests that ScanFiles.ScanFilesAsync processes files in parallel.
        /// </summary>
        [TestMethod]
        public async Task ScanFilesParallel()
        {
            var testPath = @"C:\Test\Path";
            var testFiles = new[]
            {
                    @"C:\Test\Path\file1.mp3",
                    @"C:\Test\Path\file2.mp3",
                    @"C:\Test\Path\file3.mp3",
                    @"C:\Test\Path\file4.mp3"
                }.ToSeq();

            var withStreamCallCount = 0;

            Runtime.GetFilesWithExtensionAsync = (path, ext, token) =>
            {
                return Task.FromResult(testFiles);
            };

            Runtime.WithStream = (path, action) =>
            {
                Interlocked.Increment(ref withStreamCallCount);
                return Task.CompletedTask;
            };

            Runtime.ReadSongInfo = (path, stream) =>
            {
                return new SongInfo(
                    Id: 1,
                    Name: "Test Song",
                    Path: path,
                    Artist: new[] { "Test Artist" },
                    Album: "Test Album",
                    Rating: 5,
                    ArtistStatus: SpotifyStatus.NotSearched,
                    SongStatus: SpotifyStatus.NotSearched
                );
            };

            var result = await ScanFiles.ScanFilesAsync(testPath);

            Assert.AreEqual(4, withStreamCallCount, "WithStream should be called for each file");
            Assert.IsTrue(result.IsRight, "Result should be successful");
        }

        /// <summary>
        /// Tests that ScanFiles.ScanFilesAsync handles errors in individual files gracefully.
        /// </summary>
        [TestMethod]
        public async Task ReadFileErrorHandling()
        {
            string testPath = @"C:\Test\Path";
            var testFiles = new[]
            {
                    @"C:\Test\Path\good.mp3",
                    @"C:\Test\Path\bad.mp3"
                }.ToSeq();

            Runtime.GetFilesWithExtensionAsync = (path, ext, token) =>
                Task.FromResult(testFiles);
            
            Runtime.WithStream = (path, action) =>
            {
                if (path.Contains("bad.mp3"))
                {
                    throw new IOException("Error reading file");
                }
                action(null);
                return Task.CompletedTask;
            };

            Runtime.ReadSongInfo = (path, stream) =>
             new SongInfo(
                Id: 1,
                Name: "Test Song",
                Path: path,
                Artist: new[] { "Test Artist" },
                Album: "Test Album",
                Rating: 5,
                ArtistStatus: SpotifyStatus.NotSearched,
                SongStatus: SpotifyStatus.NotSearched);           

            var result = await ScanFiles.ScanFilesAsync(testPath);

            Assert.IsTrue(result.IsRight, "Result should still be successful even with one file error");
            Assert.IsTrue(errorMessages.Any(m => m.Contains("Error reading metadata")),
                "Should log error for the bad file");

            result.Match(
                Right: songs =>
                {
                    Assert.AreEqual(1, songs.Count(), "Should return 1 song (the good one)");
                    return unit;
                },
                Left: error =>
                {
                    Assert.Fail($"Should not return error: {error.Message}");
                    return unit;
                }
            );
        }
    }
}