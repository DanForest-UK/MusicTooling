using System;

namespace MusicTools.Core
{
    public static class Types
    {
        public record SongInfo(string Id, string Name, string Path, string[] Artist, string Album);
    }  
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
