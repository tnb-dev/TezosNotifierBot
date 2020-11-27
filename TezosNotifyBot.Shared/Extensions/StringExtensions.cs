using System;
using System.Collections.Generic;
using System.Linq;

namespace TezosNotifyBot.Shared.Extensions
{
    public static class StringExtensions
    {
        public static string Join(this IEnumerable<char> source, string separator)
            => string.Join(separator, source);

        public static string Join(this IEnumerable<string> source, string separator)
            => string.Join(separator, source);

        public static string Format(this string source, params object[] values)
            => string.Format(source, values);

        public static string Replace(this string source, int index, char symbol)
            => source.Remove(index, 1).Insert(index, symbol.ToString());

        public static string Capitalize(this string source)
            => source.Replace(0, char.ToUpper(source[0]));

        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            if (false == string.IsNullOrEmpty(source))
                return source.IndexOf(value, comparisonType) != -1;

            return false;
        }

        public static bool HasValue(this string source)
            => string.IsNullOrEmpty(source) == false;


        public static string ToTitleCase(this string source) => source
            .Separate()
            .Select(Capitalize)
            .Join(" ");

        public static string ToKebabCase(this string source) => source
            .Separate()
            .Join("-");

        public static string ToSnakeCase(this string source) => source
            .Separate()
            .Join("_");

        public static string ToCamelCase(this string source) => source
            .Separate()
            .Select(Capitalize)
            .Join("");

        public static string ToLowerCamelCase(this string source) => source
            .Separate()
            .Select((word, i) => i == 0 ? word : word.Capitalize())
            .Join("");

        // TODO: #8 Replace with Span<T> realization and SplitType enum
        public static IEnumerable<string> Separate(this string source, char[] separators = null)
        {
            var parts = new List<List<char>>();
            var chunk = new List<char>();

            foreach (var symbol in source)
            {
                if (parts.Contains(chunk) == false)
                    parts.Add(chunk);

                var isChar = separators?.Contains(symbol) ??
                             char.IsSeparator(symbol) == false && char.IsPunctuation(symbol) == false;

                if (isChar)
                {
                    if (char.IsUpper(symbol) && chunk.Count > 1)
                    {
                        chunk = new List<char>();
                    }

                    chunk.Add(char.ToLower(symbol));
                }
                else if (chunk.Count > 0)
                {
                    chunk = new List<char>();
                }
            }

            return parts.Select(x => x.Join("")).ToArray();
        }

        public static string ShortAddr(this string addr)
        {
            return addr.Substring(0, 6) + "…" + addr.Substring(32);
        }
    }
}