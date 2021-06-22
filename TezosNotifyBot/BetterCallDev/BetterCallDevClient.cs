using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
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
			try
			{
				string account = Download($"v1/account/mainnet/{address}/token_balances");
				return JsonConvert.DeserializeObject<Account>(account);
			}
			catch
			{
				return new Account();
			}
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
					System.Threading.Thread.Sleep(3000);
					try
					{
						_logger.LogDebug($"retry download {_client.BaseAddress}{addr}");
						var result = _client.DownloadString(addr);
						_logger.LogDebug($"retry download complete: {_client.BaseAddress}{addr}");
						return result;
					}
					catch (Exception e1)
					{
						_logger.LogError(e1, $"Error retry downloading from {_client.BaseAddress}{addr}");

						throw;
					}
					throw;
				}
			}
		}

		IEnumerable<Token> IBetterCallDevClient.GetTokens(int minLevel)
		{
			return new List<Token>();
			/*var tokensStr = Download($"v1/tokens/mainnet?size=10");
			var tokens = JsonConvert.DeserializeObject<Tokens>(tokensStr);
			foreach(var t in tokens.tokens.Where(o => o.level > minLevel))
			{
				tokensStr = Download($"v1/contract/mainnet/{t.address}/tokens");
				var tokensList = JsonConvert.DeserializeObject<List<Token>>(tokensStr);
				foreach (var token in tokensList)
				{
					token.level = t.level;
					yield return token;
				}
			}*/
		}

		List<Operation> IBetterCallDevClient.GetOperations(string ophash)
		{
			var opsStr = Download($"v1/opg/{ophash}");
			return JsonConvert.DeserializeObject<List<Operation>>(opsStr) ?? new List<Operation>();
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
