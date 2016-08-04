using System.Collections.Generic;

namespace SwiftClient.AspNetCore.Demo
{
    public class PageViewModel
    {
        public TreeViewModel Tree { get; set; }

        public string Message { get; set; }
    }

    public class TreeViewModel
    {
        public string text { get; set; }

        public string containerId { get; set; }

        public string objectId { get; set; }

        public bool isExpandable { get; set; }

        public bool isFile { get; set; }

        public bool isVideo { get; set; }

        public bool hasNodes { get; set; }

        public List<TreeViewModel> nodes { get; set; }
    }

}
