using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.AspNetCore
{
    public class SwiftService : Client
    {
        private readonly SwiftServiceOptions _options;

        public SwiftService(IOptions<SwiftServiceOptions> options,
            ISwiftAuthManager authManager,
            ISwiftLogger logger): base(authManager, logger)
        {
            _options = options.Value;
            SetRetryCount(_options.RetryCount);
            SetRetryPerEndpointCount(_options.RetryPerEndpointCount);
        }
    }
}
