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
        MarketData IMarketDataProvider.GetMarketData()
        {
            string str =
                Download(
                    $"https://min-api.cryptocompare.com/data/price?fsym=XTZ&tsyms=BTC,USD,EUR,ETH&api_key={_cryptoCompareToken}");
            var dto = JsonConvert.DeserializeObject<CryptoComparePrice>(str);
            return new MarketData {
                price_usd = dto.USD,
                price_btc = dto.BTC,
                price_eur = dto.EUR
            };
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
