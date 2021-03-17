using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Linq.Expressions;
using Newtonsoft.Json.Serialization;
using TezosNotifyBot.Shared.Extensions;

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

		List<OperationPenalty> ITzKtClient.GetRevelationPenalties(int level)
		{
			var str = Download($"v1/operations/revelation_penalties?level={level}");
			return JsonConvert.DeserializeObject<List<OperationPenalty>>(str);
		}

		Rewards ITzKtClient.GetDelegatorRewards(string address, int cycle)
		{
			var str = Download($"v1/rewards/delegators/{address}?cycle={cycle}");
			return JsonConvert.DeserializeObject<List<Rewards>>(str)?.FirstOrDefault();
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

		public IEnumerable<Operation> GetAccountOperations(string address, string filter = "")
		{
			var response = Download($"/v1/accounts/{address}/operations?{filter}");

			return JsonConvert.DeserializeObject<Operation[]>(response, _jsonSerializerSettings);
		}
	}
}
