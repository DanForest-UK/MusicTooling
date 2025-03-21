using LanguageExt.Common;
using LanguageExt;
using System;
using static LanguageExt.Prelude;
using Windows.Security.Authorization.AppCapabilityAccess;
using MusicTools.Core;
using static MusicTools.Core.Types;
using System.Threading.Tasks;
using G = System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;

namespace MusicTools.Logic
{
    public static class ScanFiles
    {
        /// <summary>
        /// Active cancellation token source for file scanning operations
        /// </summary>
        private static CancellationTokenSource scanCts;

        /// <summary>
        /// Cancels any ongoing file scanning operation
        /// </summary>
        public static void CancelScan() =>
            CancellationHelper.CancelOperation(scanCts, "File scanning");

        /// <summary>
        /// Checks if the scan operation has been cancelled
        /// </summary>
        /// <param name="token">The cancellation token to check</param>
        public static void CheckForCancel(CancellationToken token) =>
            CancellationHelper.CheckForCancel(token, "File scanning");

        /// <summary>
        /// Scans for music files and returns song information
        /// </summary>
        /// <param name="path">The folder path to scan for music files</param>
        public async static Task<Either<Error, Seq<SongInfo>>> ScanFilesAsync(string path)
        {
            // Create a new CancellationTokenSource for this operation
            scanCts = CancellationHelper.ResetCancellationToken(ref scanCts);
            var token = scanCts.Token;

            try
            {
                var list = new ConcurrentBag<SongInfo>();

                // Check for cancellation
                CheckForCancel(token);

                // Validate the path
                if (string.IsNullOrWhiteSpace(path))
                {
                    Runtime.Error("No folder path specified", None);
                    return AppErrors.DispayError("No folder path specified");
                }

                Runtime.Info($"Scanning folder: {path} for MP3 files...");
                var mp3Files = await Runtime.GetFilesWithExtensionAsync(path, ".mp3", token);
                var totalFiles = mp3Files.Count();

                // Check for cancellation after getting file list
                CheckForCancel(token);

                Runtime.Info($"Found {totalFiles} MP3 files. Reading ID3 tags...");

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = token
                };

                await Task.Run(() =>
                {
                    Parallel.ForEach(mp3Files, options, async mp3File =>
                    {
                        await AddFileInfo(mp3File, list, token);
                    });
                }, token);

                return list.ToSeq();
            }
            catch (TaskCanceledException)
            {
                Runtime.Info("File scanning was cancelled");
                return AppErrors.OperationCancelled("File scanning");
            }
            catch (UnauthorizedAccessException ex)
            {
                Runtime.Error("Access to path denied", Some(ex as Exception));
                return AppErrors.AccessToPathDenied(path);
            }
            catch (Exception ex)
            {
                Runtime.Error("Problem scanning files", ex);
                return AppErrors.ThereWasAProblem;
            }
            finally
            {
                // Keep the token source alive in case we need to cancel
                // It will be reset on the next scan
            }
        }

        /// <summary>
        /// Adds song metadata for a file to the collection
        /// </summary>
        static async Task AddFileInfo(string path, ConcurrentBag<SongInfo> list, CancellationToken token)
        {
            try
            {
                // Check for cancellation before processing file
                CheckForCancel(token);

                await Runtime.WithStream(path, async stream =>
                {
                    list.Add(Runtime.ReadSongInfo(path, stream));
                });
            }
            catch (TaskCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error reading metadata for {path}", ex);
            }
        }
    }
}