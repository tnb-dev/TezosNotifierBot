using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class Proposal
	{
        public string hash { get; set; }
        public Baker initiator { get; set; }
        public int firstPeriod { get; set; }
        public int lastPeriod { get; set; }
        public int epoch { get; set; }
        public int upvotes { get; set; }
        public int rolls { get; set; }
        public string status { get; set; }
        public ProposalMetadata metadata { get; set; }
        public int period { get; set; }
        public string DisplayLink => $"<a target='_blank' href='https://www.tezosagora.org/proposal/{hash}?utm_source=tezosnotifierbot'>{metadata?.alias ?? (hash.Substring(0, 7) + "…" + hash.Substring(hash.Length - 5))}</a>";
    }
    public class ProposalMetadata
    {
        public string agora { get; set; }
        public string alias { get; set; }
        public int invoice { get; set; }
    }
}
