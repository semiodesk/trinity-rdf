namespace Semiodesk.Trinity.Query
{
    internal static class StringExtensions
    {
        public static string ToCamelCase(this string s)
        {
            if(string.IsNullOrEmpty(s))
            {
                return s;
            }

            string result = s.Substring(0, 1).ToLowerInvariant();

            if(s.Length > 1)
            {
                result += s.Substring(1);
            }

            return result;
        }
    }
}
