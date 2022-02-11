using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Ipfs;
using TezosNotifyBot.Tzkt;
using Microsoft.Extensions.Logging;
using TezosNotifyBot.Model;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;

namespace TezosNotifyBot
{
	partial class TezosBot
	{
		const string minioBucket = "nft";
		async Task HandleNftTransfer(Transaction tr, Token token)
		{
			var parms = (tr.Parameter.value as Newtonsoft.Json.Linq.JArray)[0];
			var from = (string)parms["from_"];
			var txs = (parms["txs"] as Newtonsoft.Json.Linq.JArray)[0];
			var to = (string)txs["to_"];
			var amount = (int)txs["amount"];
			var token_id = (int)txs["token_id"];

			Logger.LogDebug($"from_ {from} to {to} amount {amount} id {token_id}");
			var toAddresses = repo.GetUserAddresses(to).Where(o => o.NotifyTransactions).ToList();
			if (toAddresses.Count == 0)
				return;

			string s_metadata = null;

			var bmk = GetService<ITzKtClient>().GetBigmapItem(tr.Target.address, "token_metadata", token_id.ToString());
			var token_info = (string)(bmk.value as Newtonsoft.Json.Linq.JObject)["token_info"].First.First;
			var bytes = Enumerable.Range(0, token_info.Length).Where(x => x % 2 == 0)
						 .Select(x => Convert.ToByte(token_info.Substring(x, 2), 16)).ToArray();
			token_info = Encoding.Default.GetString(bytes);
			s_metadata = await GetService<IpfsClient>().GetStringAsync(token_info);
			bytes = Encoding.Default.GetBytes(s_metadata);
			Logger.LogInformation($"Put {token.MetadataBigmap}-{token_id}.json");

			var token_meta = JsonConvert.DeserializeObject<NftMetadata>(s_metadata);
			foreach (var ua in toAddresses)
			{
				string artifactUri = (token_meta.displayUri ?? token_meta.artifactUri).Replace("ipfs://", "https://ipfs.io/ipfs/");
				Logger.LogDebug("Incoming NFT transfer to {ua.DisplayName()}\nArtifact name: {token_meta.name}, " + artifactUri);
				string result = $"🟩 Incoming NFT transfer to {ua.DisplayName()}\nArtifact name: {token_meta.name}";
				System.Threading.Thread.Sleep(1000);
				try
				{
					if (token_meta.formats.Any(o => o.mimeType.Contains("video")))
						await Bot.SendVideoAsync(ua.UserId, new Telegram.Bot.Types.InputFiles.InputOnlineFile(artifactUri), caption: result);
					else
						await Bot.SendPhotoAsync(ua.UserId, new Telegram.Bot.Types.InputFiles.InputOnlineFile(artifactUri), caption: result);
				}
				catch(ApiRequestException e)
				{
					NotifyDev($"{e.Message}, Error send NFT notification: {artifactUri}", 0);
				}
			}
		}
	}
}
