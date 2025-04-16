using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTools.Core;
using System;
using System.Linq;
using System.Collections.Concurrent;
using static MusicTools.Core.Types;
using static LanguageExt.Prelude;
using LanguageExt;

namespace MusicTools.Tests
{
    [TestClass]
    public class LogicTests
    {
        /// <summary>
        /// Creates a test song with specified parameters
        /// </summary>
        private SongInfo CreateTestSong(
            int id,
            string name = null,
            string path = null,
            string[] artists = null,
            string album = null,
            int rating = 5,
            SpotifyStatus artistStatus = SpotifyStatus.NotSearched,
            SpotifyStatus songStatus = SpotifyStatus.NotSearched)
        {
            return new SongInfo(
                Id: id,
                Name: name ?? $"Song {id}",
                Path: path ?? $"test/path{id}.mp3",
                Artist: artists ?? new[] { $"Artist {id}" },
                Album: album ?? $"Album {id}",
                Rating: rating,
                ArtistStatus: artistStatus,
                SongStatus: songStatus
            );
        }

        /// <summary>
        /// Tests that SetSongs correctly assigns sequential IDs to the songs regardless of their original IDs
        /// </summary>
        [TestMethod]
        public void SetSongsSequentialIds()
        {
            var initialState = new AppModel(
                Songs: new Map<int, SongInfo>(),
                ChosenSongs: new int[0],
                MinimumRating: 0);

            var songs = new[]
            {
                        CreateTestSong(id: 100, name: "Test Song 1"),
                        CreateTestSong(id: 200, name: "Test Song 2", rating: 4)
                    };

            var updatedState = initialState.SetSongs(songs.ToSeq());

            Assert.AreEqual(2, updatedState.Songs.Count, "Should have 2 songs in state");
            Assert.IsTrue(updatedState.Songs.ContainsKey(1), "Should have sequential ID 1");
            Assert.IsTrue(updatedState.Songs.ContainsKey(2), "Should have sequential ID 2");
            Assert.AreEqual("Test Song 1", updatedState.Songs[1].Name, "First song should have correct name");
            Assert.AreEqual("Test Song 2", updatedState.Songs[2].Name, "Second song should have correct name");
            Assert.AreEqual(2, updatedState.ChosenSongs.Length, "Should have all songs chosen");
        }

        /// <summary>
        /// Tests that UpdateSongsStatus correctly updates the status of specified songs
        /// </summary>
        [TestMethod]
        public void UpdateSongStatus()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1));
            songs = songs.Add(2, CreateTestSong(id: 2));

            var initialState = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1, 2 },
                MinimumRating: 0);

            var updates = new[] { (1, SpotifyStatus.Found) };

            var updatedState = initialState.UpdateSongsStatus(updates);

            Assert.AreEqual(SpotifyStatus.Found, updatedState.Songs[1].SongStatus, "Song 1 status should be updated to Found");
            Assert.AreEqual(SpotifyStatus.NotSearched, updatedState.Songs[2].SongStatus, "Song 2 status should remain NotSearched");
        }

        /// <summary>
        /// Tests that UpdateSongsStatus doesn't downgrade the status from Liked to a lower status
        /// </summary>
        [TestMethod]
        public void UpdateSongStatusNotDowngrade()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1, songStatus: SpotifyStatus.Liked));

            var initialState = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1 },
                MinimumRating: 0);

            var updates = new[] { (1, SpotifyStatus.Found) }; // Trying to set to "Found" (a downgrade from "Liked")

            var updatedState = initialState.UpdateSongsStatus(updates);

            Assert.AreEqual(SpotifyStatus.Liked, updatedState.Songs[1].SongStatus, "Song status should remain Liked and not downgrade to Found");
        }

        /// <summary>
        /// Tests that UpdateArtistsStatus correctly updates the status of artists in songs with matching artist names
        /// </summary>
        [TestMethod]
        public void UpdateArtistsStatus()
        {
            var songs = new Map<int, SongInfo>();
            songs =songs = songs.Add(1, CreateTestSong(id: 1, artists: new[] { "Artist 1", "Artist 2" }));
            songs = songs.Add(2, CreateTestSong(id: 2, artists: new[] { "Artist 3" }));

            var initialState = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1, 2 },
                MinimumRating: 0);

            var updates = new[] { ("Artist 1", SpotifyStatus.Found) };

            var updatedState = initialState.UpdateArtistsStatus(updates);

            Assert.AreEqual(SpotifyStatus.Found, updatedState.Songs[1].ArtistStatus, "Song 1 artist status should be updated to Found");
            Assert.AreEqual(SpotifyStatus.NotSearched, updatedState.Songs[2].ArtistStatus, "Song 2 artist status should remain NotSearched");
        }

        /// <summary>
        /// Tests that UpdateArtistsStatus doesn't downgrade artist status from Liked to a lower status
        /// </summary>
        [TestMethod]
        public void UpdateArtistNotDowngrade()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1, artistStatus: SpotifyStatus.Liked));

            var initialState = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1 },
                MinimumRating: 0);

            var updates = new[] { ("Artist 1", SpotifyStatus.Found) }; // Trying to set to "Found" (a downgrade from "Liked")

            var updatedState = initialState.UpdateArtistsStatus(updates);

            Assert.AreEqual(SpotifyStatus.Liked, updatedState.Songs[1].ArtistStatus, "Artist status should remain Liked and not downgrade to Found");
        }

        /// <summary>
        /// Tests that UpdateArtistsStatus performs case-insensitive matching when comparing artist names
        /// </summary>
        [TestMethod]
        public void UpdateArtistsCaseInsensitive()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1, artists: new[] { "Artist One" }));

            var initialState = new AppModel(
                Songs: songs,
                ChosenSongs: new[] { 1 },
                MinimumRating: 0);

            var updates = new[] { ("artist one", SpotifyStatus.Found) }; // All lowercase

            var updatedState = initialState.UpdateArtistsStatus(updates);

            Assert.AreEqual(SpotifyStatus.Found, updatedState.Songs[1].ArtistStatus, "Artist status should be updated despite case differences");
        }

        /// <summary>
        /// Tests that ToggleSongSelection adds a song to ChosenSongs when it's not already selected
        /// </summary>
        [TestMethod]
        public void ToggleSongSelection()
        {
            var songs = new Map<int, SongInfo>();
            songs = songs.Add(1, CreateTestSong(id: 1));

            var initialState = new AppModel(
                Songs: songs,
                ChosenSongs: new int[0], // No songs chosen initially
                MinimumRating: 0
            );

            var updatedState = initialState.ToggleSongSelection(1);

            Assert.AreEqual(1, updatedState.ChosenSongs.Length, "Should have one chosen song");
            Assert.IsTrue(updatedState.ChosenSongs.Contains(1), "Should contain the toggled song ID");

            updatedState = updatedState.ToggleSongSelection(1);

            Assert.AreEqual(0, updatedState.ChosenSongs.Length, "Should have no chosen songs after toggling");
        }      
    }
}