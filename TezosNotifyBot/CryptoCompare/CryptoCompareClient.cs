using Io.Gate.GateApi.Api;
using Io.Gate.GateApi.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
				Configuration config = new Configuration();
				config.BasePath = "https://api.gateio.ws/api/v4";
				var apiInstance = new SpotApi(config);
				var result = apiInstance.ListCandlesticks("XTZ_USDT", 1);
                md.price_usd = decimal.Parse(result[0][3], System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture);
				result = apiInstance.ListCandlesticks("BTC_USDT", 1);
				md.price_btc = decimal.Parse(result[0][3], System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture);
                md.price_btc = md.price_usd / md.price_btc;
				result = apiInstance.ListCandlesticks("CEUR_USDT", 1);
				md.price_eur = decimal.Parse(result[0][3], System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture);
				md.price_eur = md.price_usd / md.price_eur;

				md.Received = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                
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
