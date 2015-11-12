using System;
using System.Collections.Generic;

namespace SwiftClient.Cli
{
    public class ContainerViewModel
    {
        public List<ObjectViewModel> Objects { get; set; }

        public string Message { get; set; }
    }

    public class ObjectViewModel
    {
        public string hash { get; set; }
        public DateTime last_modified { get; set; }
        public long bytes { get; set; }
        public string name { get; set; }
        public string content_type { get; set; }
    }
}
