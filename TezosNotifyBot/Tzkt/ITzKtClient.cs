using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace TezosNotifyBot.Tzkt
{
	public interface ITzKtClient
	{
		Head GetHead();
		Account GetAccount(string addr);
		List<Delegator> GetDelegators(string addr);
		Cycle GetCycle(int cycleIndex);
		CycleStats GetCycleStats(int cycleIndex);
		List<Cycle> GetCycles();
		int GetTransactionsCount(int beginLevel, int endLevel);
		List<OperationPenalty> GetRevelationPenalties(int level);
		IEnumerable<Operation> GetAccountOperations(string address, string filter = "");
		List<Transaction> GetTransactions(string filter);
		List<VotingPeriod> GetVotingPeriods();
		List<Proposal> GetProposals(int epoch);
		List<ProposalUpvote> GetUpvotes(int epoch);
		List<Ballot> GetBallots(int period);
		DateTime? GetAccountLastSeen(string address);
		DateTime? GetAccountLastActive(string address);
		Rewards GetDelegatorRewards(string address, int cycle);
		Rewards GetBakerRewards(string address, int cycle);
		List<Rewards> GetBakerFutureRewards(string address);
		List<Right> GetRights(int level, string status);
		List<Right> GetRights(string baker, int cycle);
		List<Endorsement> GetEndorsements(int level);
		Block GetBlock(int level);
		BigmapItem GetBigmapItem(string contract, string bigmap, string key);
		T Download<T>(string path);
		decimal GetBalance(string address, int level);
		IEnumerable<T> GetAccountOperations<T>(string address, string filter = "")
			where T : Operation;
		IEnumerable<T> GetAccountOperations<T>(string address, QueryString filter) where T : Operation 
			=> GetAccountOperations<T>(address, filter.ToString().TrimStart('?'));

		/// <inheritdoc cref="https://api.tzkt.io/#operation/Operations_GetOriginationsCount"/>
		IEnumerable<T> GetTransactions<T>(QueryString filter = default)
			where T : Operation;

		Protocol GetCurrentProtocol();
		List<Account> GetAccounts(string type, int offset);
	}
}
