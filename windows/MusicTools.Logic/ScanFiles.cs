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

namespace MusicTools.Logic
{
    public static class ScanFiles
    {
        /// <summary>
        /// Scans for music files and returns song information
        /// </summary>
        public async static Task<Either<Error, Seq<SongInfo>>> ScanFilesAsync()
        {
            //     var path = @"C:\Dan\Dropbox\[Music]\[Folk]";

                 var path = @"C:\Dan\Dropbox\[Music]\[Folk]\[Acapella]\Coope, Boyes & Simpson\Falling Slowley";
          //  var path = @"C:\Dan\Dropbox\[Music]";

            try
            {
                var list = new G.List<SongInfo>();
                var mp3Files = await Runtime.GetFilesWithExtensionAsync(path, ".mp3");
                Debug.WriteLine($"GetFilesWthExtension: {mp3Files.Length} found");
                foreach (var mp3File in mp3Files)
                {
                    await AddFileInfo(mp3File, list);
                }
                return list.ToSeq();
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
        }

        /// <summary>
        /// Adds song metadata for a file to the collection
        /// </summary>
        static async Task AddFileInfo(string path, List<SongInfo> list)
        {
            try
            {
                await Runtime.WithStream(path, async stream =>
                {
                    list.Add(Runtime.ReadSongInfo(path, stream));
                });
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error reading metadata for {path}", ex);
            }
        }
    }
}