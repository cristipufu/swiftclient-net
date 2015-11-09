using System;

namespace SwiftClient.Test
{
    public class ObjectInfoModel
    {
        public string hash { get; set; }
        public DateTime last_modified { get; set; }
        public long bytes { get; set; }
        public string name { get; set; }
        public string content_type { get; set; }
    }
}
