using System;
using System.Linq;

namespace SwiftClient.Extensions
{
    internal static class StringExtensions
    {
        public static string Encode(this string stringToEncode)
        {
            var stringSplit = stringToEncode.Split('/');
            var stringEncoded = stringSplit.Select(x => Uri.EscapeDataString(x));
            return string.Join("/", stringEncoded);
            
        }
    }
}
