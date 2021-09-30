using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TezosNotifyBot.Tzkt
{
	public class TzKtClient : ITzKtClient
	{
		ILogger<TzKtClient> _logger;
		WebClient _client;


		private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};
		
		public TzKtClient(ILogger<TzKtClient> logger, string url)
		{
			_client = new WebClient { BaseAddress = url };
			_logger = logger;
		}

		Head ITzKtClient.GetHead()
		{
			string head = Download("v1/head");
			return JsonConvert.DeserializeObject<Head>(head);
		}

		static Dictionary<int, Cycle> cycles = new Dictionary<int, Cycle>();
		Cycle ITzKtClient.GetCycle(int cycleIndex)
		{
			if (!cycles.ContainsKey(cycleIndex))
				cycles[cycleIndex] = JsonConvert.DeserializeObject<Cycle>(Download($"v1/cycles/{cycleIndex}"));
			return cycles[cycleIndex];
		}
		CycleStats ITzKtClient.GetCycleStats(int cycleIndex)
		{
			var str = Download($"v1/statistics/cyclic?cycle={cycleIndex}");
			return JsonConvert.DeserializeObject<List<CycleStats>>(str)[0];
		}

		List<Cycle> ITzKtClient.GetCycles()
		{
			var str = Download($"v1/cycles");
			return JsonConvert.DeserializeObject<List<Cycle>>(str);
		}

		int ITzKtClient.GetTransactionsCount(int beginLevel, int endLevel)
		{
			var str = Download($"v1/operations/transactions/count?level.ge={beginLevel}&level.le={endLevel}");
			return int.Parse(str);
		}

		List<OperationPenalty> ITzKtClient.GetRevelationPenalties(int level)
		{
			var str = Download($"v1/operations/revelation_penalties?level={level}");
			return JsonConvert.DeserializeObject<List<OperationPenalty>>(str);
		}

		public DateTime? GetAccountLastActive(string address)
		{
			var operation = GetAccountOperations(address, $"limit=1&sort.desc=timestamp&sender.eq={address}").FirstOrDefault();
			return operation?.Timestamp;
		}

		public DateTime? GetAccountLastSeen(string address)
		{
			var operation = GetAccountOperations(address, "limit=1&sort.desc=timestamp").FirstOrDefault();
			return operation?.Timestamp;
		}
		
		Rewards ITzKtClient.GetDelegatorRewards(string address, int cycle)
		{
			var str = Download($"v1/rewards/delegators/{address}/{cycle}");
			return JsonConvert.DeserializeObject<Rewards>(str);
		}
		Rewards ITzKtClient.GetBakerRewards(string address, int cycle)
		{
			var str = Download($"v1/rewards/bakers/{address}/{cycle}");
			return JsonConvert.DeserializeObject<Rewards>(str);
		}
		List<Rewards> ITzKtClient.GetBakerFutureRewards(string address)
		{			
			var str = Download($"v1/rewards/bakers/{address}?limit=12");
			return JsonConvert.DeserializeObject<List<Rewards>>(str);
		}
		List<Right> ITzKtClient.GetRights(int level)
		{
			var str = Download($"v1/rights?level={level}");
			return JsonConvert.DeserializeObject<List<Right>>(str);
		}
		List<Right> ITzKtClient.GetRights(string baker, int cycle)
		{
			var str = Download($"v1/rights?cycle={cycle}&baker={baker}&limit=10000&select=type,level,timestamp,priority,slots");
			return JsonConvert.DeserializeObject<List<Right>>(str);
		}
		List<Endorsement> ITzKtClient.GetEndorsements(int level)
		{
			var str = Download($"v1/operations/endorsements?level={level}");
			return JsonConvert.DeserializeObject<List<Endorsement>>(str);
		}
		Block ITzKtClient.GetBlock(int level)
		{
			var str = Download($"v1/blocks/{level}?operations=true");
			return JsonConvert.DeserializeObject<Block>(str);
		}

		decimal ITzKtClient.GetBalance(string address, int level)
		{
			var str = Download($"v1/accounts/{address}/balance_history/{level}");
			return ulong.Parse(str) / 1000000M;
		}
		BigmapItem ITzKtClient.GetBigmapItem(string contract, string address)
		{
			var str = Download($"v1/contracts/{contract}/bigmaps/ledger/keys/{address}");
			try
			{
				return JsonConvert.DeserializeObject<BigmapItem>(str);
			}
			catch
			{
				return null;
			}
		}
		string Download(string addr)
		{
			try
			{
				_logger.LogDebug($"download {_client.BaseAddress}{addr}");
				var result = _client.DownloadString(addr);
				_logger.LogDebug($"download complete: {_client.BaseAddress}{addr}");
				return result;
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Error downloading from {_client.BaseAddress}{addr}");
				throw;
			}
		}
		
		public T Download<T>(string path)
		{
			var result = Download(path);
			try
			{
				return JsonConvert.DeserializeObject<T>(result);
			}
			catch (Exception e)
			{
				var type = typeof(T);
				_logger.LogError(e, "Failed to deserialize result of request {Path} to {Type}", path, type);
				return default;
			}
		}

		public IEnumerable<Operation> GetAccountOperations(string address, string filter = "")
		{
			return GetAccountOperations<Operation>(address, filter);
		}

		public IEnumerable<T> GetAccountOperations<T>(string address, string filter = "")
			where T : Operation
		{
			var response = Download($"/v1/accounts/{address}/operations?{filter}");

			return JsonConvert.DeserializeObject<T[]>(response, _jsonSerializerSettings);
		}

		List<Transaction> ITzKtClient.GetTransactions(string filter)
		{
			var str = Download($"v1/operations/transactions?{filter}");
			return JsonConvert.DeserializeObject<List<Transaction>>(str);
		}

		List<VotingPeriod> ITzKtClient.GetVotingPeriods()
		{
			var str = Download($"v1/voting/periods?sort.desc=id");
			return JsonConvert.DeserializeObject<List<VotingPeriod>>(str);
		}
		List<Proposal> ITzKtClient.GetProposals(int epoch)
		{
			var str = Download($"v1/voting/proposals?epoch={epoch}&sort.desc=rolls");
			return JsonConvert.DeserializeObject<List<Proposal>>(str);
		}
	}
}
