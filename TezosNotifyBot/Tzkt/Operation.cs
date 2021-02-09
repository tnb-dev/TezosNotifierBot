using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class OperationPenalty
    {
        public string type { get; set; }
        public int id { get; set; }
        public int level { get; set; }
        public DateTime timestamp { get; set; }
        public string block { get; set; }
        public Baker baker { get; set; }
        public int missedLevel { get; set; }
        public ulong lostReward { get; set; }
        public ulong lostFees { get; set; }
        public decimal TotalLost => (lostReward + lostFees) / 1000000M;
    }
}
