using System;
using System.Collections.Generic;
using System.Text;

namespace MusicTools.Domain
{
    public record SongRating(int Value) : IComparable<SongRating>
    {
        public int CompareTo(SongRating other)
        {
            if (other == null)
                return 1;

            return Value.CompareTo(other.Value);
        }

        // Add operator overloads for >= and other comparisons
        public static bool operator >=(SongRating left, SongRating right)
            => left.CompareTo(right) >= 0;

        public static bool operator <=(SongRating left, SongRating right)
            => left.CompareTo(right) <= 0;

        public static bool operator >(SongRating left, SongRating right)
            => left.CompareTo(right) > 0;

        public static bool operator <(SongRating left, SongRating right)
            => left.CompareTo(right) < 0;
    }
}
