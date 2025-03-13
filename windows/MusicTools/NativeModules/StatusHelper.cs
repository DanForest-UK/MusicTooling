using LanguageExt;
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
        /// Sends an error status message
        /// </summary>
        public static Unit Error(string message) =>
            SendStatus(message, StatusLevel.Error);

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