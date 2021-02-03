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
		string _url;
		public BetterCallDevClient(ILogger<BetterCallDevClient> logger, string url)
		{
			_url = url;
			_logger = logger;
		}

		Account IBetterCallDevClient.GetAccount(string address)
		{
			string account = Download($"v1/account/mainnet/{address}");
			return JsonConvert.DeserializeObject<Account>(account);
		}

		string Download(string addr)
		{
			using (var _client = new WebClient { BaseAddress = _url })
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

		public IEnumerable<Token> GetTokens(int offset)
		{
			var tokensStr = Download($"v1/tokens/mainnet?size=100&offset={offset}");
			var tokens = JsonConvert.DeserializeObject<Tokens>(tokensStr);
			foreach(var t in tokens.tokens)
			{
				tokensStr = Download($"v1/contract/mainnet/{t.address}/tokens");
				foreach (var token in JsonConvert.DeserializeObject<List<Token>>(tokensStr))
					yield return token;
			}
		}

		public class TokensToken
		{
			public string network { get; set; }
			public int level { get; set; }
			public DateTime timestamp { get; set; }
			public DateTime last_action { get; set; }
			public string address { get; set; }
			public string manager { get; set; }
			public string alias { get; set; }
			public string type { get; set; }
			public int balance { get; set; }
			public int tx_count { get; set; }
		}

		public class Tokens
		{
			public int total { get; set; }
			public IList<TokensToken> tokens { get; set; }
		}
	}
}
