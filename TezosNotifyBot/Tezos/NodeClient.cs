using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TezosNotifyBot.Model;

namespace TezosNotifyBot.Tezos
{
    public class NodeClient
    {
        private string _url;
        private readonly HttpClient _http;
        private readonly string _cryptoCompareToken;

        public NodeClient(string url, HttpClient http, string cryptoCompareToken)
        {
            _url = url;
            _http = http;
            _cryptoCompareToken = cryptoCompareToken;
        }

        public MarketData GetMarketData()
        {
            string str =
                Download(
                    $"https://min-api.cryptocompare.com/data/price?fsym=XTZ&tsyms=BTC,USD,EUR,ETH&api_key={_cryptoCompareToken}");
            var dto = JsonConvert.DeserializeObject<CryptoComparePrice>(str);
            return new MarketData
            {
                price_usd = dto.USD,
                price_btc = dto.BTC,
                price_eur = dto.EUR
            };
        }

        public string Download(string addr)
        {
            return _http.GetStringAsync(addr).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}