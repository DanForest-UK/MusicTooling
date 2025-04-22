using System;
using System.Collections.Generic;
using System.Text;

namespace MusicTools.Domain
{
    public record SongId(int Value) : IComparable<SongId>
    {
        public int CompareTo(SongId other)
        {
            if (other == null)
                return 1;

            return Value.CompareTo(other.Value);
        }
    }
}
