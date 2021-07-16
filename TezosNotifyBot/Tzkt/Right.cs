using System;

namespace TezosNotifyBot.Tzkt
{
	public class Right
	{
        public string type { get; set; }
        public int cycle { get; set; }
        public int level { get; set; }
        public DateTime timestamp { get; set; }
        public int? slots { get; set; }
        public Baker baker { get; set; }
        public string status { get; set; }
        public int? priority { get; set; }
    }
}
