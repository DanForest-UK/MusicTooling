using System;
using System.Collections.Generic;
using System.Text;

namespace MusicTools.Domain
{
    public record Artist : IComparable<Artist>, IEquatable<Artist>
    {
        public string Value { get; }

        public Artist(string value)
        {
            Value = value;
        }

        public int CompareTo(Artist other)
        {
            if (other == null)
                return 1;
            // Case insensitive comparison for ordering
            return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        // Override Equals for case-insensitive equality
        public virtual bool Equals(Artist other)
        {
            if (other == null)
                return false;
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        // Override GetHashCode to be consistent with Equals
        public override int GetHashCode()
        {
            return Value?.ToUpperInvariant().GetHashCode() ?? 0;
        }

        // Override ToString to maintain record functionality
        public override string ToString()
        {
            return Value;
        }
    }
}