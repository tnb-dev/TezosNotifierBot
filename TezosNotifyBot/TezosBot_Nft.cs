using Minio;
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

			//var minio = GetService<MinioClient>();
			//var nftBucketExists = await minio.BucketExistsAsync(minioBucket);
			//if (!nftBucketExists)
			//	await minio.MakeBucketAsync(minioBucket);

			string s_metadata = null;
			try
			{
				Logger.LogInformation($"Get {token.MetadataBigmap}-{token_id}.json");
				throw new Minio.Exceptions.ObjectNotFoundException("", "");
				//await minio.GetObjectAsync(minioBucket, $"{token.MetadataBigmap}-{token_id}.json", s => s_metadata = new StreamReader(s).ReadToEnd());
			}
			catch (Minio.Exceptions.ObjectNotFoundException)
			{
				var bmk = GetService<ITzKtClient>().GetBigmapItem(tr.Target.address, "token_metadata", token_id.ToString());
				var token_info = (string)(bmk.value as Newtonsoft.Json.Linq.JObject)["token_info"].First.First;
				var bytes = Enumerable.Range(0, token_info.Length).Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(token_info.Substring(x, 2), 16)).ToArray();
				token_info = Encoding.Default.GetString(bytes);
				s_metadata = await GetService<IpfsClient>().GetStringAsync(token_info);
				bytes = Encoding.Default.GetBytes(s_metadata);
				Logger.LogInformation($"Put {token.MetadataBigmap}-{token_id}.json");
				//await minio.PutObjectAsync("nft", @$"{token.MetadataBigmap}-{token_id}.json", new MemoryStream(bytes), bytes.Length);
			}

			var token_meta = JsonConvert.DeserializeObject<NftMetadata>(s_metadata);
			//byte[] token_data = null;
			//try
			//{
			//	Logger.LogInformation($"Get {token.MetadataBigmap}-{token_id}.data");
			//	await minio.GetObjectAsync(minioBucket, $"{token.MetadataBigmap}-{token_id}.data", s => s_metadata = new StreamReader(s).ReadToEnd());
			//}
			//catch (Minio.Exceptions.ObjectNotFoundException)
			//{
			//	token_data = await GetService<IpfsClient>().GetAsync(token_meta.artifactUri);
			//	Logger.LogInformation($"Put {token.MetadataBigmap}-{token_id}.data");
			//	await minio.PutObjectAsync("nft", @$"{token.MetadataBigmap}-{token_id}.data", new MemoryStream(token_data), token_data.Length);
			//}

			var toAddresses = repo.GetUserAddresses(to).Where(o => o.NotifyTransactions).ToList();
			foreach (var ua in toAddresses)
			{
				string result = $"🟩 Incoming NFT transfer to {ua.DisplayName()}\nArtifact name: {token_meta.name}";
				if (token_meta.formats.Any(o => o.mimeType.Contains("video")))
					await Bot.SendVideoAsync(ua.UserId, new Telegram.Bot.Types.InputFiles.InputOnlineFile(token_meta.displayUri ?? token_meta.artifactUri), caption: result);
				else
					await Bot.SendPhotoAsync(ua.UserId, new Telegram.Bot.Types.InputFiles.InputOnlineFile(token_meta.displayUri ?? token_meta.artifactUri), caption: result);
			}
				//Bot.send(chatId, text, ParseMode.Html, disableWebPagePreview: true).ConfigureAwait(true).GetAwaiter()
				//		.GetResult();
		}
	}
}
