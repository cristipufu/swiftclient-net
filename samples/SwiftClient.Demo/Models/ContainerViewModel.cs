using System;
using System.Collections.Generic;

namespace SwiftClient.Demo
{
    public class PageViewModel
    {
        public List<TreeViewModel> Tree { get; set; }

        public string Message { get; set; }
    }

    public class TreeViewModel
    {
        public string text { get; set; }

        public string containerId { get; set; }

        public string objectId { get; set; }

        public List<TreeViewModel> nodes { get; set; }
    }

}
