using LanguageExt;
using LanguageExt.Common;
using System;
using System.Collections.Generic;
using static LanguageExt.Prelude; 
using System.Linq;

namespace MusicTools.Core
{
    public static class Types
    {
        // Records for application state
        // Todo might be more efficient for 'unchosen songs'
        public record AppModel(SongInfo[] Songs, Guid[] ChosenSongs, int MinimumRating)
        {
            public Seq<SongInfo> FilteredSongs()
            { 
                var chosenSongsHash = toHashSet(ChosenSongs);

                return (from s in Songs
                        where s.Rating >= MinimumRating &&
                              chosenSongsHash.Contains(s.Id)
                        select s).ToSeq();
            }

            public Seq<string> DistinctArtists() =>
                (from s in FilteredSongs()
                 from a in s.Artist
                 where a.HasValue()
                 select a).Distinct().ToSeq();
        }

        public record SongInfo(Guid Id, string Name, string Path, string[] Artist, string Album, int Rating);
        public record SpotifySettings(string ClientId, string ClientSecret);

        // Spotify domain models
        public record SpotifyTrack(
            string Id,
            string Name,
            SpotifyArtist[] Artists,
            SpotifyAlbum? Album,
            string Uri);

        public record SpotifyArtist(
            string Id,
            string Name,
            string Uri);

        public record SpotifyAlbum(
            string Id,
            string Name);
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

        public record SongNotFound(Guid TrackId, string Title, string[] Artists, string ErrorMessage)
            : SpotifyError("SONG_NOT_FOUND", $"Could not find song: {Title} by {string.Join(", ", Artists)}", TrackId.ToString());

        public record ArtistNotFound(string ArtistName, string ErrorMessage)
            : SpotifyError("ARTIST_NOT_FOUND", $"Could not find artist: {ArtistName}", ArtistName);

        public record AuthenticationError(string ErrorMessage)
            : SpotifyError("AUTH_ERROR", $"Authentication failed: {ErrorMessage}", "auth");

        public record RateLimitError(string Resource, int RetryAfterSeconds)
            : SpotifyError("RATE_LIMIT", $"Rate limit exceeded for {Resource}. Retry after {RetryAfterSeconds} seconds", Resource);

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
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}