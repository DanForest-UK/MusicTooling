using LanguageExt;
using LanguageExt.Common;
using System;

namespace MusicTools.Core
{
    public static class Types
    {
        public record AppModel(SongInfo[] Songs, int MinimumRating);
        public record SongInfo(string Id, string Name, string Path, string[] Artist, string Album, int Rating);
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
