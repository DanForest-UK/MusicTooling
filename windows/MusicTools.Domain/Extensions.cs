using LanguageExt;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using static LanguageExt.Prelude;

namespace MusicTools.Domain
{
    public static class Extensions
    {
        public static Option<string> ValueOrNone(this string? value) =>
        value.HasValue()
            ? Some(value!)
            : None;

     
        public static Option<T> ValueOrNone<S, T>(this IDictionary<S,T> dictionary, S key) =>
            dictionary.ContainsKey(key)
                ? Some(dictionary[key])
                : None;

        public static bool HasValue(this string? value) =>
            !string.IsNullOrWhiteSpace(value);

        public static T IfNoneThrow<T>(this Option<T> option, Option<string> message = default) =>
            option.Match(
                Some: v => v,
                None: () => throw new Exception(message.IfNone("Option was none")));

        /// <summary>
        /// Splits an array into batches of specified size
        /// </summary>
        public static IEnumerable<T[]> ToBatchArray<T>(this T[] source, int batchSize)
        {
            for (int i = 0; i < source.Length; i += batchSize)
            {
                yield return source.Skip(i).Take(batchSize).ToArray();
            }
        }
       
    }
}
