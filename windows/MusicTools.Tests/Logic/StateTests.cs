using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTools.Logic;
using MusicTools.Core;
using System;
using System.Linq;
using static MusicTools.Core.Types;
using System.Collections.Concurrent;
using LanguageExt;

namespace MusicTools.Tests
{
    [TestClass]
    public class ObservableStateTests
    {
        /// <summary>
        /// Helper method to create a test song with specified parameters
        /// </summary>
        private SongInfo CreateTestSong(
            int id,
            Option<string> name = default,
            Option<string> path = default,
            Option<string[]> artists = default,
            Option<string> album = default,
            int rating = 5,
            SpotifyStatus artistStatus = SpotifyStatus.NotSearched,
            SpotifyStatus songStatus = SpotifyStatus.NotSearched)
        {
            return new SongInfo(
                Id: id,
                Name: name.IfNone($"Song {id}"),
                Path: path.IfNone($"test/path{id}.mp3"),
                Artist: artists.IfNone(new[] { $"Artist {id}" }),
                Album: album.IfNone($"Album {id}"),
                Rating: rating,
                ArtistStatus: artistStatus,
                SongStatus: songStatus);
        }

        /// <summary>
        /// Resets the application state before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize() =>  
            ObservableState.Update(new AppModel(
                Songs: new Map<int, SongInfo>(),
                ChosenSongs: new int[0],
                MinimumRating: 0));        
              
        /// <summary>
        /// Verifies that FilteredSongs correctly applies rating filter and returns only songs above minimum rating
        /// </summary>
        [TestMethod]
        public void FilterMinimumRating()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1, name: "Low Rating Song", rating: 2));
            songs = songs.Add(2, CreateTestSong(id: 2, name: "High Rating Song", rating: 4));

            var model = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1, 2 },
                MinimumRating: 3);

            ObservableState.Update(model);

            var filteredSongs = model.FilteredSongs();

            Assert.AreEqual(1, filteredSongs.Count(), "Should only return songs with rating >= 3");
            Assert.AreEqual("High Rating Song", filteredSongs.First().Name, "Should return the high rating song");
        }

        /// <summary>
        /// Verifies that DistinctArtists returns a list of unique artist names from all songs
        /// </summary>
        [TestMethod]
        public void DistinctArtists()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1, artists: new[] { "Artist 1", "Artist 2" }));
            songs = songs.Add(2, CreateTestSong(id: 2, artists: new[] { "Artist 2", "Artist 3" }));

            var model = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1, 2 },
                MinimumRating: 0
            );

            ObservableState.Update(model);

            var artists = model.DistinctArtists();

            Assert.AreEqual(3, artists.Count(), "Should return 3 unique artists");
            Assert.IsTrue(artists.Contains("Artist 1"), "Should contain Artist 1");
            Assert.IsTrue(artists.Contains("Artist 2"), "Should contain Artist 2");
            Assert.IsTrue(artists.Contains("Artist 3"), "Should contain Artist 3");
        }

        /// <summary>
        /// Verifies that DistinctArtists with includeAlreadyProcessed=false correctly filters out already processed artists
        /// </summary>
        [TestMethod]
        public void DistinctArtistsNotProcessed()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1,
                artists: new[] { "Processed Artist" },
                artistStatus: SpotifyStatus.Found));

            songs = songs.Add(2, CreateTestSong(id: 2,
                artists: new[] { "Unprocessed Artist" }));

            var model = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1, 2 },
                MinimumRating: 0);

            ObservableState.Update(model);

            var artists = model.DistinctArtists(includeAlreadyProcessed: false);

            Assert.AreEqual(1, artists.Count(), "Should only return unprocessed artists");
            Assert.IsTrue(artists.Contains("Unprocessed Artist"), "Should contain the unprocessed artist");
            Assert.IsFalse(artists.Contains("Processed Artist"), "Should not contain the processed artist");
        }

        /// <summary>
        /// Verifies that FilteredSongs with includeAlreadyProcessed=false correctly filters out already processed songs
        /// </summary>
        [TestMethod]
        public void FilteredSongsNotProcessed()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1,
                name: "Processed Song",
                songStatus: SpotifyStatus.Found));

            songs = songs.Add(2, CreateTestSong(id: 2,
                name: "Unprocessed Song"));

            var model = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1, 2 },
                MinimumRating: 0);

            ObservableState.Update(model);

            var filteredSongs = model.FilteredSongs(includeAlreadyProcessed: false);

            Assert.AreEqual(1, filteredSongs.Count(), "Should only return unprocessed songs");
            Assert.AreEqual("Unprocessed Song", filteredSongs.First().Name, "Should return the unprocessed song");
        }
    }
}