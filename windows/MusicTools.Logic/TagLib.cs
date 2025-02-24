using LanguageExt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static MusicTools.Core.Types;
using static MusicTools.Core.Extensions;
using System.Linq;

namespace MusicTools.Logic
{
    public static class ReadTag
    {
        public static SongInfo ReadSongInfo(string path, Stream stream)
        {
            // Use TagLib to read metadata
            var tagFile = TagLib.File.Create(new StreamFileAbstraction(Path.GetFileName(path), stream));
            return new SongInfo(
                Id: Guid.NewGuid().ToString(),
                Name: tagFile.Tag.Title.ValueOrNone().IfNone("[No title]"),
                Path: path,
                Artist: tagFile.Tag.AlbumArtists.Union(tagFile.Tag.Artists).ToArray(),
                Album: tagFile.Tag.Album.ValueOrNone().IfNone("[No album]"));
        }

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
}
