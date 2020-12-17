using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Tezos;
using System.Net;
using Newtonsoft.Json;

namespace TezosNotifyBot
{
	public class AddressManager
	{
		string _tzKtUrl;
		public AddressManager(string tzKtUrl)
		{
			_tzKtUrl = tzKtUrl;
		}

		public ContractInfo GetContract(Client client, string hash, string addr, bool enqueue = false)
		{
			try
			{
				if (_tzKtUrl == null)
					return client.GetContractInfo(hash, addr);

				var str = client.Download(_tzKtUrl + "v1/accounts/" + addr);
				var contract = JsonConvert.DeserializeObject<TzKt.Account>(str);
				return new ContractInfo
				{
					balance = contract.balance - contract.frozenDeposits - contract.frozenRewards - contract.frozenFees,
					@delegate = contract.@delegate?.address,
					manager = contract.manager?.address,
					Hash = hash
				};
			}
			catch
			{
				var ci = client.GetContractInfo(hash, addr);
				ci.Hash = hash;

				return ci ?? new ContractInfo();
			}
		}

		public DelegateInfo GetDelegate(Client client, string hash, string addr, bool forceUpdate = false, bool enqueue = false)
		{
			try
			{
				if (_tzKtUrl == null)
					return client.GetDelegateInfo(addr, hash);
				var str = client.Download(_tzKtUrl + "v1/accounts/" + addr);
				var @delegate = JsonConvert.DeserializeObject<TzKt.Account>(str);
				var str_d = client.Download(_tzKtUrl + "v1/accounts/" + addr + "/delegators");
				return new DelegateInfo
				{
					balance = @delegate.balance - @delegate.frozenDeposits - @delegate.frozenRewards - @delegate.frozenFees,
					deactivated = !@delegate.active,
					staking_balance = @delegate.stakingBalance,
					bond = @delegate.balance,//@delegate.balance ,
					Hash = hash,
					delegated_contracts = JsonConvert.DeserializeObject<TzKt.Delegator[]>(str_d).Select(d => d.address).ToList()
				};
			}
			catch
			{
				var di = client.GetDelegateInfo(addr, hash);
				di.Hash = hash;
				return di ?? new DelegateInfo();
			}
		}

		Dictionary<(int, string), decimal?> avgPerf = new Dictionary<(int, string), decimal?>();
		public decimal? GetAvgPerformance(Model.Repository repo, string addr)
		{
			int cycle = new Level(repo.GetLastBlockLevel().Item1).Cycle;
			if (avgPerf.ContainsKey((cycle, addr)))
				return avgPerf[(cycle, addr)];
			decimal rew = 0;
			decimal max = 0;
			for (int i = 0; i < 10; i++)
			{
				rew += repo.GetRewards(addr, cycle - i, false);
				max += repo.GetRewards(addr, cycle - i, true);
			}
			if (max > 0)
				avgPerf[(cycle, addr)] = 100M * rew / max;
			else
				avgPerf[(cycle, addr)] = null;
			return avgPerf[(cycle, addr)];
		}

		internal decimal GetRewardsForCycle(Client client, string d, DelegateInfo di, int cycle)
		{
			try
			{
				var str = client.Download(_tzKtUrl + $"v1/rewards/bakers/{d}/{cycle}");
				if (str == "")
					return 0;
				var rew = JsonConvert.DeserializeObject<TzKt.DelegateReward>(str);
				return rew.TotalRewards;
			}
			catch (Exception e)
			{
				return di.frozen_balance_by_cycle.Where(o => o.cycle == cycle).Sum(o => o.rewards + o.fees);
			}
		}
	}
}

namespace TzKt
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

	public class Delegator
	{
		public string type { get; set; }
		public string address { get; set; }
		public long balance { get; set; }
		public int delegationLevel { get; set; }
		public DateTime delegationTime { get; set; }
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