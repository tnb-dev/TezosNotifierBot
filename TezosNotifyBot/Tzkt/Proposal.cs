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
        public string Hash { get; set; }
        public ulong invoice { get; set; }
    }

    public class ProposalUpvote : Operation
	{        
        public Period Period { get; set; }
        public ProposalMetadata Proposal { get; set; }
        public Delegate @Delegate { get; set; }
        public int Rolls { get; set; }
        public bool Duplicated { get; set; }
    }

    public class Period
    {
        public int Index { get; set; }
        public int Epoch { get; set; }
        public string Kind { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int Id { get; set; }
        public int StartLevel { get; set; }
        public int EndLevel { get; set; }
    }
}
