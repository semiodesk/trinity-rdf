using System;

namespace Semiodesk.Trinity.Stores.Stardog
{
    public static class StringExtensions
    {
        #region  Between
        /// <summary>
        /// Extracts the instance'th string between start and end.
        /// If start or end is null/empty then instance is assumed to always be 1 regardless of the value.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="start">Start delimiter</param>
        /// <param name="end">End delimiter</param>
        /// <param name="instance">Instance of start/end to use.  </param>
        public static string Between(this string input, string start, string end, int instance = 1)
        {
            int dontCare;
            return input.Between(start, end, out dontCare, instance);
        }

        /// <summary>
        /// Extracts the instance'th string between start and end.
        /// If start or end is null/empty then instance is assumed to always be 1 regardless of the value.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="start">Start delimiter</param>
        /// <param name="end">End delimiter</param>
        /// <param name="indexAtEnd">Pointer into input where parsing ended.  This will be at the point AFTER end was found.</param>
        /// <param name="instance">Instance of start/end to use.  </param>
        public static string Between(this string input, string start, string end, out int indexAtEnd, int instance = 1)
        {
            indexAtEnd = -1;

            var working = input;

            if (string.IsNullOrEmpty(input)) return string.Empty;

            var instancesFound = 0;
            for (; ; )
            {
                var iOfStart = string.IsNullOrEmpty(start) ? 0 : working.IndexOf(start, StringComparison.Ordinal);
                if (iOfStart == -1) return string.Empty;
                var from = iOfStart + (start?.Length ?? 0);

                var iOfEnd = string.IsNullOrEmpty(end) ? working.Length : working.IndexOf(end, from, StringComparison.Ordinal);
                if (iOfEnd == -1) return string.Empty;
                var to = iOfEnd;

                var result = working.Substring(from, to - from);
                instancesFound++;
                if (instancesFound == instance)
                {
                    indexAtEnd = from + result.Length + (end?.Length ?? 0);
                    return result;
                }
                working = working.Substring(to + (end?.Length ?? 0));
            }
        }
        #endregion
        #region BetweenSingleQuotes
        /// <summary>
        /// Returns the string between the first set of single quotes.  Supports escaped single quotes but only \\'
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="indexAtEnd">Pointer into input where parsing ended.  This will be at the point AFTER end was found.</param>
        /// <returns></returns>
        public static string BetweenSingleQuotes(this string input, out int indexAtEnd)
        {
            var c1 = (char)255;
            var dQ1 = $"{c1}{c1}";

            var working = input.Replace("\\'", dQ1);

            var found = working.Between("'", "'", out indexAtEnd);
            found = found.Replace(dQ1, "\\'");
            return found;
        }
        #endregion
    }
}
