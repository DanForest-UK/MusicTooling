using LanguageExt;
using System;
using System.Collections.Generic;
using System.Text;
using static LanguageExt.Prelude;

namespace MusicTools.Core
{
    public static class Extensions
    {
        public static Option<string> ValueOrNone(this string? value) =>
        value.HasValue()
            ? Some(value!)
            : None;

        public static bool HasValue(this string? value) =>
            !string.IsNullOrWhiteSpace(value);
    }
}
