using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public interface ITzKtClient
	{
		Head GetHead();
		Cycle GetCycle(int cycleIndex);
		List<OperationPenalty> GetRevelationPenalties(int level);
		IEnumerable<Operation> GetAccountOperations(string address, string filter = "");
		Rewards GetDelegatorRewards(string address, int cycle);
		List<Right> GetRights(int level);
	}
}
