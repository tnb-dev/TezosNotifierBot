using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class Block
	{
		public int Cycle { get; set; }
		public int Level { get; set; }
        public string Hash { get; set; }
        public DateTime Timestamp { get; set; }
        public int Proto { get; set; }
        //public int Priority { get; set; }
        public int blockRound { get; set; }
        public int Validations { get; set; }
        public ulong Deposit { get; set; }
        public ulong Reward { get; set; }
        public ulong Fees { get; set; }
        public bool NonceRevealed { get; set; }
        public Baker producer { get; set; }
        public List<Endorsement> Endorsements { get; set; }
        public List<ProposalUpvote> Proposals { get; set; }
        public List<Ballot> Ballots { get; set; }
        //public List<object> activations { get; set; }
        //public List<object> doubleBaking { get; set; }
        //public List<object> doubleEndorsing { get; set; }
        //public List<object> nonceRevelations { get; set; }
        public List<Delegation> Delegations { get; set; }
        public List<Origination> Originations { get; set; }
        public List<Transaction> Transactions { get; set; }
        //public List<object> reveals { get; set; }
    }
}
