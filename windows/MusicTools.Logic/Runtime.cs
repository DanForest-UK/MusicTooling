﻿using LanguageExt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using MusicTools.Domain;

namespace MusicTools.Logic
{
    /// <summary>
    /// Dependency injection delegates
    /// </summary>
    public static class Runtime
    {
        public static Func<string, string, CancellationToken, Task<Seq<string>>> GetFilesWithExtensionAsync = (_, _1, _2) => throw new NotImplementedException();
        public static Func<string, Stream, SongInfo> ReadSongInfo = (_, _) => throw new NotImplementedException();
        public static Func<string, Func<Stream, Task>, Task> WithStream = (_, _) => throw new NotImplementedException();
        public static Func<string, string, string, ISpotifyApi> GetSpotifyAPI = (_, _, _) => throw new NotImplementedException();

        // Status message delegates
        public static Func<string, Unit> Info = _ => throw new NotImplementedException();
        public static Func<string, Unit> Warning = _ => throw new NotImplementedException();
        public static Func<string, Option<Exception>, Unit> Error = (_, __) => throw new NotImplementedException();
        public static Func<string, StatusLevel, Unit> Status = (_, __) => throw new NotImplementedException();
    }
}