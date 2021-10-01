using System;
using System.Collections.Generic;

namespace TezosNotifyBot.Tzkt
{
	public interface ITzKtClient
	{
		Head GetHead();
		Cycle GetCycle(int cycleIndex);
		CycleStats GetCycleStats(int cycleIndex);
		List<Cycle> GetCycles();
		int GetTransactionsCount(int beginLevel, int endLevel);
		List<OperationPenalty> GetRevelationPenalties(int level);
		IEnumerable<Operation> GetAccountOperations(string address, string filter = "");
		List<Transaction> GetTransactions(string filter);
		List<VotingPeriod> GetVotingPeriods();
		List<Proposal> GetProposals(int epoch);
		DateTime? GetAccountLastSeen(string address);
		DateTime? GetAccountLastActive(string address);
		Rewards GetDelegatorRewards(string address, int cycle);
		Rewards GetBakerRewards(string address, int cycle);
		List<Rewards> GetBakerFutureRewards(string address);
		List<Right> GetRights(int level);
		List<Right> GetRights(string baker, int cycle);
		List<Endorsement> GetEndorsements(int level);
		Block GetBlock(int level);
		BigmapItem GetBigmapItem(string contract, string address);
		T Download<T>(string path);
		decimal GetBalance(string address, int level);
		IEnumerable<T> GetAccountOperations<T>(string address, string filter = "")
			where T : Operation;
		Protocol GetCurrentProtocol();
	}
}
