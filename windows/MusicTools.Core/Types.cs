using LanguageExt;
using LanguageExt.Common;
using System;
using System.Collections.Generic;
using static LanguageExt.Prelude;
using System.Linq;
using System.Collections.Concurrent;

namespace MusicTools.Core
{
    public static class Types
    {
        // Records for application state
        public record AppModel(ConcurrentDictionary<int, SongInfo> Songs, int[] ChosenSongs, int MinimumRating)
        {
            public Seq<SongInfo> FilteredSongs(bool includeAlreadyProcessed = false)
            {
                var chosenSongsHash = toHashSet(ChosenSongs);

                return (from s in Songs.Values
                        where s.Rating >= MinimumRating &&
                              chosenSongsHash.Contains(s.Id) &&
                              (includeAlreadyProcessed ||s.SongStatus == SpotifyStatus.NotSearched)
                        select s).ToSeq();
            }

            public Seq<string> DistinctArtists(bool includeAlreadyProcessed = false) =>
                (from s in FilteredSongs(includeAlreadyProcessed: true)
                 from a in s.Artist
                 where a.HasValue() &&
                      (includeAlreadyProcessed || s.ArtistStatus != SpotifyStatus.NotSearched)
                 select a).Distinct().ToSeq();
        }

        /// <summary>
        /// Defines if the item can be found on Spotify
        /// </summary>
        public enum SpotifyStatus
        {
            NotSearched = 0,
            Found = 1,
            NotFound = 2,
            Liked = 3
        }

        /// <summary>
        /// Status message severity levels
        /// </summary>
        public enum StatusLevel
        {
            Info,
            Success,
            Warning,
            Error
        }

        public record StatusMessage(string Text, StatusLevel Level, Guid Id, DateTime Timestamp)
        {
            public static StatusMessage Create(string text, StatusLevel level) =>
                new StatusMessage(text, level, Guid.NewGuid(), DateTime.Now);
        }

        /// <summary>
        /// New type for spotify artist ID - for compile time safety avoid clashes with artist name
        /// </summary>
        public class SpotifyArtistId : NewType<SpotifyArtistId, string>
        {
            public SpotifyArtistId(string value) : base(value) { }
        }

        /// <summary>
        /// New type for spotify track id, for compile time safety and avoid clashes with our song ID
        /// </summary>
        public class SpotifySongId : NewType<SpotifySongId, string>
        {
            public SpotifySongId(string value) : base(value) { }
        }

        /// <summary>
        /// Main type for a song
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Name"></param>
        /// <param name="Path"></param>
        /// <param name="Artist"></param>
        /// <param name="Album"></param>
        /// <param name="Rating"></param>
        /// <param name="ArtistStatus"></param>
        /// <param name="SongStatus"></param>
        public record SongInfo(int Id, string Name, string Path, string[] Artist, string Album, int Rating, SpotifyStatus ArtistStatus, SpotifyStatus SongStatus);

        public record SpotifySettings(string ClientId, string ClientSecret, int ApiWait);

        // Spotify domain models
        public record SpotifyTrack(
            SpotifySongId Id,
            string Name,
            SpotifyArtist[] Artists,
            string Uri);

        public record SpotifyArtist(
            SpotifyArtistId Id,
            string Name,
            string Uri);

        // todo add follow album functionality?
    }

    public static class SpotifyErrors
    {
        // Base record for all Spotify errors
        public record SpotifyError(string ErrorCode, string Message, string ResourceId);

        // No error, useful in functional code with Either types
        public static SpotifyError Empty => new SpotifyError("", "", "");

        // Specific error types 

        public record AlreadyAuthenticated()
            : SpotifyError("ALREADY_AUTHENTICATED", "Already authenticated", "authentication");

        public record SongNotFound(int TrackId, string Title, string[] Artists, string ErrorMessage)
            : SpotifyError("SONG_NOT_FOUND", $"Could not find song: {Title} by {string.Join(", ", Artists)}", TrackId.ToString());


        public record ArtistNotFound(string ArtistName, string ErrorMessage)
            : SpotifyError("ARTIST_NOT_FOUND", $"Could not find artist: {ArtistName}", ArtistName);

        public record AuthenticationError(string ErrorMessage)
            : SpotifyError("AUTH_ERROR", $"Authentication failed: {ErrorMessage}", "auth");

        public record RateLimitError(string Resource)
            : SpotifyError("RATE_LIMIT", $"Rate limit exceeded for {Resource}.", Resource);

        public record ApiError(string Resource, int StatusCode, string ErrorMessage)
            : SpotifyError("API_ERROR", $"API error ({StatusCode}): {ErrorMessage}", Resource);
    }

    public static class AppErrors
    {
        public const int DisplayErrorCode = 303;

        public static Error DispayError(string message) =>
            Error.New(DisplayErrorCode, message);

        public static readonly Error ThereWasAProblem =
            DispayError("There was a problem");

        public static readonly Error NeedFileSystemAccess =
            DispayError("File access needs to be granted for this app in Privacy & Security -> File system");

        public static Error CantAcessSong(string path) =>
            DispayError($"Can't access song: {path}");

        public static Error AccessToPathDenied(string path) =>
            DispayError($"Access to: {path} is denied");

        public static Error OperationCancelled(string operation) =>
            DispayError($"{operation} was cancelled by user");
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}