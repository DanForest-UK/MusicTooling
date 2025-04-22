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
        // todo, do we need a concurrent dictionary still?


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




        // Spotify domain models
       

        

        // todo add follow album functionality?
    }

    public static class SpotifyErrors
    {
        
    }

   
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}