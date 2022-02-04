using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TezosNotifyBot.Ipfs
{
	public class IpfsClient
	{
		HttpClient client;
		ILogger<IpfsClient> logger;
		public IpfsClient(HttpClient httpClient, ILogger<IpfsClient> logger)
		{
			client = httpClient;
			this.logger = logger;
		}

		public async Task<byte[]> GetAsync(string uri)
		{
			logger.LogDebug("IPFS get " + uri);
			var thumbnail = await client.GetAsync(uri.Replace("ipfs://", "https://ipfs.io/ipfs/"));
			return await thumbnail.Content.ReadAsByteArrayAsync();
		}

		public async Task<string> GetStringAsync(string uri)
		{
			logger.LogDebug("IPFS get " + uri);
			return await client.GetStringAsync(uri.Replace("ipfs://", "https://ipfs.io/ipfs/"));
		}
	}
}
