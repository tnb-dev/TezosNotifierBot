using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TezosNotifyBot.BetterCallDev
{
	public class BetterCallDevClient : IBetterCallDevClient
	{
		ILogger<BetterCallDevClient> _logger;
		WebClient _client;
		public BetterCallDevClient(ILogger<BetterCallDevClient> logger, string url)
		{
			_client = new WebClient();
			_client.BaseAddress = url;
			_logger = logger;
		}

		Account IBetterCallDevClient.GetAccount(string address)
		{
			string account = Download($"v1/account/mainnet/{address}");
			return JsonConvert.DeserializeObject<Account>(account);
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
	}
}
