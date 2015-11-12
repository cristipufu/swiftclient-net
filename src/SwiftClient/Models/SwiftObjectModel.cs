using Newtonsoft.Json;
using System;

namespace SwiftClient
{
    public class SwiftObjectModel
    {
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "last_modified")]
        public DateTime LastModified { get; set; }

        [JsonProperty(PropertyName = "bytes")]
        public long Bytes { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Object { get; set; }

        [JsonProperty(PropertyName = "content_type")]
        public string ContentType { get; set; }
    }
}
