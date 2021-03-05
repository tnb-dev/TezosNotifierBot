using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class Rewards
	{
        public int cycle { get; set; }
        public long balance { get; set; }
        public Baker baker { get; set; }
        public long stakingBalance { get; set; }
        public double expectedBlocks { get; set; }
        public double expectedEndorsements { get; set; }
        public int futureBlocks { get; set; }
        public long futureBlockRewards { get; set; }
        public int ownBlocks { get; set; }
        public long ownBlockRewards { get; set; }
        public int extraBlocks { get; set; }
        public long extraBlockRewards { get; set; }
        public int missedOwnBlocks { get; set; }
        public long missedOwnBlockRewards { get; set; }
        public int missedExtraBlocks { get; set; }
        public long missedExtraBlockRewards { get; set; }
        public int uncoveredOwnBlocks { get; set; }
        public long uncoveredOwnBlockRewards { get; set; }
        public int uncoveredExtraBlocks { get; set; }
        public long uncoveredExtraBlockRewards { get; set; }
        public int futureEndorsements { get; set; }
        public long futureEndorsementRewards { get; set; }
        public int endorsements { get; set; }
        public long endorsementRewards { get; set; }
        public int missedEndorsements { get; set; }
        public long missedEndorsementRewards { get; set; }
        public int uncoveredEndorsements { get; set; }
        public long uncoveredEndorsementRewards { get; set; }
        public long ownBlockFees { get; set; }
        public long extraBlockFees { get; set; }
        public long missedOwnBlockFees { get; set; }
        public long missedExtraBlockFees { get; set; }
        public long uncoveredOwnBlockFees { get; set; }
        public long uncoveredExtraBlockFees { get; set; }
        public long doubleBakingRewards { get; set; }
        public long doubleBakingLostDeposits { get; set; }
        public long doubleBakingLostRewards { get; set; }
        public long doubleBakingLostFees { get; set; }
        public long doubleEndorsingRewards { get; set; }
        public long doubleEndorsingLostDeposits { get; set; }
        public long doubleEndorsingLostRewards { get; set; }
        public long doubleEndorsingLostFees { get; set; }
        public long revelationRewards { get; set; }
        public long revelationLostRewards { get; set; }
        public long revelationLostFees { get; set; }
        public decimal TotalRewards => balance / 1000000M * stakingBalance * (ownBlockRewards + extraBlockRewards + endorsementRewards + ownBlockFees + extraBlockFees + doubleBakingRewards - doubleBakingLostRewards - doubleBakingLostFees + doubleEndorsingRewards - doubleEndorsingLostRewards - doubleEndorsingLostFees + revelationRewards - revelationLostRewards - revelationLostFees);
    }
}
