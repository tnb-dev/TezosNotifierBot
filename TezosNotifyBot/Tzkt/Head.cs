using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class Head
	{
        public int level { get; set; }
        public string hash { get; set; }
        public string protocol { get; set; }
        public DateTime timestamp { get; set; }
        public int votingEpoch { get; set; }
        public int votingPeriod { get; set; }
        public int knownLevel { get; set; }
        public DateTime lastSync { get; set; }
        public bool synced { get; set; }
        public int quoteLevel { get; set; }
        public double quoteBtc { get; set; }
        public double quoteEur { get; set; }
        public double quoteUsd { get; set; }
    }
}
