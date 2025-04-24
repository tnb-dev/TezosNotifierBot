using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using TezosNotifyBot.Tezos;

namespace TezosNotifyBot.CryptoCompare
{
	internal class CryptoCompareClient : IMarketDataProvider
	{
        ILogger<CryptoCompareClient> logger;
        HttpClient http;

        string _cryptoCompareToken;
		public CryptoCompareClient(string apiKey, HttpClient http, ILogger<CryptoCompareClient> logger)
		{
            this.logger = logger;
            this.http = http;
            _cryptoCompareToken = apiKey;

        }

		static MarketData md = new MarketData();

		MarketData IMarketDataProvider.GetMarketData()
        {
			if (DateTime.UtcNow.Subtract(md.Received).TotalMinutes < 5)
                return md;

            try
            {
                string str =
                    Download(
                        $"https://min-api.cryptocompare.com/data/price?fsym=XTZ&tsyms=BTC,USD,EUR,ETH&api_key={_cryptoCompareToken}");
                var dto = JsonConvert.DeserializeObject<CryptoComparePrice>(str);
                md.price_eur = dto.EUR;
                md.price_usd = dto.USD;
                md.price_btc = dto.BTC;
                md.Received = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to call https://min-api.cryptocompare.com/data/price?fsym=XTZ&tsyms=BTC,USD,EUR,ETH");
            }
            return md;
		}
        string Download(string addr)
        {
            try
            {
                logger.LogDebug("download " + addr);
                // TODO: Make requests async
                var result = http.GetStringAsync(addr)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                logger.LogDebug("download complete: " + addr);
                return result;
            }
            catch (HttpRequestException we)
            {
                logger.LogError(we, "Error downloading from " + addr);
                throw;
            }
        }
    }
}
