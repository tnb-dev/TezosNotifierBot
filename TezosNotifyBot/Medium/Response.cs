using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Medium
{
    public class Data
    {
        public string id { get; set; }
        public string title { get; set; }
        public string authorId { get; set; }
        public List<string> tags { get; set; }
        public string url { get; set; }
        public string canonicalUrl { get; set; }
        public string publishStatus { get; set; }
        public long publishedAt { get; set; }
        public string license { get; set; }
        public string licenseUrl { get; set; }
    }

    public class Response
    {
        public Data data { get; set; }
    }

}
