
using System.Collections.Generic;

namespace SwiftClient
{
    public class SwiftCredentials
    {
        /// <summary>
        /// List of proxy endpoints
        /// </summary>
        public List<string> Endpoints { get; set; }

        /// <summary>
        /// Format "<account>:<user>", eg: "system:root"
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Account user password
        /// </summary>
        public string Password { get; set; }
    }
}
