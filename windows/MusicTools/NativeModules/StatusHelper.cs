using LanguageExt;
using System;
using System.Diagnostics;
using static MusicTools.Core.Types;
using static MusicTools.NativeModules.StatusModule;
using static LanguageExt.Prelude;

namespace MusicTools.NativeModules
{
    /// <summary>
    /// Helper methods for sending status updates from any class
    /// </summary>
    public static class StatusHelper
    {
        /// <summary>
        /// Sends an info status message
        /// </summary>
        public static Unit Info(string message) =>
            SendStatus(message, StatusLevel.Info);

        /// <summary>
        /// Sends a success status message
        /// </summary>
        public static Unit Success(string message) =>
            SendStatus(message, StatusLevel.Success);

        /// <summary>
        /// Sends a warning status message
        /// </summary>
        public static Unit Warning(string message) =>
            SendStatus(message, StatusLevel.Warning);

        /// <summary>
        /// Sends an error status message with optional exception details
        /// </summary>
        public static Unit Error(string message, Option<Exception> ex = default)
        {
            // Log to debug console
            Debug.WriteLine($"ERROR: {message}");

            // Log stack trace if we have an exception
            ex.IfSome(exception => Debug.WriteLine(exception.StackTrace));
       
            // Send to UI
            SendStatus(message, StatusLevel.Error);
            return unit;
        }

        /// <summary>
        /// Sends a status message with the specified level
        /// </summary>
        public static Unit SendStatus(string message, StatusLevel level)
        {
            AddStatus(message, level);
            return unit;
        }
    }
}