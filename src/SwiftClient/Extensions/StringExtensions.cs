using System.Net;

namespace SwiftClient.Extensions
{
    internal static class StringExtensions
    {
        public static string Encode(this string string_to_encode)
        {
            return WebUtility.UrlEncode(string_to_encode);
        }
    }
}
