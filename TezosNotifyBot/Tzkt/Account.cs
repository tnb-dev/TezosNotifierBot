using System;

namespace TezosNotifyBot.Tzkt
{
	public class Account
	{
		public string type { get; set; }
		public bool active { get; set; }
		public string alias { get; set; }
		public string address { get; set; }
		public string publicKey { get; set; }
		public bool revealed { get; set; }
		public long balance { get; set; }
		public ulong frozenDeposits { get; set; }
		public ulong frozenRewards { get; set; }
		public ulong frozenFees { get; set; }
		public int counter { get; set; }
		public int activationLevel { get; set; }
		public DateTime activationTime { get; set; }
		public object stakingBalance { get; set; }
		public int numContracts { get; set; }
		public int numDelegators { get; set; }
		public int numBlocks { get; set; }
		public int numEndorsements { get; set; }
		public int numBallots { get; set; }
		public int numProposals { get; set; }
		public int numActivations { get; set; }
		public int numDoubleBaking { get; set; }
		public int numDoubleEndorsing { get; set; }
		public int numNonceRevelations { get; set; }
		public int numRevelationPenalties { get; set; }
		public int numDelegations { get; set; }
		public int numOriginations { get; set; }
		public int numTransactions { get; set; }
		public int numReveals { get; set; }
		public int numMigrations { get; set; }
		public int firstActivity { get; set; }
		public DateTime firstActivityTime { get; set; }
		public int lastActivity { get; set; }
		public DateTime lastActivityTime { get; set; }
		public int? deactivationLevel { get; set; }
		public DateTime? deactivationTime { get; set; }
	}
}
