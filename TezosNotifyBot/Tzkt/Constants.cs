using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
    public class Constants
    {
        public int rampUpCycles { get; set; }
        public int noRewardCycles { get; set; }
        public int preservedCycles { get; set; }
        public int blocksPerCycle { get; set; }
        public int blocksPerCommitment { get; set; }
        public int blocksPerSnapshot { get; set; }
        public int blocksPerVoting { get; set; }
        public int timeBetweenBlocks { get; set; }
        public int endorsersPerBlock { get; set; }
        public long hardOperationGasLimit { get; set; }
        public long hardOperationStorageLimit { get; set; }
        public long hardBlockGasLimit { get; set; }
        public long tokensPerRoll { get; set; }
        public long revelationReward { get; set; }
        public long blockDeposit { get; set; }
        public List<long> blockReward { get; set; }
        public long endorsementDeposit { get; set; }
        public List<long> endorsementReward { get; set; }
        public long originationSize { get; set; }
        public int byteCost { get; set; }
        public int proposalQuorum { get; set; }
        public int ballotQuorumMin { get; set; }
        public int ballotQuorumMax { get; set; }
        public long lbSubsidy { get; set; }
        public long lbSunsetLevel { get; set; }
        public long lbEscapeThreshold { get; set; }
    }
}
