using System.Collections.Generic;
using System.Linq;

namespace SwiftClient.Extensions
{
    public static class DictionaryExtensions
    {
        public static string ToQueryString(this Dictionary<string, string> dict)
        {
            var array = (from key in dict.Keys
                         select string.Format("{0}={1}", key, dict[key]))
                        .ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
