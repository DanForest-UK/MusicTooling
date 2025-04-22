using System;
using System.Collections.Generic;
using System.Text;

namespace MusicTools.Domain
{
    public enum StatusLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public record StatusMessage(string Text, StatusLevel Level, Guid Id, DateTime Timestamp)
    {
        public static StatusMessage Create(string text, StatusLevel level) =>
            new StatusMessage(text, level, Guid.NewGuid(), DateTime.Now);
    }
}
