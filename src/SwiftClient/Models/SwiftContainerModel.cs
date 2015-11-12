using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace SwiftClient
{
    public class SwiftContainerModel
    {
        [JsonProperty(PropertyName = "count")]
        public long Objects { get; set; }

        [JsonProperty(PropertyName = "bytes")]
        public long Bytes { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Container { get; set; }
    }
}
