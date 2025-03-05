using LanguageExt;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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

        /// <summary>
        /// Convert array to OrderedDictionary with indices as keys
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static OrderedDictionary ToOrderedDictionary<T, S>(this T[] array, Func<T,S> getId)
        {
            var result = new OrderedDictionary();
            array.Iter(item => result.Add(getId(item), item));
            return result;
        }
    }
}
