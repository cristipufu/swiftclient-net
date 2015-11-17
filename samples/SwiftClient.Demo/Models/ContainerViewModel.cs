using System;
using System.Collections.Generic;

namespace SwiftClient.Demo
{
    public class PageViewModel
    {
        public TreeViewModel Tree { get; set; }

        public string Message { get; set; }
    }

    public class TreeViewModel
    {
        public string Text { get; set; }

        public string ContainerId { get; set; }

        public string ObjectId { get; set; }

        public bool IsFile { get; set; }

        public bool HasNodes { get; set; }

        public List<TreeViewModel> Nodes { get; set; }
    }

}
