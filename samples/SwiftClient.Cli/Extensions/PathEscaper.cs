using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SwiftClient.Cli
{
    public static class PathEscaper
    {
        static readonly string invalidChars = @"""\/?:<>*|";
        static readonly string escapeChar = "%";

        static readonly Regex escaper = new Regex(
            "[" + Regex.Escape(escapeChar + invalidChars) + "]",
            RegexOptions.Compiled);
        static readonly Regex unescaper = new Regex(
            Regex.Escape(escapeChar) + "([0-9A-Z]{4})",
            RegexOptions.Compiled);

        public static string Escape(string path)
        {
            return escaper.Replace(path,
                m => escapeChar + ((short)(m.Value[0])).ToString("X4"));
        }

        public static string Unescape(string path)
        {
            return unescaper.Replace(path,
                m => ((char)Convert.ToInt16(m.Groups[1].Value, 16)).ToString());
        }
    }
}
