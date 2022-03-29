using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class CycleStats
	{
        public int cycle { get; set; }
        public int level { get; set; }
        public DateTime timestamp { get; set; }
        public long totalSupply { get; set; }
        public long circulatingSupply { get; set; }
        public long totalBootstrapped { get; set; }
        public long totalCommitments { get; set; }
        public long totalActivated { get; set; }
        public long totalCreated { get; set; }
        public long totalBurned { get; set; }
        //public long totalVested { get; set; }
        public long totalFrozen { get; set; }
    }
}

