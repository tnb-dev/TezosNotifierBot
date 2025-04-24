using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Tezos;
using System.Net;
using Newtonsoft.Json;
using TezosNotifyBot.Tzkt;

namespace TezosNotifyBot
{
	public class AddressManager
	{
		ITzKtClient _tzKt;
		public AddressManager(ITzKtClient tzKt)
		{
			_tzKt = tzKt;
		}

		public ContractInfo GetContract(string addr)
		{
			var contract = _tzKt.GetAccount(addr);
			if (contract == null)
				return new ContractInfo();
			return new ContractInfo {
				balance = contract.balance - contract.frozenDeposit,
				@delegate = contract.Delegate?.Address
			};
		}

		public decimal GetBalance(string addr)
		{
			return GetContract(addr).balance / 1000000M;
		}

		public DelegateInfo GetDelegate(string addr)
		{
			var @delegate = _tzKt.GetAccount(addr);
			if (@delegate == null)
				return null;
			if (@delegate.type != "delegate")
				return null;
			var d = _tzKt.GetDelegators(addr);
			return new DelegateInfo {
				balance = @delegate.balance - @delegate.frozenDeposit,
				deactivated = !@delegate.active,
				staking_balance = @delegate.stakingBalance,
				bond = @delegate.balance,
				delegated_contracts = d.Select(d => d.address).ToList(),
				NumDelegators = @delegate.numDelegators
			};
		}
	}
}

namespace TzKt_
{
	public class DelegateReward
	{
		public int cycle { get; set; }
		public long stakingBalance { get; set; }
		public long delegatedBalance { get; set; }
		public int numDelegators { get; set; }
		public double expectedBlocks { get; set; }
		public double expectedEndorsements { get; set; }
		public int futureBlocks { get; set; }
		public long futureBlockRewards { get; set; }
		public long futureBlockDeposits { get; set; }
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
		public long blockDeposits { get; set; }
		public int futureEndorsements { get; set; }
		public long futureEndorsementRewards { get; set; }
		public long futureEndorsementDeposits { get; set; }
		public int endorsements { get; set; }
		public long endorsementRewards { get; set; }
		public int missedEndorsements { get; set; }
		public long missedEndorsementRewards { get; set; }
		public int uncoveredEndorsements { get; set; }
		public long uncoveredEndorsementRewards { get; set; }
		public long endorsementDeposits { get; set; }
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

		public long TotalRewards => ownBlockRewards +
			extraBlockRewards +
			missedOwnBlockRewards +
			missedExtraBlockRewards +
			endorsementRewards +
			uncoveredEndorsementRewards +
			ownBlockFees +
			extraBlockFees +
			doubleBakingRewards +
			doubleEndorsingRewards +
			revelationRewards;
	}

	

	public class Delegate
	{
		public string alias { get; set; }
		public string address { get; set; }
		public bool active { get; set; }
	}

	public class Creator
	{
		public string address { get; set; }
	}

	public class Manager
	{
		public string address { get; set; }
		public string publicKey { get; set; }
	}

	public class Account
	{
		public string type { get; set; }
		public string address { get; set; }
		public string publicKey { get; set; }
		public bool revealed { get; set; }
		public long balance { get; set; }
		public int counter { get; set; }
		public Delegate @delegate { get; set; }
		public int delegationLevel { get; set; }
		public DateTime delegationTime { get; set; }
		public int numContracts { get; set; }
		public int numActivations { get; set; }
		public int numDelegations { get; set; }
		public int numOriginations { get; set; }
		public int numTransactions { get; set; }
		public int numReveals { get; set; }
		public int numMigrations { get; set; }
		public int firstActivity { get; set; }
		public DateTime firstActivityTime { get; set; }
		public int lastActivity { get; set; }
		public DateTime lastActivityTime { get; set; }

		public bool active { get; set; }
		public long frozenDeposits { get; set; }
		public long frozenRewards { get; set; }
		public long frozenFees { get; set; }
		public int activationLevel { get; set; }
		public DateTime activationTime { get; set; }
		public long stakingBalance { get; set; }
		public int numDelegators { get; set; }
		public int numBlocks { get; set; }
		public int numEndorsements { get; set; }
		public int numBallots { get; set; }
		public int numProposals { get; set; }
		public int numDoubleBaking { get; set; }
		public int numDoubleEndorsing { get; set; }
		public int numNonceRevelations { get; set; }
		public int numRevelationPenalties { get; set; }

		public string kind { get; set; }
		public Creator creator { get; set; }
		public Manager manager { get; set; }
	}
}