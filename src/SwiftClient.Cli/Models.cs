using Humanizer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftClient.Cli
{
    public class ContainerInfoModel
    {
        [JsonProperty(PropertyName = "count")]
        public long Objects { get; set; }

        [JsonProperty(PropertyName = "bytes")]
        public long Bytes { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Container { get; set; }

        public string Size
        {
            get
            {
                return Bytes.Bytes().Humanize("0.00");
            }
        }
    }

    public class ContainerViewModel
    {
        public List<ObjectViewModel> Objects { get; set; }

        public string Message { get; set; }
    }

    public class ObjectViewModel
    {
        public string hash { get; set; }

        [JsonProperty(PropertyName = "last_modified")]
        public DateTime LastModified { get; set; }

        [JsonProperty(PropertyName = "bytes")]
        public long Bytes { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Object { get; set; }

        [JsonProperty(PropertyName = "content_type")]
        public string ContentType { get; set; }

        public string Size
        {
            get
            {
                return Bytes.Bytes().Humanize("0.00");
            }
        }
    }
}
