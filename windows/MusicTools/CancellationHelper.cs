using System;
using System.Threading;
using MusicTools.Core;
using LanguageExt;
using static LanguageExt.Prelude;
using System.Threading.Tasks;
using MusicTools.Logic;

namespace MusicTools
{
    /// <summary>
    /// Helper class for managing cancellation tokens across the application
    /// </summary>
    public static class CancellationHelper
    {
        /// <summary>
        /// Checks if the operation has been cancelled and throws TaskCanceledException if so
        /// </summary>
        /// <param name="token">The cancellation token to check</param>
        /// <param name="operationName">Optional name of the operation for logging</param>
        public static void CheckForCancel(CancellationToken token, string operationName = null)
        {
            if (token.IsCancellationRequested)
            {
                if (operationName != null)
                    Runtime.Info($"{operationName} operation was cancelled by user");

                throw new TaskCanceledException("Operation was cancelled by user");
            }
        }

        /// <summary>
        /// Safely disposes a CancellationTokenSource and creates a new one
        /// </summary>
        /// <param name="cts">Reference to the current CancellationTokenSource</param>
        /// <returns>A new CancellationTokenSource</returns>
        public static CancellationTokenSource ResetCancellationToken(ref CancellationTokenSource cts)
        {
            try
            {
                cts?.Dispose();
            }
            catch (Exception ex)
            {
                Runtime.Warning($"Error disposing cancellation token: {ex.Message}");
            }

            cts = new CancellationTokenSource();
            return cts;
        }

        /// <summary>
        /// Safely cancels an operation using the provided CancellationTokenSource
        /// </summary>
        /// <param name="cts">The CancellationTokenSource to cancel</param>
        /// <param name="operationName">Optional operation name for logging</param>
        /// <returns>True if cancel was successful, false otherwise</returns>
        public static bool CancelOperation(CancellationTokenSource cts, string operationName = null)
        {
            try
            {
                if (cts != null && !cts.IsCancellationRequested)
                {
                    cts.Cancel();

                    if (operationName != null)
                        Runtime.Info($"{operationName} operation cancelled");

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error cancelling {operationName ?? "operation"}", ex);
                return false;
            }
        }
    }
}