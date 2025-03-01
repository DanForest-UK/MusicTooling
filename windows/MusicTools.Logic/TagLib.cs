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
        /// <summary>
        /// Reads metadata from an audio file stream
        /// </summary>
        public static SongInfo ReadSongInfo(string path, Stream stream)
        {
            // Use TagLib to read metadata
            var tagFile = TagLib.File.Create(new StreamFileAbstraction(Path.GetFileName(path), stream));
            var id3v2Tag = tagFile.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
            var rating = 0;

            if (id3v2Tag != null)
            {
                var frameSet = id3v2Tag.GetFrames<TagLib.Id3v2.PopularimeterFrame>();

                if (frameSet.Any())
                {
                    var maxRating = frameSet.Max(f => f.Rating);

                    rating = maxRating switch
                    {
                        >= 255 => 5,  // 255 is always 5 stars
                        >= 192 => 4,  // 192-254 is 4 stars
                        >= 128 => 3,  // 128-191 is 3 stars
                        >= 64 => 2,  // 64-127 is 2 stars
                        >= 1 => 1,  // 1-63 is 1 star
                        _ => 0   // 0 means no rating
                    };
                }
            }
            else
                Console.WriteLine("No ID3v2 tag found.");

            return new SongInfo(
                Guid.NewGuid(),
                tagFile.Tag.Title.ValueOrNone().IfNone("[No title]"),
                path,
                tagFile.Tag.AlbumArtists.Union(tagFile.Tag.Artists).ToArray(),
                tagFile.Tag.Album.ValueOrNone().IfNone("[No album]"),
                rating);
        }

        /// <summary>
        /// Implements the file abstraction from the taglib library so opening files as a stream
        /// through react native file access methods is possible
        /// </summary>
        public class StreamFileAbstraction : TagLib.File.IFileAbstraction
        {
            readonly Stream _stream;

            public StreamFileAbstraction(string name, Stream stream)
            {
                Name = name;
                _stream = stream;
            }

            public string Name { get; }

            public Stream ReadStream => _stream;

            public Stream WriteStream => throw new NotSupportedException("This file abstraction is read-only.");

            public void CloseStream(Stream stream) =>
                stream?.Dispose();
        }
    }
}