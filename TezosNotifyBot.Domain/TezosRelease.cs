using System;

namespace TezosNotifyBot.Domain
{
#nullable disable
	public class TezosRelease
    {
        public string Tag { get; set; }

        public string Url { get; set; }
        
        public string Name { get; set; }

        public string Description { get; set; }
        
        public string AnnounceUrl { get; set; }
        
        public DateTime ReleasedAt { get; set; }
        
    }
}