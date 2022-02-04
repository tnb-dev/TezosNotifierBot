using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TezosNotifyBot.Model
{
	public class NftMetadata
	{
        public string name { get; set; }
        public string description { get; set; }
        public List<string> tags { get; set; }
        public string symbol { get; set; }
        public string artifactUri { get; set; }
        public string displayUri { get; set; }
        public string thumbnailUri { get; set; }
        public List<string> creators { get; set; }
        public List<Format> formats { get; set; }
        public int decimals { get; set; }
        public bool isBooleanAmount { get; set; }
        public bool shouldPreferSymbol { get; set; }
    }

    public class Format
    {
        public string uri { get; set; }
        public string mimeType { get; set; }
    }
}
