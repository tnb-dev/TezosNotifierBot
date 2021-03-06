﻿using System;
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
        private readonly ILogger<Node> _logger;

        public NodeClient(string url, HttpClient http, ILogger<Node> logger)
        {
            _url = url;
            _http = http;
            _logger = logger;
        }

        public DateTime Run(int lastLevel)
        {
            return Download("head", lastLevel);
        }

        public DateTime Download(string hash, int lastLevel)
        {
            int index = 0;
            do
            {
                var str = Download(_url + "/chains/main/blocks/" + hash + (index > 0 ? "~" + index.ToString() : "") +
                                   "/header");
                var bh = JsonConvert.DeserializeObject<BlockHeader>(str);
                if (bh == null)
                {
                    return DateTime.Now;
                }

                //TezosBot.WriteLine("Downloaded header: " + bh.level.ToString() + " (" + bh.timestamp.ToString() + ")");
                if (lastLevel == bh.level)
                    return bh.timestamp;

                if (bh.level > lastLevel + 1 && lastLevel > 0)
                {
                    index = bh.level - lastLevel - 1;
                    hash = bh.hash;
                    continue;
                }

                if (bh.level == lastLevel)
                {
                    if (index > 0)
                    {
                        index--;
                        continue;
                    }
                    else
                    {
                        return bh.timestamp;
                    }
                }

                if (bh.level < lastLevel)
                {
                    hash = "head";
                    index = 0;
                    continue;
                }

                // Скачать операции
                var ops = GetBlockOperations(bh.hash);
                if (ops == null)
                    return DateTime.Now;
                var ok = BlockReceived(bh, GetBlockMetadata(bh.hash), ops);
                if (!ok)
                    return DateTime.Now;

                if (index > 0)
                {
                    index--;
                    if (index == 0)
                        hash = "head";
                }

                lastLevel = bh.level;
            } while (true);
        }

        public delegate bool BlockRecieved(BlockHeader header, BlockMetadata blockMetadata, Operation[] operations);

        public event BlockRecieved BlockReceived;

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

        public Dictionary<string, int> GetProposals(string hash)
        {
            var data = Download($"{_url}/chains/main/blocks/{hash}/votes/proposals");
            var arr = JsonConvert.DeserializeObject<string[][]>(data);
            return arr.Select(o => new Tuple<string, int>(o[0], int.Parse(o[1])))
                .ToDictionary(o => o.Item1, o => o.Item2);
        }

        public Operation[] GetBlockOperations(string hash)
        {
            var opstr = Download(_url + "/chains/main/blocks/" + hash + "/operations");
            if (opstr == null)
                return null;
            var arr = JsonConvert.DeserializeObject<Operation[][]>(opstr);
            return arr[0].Union(arr[1]).Union(arr[2]).Union(arr[3]).ToArray();
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

        public BlockMetadata GetBlockMetadata(string hash)
        {
            return JsonConvert.DeserializeObject<BlockMetadata>(Download(_url + "/chains/main/blocks/" + hash +
                                                                         "/metadata"));
        }

        public Ballots GetBallots(string hash)
        {
            return JsonConvert.DeserializeObject<Ballots>(Download(_url + "/chains/main/blocks/" + hash +
                                                                   "/votes/ballots"));
        }

        public int GetQuorum(string hash)
        {
            return JsonConvert.DeserializeObject<int>(Download(_url + "/chains/main/blocks/" + hash +
                                                               "/votes/current_quorum"));
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

        //public TzScanDelegates[] GetTzScanDelegatesList()
        //{
        //    string str = Download("http://tzscan.io/services.json");
        //    return JsonConvert.DeserializeObject<TzScanDelegates[]>(str);
        //}

        public MarketData GetMarketData()
        {
            string str =
                Download(
                    "https://min-api.cryptocompare.com/data/price?fsym=XTZ&tsyms=BTC,USD,EUR,ETH&api_key=378ecd1eb63001a82b202939e2c731e12b65b4854d308b580e9b5c448565a54f");
            var dto = JsonConvert.DeserializeObject<CryptoComparePrice>(str);
            return new MarketData
            {
                price_usd = dto.USD,
                price_btc = dto.BTC,
                price_eur = dto.EUR
            };
        }

       /* public BakingRights[] GetBakingRights(string hash)
        {
            string str = Download(_url + "/chains/main/blocks/" + hash + "/helpers/baking_rights");
            return JsonConvert.DeserializeObject<BakingRights[]>(str);
        }

        public BakingRights[] GetBakingRights(string hash, int cycle)
        {
            string str =
                Download($"{_url}/chains/main/blocks/{hash}/helpers/baking_rights?cycle={cycle}&max_priority=1");
            return JsonConvert.DeserializeObject<BakingRights[]>(str);
        }

        public EndorsingRights[] GetEndorsingRights(string hash)
        {
            string str = Download(_url + "/chains/main/blocks/" + hash + "/helpers/endorsing_rights");
            return JsonConvert.DeserializeObject<EndorsingRights[]>(str);
        }

        public EndorsingRights[] GetEndorsingRights(string hash, int cycle)
        {
            string str = Download($"{_url}/chains/main/blocks/{hash}/helpers/endorsing_rights?cycle={cycle}");
            return JsonConvert.DeserializeObject<EndorsingRights[]>(str);
        }*/

        public string[] GetDelegates(string hash)
        {
            string str = Download(_url + "/chains/main/blocks/" + hash + "/context/delegates?active");
            return JsonConvert.DeserializeObject<string[]>(str);
        }

        public decimal GetDelegateStake(string hash, string addr)
        {
            string str = Download(_url + "/chains/main/blocks/" + hash + "/context/delegates/" + addr +
                                  "/staking_balance");
            return JsonConvert.DeserializeObject<decimal>(str);
        }

        public Constants GetConstants(string hash)
        {
            string str = Download(_url + "/chains/main/blocks/" + hash + "/context/constants");
            return JsonConvert.DeserializeObject<Constants>(str);
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

            // catch (Exception e1)
            // {
            //     _logger?.LogError(e1, "Error downloading from " + addr);
            //     _logger?.LogDebug("Sleep 10 s");
            //     System.Threading.Thread.Sleep(10000);
            //     try
            //     {
            //         _logger?.LogDebug("download " + addr);
            //         lock (wc)
            //         {
            //             return wc.DownloadString(addr);
            //         }
            //     }
            //     catch (Exception e)
            //     {
            //         throw new Exception("Error downloading from " + addr, e);
            //     }
            // }
        }

        public void SetNodeUrl(string nodeUrl)
        {
            _url = nodeUrl;
        }

        //   public string Inject(string operationHex)
        //   {
        //       try
        //       {
        //           lock (wc)
        //           {
        //               wc.Headers[HttpRequestHeader.ContentType] = "application/json";
        //logger.Verbose("upload " + addr);
        //return wc.UploadString(url + "/injection/operation", JsonConvert.SerializeObject(operationHex));
        //           }
        //       }
        //       catch (System.Net.WebException we)
        //       {
        //           if (we.Response == null || we.Response.ContentLength <= 0)
        //               return we.Message;
        //           else
        //               return new System.IO.StreamReader(we.Response.GetResponseStream()).ReadToEnd();
        //       }
        //       catch (Exception e)
        //       {
        //           return e.Message;
        //       }
        //   }

        //public Dictionary<string, MyTezosBaker.Baker> GetBakers()
        //{
        //	string str = Download("https://api.mytezosbaker.com/v1/bakers/");
        //	return JsonConvert.DeserializeObject<MyTezosBaker.RootObject>(str).bakers.ToDictionary(o => o.delegation_code, o => o);
        //}
    }
}