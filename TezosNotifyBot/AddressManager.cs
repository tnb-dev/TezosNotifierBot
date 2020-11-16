using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Tezos;

namespace TezosNotifyBot
{
	public class AddressManager
	{
		Dictionary<string, ContractInfo> cache = new Dictionary<string, ContractInfo>();
		Dictionary<string, DelegateInfo> dcache = new Dictionary<string, DelegateInfo>();
		HashSet<string> queue = new HashSet<string>();
		public string LastHash;

		public List<Tuple<string,long>> GetContractsBalances()
		{
			return cache.ToList().Select(o => new Tuple<string, long>(o.Key, o.Value.balance)).ToList();
		}

		public ContractInfo GetContract(Client client, string hash, string addr, bool enqueue = false)
		{
			if (cache.ContainsKey(addr))
				return cache[addr];

			var ci = client.GetContractInfo(hash, addr);
			ci.Hash = hash;
			if (!enqueue)
			{
				cache[addr] = ci;
			}
			else if (!queue.Contains(addr))
			{
				queue.Add(addr);
			}
			return ci ?? new ContractInfo();
		}

		public void LoadQueue(Client client, string hash)
		{
			foreach (var addr in queue.ToList())
			{
				GetContract(client, hash, addr);
				queue.Remove(addr);
			}
		}

		public void UpdateBalance(IEnumerable<BalanceUpdate> bu_list, string hash, Action<string, long> update)
		{
			if (bu_list == null)
				return;
			foreach (var bu in bu_list)
			{
				string addr = "";
				if (bu.kind == "contract")
					addr = bu.contract;
				if (cache.ContainsKey(addr) && cache[addr].Hash != hash)
				{
					cache[addr].balance += bu.change;
					update(addr, cache[addr].balance);
				}
			}
		}

		public DelegateInfo GetDelegate(Client client, string hash, string addr, bool forceUpdate = false, bool enqueue = false)
		{
			if (!forceUpdate && dcache.ContainsKey(addr) && DateTime.Now.Subtract(dcache[addr].Received).TotalHours < 2)
			{
				var ci1 = GetContract(client, hash, addr, enqueue);
				return dcache[addr];
			}

			var di = client.GetDelegateInfo(addr, hash);
			var ci = GetContract(client, hash, addr, enqueue);
			dcache[addr] = di;
			di.Hash = hash;
			return di ?? new DelegateInfo();
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

		public void UpdateDelegate(string addr, string @delegate)
		{
			if (cache.ContainsKey(addr))
			{
				cache[addr].@delegate = @delegate;
			}
		}

		public void Clear()
		{
			cache.Clear();
			dcache.Clear();
			LastHash = null;
		}

		internal void Remove(string addr)
		{
			if (cache.ContainsKey(addr))
				cache.Remove(addr);
			if (dcache.ContainsKey(addr))
				dcache.Remove(addr);
		}
	}
}
