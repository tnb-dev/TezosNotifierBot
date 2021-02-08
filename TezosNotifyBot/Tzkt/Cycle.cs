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
        public int totalRolls { get; set; }
        public ulong totalStaking { get; set; }
        public int totalDelegators { get; set; }
        public ulong totalDelegated { get; set; }
    }
}
