using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
    public class Cycle
    {
        public int index { get; set; }
        public int snapshotIndex { get; set; }
        public int snapshotLevel { get; set; }
        public string randomSeed { get; set; }
        public int totalBakers { get; set; }
        //public int totalRolls { get; set; }
        public ulong selectedStake { get; set; }
        public ulong totalStaking { get; set; }
        public int totalDelegators { get; set; }
        public ulong totalDelegated { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public int firstLevel { get; set; }
        public int lastLevel { get; set; }
        public TimeSpan Length => endTime.Subtract(startTime);
    }
}
