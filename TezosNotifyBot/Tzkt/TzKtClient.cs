using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;

namespace TezosNotifyBot.Tzkt
{
	public class TzKtClient : ITzKtClient
	{
		ILogger<TzKtClient> _logger;
		WebClient _client;
		public TzKtClient(ILogger<TzKtClient> logger, string url)
		{
			_client = new WebClient();
			_client.BaseAddress = url;
			_logger = logger;
		}

		Head ITzKtClient.GetHead()
		{
			string head = Download("v1/head");
			return JsonConvert.DeserializeObject<Head>(head);
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
