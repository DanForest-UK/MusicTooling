using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicTools
{
    public record SongInfo(string Id, string Name, string Path, string Artist, string Album);

    //public class SongInfo
    //{
    //    public readonly string Id;
    //    public readonly string Name;
    //    public readonly string Path;
    //    public readonly string Artist;
    //    public readonly string Album;

    //    public SongInfo(string id, string name, string path, string artist, string album)
    //    {
    //        Id = id;
    //        Name = name;
    //        Path = path;
    //        Artist = artist;
    //        Album = album;
    //    }
    //}
}
