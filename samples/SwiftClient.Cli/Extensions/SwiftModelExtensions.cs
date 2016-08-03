using Humanizer;

namespace SwiftClient.Cli
{
    public class SwiftObject : SwiftObjectModel
    {
        public string Size
        {
            get
            {
                return Bytes.Bytes().Humanize("0.00");
            }
        }
    }

    public class SwiftContainer : SwiftContainerModel
    {
        public string Size
        {
            get
            {
                return Bytes.Bytes().Humanize("0.00");
            }
        }
    }

    public class SwiftAccountStats : SwiftAccountResponse
    {
        public string Size
        {
            get
            {
                return TotalBytes.Bytes().Humanize("0.00");
            }
        }
    }
}
