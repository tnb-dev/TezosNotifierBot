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
		public int? totalVotingPower { get; set; }
		public decimal? ballotsQuorum { get; set; }
		public int? supermajority { get; set; }
        public int? yayBallots { get; set; }
		public int? yayVotingPower { get; set; }
		public int? nayBallots { get; set; }
		public int? nayVotingPower { get; set; }
		public int? passBallots { get; set; }
		public int? passVotingPower { get; set; }
		public decimal? upvotesQuorum { get; set; }
        public int? proposalsCount { get; set; }
        public int? topUpvotes { get; set; }
        public int? topVotingPower { get; set; }
    }
}