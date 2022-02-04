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
        private readonly ILogger<Node> _logger;

        public NodeClient(string url, HttpClient http, string cryptoCompareToken, ILogger<Node> logger)
        {
            _url = url;
            _http = http;
            _cryptoCompareToken = cryptoCompareToken;
            _logger = logger;
        }
        /*
        public ContractInfo GetContractInfo(string block, string addr)
        {
            ContractInfo ci;
            if (block == "head")
            {
                var json = Download(_url + $"/chains/main/blocks/{block}/context/contracts/{addr}");
                if (json.Contains("setable"))
                {
                    var a = JsonConvert.DeserializeObject<ContractInfo_alfa>(json);
                    ci = new ContractInfo
                    {
                        balance = a.balance,
                        manager = a.manager,
                        @delegate = a.@delegate.value
                    };
                }
                else
                    ci = JsonConvert.DeserializeObject<ContractInfo>(json);
            }
            else
            {
                var json = Download(_url + $"/chains/main/blocks/{block}/context/contracts/{addr}");

                if (json.Contains("setable"))
                {
                    var a = JsonConvert.DeserializeObject<ContractInfo_alfa>(json);
                    ci = new ContractInfo
                    {
                        balance = a.balance,
                        manager = a.manager,
                        @delegate = a.@delegate.value
                    };
                }
                else
                    ci = JsonConvert.DeserializeObject<ContractInfo>(json);
            }

            return ci;
        }

        public VoteListing[] GetVoteListings(string hash)
        {
            var data = Download($"{_url}/chains/main/blocks/{hash}/votes/listings");
            return JsonConvert.DeserializeObject<VoteListing[]>(data);
        }
        
		public BlockHeader GetBlockHeader(string hash)
		{
			var str = Download(_url + "/chains/main/blocks/" + hash + "/header");
			return JsonConvert.DeserializeObject<BlockHeader>(str);
		}
        
		public BlockHeader GetBlockHeader(int level)
		{
			var str = Download(_url + "/chains/main/blocks/head/header");
			var head = JsonConvert.DeserializeObject<BlockHeader>(str);
			if (head.level == level)
				return head;
			str = Download($"{_url}/chains/main/blocks/{head.hash}~{head.level - level}/header");
			return JsonConvert.DeserializeObject<BlockHeader>(str);
		}

		public DelegateInfo GetDelegateInfo(string addr, string hash = "head")
        {
            var str = Download(_url + "/chains/main/blocks/" + hash + "/context/delegates/" + addr);
            if (!str.Contains("staking_balance"))
                return null;
            return JsonConvert.DeserializeObject<DelegateInfo>(str);
        }

        public string GetCurrentProposal(string hash)
        {
            return JsonConvert.DeserializeObject<string>(Download(_url + "/chains/main/blocks/" + hash +
                                                                  "/votes/current_proposal"));
        }

        public Ballot[] GetBallotList(string hash)
        {
            return JsonConvert.DeserializeObject<Ballot[]>(Download(_url + "/chains/main/blocks/" + hash +
                                                                    "/votes/ballot_list"));
        }
        */

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
            try
            {
                _logger?.LogDebug("download " + addr);
                // TODO: Make requests async
                var result = _http.GetStringAsync(addr)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                _logger?.LogDebug("download complete: " + addr);
                return result;
            }
            catch (HttpRequestException we)
            {
                _logger?.LogError(we, "Error downloading from " + addr);
                //            var rs = we.Response?.GetResponseStream();
                //if (rs != null)
                //{
                //	var err = new StreamReader(rs).ReadToEnd();
                //	logger?.Error(we, "Error downloading from " + addr);
                //}
                //else
                throw;
            }
        }
        /*
        public void SetNodeUrl(string nodeUrl)
        {
            _url = nodeUrl;
        }
        */
    }
}