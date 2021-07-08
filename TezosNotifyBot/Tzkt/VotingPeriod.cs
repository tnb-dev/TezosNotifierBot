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
        public int? totalBakers { get; set; }
        public int? totalRolls { get; set; }
        public double? ballotsQuorum { get; set; }
        public int? supermajority { get; set; }
        public int? yayBallots { get; set; }
        public int? yayRolls { get; set; }
        public int? nayBallots { get; set; }
        public int? nayRolls { get; set; }
        public int? passBallots { get; set; }
        public int? passRolls { get; set; }
        public double? upvotesQuorum { get; set; }
        public int? proposalsCount { get; set; }
        public int? topUpvotes { get; set; }
        public int? topRolls { get; set; }
    }
}
