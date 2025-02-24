using LanguageExt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicTools
{
    public record SongInfo(string Id, string Name, string Path, string[] Artist, string Album);

    /// <summary>
    /// Implements the file abstraction from the taglib library so opening files as a stream
    /// through react native file access methods is possible
    /// </summary>
    public class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        private readonly Stream _stream;

        public StreamFileAbstraction(string name, Stream stream)
        {
            Name = name;
            _stream = stream;
        }

        public string Name { get; }

        public Stream ReadStream => _stream;

        public Stream WriteStream => throw new NotSupportedException("This file abstraction is read-only.");

        public void CloseStream(Stream stream)
        {
            stream?.Dispose();
        }
    }
}
