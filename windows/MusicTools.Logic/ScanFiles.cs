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
        public async static Task<Either<Error, Seq<SongInfo>>> ScanFilesAsync()
        {
            string path = @"C:\Dan\Dropbox\Dropbox\[Music]\[Folk]\[Acapella]";

            try
            {
                var list = new G.List<SongInfo>();
                var mp3Files = await Runtime.GetFilesWithExtensionAsync(path, ".mp3");
                mp3Files.Iter(async file => await AddFileInfo(file, list));
                return list.ToSeq();
            }
            catch (UnauthorizedAccessException)
            {
                return AppErrors.AccessToPathDenied(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return AppErrors.ThereWasAProblem;
            }
        }

        static async Task AddFileInfo(string path, List<SongInfo> list)
        {
            try
            {
                await Runtime.WithStream(path, async stream =>
                    await Task.Run(() => list.Add(Runtime.ReadSongInfo(path, stream))));               
            }
            catch (Exception ex)
            {
                // todo we want to return a list of errors
                Debug.WriteLine($"Error reading metadata for {path}: {ex.Message}");
            }
        }

    }
}
