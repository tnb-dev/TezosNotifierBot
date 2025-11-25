using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
    public class VotingPeriod
	{
        public int index { get; set; }
        public int epoch { get; set; }
        public int firstLevel { get; set; }
        public DateTime startTime { get; set; }
        public int lastLevel { get; set; }
        public DateTime endTime { get; set; }
        public string kind { get; set; }
        public string status { get; set; }
		public string dictator { get; set; }
		public int? totalBakers { get; set; }
		public long? totalVotingPower { get; set; }
		public decimal? ballotsQuorum { get; set; }
		public long? supermajority { get; set; }
        public long? yayBallots { get; set; }
		public long? yayVotingPower { get; set; }
		public long? nayBallots { get; set; }
		public long? nayVotingPower { get; set; }
		public long? passBallots { get; set; }
		public long? passVotingPower { get; set; }
		public decimal? upvotesQuorum { get; set; }
        public long? proposalsCount { get; set; }
        public long? topUpvotes { get; set; }
        public long? topVotingPower { get; set; }
    }
}