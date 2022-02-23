using System.Collections.Generic;

namespace SwiftClient.AspNetCore
{
    public class SwiftServiceOptions
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

        /// <summary>
        /// Default container to use for requests on objects
        /// </summary>
        public string DefaultContainer { get; set; }

        /// <summary>
        /// Set retries count for all proxy nodes
        /// </summary>
        public int RetryCount { get; set; } = 1;

        /// <summary>
        /// Disables disposing httpclient
        /// </summary>
        public bool NoHttpDispose { get; set; } = true;

        /// <summary>
        /// Set retries count per proxy node request
        /// </summary>
        public int RetryPerEndpointCount { get; set; } = 1;
    }
}
