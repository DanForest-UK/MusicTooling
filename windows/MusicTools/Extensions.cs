using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace MusicTools
{
    public static class Extensions
    {
        public static Option<string> ValueOrNone(this string value) =>
            value.HasValue()
                ? Some(value)
                : None;

        public static bool HasValue(this string value) => !string.IsNullOrWhiteSpace(value);
    }
}
