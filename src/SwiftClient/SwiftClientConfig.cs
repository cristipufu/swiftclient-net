using System;

namespace SwiftClient
{
    public partial class Client : ISwiftClient, IDisposable
    {

        /// <summary>
        /// Set credentials (username, password, list of proxy endpoints)
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public Client WithCredentials(SwiftCredentials credentials)
        {
            if (_manager == null)
            {
                var authManager = new SwiftAuthManager(credentials);

                authManager.Authenticate = Authenticate;

                authManager.Credentials = credentials;

                _manager = new SwiftRetryManager(authManager);
            }

            return this;
        }

        /// <summary>
        /// Log authentication errors, reauthorization events and request errors
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public Client SetLogger(ISwiftLogger logger)
        {
            _logger = logger;
            _manager.SetLogger(logger);

            return this;
        }

        /// <summary>
        /// Set retries count for all proxy nodes
        /// </summary>
        /// <param name="retryCount">Default value 1</param>
        /// <returns></returns>
        public Client SetRetryCount(int retryCount)
        {
            _manager.SetRetryCount(retryCount);

            return this;
        }

        /// <summary>
        /// Set retries count per proxy node request
        /// </summary>
        /// <param name="retryPerEndpointCount">Default value 1</param>
        /// <returns></returns>
        public Client SetRetryPerEndpointCount(int retryPerEndpointCount)
        {
            _manager.SetRetryPerEndpointCount(retryPerEndpointCount);

            return this;
        }
    }
}
