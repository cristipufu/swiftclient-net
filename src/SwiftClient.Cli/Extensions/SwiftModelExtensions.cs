using Humanizer;

namespace SwiftClient.Cli
{
    public static class SwiftModelExtensions
    {
        public static string Size(this SwiftObjectModel model)
        {
            return model.Bytes.Bytes().Humanize("0.00");
        }

        public static string Size(this SwiftContainerModel model)
        {
            return model.Bytes.Bytes().Humanize("0.00");
        }
    }
}
