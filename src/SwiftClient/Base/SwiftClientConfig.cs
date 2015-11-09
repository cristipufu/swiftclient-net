using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

using SwiftClient.Extensions;
using System.Net.Http;
using System.Threading.Tasks;

namespace SwiftClient
{
    public abstract partial class SwiftClientBase : ISwiftClient
    {

        /// <summary>
        /// Set credentials (username, password, list of proxy endpoints)
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public SwiftClientBase WithCredentials(SwiftCredentials credentials)
        {
            _credentials = credentials;

            return this;
        }

        /// <summary>
        /// Log authentication errors, reauthorization events and request errors
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public SwiftClientBase SetLogger(ISwiftLogger logger)
        {
            _logger = logger;

            return this;
        }

        /// <summary>
        /// Set retries count for all proxy nodes
        /// </summary>
        /// <param name="retryCount">Default value 1</param>
        /// <returns></returns>
        public SwiftClientBase SetRetryCount(int retryCount)
        {
            _manager.SetRetryCount(retryCount);

            return this;
        }

        /// <summary>
        /// Set retries count per proxy node request
        /// </summary>
        /// <param name="retryPerEndpointCount">Default value 1</param>
        /// <returns></returns>
        public SwiftClientBase SetRetryPerEndpointCount(int retryPerEndpointCount)
        {
            _manager.SetRetryPerEndpointCount(retryPerEndpointCount);

            return this;
        }

    }
}
