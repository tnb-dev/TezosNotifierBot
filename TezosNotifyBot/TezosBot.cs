using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.BetterCallDev;
using TezosNotifyBot.Dialog;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Events;
using TezosNotifyBot.Model;
//using TezosNotifyBot.Nodes;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Tezos;
using TezosNotifyBot.Tzkt;
using Account = TezosNotifyBot.Tzkt.Account;
using File = System.IO.File;
using Message = Telegram.Bot.Types.Message;
using Operation = TezosNotifyBot.Tezos.Operation;
using Token = TezosNotifyBot.Domain.Token;
using User = TezosNotifyBot.Domain.User;

namespace TezosNotifyBot
{
    public partial class TezosBot
    {
        private readonly IServiceProvider _serviceProvider;
        private BotConfig Config { get; set; }
        private ILogger<TezosBot> Logger { get; }
        
        /// <summary>
        /// Try to use and extend `botClient`
        /// </summary>
        /// <see cref="botClient"/>
        TelegramBotClient Bot;
        
        MarketData md = new MarketData();

        public MarketData MarketData => md;

        DateTime mdReceived;

        //DateTime bakersReceived;
        public static List<Command> Commands;
        //public static string LogsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        bool lastBlockChanged = false;

        //Dictionary<string, MyTezosBaker.Baker> bakers = new Dictionary<string, MyTezosBaker.Baker>();
        // List<Node> Nodes;
        // Node CurrentNode;
        int NetworkIssueMinutes = 2;
        //Worker worker;
        //RewardsManager rewardsManager;
        AddressManager addrMgr;
        private readonly ResourceManager resMgr;
		//string lastHash;
		Constants _currentConstants;

		DateTime lastReceived = DateTime.Now; //Дата и время получения последнего блока
        DateTime lastWebExceptionNotify = DateTime.MinValue;
        TwitterClient twitter;
        //private readonly NodeManager _nodeManager;
        private readonly CommandsManager commandsManager;
        private readonly TezosBotFacade botClient;
        string twitterAccountName;

        bool paused = false;
        string botUserName;
        Queue<DateTime> blockProcessings = new Queue<DateTime>();

        public TezosBot(IServiceProvider serviceProvider, ILogger<TezosBot> logger, IOptions<BotConfig> config,
            TelegramBotClient bot, ResourceManager resourceManager, TwitterClient twitterClient,
            /*NodeManager nodeManager, */CommandsManager commandsManager, TezosBotFacade botClient)
        {
            _serviceProvider = serviceProvider;
            Logger = logger;
            Config = config.Value;
            Bot = bot;
            twitter = twitterClient;
            //_nodeManager = nodeManager;
            this.commandsManager = commandsManager;
            this.botClient = botClient;

            addrMgr = _serviceProvider.GetRequiredService<AddressManager>();
            resMgr = resourceManager;
        }

        public async Task Run(CancellationToken cancelToken)
        {
            var version = GetType().Assembly.GetName().Version?.ToString(3);

            //worker = new Worker();
            //worker.OnError += Worker_OnError;
            try
            {
                Commands = JsonConvert.DeserializeObject<List<Command>>(
                    File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "commands.json")));
                // Nodes = JsonConvert.DeserializeObject<List<Node>>(
                //     File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nodes.json")));
                // CurrentNode = Nodes[0];
                                
                twitter.OnTwit += Twitter_OnTwit;
                twitter.OnTwitResponse += Twitter_OnTwitResponse;

                Logger.LogDebug("Connecting to Telegram Server...");

                await Bot.SetWebhookAsync("");

                Logger.LogDebug("Connected to Telegram Server.");
                Bot.OnCallbackQuery += OnCallbackQuery;
                Bot.OnUpdate += OnUpdate;
                Bot.StartReceiving();
                var me = await Bot.GetMeAsync();
                botUserName = me.Username;
                Logger.LogInformation("Старт обработки сообщений @" + me.Username);
                                
                var message = new StringBuilder();
                message.AppendLine($"{me.Username} v{version} started");
                message.AppendLine();
                message.AppendLine($"Using TzKt api: {(Config.TzKtUrl.Contains("localhost") ? "local" : Config.TzKtUrl)}");
                message.AppendLine($"Using BCD api: {(Config.BetterCallDevUrl.Contains("localhost") ? "local" : Config.BetterCallDevUrl)}");

                {
                    using var scope = _serviceProvider.CreateScope();
                    var provider = scope.ServiceProvider;
                    using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
                    NotifyDev(db, message.ToString(), 0);
                }
                var tzkt = _serviceProvider.GetRequiredService<ITzKtClient>();
                
                if (Config.Twitter.ConsumerKey != null)
                {
                    try
                    {
                        var settings = await twitter.GetSettings();
                        twitterAccountName = (string) JObject.Parse(settings)["screen_name"];
                        if (Config.Twitter.ChatId != 0)
                            await Bot.SendTextMessageAsync(Config.Twitter.ChatId,
                                $"🚀 Bot started using twitter account: https://twitter.com/{twitterAccountName}");
                    }
                    catch (Exception)
                    {
                        // skip exception
                    }
                }
                                
                //DateTime lastBlock;
                DateTime lastWarn = DateTime.Now;
                do
                {
                    using var scope = _serviceProvider.CreateScope();
                    var provider = scope.ServiceProvider;
                    using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
                    if (paused)
					{
                        Thread.Sleep(5000);
                        continue;
					}
                    try
                    {

                        if (DateTime.Now.Subtract(mdReceived).TotalMinutes > 5)
                        {
                            try
                            {
                                md = _serviceProvider.GetRequiredService<TezosNotifyBot.CryptoCompare.IMarketDataProvider>().GetMarketData();
                            }
                            catch
                            {
                            }

                            mdReceived = DateTime.Now;
                        }

                        var block = db.GetLastBlockLevel();
                        
                        if (!Client_BlockReceived(db, tzkt, block.Item1 + 1))
                            Thread.Sleep(5000);

                        if (DateTime.Now.Subtract(lastReceived).TotalMinutes > 5 &&
                            DateTime.Now.Subtract(lastWarn).TotalMinutes > 10)
                        {
                            NotifyDev(db,
                                $"‼️ Last block {block} received {(int) DateTime.Now.Subtract(lastReceived).TotalMinutes} minutes ago, check node‼️",
                                0);
                            lastWarn = DateTime.Now;
                        }
                    }
                    catch (WebException ex)
                    {
                        if (DateTime.Now.Subtract(lastWebExceptionNotify).TotalMinutes > 5)
                        {
                            LogError(ex);
                            NotifyDev(db, $"‼️ WebException: " + ex.Message, 0);
                            lastWebExceptionNotify = DateTime.Now;
                        }

                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                        NotifyDev(db, $"‼️{ex.Message}\n🧱{prevBlock.Level + 1}", 0);
                        _serviceProvider.GetService<IMemoryCache>().Clear(); 
                        Thread.Sleep(10000);
                    }
                } while (cancelToken.IsCancellationRequested is false);
            }
            catch (Exception fe)
            {
                Logger.LogCritical(fe, "Fatal error");
            }
        }

        private void Twitter_OnTwitResponse(int twitId, string response)
        {
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
            var tw = db.TwitterMessages.Single(o => o.Id == twitId);
            var jObject = JObject.Parse(response);
            if (jObject["errors"] != null)
            {
                if (Config.Twitter.ChatId != 0)
                    Bot.SendTextMessageAsync(Config.Twitter.ChatId, $"Twitter Errors: {response}", ParseMode.Default);
            }
            else
            {
                tw.TwitterId = (string) jObject["id"];
                db.SaveChanges();
                if (Config.Twitter.ChatId != 0)
                    Bot.SendTextMessageAsync(Config.Twitter.ChatId,
                        $"Published: https://twitter.com/{twitterAccountName}/status/{tw.TwitterId}", ParseMode.Default,
                        replyMarkup: ReplyKeyboards.TweetSettings(twitId));
            }
        }

        private int Twitter_OnTwit(string text)
        {
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
            if (Config.Twitter.ChatId != 0)
                Bot.SendTextMessageAsync(Config.Twitter.ChatId,
                    text +
                    $"\n\n📏 {Regex.Replace(text, "http(s)?://([\\w-]+.)+[\\w-]+(/[\\w- ./?%&=])?", "01234567890123456789123").Length}",
                    ParseMode.Default);
            var twm = new TwitterMessage { Text = text, CreateDate = DateTime.Now };
            db.Add(twm);
            db.SaveChanges();
            return twm.Id;
        }

        //BlockHeader lastHeader;
        //BlockMetadata lastMetadata;
        Block prevBlock;

        private bool Client_BlockReceived(Storage.TezosDataContext db, ITzKtClient tzKt, int blockLevel)
        {
            lastReceived = DateTime.Now;            
            if (_currentConstants == null)
                _currentConstants = tzKt.GetCurrentProtocol().constants;
            var tzKtHead = tzKt.GetHead();
            Logger.LogDebug($"TzKt level: {tzKtHead.level}, known level: {tzKtHead.knownLevel}");
            if (tzKtHead.level < blockLevel + 1)
                return false;

            var block = tzKt.GetBlock(blockLevel);
            Logger.LogDebug($"Block {block.Level} received");

            if (prevBlock == null)
                prevBlock = tzKt.GetBlock(blockLevel - 1);
            
            ProcessBlockBakingData(db, block);

            ProcessBlockMetadata(db, block, tzKt);

            // var periods = tzKt.GetVotingPeriods();
            // var currentPeriod = periods.FirstOrDefault(c => c.firstLevel <= block.Level && block.Level <= c.lastLevel);
            // var prevPeriod = periods.Single(p => p.index == currentPeriod.index - 1);
            
 
            //if (currentPeriod.lastLevel == block.Level && currentPeriod.kind == "proposal" &&
            //    prevMD.voting_period_kind == "promotion_vote")
            //{
            //    var hash = _nodeManager.Client.GetCurrentProposal(prevHeader.hash);
            //    var p = rep_o.GetProposal(hash);
            //    foreach (var u in rep_o.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
            //    {
            //        var result = blockMetadata.next_protocol == hash
            //            ? resMgr.Get(Res.PromotionVoteSuccess,
            //                new ContextObject {p = p, u = u, Block = blockMetadata.level.level, Period = blockMetadata.level.voting_period })
            //            : resMgr.Get(Res.PromotionVoteFailed,
            //                new ContextObject {p = p, u = u, Block = blockMetadata.level.level});
            //        if (!u.HideHashTags)
            //            result += "\n\n#proposal" + p.HashTag();
            //        SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
            //    }
            //}
 
            var allUsers = db.Users.ToList();/*
            foreach (var op in operations)
            {
                foreach (var content in op.contents)
                {
                    if (content.kind == "double_baking_evidence" || content.kind == "double_endorsement_evidence")
                    {
                        // Двойная выпечка.
                        var offend = content.metadata.balance_updates.FirstOrDefault(o => o.change < 0);
                        if (offend != null)
                        {
                            var offender = offend.@delegate;
                            decimal lost = -content.metadata.balance_updates.Where(o => o.change < 0)
                                .Sum(o => o.change);
                            decimal rewards = content.metadata.balance_updates.Where(o => o.change > 0)
                                .Sum(o => o.change);

                            var offenderAddresses = rep_o.GetUserAddresses(offender);
                            foreach (var ua in offenderAddresses)
                            {
                                string result = resMgr.Get(Res.DoubleBakingOccured,
                                    new ContextObject
                                    {
                                        u = ua.User, ua = ua, Amount = lost / 1000000, Block = header.level, md = md,
                                        OpHash = op.hash
                                    });
                                if (!ua.User.HideHashTags)
                                    result += "\n\n#doublebaking" + ua.HashTag();
                                SendTextMessageUA(ua, result);
                            }

                            var bakerAddresses = rep_o.GetUserAddresses(block.Baker.address);
                            foreach (var ua in bakerAddresses)
                            {
                                string result = resMgr.Get(Res.DoubleBakingEvidence,
                                    new ContextObject
                                    {
                                        u = ua.User, ua = ua, Amount = rewards / 1000000, Block = header.level, md = md,
                                        OpHash = op.hash
                                    });
                                if (!ua.User.HideHashTags)
                                    result += "\n\n#doublebaking" + ua.HashTag();
                                SendTextMessageUA(ua, result);
                            }
                        }
                    }
                }
            }*/

            var fromToAmountHash = new List<(string from, string to, decimal amount, string hash, Token token)>();
            ProcessTransactions(db, block.Transactions, fromToAmountHash, allUsers);
            foreach (var t in fromToAmountHash.Where(o => o.amount >= 10000 && o.token == null))
                db.AddWhaleTransaction(t.from, t.to, block.Level, block.Timestamp, t.amount, t.hash);

            ProcessDelegations(db, block.Delegations);
            ProcessOriginations(db, block.Originations);

            var fromGroup = fromToAmountHash.Where(o => o.from != "").GroupBy(o => new {o.from, o.token});
            foreach (var from in fromGroup)
            {
                //decimal total = from.Sum(o => o.Item3);
                decimal tokenBalance = 0;
                if (from.Key.token != null)
                {
                    //var bcd = _serviceProvider.GetService<IBetterCallDevClient>();
                    //var acc = bcd.GetAccount(from.Key.from);
                    //var token = acc.balances.FirstOrDefault(o =>
                    //    o.contract == from.Key.token.ContractAddress && o.token_id == from.Key.token.Token_id);
                    //if (token != null)
                    //    tokenBalance = token.Balance;
                    var item = tzKt.GetBigmapItem(from.Key.token.ContractAddress, "ledger", from.Key.from);
                    if (item != null)
					{
                        JObject p = item.value as JObject;
                        if (p != null && p["balance"] != null)
                            tokenBalance = Utils.TokenAmountToDecimal((string)((JValue)p["balance"]).Value, from.Key.token.Decimals);
                    }
                }

                var fromAddresses = db.UserAddresses.Include(x => x.User)
                    .Where(o => o.Address == from.Key.from && !o.IsDeleted && !o.User.Inactive && o.NotifyTransactions).ToList();
                decimal fromBalance = 0;
                bool fromDelegate = false;
                if (fromAddresses.Count > 0)
                {
                    if (db.Delegates.Any(o => o.Address == from.Key.from))
                    {
                        fromDelegate = true;
                        var di = addrMgr.GetDelegate(block.Hash, from.Key.from);
                        if (di != null)
                            fromBalance = di.Bond / 1000000;
                        else
                            fromBalance = addrMgr.GetContract(block.Hash, from.Key.from).balance /
                                          1000000M;
                    }
                    else
                    {
                        fromBalance = addrMgr.GetContract(block.Hash, from.Key.from).balance /
                                      1000000M;
                    }
                }

                foreach (var ua in fromAddresses)
                {
                    if (ua.AmountThreshold > from.Sum(o => o.Item3))
                        continue;
                    if (!fromDelegate)
                        ua.Balance = fromBalance;
                    else
                        ua.FullBalance = fromBalance;
                    //string usdBalance = ua.UsdBalance(md.price_usd);
                    //string btcBalance = ua.BtcBalance(md.price_btc);
                    //string balance = ua.TezBalance();
                    string result = "";
                    string tags = "";
                    var from_ua = from.Where(o => ua.User.WhaleThreshold == 0 || o.Item3 < ua.User.WhaleThreshold);
                    if (from_ua.Count() == 0)
                        continue;
                    if (from_ua.Count() == 1)
                    {
                        var to = from_ua.Single().Item2;
                        var ua_to = db.GetUserTezosAddress(ua.UserId, to);
                        result = resMgr.Get(Res.OutgoingTransaction,
                            new ContextObject
                            {
                                u = ua.User, OpHash = from_ua.Single().Item4, Block = block.Level,
                                Amount = from_ua.Sum(o => o.Item3), md = md, ua_from = ua, ua_to = ua_to,
                                Token = from.Key.token
                            }) + "\n";
                        tags = ua_to.HashTag();
                    }
                    else
                    {
                        result = resMgr.Get(Res.OutgoingTransactions,
                            new ContextObject
                            {
                                u = ua.User, ua = ua, Block = block.Level, Amount = from_ua.Sum(o => o.Item3), md = md,
                                Token = from.Key.token
                            }) + "\n";
                        int cnt = 0;
                        foreach (var to in from_ua.OrderByDescending(o => o.Item3))
                        {
                            cnt++;
                            var targetAddr = db.GetUserTezosAddress(ua.UserId, to.Item2);
                            result += resMgr.Get(Res.To,
                                          new ContextObject
                                              {u = ua.User, Amount = to.Item3, ua = targetAddr, Token = to.token}) +
                                      "\n";
                            if (!tags.Contains(targetAddr.HashTag()) && (cnt < 6 || targetAddr.UserId == ua.UserId))
                                tags += targetAddr.HashTag();
                            if (cnt > 40)
                            {
                                result += resMgr.Get(Res.NotAllShown,
                                    new ContextObject {u = ua.User, Block = block.Level }) + "\n";
                                break;
                            }
                        }
                    }

                    result += "\n";
                    if (from.Key.token == null)
                    {
                        if (fromDelegate)
                            result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
                        else
                            result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
                    }
                    else
                    {
                        result += resMgr.Get(Res.TokenBalance,
                            new ContextObject {u = ua.User, Amount = tokenBalance, Token = from.Key.token}) + "\n";
                    }

                    if (!ua.User.HideHashTags)
                        result += "\n#outgoing" +
                                  (from.Key.token != null ? " #" + from.Key.token.Symbol.ToLower() : "") +
                                  ua.HashTag() + tags;
                    SendTextMessageUA(db, ua, result);
                }
            }

            var toGroup = fromToAmountHash.GroupBy(o => new {o.to, o.token});
            foreach (var to in toGroup)
            {
                //decimal total = to.Sum(o => o.Item3);
                decimal tokenBalance = 0;
                if (to.Key.token != null)
                {
                    //var bcd = _serviceProvider.GetService<IBetterCallDevClient>();
                    //var acc = bcd.GetAccount(to.Key.to);
                    //var token = acc.balances.FirstOrDefault(o =>
                    //    o.contract == to.Key.token.ContractAddress && o.token_id == to.Key.token.Token_id);
                    //if (token != null)
                    //    tokenBalance = token.Balance;
                    var item = tzKt.GetBigmapItem(to.Key.token.ContractAddress, "ledger", to.Key.to);
                    if (item != null)
                    {
                        JObject p = item.value as JObject;
                        if (p != null && p["balance"] != null)
                            tokenBalance = Utils.TokenAmountToDecimal((string)((JValue)p["balance"]).Value, to.Key.token.Decimals);
                    }
                }

                var toAddresses = db.GetUserAddresses(to.Key.to).Where(o => o.NotifyTransactions).ToList();
                decimal toBalance = 0;
                bool toDelegate = false;
                if (toAddresses.Count > 0)
                {
                    if (db.Delegates.Any(o => o.Address == to.Key.to))
                    {
                        toDelegate = true;
                        var di = addrMgr.GetDelegate(block.Hash, to.Key.to);
                        if (di != null)
                            toBalance = (di?.Bond ?? 0) / 1000000;
                        else
                            toBalance = addrMgr.GetContract(block.Hash, to.Key.to).balance /
                                        1000000M;
                    }
                    else
                    {
                        toBalance = addrMgr.GetContract(block.Hash, to.Key.to).balance / 1000000M;
                    }
                }

                var amount = to.Sum(o => o.Item3);
                
                var contract = addrMgr.GetContract(block.Hash, to.Key.to);
                if (contract.@delegate != null && to.Key.token == null)
                {
                    HandleDelegatorsBalance();
                }
                
                void HandleDelegatorsBalance()
                {
                    var receiverAddr = to.Key.to;
                    var senderAddr = to.First().Item1;
                    var isPayout = db.IsPayoutAddress(senderAddr);
                    if (isPayout)
                        return;
                    
                    var receiver = new UserAddress
                    {
                        Address = receiverAddr, 
                        Balance = addrMgr.GetBalance(block.Hash, receiverAddr)
                    };
                    
                    if (amount < receiver.InflationValue)
                        return;
                    
                    var delegatesAddr = db.GetUserAddresses(contract.@delegate)
                        .Where(x => x.NotifyDelegatorsBalance && x.DelegatorsBalanceThreshold < amount && x.User.Type == 0);
                        
                    foreach (var delegateAddress in delegatesAddr)
                    {
                        var tags = new List<string>
                        {
                            "#delegator_balance", 
                            receiver.HashTag(), 
                            delegateAddress.HashTag()
                        };
                        var textData = new ContextObject
                        {
                            u = delegateAddress.User,
                            md = md,
                            ua = receiver,
                            OpHash = to.First().Item4,
                            Block = block.Level,
                            Amount = amount,
                            Delegate = delegateAddress,
                        };
                        var text = new StringBuilder();
                        text.AppendLine(resMgr.Get(Res.DelegatorsBalance, textData));

                        text.AppendLine();
                        text.AppendLine(resMgr.Get(Res.CurrentDelegatorBalance, textData));

                        if (delegateAddress.User.HideHashTags is false)
                        {
                            text.AppendLine();
                            text.AppendLine(tags.Select(x => x.Trim()).Join(" "));
                        }

                        PushTextMessage(db, delegateAddress, text.ToString());
                        // TODO: Using ChatId instead of UserId?
                        //SendTextMessage(delegateAddress.UserId, text.ToString());
                    }
                }
                
                foreach (var ua in toAddresses)
                {
                    if (ua.AmountThreshold > amount)
                        continue;
                    if (!toDelegate)
                        ua.Balance = toBalance;
                    else
                        ua.FullBalance = toBalance;
                    //string usdBalance = ua.UsdBalance(md.price_usd);
                    //string btcBalance = ua.BtcBalance(md.price_btc);
                    //string balance = ua.TezBalance();
                    var result = "";
                    var tags = "";
                    var operationTag = "#incoming";
                    
                    var to_ua = to.Where(o => ua.User.WhaleThreshold == 0 || o.Item3 < ua.User.WhaleThreshold);
                    if (to_ua.Count() == 0)
                        continue;
                    if (to_ua.Count() == 1)
                    {
                        
                        var from = to_ua.Single().Item1;
                        if (from == "")
                        {
                            result = resMgr.Get(Res.Mint,
                                new ContextObject
                                {
                                    u = ua.User,
                                    OpHash = to_ua.Single().Item4,
                                    Block = block.Level,
                                    Amount = to_ua.Sum(o => o.Item3),
                                    md = md,
                                    ua = ua,
                                    Token = to.Key.token
                                }) + "\n";
                            operationTag = "#mint";
                        }
                        else
                        {
                            var ua_from = db.GetUserTezosAddress(ua.UserId, from);

                            var isSenderPayout = db.IsPayoutAddress(from);
                            var isReceiverPayout = db.IsPayoutAddress(ua.Address);

                            var isPayoutOperation = isSenderPayout && isReceiverPayout is false;
                            if (isPayoutOperation)
                                operationTag = "#payout";

                            var messageType = isPayoutOperation && ua_from.NotifyPayout
                                ? Res.Payout
                                : Res.IncomingTransaction;

                            result = resMgr.Get(messageType,
                                new ContextObject
                                {
                                    u = ua.User,
                                    OpHash = to_ua.Single().Item4,
                                    Block = block.Level,
                                    Amount = to_ua.Sum(o => o.Item3),
                                    md = md,
                                    ua_from = ua_from,
                                    ua_to = ua,
                                    Token = to.Key.token
                                }) + "\n";
                            tags = ua_from.HashTag();
                        }
                    }
                    else
                    {
                        result = resMgr.Get(Res.IncomingTransactions,
                            new ContextObject
                            {
                                u = ua.User, ua = ua, Block = block.Level, Amount = to_ua.Sum(o => o.Item3), md = md,
                                Token = to.Key.token
                            }) + "\n";
                        int cnt = 0;
                        foreach (var from in to_ua.OrderByDescending(o => o.Item3))
                        {
                            cnt++;
                            var sourceAddr = db.GetUserTezosAddress(ua.UserId, from.Item1);
                            result += resMgr.Get(Res.From,
                                new ContextObject
                                    {u = ua.User, Amount = from.Item3, ua = sourceAddr, Token = from.token}) + "\n";
                            if (!tags.Contains(sourceAddr.HashTag()) && (cnt < 6 || sourceAddr.UserId == ua.UserId))
                                tags += sourceAddr.HashTag();
                            if (cnt > 40)
                            {
                                result += resMgr.Get(Res.NotAllShown,
                                    new ContextObject {u = ua.User, Block = block.Level }) + "\n";
                                break;
                            }
                        }
                    }

                    result += "\n";
                    if (to.Key.token == null)
                    {
                        if (toDelegate)
                            result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
                        else
                            result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
                    }
                    else
                    {
                        result += resMgr.Get(Res.TokenBalance,
                            new ContextObject {u = ua.User, Amount = tokenBalance, Token = to.Key.token}) + "\n";
                    }

                    if (!ua.User.HideHashTags)
                        result += $"\n{operationTag}" + (to.Key.token != null ? " #" + to.Key.token.Symbol.ToLower() : "") +
                                  ua.HashTag() + tags;
                    SendTextMessageUA(db, ua, result);
                }
            }
			
            if (!lastBlockChanged)
                db.SetLastBlockLevel(block.Level, block.blockRound, block.Hash);
            Logger.LogInformation($"Block {block.Level} processed");
            //lastHeader = header;
            //lastHash = header.hash;
            prevBlock = block;
            blockProcessings.Enqueue(DateTime.Now);
            if (blockProcessings.Count > 21)
                blockProcessings.Dequeue();
            if (lastBlockChanged)
            {
                lastBlockChanged = false;
                return false;
            }

            return true;
        }

        void ProcessTransactions(Storage.TezosDataContext db, List<Transaction> ops, List<(string from, string to, decimal amount, string hash, Token token)> fromToAmountHash, List<User> allUsers)
        {
            foreach (var op in ops)
            {
                if (op.Status != "applied")
                    continue;

                var from = op.Sender.address;
                var to = op.Target.address;
                var amount = op.Amount / 1000000M;

                Token token = null;
                if (op.Parameter?.entrypoint == "transfer")
                {
                    //Logger.LogDebug("transfer " + to + " " + (op.Parameter.value is JObject).ToString() + " " + op.Parameter?.value?.GetType()?.FullName);
                    token = db.Tokens.FirstOrDefault(o => o.ContractAddress == to);
                    //if (token?.ContractAddress == "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton" && op.Parameter?.value is JArray)
                    //    HandleNftTransfer(op, token).ConfigureAwait(true).GetAwaiter().GetResult();
                    if (token != null && op.Parameter.value is JObject)
                    {
                        JObject p = op.Parameter.value as JObject;
                        if (p["from"] == null || p["to"] == null || p["value"] == null)
                            continue;
                        from = (string)((JValue)p["from"]).Value;
                        to = (string)((JValue)p["to"]).Value;
                        amount = Utils.TokenAmountToDecimal((string)((JValue)p["value"]).Value, token.Decimals);
                    }
                }
                if (op.Parameter?.entrypoint == "mint" && op.Parameter.value is JObject)
                {
                    token = db.Tokens.FirstOrDefault(o => o.ContractAddress == to);
                    if (token != null)
                    {
                        JObject p = op.Parameter.value as JObject;
                        if (p["to"] == null || p["value"] == null)
                            continue;
                        from = "";
                        to = (string)((JValue)p["to"]).Value;
                        amount = Utils.TokenAmountToDecimal((string)((JValue)p["value"]).Value, token.Decimals);
                    }
                }
                if (amount == 0)
                    continue;
                fromToAmountHash.Add((from, to, amount, op.Hash, token));
                // Уведомления о китах
                if (token == null)
                {
                    var ka_from = db.KnownAddresses.SingleOrDefault(x => x.Address == from) ?? new KnownAddress(from, null);
                    var ka_to = db.KnownAddresses.SingleOrDefault(x => x.Address == to) ?? new KnownAddress(to, null);
                    if (!ka_from.ExcludeWhaleAlert && !ka_to.ExcludeWhaleAlert)
                    {
                        foreach (var u in allUsers.Where(o =>
                            !o.Inactive && o.WhaleThreshold > 0 && o.WhaleThreshold <= amount))
                        {
                            var ua_from = db.GetUserTezosAddress(u.Id, from);
                            var ua_to = db.GetUserTezosAddress(u.Id, to);
                            string result = resMgr.Get(Res.WhaleTransaction,
                                new ContextObject
                                {
                                    u = u,
                                    OpHash = op.Hash,
                                    Amount = amount,
                                    md = md,
                                    ua_from = ua_from,
                                    ua_to = ua_to
                                });
                            if (!u.HideHashTags)
                            {
                                result += "\n\n#whale" + ua_from.HashTag() + ua_to.HashTag();
                            }

                            SendTextMessage(db, u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
                        }

                        // Уведомления о китах для твиттера
                        if (amount >= 999000)
                        {
                            var ua_from = db.GetUserTezosAddress(0, from);
                            var ua_to = db.GetUserTezosAddress(0, to);
                            string result = resMgr.Get(Res.TwitterWhaleTransaction,
                                new ContextObject
                                { OpHash = op.Hash, Amount = amount, md = md, ua_from = ua_from, ua_to = ua_to });
                            twitter.TweetAsync(result);
                        }
                    }
                }
            }
        }
        public async void Tweet(string text)
		{
            await twitter.TweetAsync(text);
        }
        void ProcessDelegations(Storage.TezosDataContext db, List<Delegation> ops)
		{
            foreach(var op in ops)
            {
                if (op.Status != "applied")
                    continue;
                var from = op.Sender.address;
                var to = op.NewDelegate?.address;
                var fromAddresses = db.GetUserAddresses(from);
                if (to != null)
                {
                    var toAddresses = db.GetUserAddresses(to);
                    foreach (var ua in db.GetUserAddresses(from).Where(o => o.NotifyDelegations))
                    {
                        var targetAddr = db.GetUserTezosAddress(ua.UserId, to);
                        string result = resMgr.Get(Res.NewDelegation,
                            new ContextObject
                            {
                                u = ua.User,
                                OpHash = op.Hash,
                                Amount = op.Amount / 1000000M,
                                ua_from = ua,
                                ua_to = targetAddr
                            });
                        if (!ua.User.HideHashTags)
                            result += "\n\n#delegation" + targetAddr.HashTag() + ua.HashTag();
                        SendTextMessageUA(db, ua, result);
                    }

                    foreach (var ua in db.GetUserAddresses(to).Where(o => o.NotifyDelegations))
                    {                        
                        if (ua.DelegationAmountThreshold > op.Amount / 1000000M)
                            continue;
                        var sourceAddr = db.GetUserTezosAddress(ua.UserId, from);
                        string result = resMgr.Get(Res.NewDelegation,
                            new ContextObject
                            {
                                u = ua.User,
                                OpHash = op.Hash,
                                Amount = op.Amount / 1000000M,
                                ua_from = sourceAddr,
                                ua_to = ua
                            });
                        if (!ua.User.HideHashTags)
                            result += "\n\n#delegation" + sourceAddr.HashTag() + ua.HashTag();
                        SendTextMessageUA(db, ua, result);
                    }
                }

                var prevdelegate = op.PrevDelegate?.address;
                if (prevdelegate != null)
                {
                    foreach (var ua in db.GetUserAddresses(prevdelegate).Where(o => o.NotifyDelegations))
                    {
                        if (ua.DelegationAmountThreshold > op.Amount / 1000000M)
                            continue;
                        var sourceAddr = db.GetUserTezosAddress(ua.UserId, from);
                        string result = resMgr.Get(Res.UnDelegation,
                            new ContextObject
                            {
                                u = ua.User,
                                OpHash = op.Hash,
                                Amount = op.Amount / 1000000M,
                                ua_from = sourceAddr,
                                ua_to = ua
                            });
                        if (!ua.User.HideHashTags)
                            result += "\n\n#leave_delegate" + sourceAddr.HashTag() + ua.HashTag();
                        SendTextMessageUA(db, ua, result);
                    }
                }
            }
        }
        void ProcessOriginations(Storage.TezosDataContext db, List<Origination> ops)
		{
            foreach (var op in ops)
            {
                if (op.Status != "applied")
                    continue;
                var to = op.ContractDelegate?.address;
                if (to == null)
                    continue;
                var amount = op.ContractBalance / 1000000M;
                string tezAmount = amount.TezToString();
                var toAddresses = db.GetUserAddresses(to);
                foreach (var ua in db.GetUserAddresses(op.Sender.address))
                {
                    var targetAddr = db.GetUserTezosAddress(ua.UserId, to);
                    string result = resMgr.Get(Res.NewDelegation,
                        new ContextObject
                        { u = ua.User, OpHash = op.Hash, Amount = amount, ua_from = ua, ua_to = targetAddr });
                    if (!ua.User.HideHashTags)
                        result += "\n\n#delegation" + targetAddr.HashTag() + ua.HashTag();
                    SendTextMessageUA(db, ua, result);
                }

                foreach (var ua in db.GetUserAddresses(to).Where(o => o.NotifyDelegations))
                {
                    if (ua.DelegationAmountThreshold > amount)
                        continue;
                    var sourceAddr = db.GetUserTezosAddress(ua.UserId, op.OriginatedContract.address);
                    string result = resMgr.Get(Res.NewDelegation,
                        new ContextObject
                        { u = ua.User, OpHash = op.Hash, Amount = amount, ua_from = sourceAddr, ua_to = ua });
                    if (!ua.User.HideHashTags)
                        result += "\n\n#delegation " + sourceAddr.HashTag() + ua.HashTag();
                    SendTextMessageUA(db, ua, result);
                }
            }
        }
        /*List<(string from, string to, decimal amount)> TokenTransfers(Token token, Operation op)
        {
            List<(string from, string to, decimal amount)>
                result = new List<(string from, string to, decimal amount)>();
            var bcd = _serviceProvider.GetService<IBetterCallDevClient>();
            var ops = bcd.GetOperations(op.hash);
            foreach (var transfer in ops.Where(o =>
                o.destination == token.ContractAddress && (o.entrypoint == "transfer" || o.entrypoint == "mint") && o.status == "applied"))
            {
                if (transfer.parameters?.Count == 1 &&
                    transfer.parameters[0].children?.Count == 3 &&
                    transfer.parameters[0].children[0].name == "from" &&
                    transfer.parameters[0].children[1].name == "to" &&
                    transfer.parameters[0].children[2].name == "value")
                {
                    var from = transfer.parameters[0].children[0].value;
                    var to = transfer.parameters[0].children[1].value;

                    decimal value = Utils.TokenAmountToDecimal(transfer.parameters[0].children[2].value, token.Decimals);
                    result.Add((from, to, (decimal) value));                    
                }
                if (transfer.parameters?.Count == 1 &&
                    transfer.parameters[0].name == "mint" &&
                    transfer.parameters[0].children?.Count == 2 &&
                    transfer.parameters[0].children[0].name == "to" &&
                    transfer.parameters[0].children[1].name == "value")
                {
                    var to = transfer.parameters[0].children[0].value;
                    decimal value = Utils.TokenAmountToDecimal(transfer.parameters[0].children[1].value, token.Decimals);
                    result.Add(("", to, (decimal)value));
                }
			}

			return result;
        }
        */
        void ProcessBlockBakingData(Storage.TezosDataContext db, Block block/*, BlockHeader header, BlockMetadata blockMetadata_*/)
        {
            Logger.LogDebug($"ProcessBlockBakingData {block.Level}");

            var tzktClient = _serviceProvider.GetService<ITzKtClient>();
            var missedRights = tzktClient.GetRights(block.Level, "missed");
            foreach (var right in missedRights)
			{
                var uaddrs = db.GetUserAddresses(right.baker.address);
                ContractInfo info = null;
                if (uaddrs.Count > 0)
                    info = addrMgr.GetContract(block.Hash, right.baker.address);

                if (right.type == "baking")
                {
                    foreach (var ua in uaddrs.Where(o => o.NotifyMisses))
                    {
                        ua.Balance = info.balance / 1000000M;
                        var result = resMgr.Get(Res.MissedBaking,
                            new ContextObject {
                                u = ua.User,
                                ua = ua,
                                Block = block.Level
                            });

                        if (!ua.User.HideHashTags)
                            result += "\n\n#missed_baking" + ua.HashTag();
                        PushTextMessage(db, ua, result);
                    }
                }
                if (right.type == "endorsing")
                {
                    foreach (var ua in uaddrs.Where(o => o.NotifyMisses))
                    {
                        ua.Balance = info.balance / 1000000M;                        
                        var result = resMgr.Get(Res.MissedEndorsing,
                            new ContextObject {
                                u = ua.User,
                                ua = ua,
                                Block = block.Level
                            });
                        if (!ua.User.HideHashTags)
                            result += "\n\n#missed_endorsing" + ua.HashTag();
                        PushTextMessage(db, ua, result);
                    }
                }
            }
            Logger.LogInformation($"Block {block.Level} baking data processed");
        }

        class RewardMsg
        {
            public User User;
            public string Message;
            public string Tags;
            public UserAddress UserAddress;
        }

        Cycle currentCycle;
        void ProcessBlockMetadata(Storage.TezosDataContext db, Block block, ITzKtClient tzKtClient)
        {
            Logger.LogDebug($"ProcessBlockMetadata {block.Level}");
            var cycles = tzKtClient.GetCycles();
            var cycle = cycles.Single(c => c.firstLevel <= block.Level && block.Level <= c.lastLevel);
            currentCycle = cycle;
            /*if (cycle.lastLevel == block.Level)
            {
                Logger.LogDebug($"Calc delegates rewards on {block.Level}");
                //User,rewards,tags,lang
                List<RewardMsg> msgList = new List<RewardMsg>();
                var delegates = rep_o.GetUserDelegates();
                foreach (var d in delegates.Where(ua => ua.NotifyBakingRewards).GroupBy(ua => ua.Address))
                {
                    var rewards = tzKtClient.GetBakerRewards(d.Key, cycle.index - 5);
                    if (rewards != null && rewards.TotalBakerRewards > 0)
                    {
                        var ualist = d.ToList();
                        if (ualist.Count > 0)
                        {
                            DelegateInfo di;
                            try
                            {
                                di = addrMgr.GetDelegate(block.Hash, d.Key);
                            }
                            catch
                            {
                                continue;
                            }

                            foreach (var ua in ualist)
                            {
                                var t = Explorer.FromId(ua.User.Explorer);
                                string result = resMgr.Get(Res.RewardDeliveredItem,
                                    new ContextObject { u = ua.User, ua = ua, Amount = rewards.TotalBakerRewards / 1000000M }) + " ";
                                ua.FullBalance = di.Bond / 1000000;
                                result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n\n";

                                var reward = msgList.LastOrDefault(o =>
                                    o.User == ua.User && o.UserAddress.ChatId == ua.ChatId);
                                if (reward == null || reward.Message.Length > 3500)
                                {
                                    reward = new RewardMsg { User = ua.User, Message = "", Tags = "", UserAddress = ua };
                                    msgList.Add(reward);
                                }

                                if (!ua.User.HideHashTags)
                                    reward.Tags += ua.HashTag();
                                reward.Message += result;
                            }
                        }
                    }
                }

                foreach (var msg in msgList)
                {
                    string result = resMgr.Get(Res.RewardDelivered,
                            new ContextObject { u = msg.User, Cycle = cycle.index - 5 }) + "\n\n" +
                        msg.Message + (msg.Tags != "" ? "#reward" + msg.Tags : "");
                    //SendTextMessageUA(msg.UserAddress,
                    //    resMgr.Get(Res.RewardDelivered,
                    //        new ContextObject { u = msg.User, Cycle = cycle.index - 5 }) + "\n\n" +
                    //    msg.Message + (msg.Tags != "" ? "#reward" + msg.Tags : ""));
                    PushTextMessage(msg.UserAddress, result);
                }
                Logger.LogDebug($"Calc delegates rewards finished on {block.Level}");
            }*/
            if (false)//cycle.firstLevel == block.Level)@fix it!
            {
                var uad = db.UserAddresses.Where(o => !o.IsDeleted && (o.NotifyCycleCompletion || o.NotifyBakingRewards || o.NotifyOutOfFreeSpace) && !o.User.Inactive)
                    .Join(db.Delegates, o => o.Address, o => o.Address, (o, d) => o).Include(x => x.User).ToList();

                var penalties = tzKtClient.GetRevelationPenalties(block.Level - 1);
                foreach (var penalty in penalties)
                {
                    foreach (var ua in uad.Where(a => a.Address == penalty.baker.address && a.NotifyMisses))
                    {
                        var result = resMgr.Get(Res.RevelationPenalty,
                            new ContextObject
                            {
                                ua = ua,
                                u = ua.User,
                                Block = penalty.missedLevel,
                                Amount = penalty.TotalLost
                            });
                        if (!ua.User.HideHashTags)
                            result += "\n\n#missed_revelation" + ua.HashTag();
                        //SendTextMessageUA(ua, result);
                        PushTextMessage(db, ua, result);
                    }
                }

                //foreach (var d in uad.Select(o => o.Address).Distinct())
                //{
                //    DelegateInfo di;
                //    try
                //    {
                //        di = addrMgr.GetDelegate(_nodeManager.Client, block.Hash, d, true);
                //    }
                //    catch
                //    {
                //        continue;
                //    }

                //    if (di != null)
                //    {
                //        decimal accured =
                //            addrMgr.GetRewardsForCycle(_nodeManager.Client, d, di, blockMetadata.level.cycle - 1);
                //        rep_o.UpdateDelegateAccured(d, (blockMetadata.level.cycle - 1), (long) accured);
                //    }
                //}

                var dispatcher = GetService<IEventDispatcher>();

                dispatcher.Dispatch(new CycleCompletedEvent());
                
                Logger.LogDebug($"Calc delegates performance on {block.Level - 1}");

                var cyclePast = cycles.Single(o => o.index == cycle.index - 1);
                Dictionary<string, Rewards> rewards = new Dictionary<string, Rewards>();
                foreach (var d in uad.Where(o => o.NotifyCycleCompletion).GroupBy(o => o.Address))
                    rewards.Add(d.Key, tzKtClient.GetBakerRewards(d.Key, cyclePast.index));
                foreach (var usr in uad.Where(o => o.NotifyCycleCompletion).GroupBy(o => new {o.UserId, o.ChatId}))
                {
                    string perf = resMgr.Get(Res.CycleCompleted,
                        new ContextObject {u = usr.First().User, Cycle = cycle.index - 1, CycleLength = cyclePast.Length, NextEnd = cycle.endTime});
                    foreach (var dr in usr)
                    {
                        var r = rewards[dr.Address];
                        var rew = r?.TotalBakerRewards ?? 0;
                        var rewPlan = r?.TotalBakerRewardsPlan ?? 0;
                        var rewMax = rewPlan + r?.TotalBakerLoss ?? 0;
                        if (rewMax > 0)
                        {
                            dr.Performance = 100M * rew / rewMax;
                            perf += "\n\n" + resMgr.Get(Res.Performance, dr);
                            perf += "\n" + resMgr.Get(Res.Accrued,
                                new ContextObject
                                {
                                    u = usr.First().User, Cycle = cycle.index - 1, Amount = rew / 1000000M
                                });
                        }
                    }

                    if (!usr.First().User.HideHashTags)
                        perf += "\n\n#cycle" + String.Join("", usr.Select(o => o.HashTag()));
                    //SendTextMessageUA(usr.First(), perf);
                    PushTextMessage(db, usr.First(), perf);
                }
                Logger.LogDebug($"Calc delegates performance on {block.Level - 1} finished");
                // TODO: TNB-22

                NotifyAssignedRights(db, tzKtClient, uad, cycle.index);

                //NotifyOutOfFreeSpace(block, tzKtClient, uad, cycle.index, cycles);

                LoadAddressList(tzKtClient, db);
                /*
                Logger.LogDebug($"Calc delegators awards on {block.Level - 1}");
                // Notification of the availability of the award to the delegator
                var userAddressDelegators = rep_o.GetDelegators();
                var addrs = userAddressDelegators.Select(o => o.Address).Distinct();
                int cycle1 = cycle.index - 6;
                foreach (var addr in addrs)
				{
                    var ua_rewards = tzKtClient.GetDelegatorRewards(addr, cycle1);
                    if (ua_rewards != null && ua_rewards.TotalRewards > 0)
					{
                        foreach(var ua in userAddressDelegators.Where(o => o.Address == addr))
						{
                            var context = new ContextObject
                            {
                                u = ua.User,
                                Cycle = cycle1,
                                Amount = ua_rewards.TotalRewards,
                                ua_to = rep_o.GetUserTezosAddress(ua.UserId, ua_rewards.baker.address),
                                ua = ua
                            };
                            var message = resMgr.Get(Res.AwardAvailable, context);
                            if (!ua.User.HideHashTags)
                                message += "\n\n#award " + ua.HashTag() + context.ua_to.HashTag();
                                                        
                            PushTextMessage(ua, message);
                        }
					}
                }
                Logger.LogDebug($"Calc delegators awards on {block.Level - 1} finished");*/
            }

            VotingNotify(db, block, cycle, tzKtClient);

            Logger.LogDebug($"ProcessBlockMetadata {block.Level} completed");
        }

        void NotifyAssignedRights(Storage.TezosDataContext db, ITzKtClient tzKtClient, List<UserAddress> userAddresses, int cycle)
		{
            var delegates = userAddresses.Where(ua => ua.NotifyCycleCompletion).Select(ua => ua.Address).Distinct();
            Dictionary<string, RightsInfo> rights = new Dictionary<string, RightsInfo>();
            foreach(var addr in delegates)
			{
                var r = tzKtClient.GetRights(addr, cycle);
                rights.Add(addr, new RightsInfo(r));
            }

            foreach(var u in userAddresses.Where(ua => ua.NotifyCycleCompletion).GroupBy(ua => ua.User))
			{
                var message = resMgr.Get(Res.RightsAssigned, new ContextObject 
                {
                    u = u.Key,
                    Cycle = cycle
                }) + "\n\n";
                string tags = "";
                foreach (var ua in u)
                {
                    var r = rights[ua.Address];
                    message += resMgr.Get(Res.RightsAssignedItem, new ContextObject
                    {
                        ua = ua,
                        u = u.Key,
                        Rights = r
                    }) + "\n\n";

                    tags += ua.HashTag();
                }
                if (!u.Key.HideHashTags)
                    message += "#rights_assigned" + tags;
                //SendTextMessage(u.Key.Id, message, ReplyKeyboards.MainMenu(resMgr, u.Key));
                PushTextMessage(db, u.Key.Id, message);
            }
		}
        /*
        void NotifyOutOfFreeSpace(Block block, ITzKtClient tzKtClient, List<UserAddress> userAddresses, int cycle, List<Cycle> cycles)
        {
            var delegates = userAddresses.Where(ua => ua.NotifyOutOfFreeSpace).Select(ua => ua.Address).Distinct();
            Dictionary<string, Cycle> delegateUncoveredCycle = new Dictionary<string, Cycle>();
            foreach (var addr in delegates)
            {
                var fr = tzKtClient.GetBakerFutureRewards(addr);
                if (fr == null || fr.Count < 12)
                    continue;
                fr.Reverse();
                long freeBalance = (long)addrMgr.GetDelegate(block.Hash, addr).balance
                    - fr[6].futureBlockDeposits - fr[6].futureEndorsementDeposits;
                for (int c = 7; c < 12; c++)
                {
                    freeBalance += fr[c - 6].blockDeposits + fr[c - 6].endorsementDeposits;
                    freeBalance -= fr[c].futureBlockDeposits + fr[c].futureEndorsementDeposits;
                    freeBalance += fr[c - 6].TotalBakerRewards;
                    if (freeBalance < 0)
					{
                        var c1 = cycles.SingleOrDefault(o => o.index == fr[c].cycle);
                        if (c1 != null)
                            delegateUncoveredCycle[addr] = c1;
                        break;
					}
                }
            }

            foreach (var ua in userAddresses.Where(ua => ua.NotifyOutOfFreeSpace && delegateUncoveredCycle.ContainsKey(ua.Address)))
            {
                string tags = "";

                var message = resMgr.Get(Res.OutOfFreeSpace, new ContextObject
                {
                    u = ua.User,
                    ua = ua,
                    Cycle = delegateUncoveredCycle[ua.Address].index,
                    NextEnd = delegateUncoveredCycle[ua.Address].startTime
                }) + "\n\n";

                tags += ua.HashTag();

                if (!ua.User.HideHashTags)
                    message += "#nofreespace" + tags;
                //SendTextMessage(ua.User.Id, message, ReplyKeyboards.MainMenu(resMgr, ua.User));
                PushTextMessage(ua, message);
            }
        }*/
        private void LoadAddressList(ITzKtClient tzKt, Storage.TezosDataContext db)
        {
            try
            {
                var t = Explorer.FromId(3);
                var delegates = db.Delegates.OrderBy(o => o.Name).ToList();
                var knownNames = db.KnownAddresses.OrderBy(o => o.Name).ToList();
                string result = "";
                int cnt = 0;
                List<string> updated = new List<string>();

                foreach (var a in tzKt.GetAccounts("delegate", 0))
                {
                    var addr = a.address;
                    var name = a.alias;
                    if (string.IsNullOrEmpty(a.alias))
                        continue;
                    updated.Add(addr);
                    var dg = delegates.FirstOrDefault(o => o.Address == addr);
                    if (dg != null)
                    {
                        if (dg.Name != name)
                        {
                            dg.Name = name;
                            result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
                            cnt++;
                        }

                        continue;
                    }
                    var kn = knownNames.FirstOrDefault(o => o.Address == addr);
                    if (kn != null)
                    {
                        if (kn.Name != name)
                        {
                            kn.Name = name;
                            result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
                            cnt++;
                        }

                        continue;
                    }
                    else
                        db.KnownAddresses.Add(new KnownAddress(addr, name));
                    
                    result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
                    cnt++;
                }
                db.SaveChanges();

                for (int i = 0; i < 10; i++)
                {
                    foreach (var a in tzKt.GetAccounts("user", i * 10000))
                    {
                        var addr = a.address;
                        var name = a.alias;
                        if (string.IsNullOrEmpty(a.alias) || updated.Contains(addr))
                            continue;
                        updated.Add(addr);
                        var kn = knownNames.FirstOrDefault(o => o.Address == addr);
                        if (kn != null)
                        {
                            if (kn.Name != name)
                            {
                                kn.Name = name;
                                result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
                                cnt++;
                            }

                            continue;
                        }
                        else
                            db.KnownAddresses.Add(new KnownAddress(addr, name));
                        
                        result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
                        cnt++;
                    }
                }
                db.SaveChanges();

                result = "Updated names: " + cnt + "\n" + result;
                NotifyDev(db, result, 0, ParseMode.Html);
            }
            catch (Exception e)
            {
                LogError(e);
                NotifyDev(db, "Fail to update address list from GitHub: " + e.Message, 0);
            }
        }

        async void OnCallbackQuery(object sc, CallbackQueryEventArgs ev)
        {
            try
            {
                var message = ev.CallbackQuery.Message;
                var callbackData = ev.CallbackQuery.Data;
                long userId = ev.CallbackQuery.From.Id;
                if (callbackData.StartsWith("_"))
				{
                    userId = long.Parse(callbackData.Substring(1, callbackData.IndexOf('_', 1) - 1));
                    callbackData = callbackData.Substring(callbackData.IndexOf('_', 1) + 1);
                    var cm = Bot.GetChatAdministratorsAsync(userId).ConfigureAwait(true).GetAwaiter().GetResult();
                    if (!cm.Any(m => m.User.Id == ev.CallbackQuery.From.Id))
                    {
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, "🤚 Forbidden");
                        return;
					}
                }
                var callbackArgs = callbackData.Split(' ').Skip(1).ToArray();
                using var scope = _serviceProvider.CreateScope();
                var provider = scope.ServiceProvider;
                using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();

                Func<UserAddress> useraddr = () => {
                    var addrId = int.Parse(callbackArgs[0]);
                    return db.UserAddresses.FirstOrDefault(x => x.UserId == userId && x.Id == addrId);
                };

                db.LogMessage(userId, message.MessageId, null, callbackData);
                var user = db.Users.SingleOrDefault(x => x.Id == userId);
                Logger.LogInformation(user.ToString() + ": button " + callbackData);
                var t = Explorer.FromId(user.Explorer);
                if (callbackData == "donate")
                {
                    var file = new InputOnlineFile(File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "Resources", "DonateQR.jpg")));
                    await Bot.SendPhotoAsync(userId, file, resMgr.Get(Res.DonateInfo, user),
                        ParseMode.Html, replyMarkup: ReplyKeyboards.MainMenu(resMgr, user));
                }

                if (commandsManager.HasCallbackHandler(ev))
                {
                    await commandsManager.ProcessCallbackHandler(sc, ev);
                    return;
                }

                if (callbackData.StartsWith("twdelete "))
                {
                    var twitterMessageId = int.Parse(callbackData.Substring("twdelete ".Length));
                    var twm = db.TwitterMessages.Single(o => o.Id == twitterMessageId);
                    if (twm != null && twm.TwitterId != null)
                    {
                        await twitter.DeleteTweetAsync(twm.TwitterId);
                        db.Remove(twm);
                        db.SaveChanges();
                        await Bot.DeleteMessageAsync(ev.CallbackQuery.Message.Chat.Id, message.MessageId);
                    }
                }

                if (callbackData.StartsWith("deleteaddress"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        ua.IsDeleted = true;
                        db.SaveChanges();
                        string result = resMgr.Get(Res.AddressDeleted, ua);
                        if (!user.HideHashTags)
                            result += "\n\n#deleted" + ua.HashTag();
                        SendTextMessage(db, user.Id, result, null, ev.CallbackQuery.Message.MessageId);
                        NotifyUserActivity(db, $"User {UserLink(user)} deleted [{ua.Address}]({t.account(ua.Address)})");
                    }
                    else
                        SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null,
                            ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("addaddress"))
                {
                    var addr = callbackData.Substring("addaddress ".Length);
                    OnNewAddressEntered(db, user, addr);
                }

                if (callbackData.StartsWith("setthreshold"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        user.UserState = UserState.SetAmountThreshold;
                        user.EditUserAddressId = ua.Id;
                        db.SaveChanges();
                        string result = resMgr.Get(Res.EnterAmountThreshold, ua);
                        if (!user.HideHashTags)
                            result += "\n\n#txthreshold" + ua.HashTag();
                        SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu(resMgr, user));
                    }
                    else
                        SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null,
                            ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("setname"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        user.UserState = UserState.SetName;
                        user.EditUserAddressId = ua.Id;
                        db.SaveChanges();
                        string result = resMgr.Get(Res.EnterNewName, ua);
                        if (!user.HideHashTags)
                            result += "\n\n#rename" + ua.HashTag();
                        SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu(resMgr, user));
                    }
                    else
                        SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null,
                            ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("notifyfollowers"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        var cycles = _serviceProvider.GetService<ITzKtClient>().GetCycles();
                        if (ua.IsOwner && !user.IsAdmin(Config.Telegram) &&
                            cycles.Single(c => c.firstLevel <= prevBlock.Level && prevBlock.Level <= c.lastLevel) ==
                            cycles.Single(c => c.firstLevel <= ua.LastMessageLevel && ua.LastMessageLevel <= c.lastLevel))
                        {
                            SendTextMessage(user.Id, resMgr.Get(Res.OwnerLimitReached, user));
                            return;
						}
                        // TODO: Maybe reuse user var? 
                        user.UserState = UserState.NotifyFollowers;
                        user.EditUserAddressId = ua.Id;
                        db.SaveChanges();

                        var result = resMgr.Get(Res.EnterMessageForAddressFollowers, ua);
                        if (user.IsAdmin(Config.Telegram))
                        {
                            foreach (var follower in GetFollowers(db, ua.Address))
                                result += $"\n{follower} [{follower.Id}]";
                        }
                        SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu(resMgr, user));
                    }
                    else
                        SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null,
                            ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("setdlgthreshold"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        user.UserState = UserState.SetDlgAmountThreshold;
                        user.EditUserAddressId = ua.Id;
                        db.SaveChanges();
                        string result = resMgr.Get(Res.EnterDlgAmountThreshold, ua);
                        if (!user.HideHashTags)
                            result += "\n\n#dlgthreshold" + ua.HashTag();
                        SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu(resMgr, user));
                    }
                    else
                        SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null,
                            ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("change_delegators_balance_threshold "))
                {
                    var ua = useraddr();
                    if (ua == null)
                    {
                        SendTextMessage(db, userId, resMgr.Get(Res.AddressNotExist, user), null,
                            ev.CallbackQuery.Message.MessageId);
                    }
                    else
                    {
                        user.UserState = UserState.SetDelegatorsBalanceThreshold;
                        user.EditUserAddressId = ua.Id;
                        db.SaveChanges();
                        var text = resMgr.Get(Res.EnterDelegatorsBalanceThreshold, ua);
                        SendTextMessage(db, user.Id, text, ReplyKeyboards.BackMenu(resMgr, user));
                    }
                }

                if (callbackData.StartsWith("toggle_payout_notify"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        ua.NotifyPayout = !ua.NotifyPayout;
                        db.SaveChanges();
                        ViewAddress(db, user.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                }
                
                if (callbackData.StartsWith("toggle_delegators_balance"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        ua.NotifyDelegatorsBalance = !ua.NotifyDelegatorsBalance;
                        db.SaveChanges();
                        // TODO: Update message keyboard
                        ViewAddress(db, user.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                }

                if (callbackData == "set_explorer")
                {
                    SendTextMessage(db, user.Id, resMgr.Get(Res.ChooseExplorer, user), ReplyKeyboards.ExplorerSettings(user),
                        ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_explorer_"))
                {
                    var exp = int.Parse(callbackData.Substring("set_explorer_".Length));
                    if (user.Explorer != exp)
                    {
                        user.Explorer = exp;
                        db.SaveChanges();
                    }
                    SendTextMessage(db, user.Id, resMgr.Get(Res.ExplorerChanged, user), null,
                        ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_whalealert"))
                {
                    SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertsTip, user),
                        ReplyKeyboards.WhaleAlertSettings(resMgr, user), ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_wa_"))
                {
                    int wat = int.Parse(callbackData.Substring("set_wa_".Length));
                    user.WhaleAlertThreshold = wat * 1000;
                    db.SaveChanges();
                    SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertSet, user), null, ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_swa_off"))
                {
                    user.SmartWhaleAlerts = false;
                    db.SaveChanges();
                    SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertsTip, user), ReplyKeyboards.WhaleAlertSettings(resMgr, user), ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_swa_on"))
                {
                    user.SmartWhaleAlerts = true;
                    db.SaveChanges();
                    SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertsTip, user), ReplyKeyboards.WhaleAlertSettings(resMgr, user), ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_nialert"))
                {
                    SendTextMessage(db, user.Id, resMgr.Get(Res.NetworkIssueAlertsTip, user),
                        ReplyKeyboards.NetworkIssueAlertSettings(resMgr, user), ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_ni_"))
                {
                    int nin = int.Parse(callbackData.Substring("set_ni_".Length));
                    user.NetworkIssueNotify = nin;
                    db.SaveChanges();
                    SendTextMessage(db, user.Id, resMgr.Get(Res.NetworkIssueAlertSet, user), null,
                        ev.CallbackQuery.Message.MessageId);
                }
                else if (callbackData.StartsWith("set_"))
                {
                    user.Language = callbackData.Substring("set_".Length);
                    db.SaveChanges();
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                    SendTextMessage(db, user.Id, resMgr.Get(Res.Welcome, user), ReplyKeyboards.MainMenu(resMgr, user));
                }

                if (callbackData.StartsWith("manageaddress"))
                {
                    var ua = useraddr();
                    if (ua != null)
                    {
                        ViewAddress(db, user.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                    else
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, user));
                }

                Action<string, Action<UserAddress>> editUA = async (cmd, action) => {
                    if (callbackData.StartsWith(cmd))
                    {
                        var ua = useraddr();
                        if (ua != null)
                        {
                            action(ua);
                            db.SaveChanges();
                            ViewAddress(db, user.Id, ua, ev.CallbackQuery.Message.MessageId)();
                        }
                        else
                            await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, user));
                    }
                };

                editUA("bakingon", ua => ua.NotifyBakingRewards = true);
                editUA("bakingoff", ua => ua.NotifyBakingRewards = false);
                editUA("cycleon", ua => ua.NotifyCycleCompletion = true);
                editUA("cycleoff", ua => ua.NotifyCycleCompletion = false);
                editUA("owneron", ua => ua.IsOwner = true);
                editUA("owneroff", ua => ua.IsOwner = false);
                editUA("rightson", ua => ua.NotifyRightsAssigned = true);
                editUA("rightsoff", ua => ua.NotifyRightsAssigned = false);
                editUA("outoffreespaceon", ua => ua.NotifyOutOfFreeSpace = true);
                editUA("outoffreespaceoff", ua => ua.NotifyOutOfFreeSpace = false);
                editUA("tranon", ua => ua.NotifyTransactions = true);
                editUA("tranoff", ua => ua.NotifyTransactions = false);
                editUA("awardon", ua => ua.NotifyAwardAvailable = true);
                editUA("awardoff", ua => ua.NotifyAwardAvailable = false);
                editUA("dlgon", ua => ua.NotifyDelegations = true);
                editUA("dlgoff", ua => ua.NotifyDelegations = false);
                editUA("misseson", ua => ua.NotifyMisses = true);
                editUA("missesoff", ua => ua.NotifyMisses = false);
                editUA("toggle-delegate-status", ua => ua.NotifyDelegateStatus = !ua.NotifyDelegateStatus);

                if (callbackData.StartsWith("hidehashtags"))
                {
                    user.HideHashTags = true;
                    db.SaveChanges();
                    SendTextMessage(user.Id, resMgr.Get(Res.HashTagsOff, user));
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("showhashtags"))
                {
                    user.HideHashTags = false;
                    db.SaveChanges();
                    SendTextMessage(user.Id, resMgr.Get(Res.HashTagsOn, user));
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                }
                
                if (callbackData.StartsWith("change_currency"))
                {
                    user.Currency = user.Currency == UserCurrency.Usd ? UserCurrency.Eur : UserCurrency.Usd;
                    db.SaveChanges();
                    SendTextMessage(user.Id, resMgr.Get(Res.UserCurrencyChanged, user));
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("showvotingnotify"))
                {
                    user.VotingNotify = true;
                    db.SaveChanges();
                    SendTextMessage(user.Id, resMgr.Get(Res.VotingNotifyChanged, user));
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("hidevotingnotify"))
                {
                    user.VotingNotify = false;
                    db.SaveChanges();
                    SendTextMessage(user.Id, resMgr.Get(Res.VotingNotifyChanged, user));
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("tezos_release_on"))
                {
                    user.ReleaseNotify = true;
                    db.SaveChanges();
                    SendTextMessage(user.Id, resMgr.Get(Res.ReleaseNotifyChanged, user));
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                }

                if (callbackData.StartsWith("tezos_release_off"))
                {
                    user.ReleaseNotify = false;
                    db.SaveChanges();
                    SendTextMessage(user.Id, resMgr.Get(Res.ReleaseNotifyChanged, user));
                    SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(resMgr, user, Config.Telegram),
                        ev.CallbackQuery.Message.MessageId);
                }

                if (Config.Telegram.DevUsers.Contains(user.Username))
                {
                    if (callbackData.StartsWith("broadcast"))
                    {
                        user.UserState = UserState.Broadcast;
                        SendTextMessage(db, user.Id, $"Enter your message for [{user.Language}] bot users",
                            ReplyKeyboards.BackMenu(resMgr, user));
                    }

                    if (callbackData.StartsWith("getuseraddresses"))
                    {
                        OnSql(db, user, "select * from user_address");
                    }

                    if (callbackData.StartsWith("getusermessages"))
                    {
                        OnSql(db, user, "select * from message");
                    }

                    if (callbackData.StartsWith("cmd"))
                    {
                        var cmd = Commands[int.Parse(callbackData.Substring("cmd".Length))];
                        var process = new Process()
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = cmd.filepath,
                                Arguments = cmd.arguments,
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                            }
                        };
                        Logger.LogInformation(UserTitle(ev.CallbackQuery.From) + " started: " + cmd.filepath + " " +
                                              cmd.arguments);
                        try
                        {
                            process.Start();
                            string result = process.StandardOutput.ReadToEnd();
                            process.WaitForExit(10000);
                            Logger.LogInformation(result);
                            int pos = 0;
                            int limit = 4096;
                            do
                            {
                                string resultSplit = result.Substring(pos, Math.Min(limit, result.Length - pos));
                                pos += resultSplit.Length;
                                if (resultSplit.Trim() == "")
                                    continue;
                                SendTextMessage(db, user.Id, resultSplit, ReplyKeyboards.MainMenu(resMgr, user));
                            } while (pos < result.Length);
                        }
                        catch (Exception ex)
                        {
                            LogError(ex);
                            SendTextMessage(db, user.Id, "❗️" + ex.Message, ReplyKeyboards.MainMenu(resMgr, user));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        List<User> GetFollowers(Storage.TezosDataContext db, string addr)
        {
            var di = addrMgr.GetDelegate("", addr);
            var results = db.GetUserAddresses(addr).Select(o => o.User).ToList();
            foreach (var d in di.delegated_contracts)
            {
                var users = db.GetUserAddresses(d);
                foreach (var u in users)
                    if (!results.Contains(u.User))
                        results.Add(u.User);
            }

            return results;
        }

        void OnUpdate(object su, UpdateEventArgs evu)
        {
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();

            if (evu.Update.ChosenInlineResult != null)
            {
                if (evu.Update.ChosenInlineResult.ResultId == "info")
				{
                    return;
				}
                OnNewAddressEntered(db, db.GetUser(evu.Update.ChosenInlineResult.From.Id),
                    evu.Update.ChosenInlineResult.ResultId);
                return;
            }

            if (evu.Update.InlineQuery != null)
            {
                var q = evu.Update.InlineQuery.Query.Trim().ToLower();
                q = q.Replace("'", "").Replace("`", "").Replace(" ", "").Replace("а", "a").Replace("б", "b")
                    .Replace("в", "v").Replace("г", "g").Replace("д", "d").Replace("е", "e").Replace("ж", "zh")
                    .Replace("з", "z").Replace("и", "i").Replace("й", "y").Replace("к", "k").Replace("л", "l")
                    .Replace("м", "m").Replace("н", "n").Replace("о", "o").Replace("п", "p").Replace("р", "r")
                    .Replace("с", "s").Replace("т", "t").Replace("у", "u").Replace("ф", "f").Replace("х", "h")
                    .Replace("ц", "c").Replace("ч", "ch").Replace("ш", "sh").Replace("щ", "sh").Replace("э", "e")
                    .Replace("ю", "u").Replace("я", "ya");
                if (q.Length < 3)
                {
                    string result = $"1 ꜩ = ${1M.TezToUsd(md)} ({mdReceived.ToString("dd.MM.yyyy HH:mm")} UTC)";
                    var results_info = new InlineQueryResultArticle[]{new InlineQueryResultArticle("info", result,
                              new InputTextMessageContent("<b>Tezos blockchain info</b>\n\n" + result + periodStatus + votingStatus +
                              "\n\n@TezosNotifierBot notifies users about transactions and other events in the Tezos blockchain")
                              { ParseMode = ParseMode.Html }){  Description = (periodStatus + votingStatus).Trim(), HideUrl = true} };
                    Bot.AnswerInlineQueryAsync(evu.Update.InlineQuery.Id, results_info, 10);
                    return;
                }
                var ka = db.KnownAddresses.ToList()
                    .Where(o => o.Name.Replace("'", "").Replace("`", "").Replace(" ", "").ToLower().Contains(q))
                    .Select(o => new {o.Address, o.Name});
                var da = db.Delegates.ToList()
                    .Where(o => o.Name != null &&
                                o.Name.Replace("'", "").Replace("`", "").Replace(" ", "").ToLower().Contains(q))
                    .Select(o => new {o.Address, o.Name});
                var results = ka.Union(da).GroupBy(o => o.Address)
                    .Select(o => new {Address = o.Key, Name = o.First().Name}).OrderBy(o => o.Name).Select(o =>
                        new InlineQueryResultArticle(o.Address, o.Name,
                            new InputTextMessageContent($"<i>{o.Address}</i>\n<b>{o.Name}</b>")
                                {ParseMode = ParseMode.Html}) {Description = o.Address});
                //evu.Update.InlineQuery.
                Bot.AnswerInlineQueryAsync(evu.Update.InlineQuery.Id, results.Take(50), 10);
                return;
            }

            if (evu.Update.CallbackQuery != null || evu.Update.InlineQuery != null) return;
            var update = evu.Update;
            var message = update.Message;
            //if (update.ChannelPost != null && update.ChannelPost.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            //{
            //    var user = rep_o.GetUser(update.ChannelPost.From.Id);
            //    if (Regex.IsMatch(update.ChannelPost.Text, "(tz|KT)[a-zA-Z0-9]{34}"))
            //    {
            //        OnNewAddressEntered(user, message.Text, update.ChannelPost.Chat);
            //    }
            //}
            if (update.ChannelPost != null)
            {
                if (update.ChannelPost.Text == "/chatid")
                    Bot.SendTextMessageAsync(update.ChannelPost.Chat.Id, $"Chat Id: {update.ChannelPost.Chat.Id}");
            }

            try
            {
                if (message != null && message.From.IsBot)
                    return;
                var user = message != null ? db.Users.SingleOrDefault(x => x.Id == message.From.Id) : null;

                if (message != null && message.Type == MessageType.Photo &&
                    message.Chat.Type == ChatType.Private &&
                    (user?.UserState == UserState.Broadcast ||
                     user?.UserState == UserState.NotifyFollowers))
                {
                    var count = 0;
                    using (var fileStream = new MemoryStream())
                    {
                        var properSize = message.Photo.OrderByDescending(o => o.Width).First();
                        Bot.GetInfoAndDownloadFileAsync(properSize.FileId, fileStream)
                            .ConfigureAwait(true).GetAwaiter().GetResult();

                        var users = db.Users.Where(o => !o.Inactive && o.Language == user.Language).ToList();
                        if (user.UserState == UserState.NotifyFollowers)
                        {
                            var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
                            users = GetFollowers(db, ua.Address);
                            if (!user.IsAdmin(Config.Telegram))
							{
                                ua.LastMessageLevel = prevBlock.Level;
                                db.SaveChanges();
							}
                        }

                        foreach (var user1 in users)
                        {
                            fileStream.Seek(0, SeekOrigin.Begin);
                            var photo = new InputOnlineFile(fileStream);

                            try
                            {
                                var caption = ApplyEntities(message.Caption, message.CaptionEntities);
                                Bot.SendPhotoAsync(user1.Id, photo, caption: caption, parseMode: ParseMode.Html,
                                        replyMarkup: ReplyKeyboards.MainMenu(resMgr, user1)).ConfigureAwait(true)
                                    .GetAwaiter().GetResult();
                                Thread.Sleep(50);
                                count++;
                            }
                            catch (ChatNotFoundException)
                            {
                                user1.Inactive = true;
                                db.SaveChanges();
                                NotifyUserActivity(db, "😕 User " + UserLink(user1) + " not started chat with bot");
                            }
                            catch (ApiRequestException are)
                            {
                                NotifyUserActivity(db, "🐞 Error while sending message for " + UserLink(user) + ": " +
                                                   are.Message);
                                if (are.Message.StartsWith("Forbidden"))
                                {
                                    user.Inactive = true;
                                    db.SaveChanges();
                                }
                                else
                                    LogError(are);
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message == "Forbidden: bot was blocked by the user")
                                {
                                    user.Inactive = true;
                                    db.SaveChanges();
                                    NotifyUserActivity(db, "😕 Bot was blocked by the user " + UserLink(user));
                                }
                                else
                                    LogError(ex);
                            }
                        }
                    }

                    SendTextMessage(db, user.Id,
                        resMgr.Get(
                            user.UserState == UserState.Broadcast ? Res.MessageDelivered : Res.MessageDeliveredForUsers,
                            user) + " (" + count.ToString() + ")", ReplyKeyboards.MainMenu(resMgr, user));
                    user.UserState = UserState.Default;
                }

                if (message != null && message.Type == MessageType.Text &&
                    message.Chat.Type == ChatType.Private)
                {
                    bool newUser = !db.Users.Any(x => x.Id == message.From.Id);

                    db.LogMessage(message.From, message.MessageId, message.Text, null);
                    user = db.GetUser(message.From.Id);
                    Logger.LogInformation(UserTitle(message.From) + ": " + message.Text);
                    if (newUser)
                        NotifyUserActivity(db, "🔅 New user: " + UserLink(user));
                    bool welcomeBack = false;
                    if (user.Inactive)
                    {
                        user.Inactive = false;
                        db.SaveChanges();
                        welcomeBack = true;
                        NotifyUserActivity(db, "🤗 User " + UserLink(user) + " is back");
                    }
                    if (message.Text.StartsWith("/start"))
                    {
                        if (newUser || welcomeBack)
                            SendTextMessage(db, user.Id,
                                welcomeBack ? resMgr.Get(Res.WelcomeBack, user) : resMgr.Get(Res.Welcome, user),
                                ReplyKeyboards.MainMenu(resMgr, user));
                        var cmd = message.Text.Substring("/start".Length).Replace("_", " ").Trim();
                        if (Regex.IsMatch(cmd, "(tz|KT)[a-zA-Z0-9]{34}"))
                        {
                            var explorer = Explorer.FromStart(message.Text);
                            user.Explorer = explorer.id;
                            db.SaveChanges();
                            var addr = cmd.Replace(explorer.buttonprefix + " ", "");
                            OnNewAddressEntered(db, user, addr);
                        }
                        else if (!newUser && !welcomeBack)
                            SendTextMessage(db, user.Id, resMgr.Get(Res.Welcome, user),
                                ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text.Contains("Tezos blockchain info"))
                    {
                        return;
                    }
                    else if (Config.Telegram.DevUsers.Contains(message.From.Username) &&
                             message.ReplyToMessage != null &&
                             message.ReplyToMessage.Entities.Length > 0 &&
                             message.ReplyToMessage.Entities[0].User != null)
                    {
                        var replyUser = db.GetUser(message.ReplyToMessage.Entities[0].User.Id);
                        SendTextMessage(db, replyUser.Id, resMgr.Get(Res.SupportReply, replyUser) + "\n\n" + message.Text,
                            ReplyKeyboards.MainMenu(resMgr, replyUser));
                        NotifyDev(db,
                            "📤 Message for " + UserLink(replyUser) + " from " + UserLink(user) + ":\n\n" +
                            message.Text.Replace("_", "__").Replace("`", "'").Replace("*", "**").Replace("[", "(")
                                .Replace("]", ")") + "\n\n#outgoing", user.Id);
                    }
                    
                    else if (message.Text == ReplyKeyboards.CmdNewAddress(resMgr, user))
                    {
                        OnNewAddress(db, user);
                    }
                    else if (message.Text == "/outflow_off")
                    {
                        user.SmartWhaleAlerts = false;
                        db.SaveChanges();
                        SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleOutflowOff, user), ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text.StartsWith("/medium ") && user.IsAdmin(Config.Telegram))
                    {
                        var str = message.Text.Substring("/medium ".Length);
                        var tzKtClient = _serviceProvider.GetService<ITzKtClient>();
                        var mw = new Workers.MediumWorker(tzKtClient, _serviceProvider.GetService<IOptions<MediumOptions>>(),
                            _serviceProvider.GetService<IOptions<BotConfig>>(),
                            _serviceProvider.GetService<ILogger<Workers.MediumWorker>>(), _serviceProvider);
                        var cycles = tzKtClient.GetCycles();
                        var currentCycle = cycles.FirstOrDefault(c => c.index.ToString() == str);
                        var prevCycle = cycles.FirstOrDefault(c => c.index == currentCycle.index - 1);
                        try
                        {
                            var result = mw.CreatePost(db, prevCycle, currentCycle);
                            NotifyUserActivity(db, $"New Medium post: [{result.data.title}]({result.data.url})");
                            var tweet = $"Check-out general {prevCycle.index} cycle stats in our blog: {result.data.url}\n\n#Tezos #XTZ #cryprocurrency #crypto #blockchain";
                            twitter.TweetAsync(tweet);
                        }
                        catch (Exception e)
                        {
                            NotifyDev(db, "Failed to create medium post: " + e.Message + "\nAuth token:" + _serviceProvider.GetService<IOptions<MediumOptions>>().Value.AuthToken + "\n" + e.StackTrace, user.Id, ParseMode.Default, true);
                        }
                    }
                    else if (message.Text == "/stop" && user.IsAdmin(Config.Telegram))
                    {
                        paused = true;
                        NotifyDev(db, "Blockchain processing paused", 0);
                    }
                    else if (message.Text == "/resume" && user.IsAdmin(Config.Telegram))
                    {
                        paused = false;
                        NotifyDev(db, "Blockchain processing resumed", user.Id, ParseMode.Default);
                    }
                    else if (message.Text.StartsWith("/forward") && user.IsAdmin(Config.Telegram))
                    {
                        var msgid = int.Parse(message.Text.Substring("/forward".Length).Trim());
                        var msg = db.Messages.FirstOrDefault(o => o.Id == msgid);
                        var m = Bot.ForwardMessageAsync(msg.UserId, msg.UserId, (int)msg.TelegramMessageId)
                            .ConfigureAwait(true).GetAwaiter().GetResult();
                        SendTextMessage(db, user.Id, $"Message forwarded for user {UserLink(db.GetUser(msg.UserId))}",
                            ReplyKeyboards.MainMenu(resMgr, user), parseMode: ParseMode.Markdown);
                        Bot.ForwardMessageAsync(user.Id, msg.UserId, (int)msg.TelegramMessageId);
                    }
                    else if (message.Text.StartsWith("/help") && Config.Telegram.DevUsers.Contains(message.From.Username))
                    {
                        SendTextMessage(db, user.Id, @"Administrator commands:
/sql {query} - run sql query
/block - show current block
/setblock {number} - set last processed block
/names
{addr1} {name1}
{addr2} {name2}
...etc - add public known addresses
/defaultnode - switch to localnode
/setdelegatename {addr} {name} - set delegate name
/addrlist {userid} - view user addresses
/msglist {userid} - view user messages (last month)
/userinfo {userid} - view user info and settings
/set_ru - switch tu russian
/set_en - switch to english
/forward messageid - forward message to user and caller
/twclean - clean published twitter messages
/stop - stop processing blockchain
/resume - resume processing blockchain
/medium {cycle} - post medium article about cycle {cycle}", ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text.StartsWith("/sql") && Config.Telegram.DevUsers.Contains(message.From.Username))
                    {
                        OnSql(db, user, message.Text.Substring("/sql".Length));
                    }
                    else if (message.Text.StartsWith("/devstat") && Config.Telegram.DevUsers.Contains(message.From.Username))
                    {
                        Stream s = GenerateStreamFromString(_serviceProvider.GetService<StatCounter>().ToString());
                        string fileName = "stat.txt";
                        var f = new InputOnlineFile(s, fileName);
                        Bot.SendDocumentAsync(user.Id, f).ConfigureAwait(true).GetAwaiter().GetResult();
                    }
                    else if (message.Text == "/twclean")
                    {
                        int cnt = 0;
                        foreach (var twm in db.TwitterMessages.Where(o => o.CreateDate >= DateTime.Now.AddDays(-7)).OrderBy(o => o.Id).ToList())
                        {
                            if (twm.TwitterId != null)
                            {
                                twitter.DeleteTweetAsync(twm.TwitterId).ConfigureAwait(true).GetAwaiter().GetResult();
                                db.TwitterMessages.Remove(twm);
                                db.SaveChanges();
                                cnt++;
                            }
                        }

                        SendTextMessage(db, user.Id, $"Deleted tweets: {cnt}", ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text == ReplyKeyboards.CmdMyAddresses(resMgr, user) ||
                        message.Text.StartsWith("/list"))
                    {
                        OnMyAddresses(db, message.From.Id, user);
                    }
                    else if (message.Text.StartsWith("/addrlist") &&
                             Config.DevUserNames.Contains(message.From.Username))
                    {
                        if (int.TryParse(message.Text.Substring("/addrlist".Length).Trim(), out int userid))
                        {
                            var u1 = db.GetUser(userid);
                            if (u1 != null)
                                OnMyAddresses(db, message.From.Id, u1);
                            else
                                SendTextMessage(db, user.Id, $"User not found: {userid}",
                                    ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else
                            SendTextMessage(db, user.Id, "Command syntax:\n/addrlist {userid}",
                                ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text.StartsWith("/msglist") && Config.DevUserNames.Contains(message.From.Username))
                    {
                        if (int.TryParse(message.Text.Substring("/msglist".Length).Trim(), out int userid))
                        {
                            OnSql(db, user,
                                $"select * from message where user_id = {userid} and create_date >= 'now'::timestamp - '1 month'::interval order by create_date");
                        }
                        else
                            SendTextMessage(db, user.Id, "Command syntax:\n/msglist {userid}",
                                ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text.StartsWith("/userinfo") &&
                             Config.DevUserNames.Contains(message.From.Username))
                    {
                        if (int.TryParse(message.Text.Substring("/userinfo".Length).Trim(), out int userid))
                        {
                            var u1 = db.GetUser(userid);
                            if (u1 != null)
                            {
                                string result = $"User {u1.ToString()} [{u1.Id}]\n";
                                result += $"Created: {u1.CreateDate.ToString("dd.MM.yyyy HH:mm")}\n";
                                result += $"Explorer: {Explorer.FromId(u1.Explorer)}\n";
                                result += $"Hashtags: {(u1.HideHashTags ? "off" : "on")}\n";
                                result += $"Inactive: {(u1.Inactive ? "yes" : "no")}\n";
                                result += $"Language: {u1.Language}\n";
                                result += $"Network Issue Notify: {u1.NetworkIssueNotify}\n";
                                result += $"Voting Notify: {(u1.VotingNotify ? "on" : "off")}\n";
                                result += $"Whale Alert Threshold: {u1.WhaleAlertThreshold}\n";
                                SendTextMessage(db, user.Id, result, ReplyKeyboards.MainMenu(resMgr, user));
                            }
                            else
                                SendTextMessage(db, user.Id, $"User not found: {userid}",
                                    ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else
                            SendTextMessage(db, user.Id, "Command syntax:\n/addrlist {userid}",
                                ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text == "/res" && Config.DevUserNames.Contains(message.From.Username))
                    {
                        var ua1 = db.GetUserAddresses(user.Id)[0];
                        ua1.AveragePerformance = 92.321M;
                        ua1.Delegators = 123;
                        ua1.FreeSpace = 190910312.123M;
                        ua1.FullBalance = 1392103913.1451M;
                        ua1.Performance = 12.32M;
                        ua1.StakingBalance = 178128312.23M;

                        var ua2 = db.GetUserAddresses(user.Id)[1];
                        var ua3 = db.GetUserAddresses(user.Id)[2];
                        var p = new Domain.Proposal { Hash = "PsDELPH1Kxsxt8f9eWbxQeRxkjfbxoqM52jvs5Y5fBxWWh4ifpo", Name = "Delphi" };
                        p.Delegates = new List<UserAddress>();
                        p.Delegates.Add(ua1);
                        p.Delegates.Add(ua2);
                        p.Delegates.Add(ua3);
                        var contextObject = new ContextObject
                        {
                            Amount = 450_230.123423M,
                            Block = 1_000_230,
                            Cycle = 221,
                            md = md,
                            Minutes = 21,
                            OpHash = "ooqrBVMcUvf6A9RVh7KMhjHVVv9NoMUz5TjRzv8cAubusyKNzor",
                            p = p,
                            Priority = 2,
                            TotalRolls = 90301,
                            u = user,
                            ua = ua1,
                            ua_from = ua2,
                            ua_to = ua3
                        };
                        foreach (Res res in Enum.GetValues(typeof(Res)))
                        {
                            SendTextMessage(db, user.Id, $"#{res.ToString()}\n\n" + resMgr.Get(res, contextObject),
                                ReplyKeyboards.MainMenu(resMgr, user));
                        }
                    }
                    /*
                    else if (message.Text.StartsWith("/perf"))
                    {
                        string addr = null;
                        int cycle = 0;
                        var m = Regex.Match(message.Text, "/perf (\\d*) (tz[a-zA-Z0-9]{34})");
                        if (m.Success)
                        {
                            addr = m.Groups[2].Value;
                            cycle = int.Parse(m.Groups[1].Value);
                        }
                        else
                        {
                            m = Regex.Match(message.Text, "/perf (\\d*) (.*)");
                            if (m.Success)
                            {
                                cycle = int.Parse(m.Groups[1].Value);
                                var q = m.Groups[2].Value.ToLower();
                                q = q.Replace("'", "").Replace("`", "").Replace(" ", "").Replace("а", "a")
                                    .Replace("б", "b").Replace("в", "v").Replace("г", "g").Replace("д", "d")
                                    .Replace("е", "e").Replace("ж", "zh").Replace("з", "z").Replace("и", "i")
                                    .Replace("й", "y").Replace("к", "k").Replace("л", "l").Replace("м", "m")
                                    .Replace("н", "n").Replace("о", "o").Replace("п", "p").Replace("р", "r")
                                    .Replace("с", "s").Replace("т", "t").Replace("у", "u").Replace("ф", "f")
                                    .Replace("х", "h").Replace("ц", "c").Replace("ч", "ch").Replace("ш", "sh")
                                    .Replace("щ", "sh").Replace("э", "e").Replace("ю", "u").Replace("я", "ya");
                                var da = rep_o.GetDelegates()
                                    .Where(o => o.Name != null && o.Name.Replace("'", "").Replace("`", "")
                                        .Replace(" ", "").ToLower().Contains(q)).Select(o => new { o.Address, o.Name });
                                addr = da.Select(o => o.Address).FirstOrDefault();
                            }
                        }

                        if (addr != null && cycle > 0)
                        {
                            var t = Explorer.FromId(user.Explorer);
                            var d = rep_o.GetDelegateName(addr);
                            string result =
                                $"Baking statistics for <a href='{t.account(addr)}'>{d}</a> in cycle {cycle}:\n";
                            var bu = rep_o.GetBalanceUpdates(addr, cycle);
                            result +=
                                $"Baked / Missed: <b>{bu.Count(o => o.Type == 1)} ({(bu.Where(o => o.Type == 1).Sum(o => o.Amount) / 1000000M).TezToString()}) / {bu.Count(o => o.Type == 3)} ({(bu.Where(o => o.Type == 3).Sum(o => o.Amount) / 1000000M).TezToString()})</b>\n";
                            result +=
                                $"Endorsed / Missed: <b>{bu.Where(o => o.Type == 2).Sum(o => o.Slots)} ({(bu.Where(o => o.Type == 2).Sum(o => o.Amount) / 1000000M).TezToString()}) / {bu.Where(o => o.Type == 4).Sum(o => o.Slots)} ({(bu.Where(o => o.Type == 4).Sum(o => o.Amount) / 1000000M).TezToString()})</b>\n";
                            result +=
                                $"Actual / Maximum rewards: <b>{(bu.Where(o => o.Type < 3).Sum(o => o.Amount) / 1000000M).TezToString()} / {(bu.Sum(o => o.Amount) / 1000000M).TezToString()}</b>\n";
                            result += "\nAverage 10-cycle performance calculation\n";
                            result += "<pre>Cycle      Actual      Maximum</pre>\n";
                            decimal rew = 0;
                            decimal max = 0;
                            for (int i = 0; i < 10; i++)
                            {
                                var rew1 = rep_o.GetRewards(addr, cycle - i, false);
                                var max1 = rep_o.GetRewards(addr, cycle - i, true);
                                rew += rew1;
                                max += max1;
                                result +=
                                    $"<pre> {cycle - i} {(rew1 / 1000000).ToString("0.00").PadLeft(12, ' ')} {(max1 / 1000000).ToString("0.00").PadLeft(12, ' ')}</pre>\n";
                            }

                            if (max > 0)
                            {
                                result += $"Average 10-cycle performance: {(100M * rew / max).ToString("#0.000")}%\n";
                            }

                            SendTextMessage(user.Id, result, ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else
                        {
                            SendTextMessage(user.Id, "Command syntax:\n/perf <i>cycle</i> <i>delegate</i>",
                                ReplyKeyboards.MainMenu(resMgr, user));
                        }
                    }
                    */
                    else if (message.Text == "/info" || message.Text == "info")
                    {
                        Info(update);
                    }
                    else if (message.Text == "/stat")
                    {
                        Stat(db, update);
                    }
                    else if (message.Text == "/set_ru" || message.Text == "/set_en")
                    {
                        var lang = message.Text.Replace("/set_", string.Empty);
                        if (lang != null)
                        {
                            user.Language = lang;
                            db.SaveChanges();
                            SendTextMessage(db, user.Id, resMgr.Get(Res.Welcome, user), ReplyKeyboards.MainMenu(resMgr, user));
                        }
                            
                    }
                    else if (message.Text == "/block")
                    {
                        int l = db.GetLastBlockLevel().Item1;
                        //int c = (l - 1) / 4096;
                        //int p = l - c * 4096 - 1;
                        var dtlist = blockProcessings.ToList();
                        var avg = dtlist.Count > 1 ? (int)dtlist.Skip(1).Select((o, i) => o.Subtract(dtlist[i]).TotalSeconds).Average() : double.NaN;
                        var cs = ((MemoryCache)_serviceProvider.GetService<IMemoryCache>()).Count;
                        SendTextMessage(db, user.Id, $"Last block processed: {l}, msh sent: {msgSent}\nAvg. processing time: {avg}\nCache size: {cs}",
                            ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text.StartsWith("/setblock") &&
                             Config.DevUserNames.Contains(message.From.Username))
                    {
                        if (int.TryParse(message.Text.Substring("/setblock ".Length), out int num))
                        {
                            var tzKt = _serviceProvider.GetService<ITzKtClient>();
                            var b = tzKt.GetBlock(num);
                            var lbl = db.LastBlock.Single();
                            lbl.Level = num;
                            lbl.Priority = b.blockRound;
                            lbl.Hash = b.Hash;
                            db.SaveChanges();
                            lastBlockChanged = true;
                            var c = tzKt.GetCycles().Single(c => c.firstLevel <= num && num <= c.lastLevel);
                            NotifyDev(db,
                                $"Last block processed changed: {db.GetLastBlockLevel().Item1}, {db.GetLastBlockLevel().Item3}\nCurrent cycle: {c.index}, totalStaking: {c.totalStaking}",
                                0);
                            _currentConstants = null;
                        }
                    }
                    else if (message.Text.StartsWith("/names") && Config.Telegram.DevUsers.Contains(message.From.Username))
                    {
                        var t = Explorer.FromId(0);
                        var data = message.Text.Substring("/names".Length).Trim().Split('\n');
                        string result = "";
                        foreach (var item in data)
                        {
                            var dnMatch = Regex.Match(item, "((tz|KT)[a-zA-Z0-9]{34})(.*)");
                            if (dnMatch.Success)
                            {
                                string addr = dnMatch.Groups[1].Value.Trim();
                                string name = dnMatch.Groups[3].Value.Trim();
                                var d = db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
                                if (d == null)
                                {
                                    d = new KnownAddress(addr, name);
                                    db.KnownAddresses.Add(d);
                                }

                                d.Name = name;
                                db.SaveChanges();
                                result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
                            }
                        }

                        if (result != "")
                            NotifyDev(db, "Names assigned:\n\n" + result, 0, ParseMode.Html);
                    }
                    //else if (Config.DevUserNames.Contains(message.From.Username) &&
                    //         message.Text.StartsWith("/processmd"))
                    //{
                    //    var md = _nodeManager.Client.GetBlockMetadata(message.Text.Substring("/processmd ".Length));
                    //    ProcessBlockMetadata(md, message.Text.Substring("/processmd ".Length));
                    //}
                    //else if (Config.DevUserNames.Contains(message.From.Username) && message.Text == "/defaultnode")
                    //{
                    //    _nodeManager.SwitchTo(0);
                    //    NotifyDev("Switched to " + _nodeManager.Active.Name, 0);
                    //}
                    //else if (Config.DevUserNames.Contains(message.From.Username) && message.Text.StartsWith("/node"))
                    //{
                    //    var n = message.Text.Substring(5);
                    //    if (int.TryParse(n, out int n_i) && n_i < _nodeManager.Nodes.Length)
                    //    {
                    //        _nodeManager.SwitchTo(n_i);
                    //        NotifyDev("Switched to " + _nodeManager.Active.Name, 0);
                    //    }
                    //    else
                    //    {
                    //        var result = $"Current node: {_nodeManager.Active.Name}\n\n";
                    //        for (var i = 0; i < _nodeManager.Nodes.Length; i++)
                    //        {
                    //            var node = _nodeManager.Nodes[i];
                    //            result += $"/node{i} = {node.Name}\n<b>Status:</b> {_nodeManager.GetStatus(node)}\n\n";
                    //        }

                    //        SendTextMessage(user.Id, result, ReplyKeyboards.MainMenu(resMgr, user));
                    //    }
                    //}
                    else if (Config.Telegram.DevUsers.Contains(message.From.Username) &&
                             message.Text.StartsWith("/loaddelegatelist"))
                    {
                        //LoadDelegateList();
                        SendTextMessage(db, user.Id, "Not implemented", ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (Config.Telegram.DevUsers.Contains(message.From.Username) &&
                             message.Text.StartsWith("/setdelegatename"))
                    {
                        var dn = message.Text.Substring("/setdelegatename ".Length);
                        var dnMatch = Regex.Match(dn, "(tz[a-zA-Z0-9]{34})(.*)");
                        if (dnMatch.Success)
                        {
                            string addr = dnMatch.Groups[1].Value.Trim();
                            string name = dnMatch.Groups[2].Value.Trim();
                            var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
                            if (d == null)
                            {
                                d = new Domain.Delegate { Address = addr };
                                db.Delegates.Add(d);
                            }

                            d.Name = name;
                            db.SaveChanges();
                            SendTextMessage(db, user.Id, $"👑 {addr}: <b>{name}</b>",
                                ReplyKeyboards.MainMenu(resMgr, user));
                        }
                    }
                    else if (commandsManager.HasUpdateHandler(evu))
                    {
                        commandsManager.ProcessUpdateHandler(su, evu)
                            .ConfigureAwait(true).GetAwaiter().GetResult();
                    }
                    else if (message.Text.StartsWith("/add") && !Regex.IsMatch(message.Text, "(tz|KT)[a-zA-Z0-9]{34}"))
					{
                        OnNewAddress(db, user);
					}
                    else if (message.Text.StartsWith("/trnthreshold"))
					{
                        if (Regex.IsMatch(message.Text, "(tz|KT)[a-zA-Z0-9]{34}"))
                        {
                            var msg = message.Text.Substring($"/trnthreshold ".Length);
                            string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                            string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
                            if (long.TryParse(threshold, out long t))
                            {
                                var ua = db.GetUserTezosAddress(user.Id, addr);
                                if (ua.Id != 0)
                                {
                                    ua.AmountThreshold = t;
                                    db.SaveChanges();
                                    SendTextMessage(db, user.Id, resMgr.Get(Res.ThresholdEstablished, ua), null);
                                    return;
                                }
                            }
                        }

                        SendTextMessage(user.Id, $"Use <b>trnthreshold</b> command with Tezos address and the transaction amount (XTZ) threshold for this address. For example::\n/trnthreshold <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
                    }
                    else if (message.Text.StartsWith("/dlgthreshold"))
                    {
                        if (Regex.IsMatch(message.Text, "(tz|KT)[a-zA-Z0-9]{34}"))
                        {
                            var msg = message.Text.Substring($"/dlgthreshold ".Length);
                            string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                            string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
                            if (long.TryParse(threshold, out long t))
                            {
                                var ua = db.GetUserTezosAddress(user.Id, addr);
                                if (ua.Id != 0)
                                {
                                    ua.DelegationAmountThreshold = t;
                                    db.SaveChanges();
                                    SendTextMessage(db, user.Id, resMgr.Get(Res.DlgThresholdEstablished, ua), null);
                                    return;
                                }
                            }
                        }

                        SendTextMessage(user.Id, $"Use <b>dlgthreshold</b> command with Tezos address and the delegation amount (XTZ) threshold for this address. For example::\n/dlgthreshold <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
                    }
                    else if (Regex.IsMatch(message.Text, "(tz|KT)[a-zA-Z0-9]{34}") &&
                             user.UserState != UserState.Broadcast && user.UserState != UserState.Support &&
                             user.UserState != UserState.NotifyFollowers)
                    {
                        OnNewAddressEntered(db, user, message.Text.Replace("/add", ""));
                    }
                    else if (message.Text == ReplyKeyboards.CmdGoBack(resMgr, user))
                    {
                        SendTextMessage(db, user.Id, resMgr.Get(Res.SeeYou, user), ReplyKeyboards.MainMenu(resMgr, user));
                    }
                    else if (message.Text == ReplyKeyboards.CmdContact(resMgr, user))
                    {
                        user.UserState = UserState.Support;
                        SendTextMessage(db, user.Id, resMgr.Get(Res.WriteHere, user),
                            ReplyKeyboards.BackMenu(resMgr, user));
                        return;
                    }
                    else if (message.Text == ReplyKeyboards.CmdSettings(resMgr, user) ||
                        message.Text.StartsWith("/settings"))
                    {
                        SendTextMessage(db, user.Id, resMgr.Get(Res.Settings, user).Substring(2), ReplyKeyboards.Settings(resMgr, user, Config.Telegram));
                    }
                    else
                    {
                        if (user.UserState == UserState.Support)
                        {
                            user.UserState = UserState.Default;

                            var dialog = _serviceProvider.GetRequiredService<DialogService>();
                            var (action, answer) = dialog.Intent(user.Id.ToString(), message.Text, user.Culture);
                            // TODO: Add `action == input.unknown` handling
                            SendTextMessage(db, user.Id, answer,
                                ReplyKeyboards.MainMenu(resMgr, user));

                            var messageBuilder = new MessageBuilder();

                            messageBuilder.AddLine("💌 Message from " + UserLink(user) + ":\n" + message.Text
                                .Replace("_", "__")
                                .Replace("`", "'").Replace("*", "**").Replace("[", "(").Replace("]", ")"));

                            messageBuilder.AddEmptyLine();
                            messageBuilder.AddLine($"The answer: {answer}");

                            if (action == "input.unknown")
                            {
                                messageBuilder.AddEmptyLine();
                                messageBuilder.AddLine(Config.Telegram.DevUsers.Select(x => '@' + x).Join(" "));
                            }
                            
                            messageBuilder.WithHashTag("inbox");
                            
                            NotifyDev(db, messageBuilder.Build(), 0);
                        }
                        else if (user.UserState == UserState.SetAmountThreshold)
                        {
                            var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
                            if (ua != null && decimal.TryParse(message.Text.Replace(" ", "").Replace(",", "."),
                                out decimal amount) && amount >= 0)
                            {
                                ua.AmountThreshold = amount;
                                db.SaveChanges();
                                SendTextMessage(db, user.Id, resMgr.Get(Res.ThresholdEstablished, ua),
                                    ReplyKeyboards.MainMenu(resMgr, user));
                            }
                            else
                                SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user),
                                    ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else if (user.UserState == UserState.SetDlgAmountThreshold)
                        {
                            var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
                            if (ua != null && decimal.TryParse(message.Text.Replace(" ", "").Replace(",", "."),
                                out decimal amount) && amount >= 0)
                            {
                                ua.DelegationAmountThreshold = amount;
                                db.SaveChanges();
                                SendTextMessage(db, user.Id, resMgr.Get(Res.DlgThresholdEstablished, ua),
                                    ReplyKeyboards.MainMenu(resMgr, user));
                            }
                            else
                                SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user),
                                    ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else if (user.UserState == UserState.SetDelegatorsBalanceThreshold)
                        {
                            var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
                            if (ua != null && decimal.TryParse(message.Text.Replace(" ", "").Replace(",", "."),
                                out var amount) && amount >= 0)
                            {
                                ua.DelegatorsBalanceThreshold = amount;
                                db.SaveChanges();
                                SendTextMessage(db, user.Id, resMgr.Get(Res.ChangedDelegatorsBalanceThreshold, ua),
                                    ReplyKeyboards.MainMenu(resMgr, user));
                            }
                            else
                                SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user),
                                    ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else if (user.UserState == UserState.SetName)
                        {
                            var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
                            if (ua != null)
                            {
                                ua.Name = Regex.Replace(message.Text.Trim(), "<.*?>", "");
                                db.SaveChanges();
                                string result = resMgr.Get(Res.AddressRenamed, ua);
                                if (!ua.User.HideHashTags)
                                    result += "\n\n#rename" + ua.HashTag();
                                SendTextMessage(db, user.Id, result, ReplyKeyboards.MainMenu(resMgr, user));
                            }
                            else
                                SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user),
                                    ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else if (user.UserState == UserState.NotifyFollowers)
                        {
                            var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
                            string text = ApplyEntities(message.Text, message.Entities);
                            if (!user.IsAdmin(Config.Telegram))
                            {
                                ua.LastMessageLevel = prevBlock.Level;
                                db.SaveChanges();
                                text = resMgr.Get(Res.DelegateMessage, ua) + "\n\n" + text;
                            }
                            int count = 0;
                            foreach (var u1 in GetFollowers(db, ua.Address))
                            {
                                var tags = !u1.HideHashTags ? "\n\n#delegate_message" + ua.HashTag() : "";
                                SendTextMessage(db, u1.Id, text + tags, ReplyKeyboards.MainMenu(resMgr, user), disableNotification: true);
                                count++;
                            }
                            SendTextMessage(db, user.Id, resMgr.Get(Res.MessageDeliveredForUsers, new ContextObject { u = user, Amount = count }),
                                ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else if (user.UserState == UserState.Broadcast)
                        {
                            int count = 0;
                            string text = ApplyEntities(message.Text, message.Entities);
                            foreach (var user1 in db.Users.Where(o => !o.Inactive).ToList())
                            {
                                if (user1.Language == user.Language)
                                {
                                    SendTextMessage(db, user1.Id, text, ReplyKeyboards.MainMenu(resMgr, user1),
                                        disableNotification: true);
                                    count++;
                                }
                            }

                            user.UserState = UserState.Default;
                            SendTextMessage(db, user.Id,
                                resMgr.Get(Res.MessageDelivered, user) + "(" + count.ToString() + ")",
                                ReplyKeyboards.MainMenu(resMgr, user));
                        }
                        else
                        {
                            SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user) + ": " + message.Text,
                                ReplyKeyboards.MainMenu(resMgr, user));
                        }
                    }

                    user.UserState = UserState.Default;
                }
                if ((message?.Type == MessageType.Text || update.ChannelPost?.Type == MessageType.Text) &&
                    (message?.Chat.Type == ChatType.Group || message?.Chat.Type == ChatType.Supergroup ||
                    update.ChannelPost != null))
                {
                    bool newChat = db.GetUser(message?.Chat.Id ?? update.ChannelPost.Chat.Id) == null;
                    var chat = message?.Chat ?? update.ChannelPost.Chat;
                    var from = message?.From ?? update.ChannelPost.From;
                    if (from?.IsBot ?? false)
                        return;
                    int messageId = message?.MessageId ?? update.ChannelPost.MessageId;
                    string messageText = message?.Text ?? update.ChannelPost.Text;
                    messageText = messageText.Replace($"@{botUserName}", "");
                    if (!messageText.StartsWith("/info") &&
                        !messageText.StartsWith("/settings") &&
                        !messageText.StartsWith("/add") &&
                        !messageText.StartsWith("/list") &&
                        !messageText.StartsWith("/trnthreshold") &&
                        !messageText.StartsWith("/dlgthreshold") &&
                        !Regex.IsMatch(messageText, "(tz|KT)[a-zA-Z0-9]{34}"))
                        return;
                    db.LogMessage(chat, messageId, messageText, null);
                    user = db.GetUser(chat.Id);
                    Logger.LogInformation(ChatTitle(chat) + ": " + messageText);
                    if (newChat)
                        NotifyUserActivity(db, "👥 New chat: " + ChatLink(user));
    
                    if (messageText.StartsWith("/info"))
                    {
                        Info(update);
                    }

                    if (messageText.StartsWith("/settings"))
					{
                        if (update.ChannelPost != null ||
                            Bot.GetChatAdministratorsAsync(chat.Id).ConfigureAwait(true).GetAwaiter().GetResult().Any(m => m.User.Id == from.Id))
                            SendTextMessage(db, user.Id, resMgr.Get(Res.Settings, user).Substring(2), ReplyKeyboards.Settings(resMgr, user, Config.Telegram));
                    }

                    if (messageText.StartsWith("/add") || (Regex.IsMatch(messageText, "(tz|KT)[a-zA-Z0-9]{34}") && update.ChannelPost == null))
                    {
                        if (update.ChannelPost != null ||
                            Bot.GetChatAdministratorsAsync(chat.Id).ConfigureAwait(true).GetAwaiter().GetResult().Any(m => m.User.Id == from.Id))
						{
                            if (Regex.IsMatch(messageText, "(tz|KT)[a-zA-Z0-9]{34}"))
                            {
                                string addr = Regex.Matches(messageText, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                                OnNewAddressEntered(db, user, messageText.Substring(messageText.IndexOf(addr)));
                            }
                            else
                                SendTextMessage(user.Id, $"Use <b>add</b> command with Tezos address and the title for this address (optional). For example::\n/add@{botUserName} <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf Аrthur</i>");
                        }
                    }

                    if (messageText.StartsWith("/list"))
					{
                        if (update.ChannelPost != null ||
                            Bot.GetChatAdministratorsAsync(chat.Id).ConfigureAwait(true).GetAwaiter().GetResult().Any(m => m.User.Id == from.Id))
                        {
                            OnMyAddresses(db, chat.Id, user);
                        }
                    }

                    if (messageText.StartsWith("/trnthreshold"))
                    {
                        if (update.ChannelPost != null ||
                            Bot.GetChatAdministratorsAsync(chat.Id).ConfigureAwait(true).GetAwaiter().GetResult().Any(m => m.User.Id == from.Id))
                        {
                            if (Regex.IsMatch(messageText, "(tz|KT)[a-zA-Z0-9]{34}"))
                            {
                                var msg = messageText.Substring($"/trnthreshold ".Length);
                                string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                                string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
                                if (long.TryParse(threshold, out long t))
                                {
                                    var ua = db.GetUserTezosAddress(chat.Id, addr);
                                    if (ua.Id != 0)
                                    {
                                        ua.AmountThreshold = t;
                                        db.SaveChanges();
                                        SendTextMessage(db, chat.Id, resMgr.Get(Res.ThresholdEstablished, ua), null);
                                        return;
                                    }
                                }
                            }
                            
                            SendTextMessage(user.Id, $"Use <b>trnthreshold</b> command with Tezos address and the transaction amount (XTZ) threshold for this address. For example::\n/trnthreshold@{botUserName} <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
                        }
                    }

                    if (messageText.StartsWith("/dlgthreshold"))
                    {
                        if (update.ChannelPost != null ||
                            Bot.GetChatAdministratorsAsync(chat.Id).ConfigureAwait(true).GetAwaiter().GetResult().Any(m => m.User.Id == from.Id))
                        {
                            if (Regex.IsMatch(messageText, "(tz|KT)[a-zA-Z0-9]{34}"))
                            {
                                var msg = messageText.Substring($"/dlgthreshold ".Length);
                                string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                                string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
                                if (long.TryParse(threshold, out long t))
                                {
                                    var ua = db.GetUserTezosAddress(chat.Id, addr);
                                    if (ua.Id != 0)
                                    {
                                        ua.DelegationAmountThreshold = t;
                                        db.SaveChanges();
                                        SendTextMessage(db, chat.Id, resMgr.Get(Res.DlgThresholdEstablished, ua), null);
                                        return;
                                    }
                                }
                            }

                            SendTextMessage(user.Id, $"Use <b>dlgthreshold</b> command with Tezos address and the delegation amount (XTZ) threshold for this address. For example::\n/dlgthreshold@{botUserName} <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
                        }
                    }
                    /*
                    var chatAdmins = Bot.GetChatAdministratorsAsync(message.Chat.Id).ConfigureAwait(true).GetAwaiter().GetResult();
                    if (Regex.IsMatch(message.Text, "(tz|KT)[a-zA-Z0-9]{34}"))
                    {
                        if (chatAdmins.Any(o => o.User.Id == message.From.Id))
                        {
                            var u = rep_o.GetUser(message.From.Id);
                            if (message.ForwardFrom == null)
                                OnNewAddressEntered(u, message.Text, message.Chat);
                            else
                            {
                                var nameMatch = Regex.Match(message.Text, "👑?(.*)?\\s*([tK][zT][a-zA-Z0-9]{34})");
                                OnNewAddressEntered(u, nameMatch.Groups[2].Value + " " + nameMatch.Groups[1].Value, message.Chat);
                            }
                        }
                    }*/
                }

                if (message?.Type == MessageType.Document && Config.Telegram.DevUsers.Contains(message.From.Username))
                {
                    Upload(db, message, db.GetUser(message.From.Id));
                }
            }
            catch (Exception e)
            {
                LogError(e);
                try
                {
                    NotifyDev(db, "‼️ " + e.Message, 0);
                }
                catch
                {
                }
            }
        }

        string ApplyEntities(string text, MessageEntity[] entities)
        {
            if (entities == null)
                return text;
            string result = "";
            int currentIndex = 0;
            foreach (var e in entities)
            {
                if (e.Type == MessageEntityType.Bold ||
                    e.Type == MessageEntityType.Italic ||
                    e.Type == MessageEntityType.TextLink ||
                    e.Type == MessageEntityType.Pre)
                {
                    if (currentIndex < e.Offset)
                        result += text.Substring(currentIndex, e.Offset - currentIndex);
                    if (e.Type == MessageEntityType.Bold)
                        result += $"<b>{text.Substring(e.Offset, e.Length)}</b>";
                    if (e.Type == MessageEntityType.Italic)
                        result += $"<i>{text.Substring(e.Offset, e.Length)}</i>";
                    if (e.Type == MessageEntityType.Pre)
                        result += $"<pre>{text.Substring(e.Offset, e.Length)}</pre>";
                    if (e.Type == MessageEntityType.TextLink)
                        result += $"<a href='{e.Url}'>{text.Substring(e.Offset, e.Length)}</a>";
                    currentIndex = e.Offset + e.Length;
                }
            }

            if (currentIndex < text.Length + 1)
                result += text.Substring(currentIndex);
            return result;
        }

        void Info(Update update)
        {
            var chatId = update.Message.Chat?.Id ?? update.Message.From.Id;
            string result = $"1 <b>ꜩ</b> = ${1M.TezToUsd(md)} ({mdReceived.ToString("dd.MM.yyyy HH:mm")})" + periodStatus + votingStatus;
            //var bh = _nodeManager.Client.GetBlockHeader(lastHash);
            //var bm = _nodeManager.Client.GetBlockMetadata(lastHash);
            //result += $"#{bh.level} ({bh.timestamp.ToString("dd.MM.yyyy HH:mm:ss")})\n";
            //if (bm.voting_period_kind == "proposal")
            //{
            //    result += "Голосование: период подачи предложений\n";
            //    var proposals = _nodeManager.Client.GetProposals(lastHash);
            //    if (proposals.Count == 0)
            //        result += "Ни одного предложения не поступило";
            //    else
            //        result += "Текущие предложения: " + String.Join("; ",
            //            proposals.Select(o => (rep_o.GetProposal(o.Key)?.Name ?? o.Key) + $" - {o.Value} rolls")
            //                .ToArray());
            //}

            Bot.SendTextMessageAsync(chatId, result, ParseMode.Html).ConfigureAwait(true).GetAwaiter().GetResult();
        }

        void Stat(Storage.TezosDataContext db, Update update)
        {
            var chatId = update.Message.Chat?.Id ?? update.Message.From.Id;
            string result = $"Active users: {db.Users.Count(o => !o.Inactive)}\n";
            result += $"Monitored addresses: {db.UserAddresses.Where(o => !o.IsDeleted && !o.User.Inactive).Select(o => o.Address).Distinct().Count()}\n";

            Bot.SendTextMessageAsync(chatId, result, ParseMode.Html).ConfigureAwait(true).GetAwaiter().GetResult();
        }


        #region Commands

        void OnNewAddress(Storage.TezosDataContext db, User user)
        {
            SendTextMessage(db, user.Id, resMgr.Get(Res.NewAddressHint, user), ReplyKeyboards.Search(resMgr, user));
        }

        void OnSql(Storage.TezosDataContext db, User u, string sql)
        {
            try
            {
                var res = db.RunSql(sql);
                string allData = String.Join("\r\n", res.Select(o => String.Join(';', o)).ToArray());
                if (res[0].Length <= 3 && res.Count <= 20)
                    SendTextMessage(db, u.Id, allData, ReplyKeyboards.MainMenu(resMgr, u));
                else
                {
                    Stream s = GenerateStreamFromString(allData);
                    string fileName = "result.txt";
                    if (allData.Length > 100000)
                    {
                        s = Utils.CreateZipToMemoryStream(s, "result.txt");
                        fileName = "result.zip";
                    }

                    var f = new InputOnlineFile(s, fileName);
                    Bot.SendDocumentAsync(u.Id, f).ConfigureAwait(true).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                SendTextMessage(db, u.Id, e.Message, ReplyKeyboards.MainMenu(resMgr, u));
            }
        }

        void OnNewAddressEntered(Storage.TezosDataContext db, User user, string msg, Telegram.Bot.Types.Chat chat = null)
        {
            Bot.SendChatActionAsync(chat?.Id ?? user.Id, ChatAction.Typing);
            string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
            var nameMatch = Regex.Match(msg, "([^ ]* )?.*(tz|KT)[a-zA-Z0-9]{34}[^a-zA-Z0-9а-яА-Я<]*(.*)");
            var name = nameMatch.Success
                ? (nameMatch.Groups[3].Value.Trim() != ""
                    ? nameMatch.Groups[3].Value.Trim()
                    : nameMatch.Groups[1].Value.Trim())
                : "";
            if (name == addr)
                name = addr.ShortAddr().Replace("…", "");
            name = Regex.Replace(name, "<.*?>", "");
            if (String.IsNullOrEmpty(name))
                name = db.GetKnownAddressName(addr);
            if (String.IsNullOrEmpty(name))
                name = db.GetDelegateName(addr).Replace("…", "");
            try
            {
                var ci = addrMgr.GetContract(prevBlock.Hash, addr);
                var t = Explorer.FromId(user.Explorer);
                if (ci != null)
                {
                    decimal bal = ci.balance / 1000000M;
                    (UserAddress ua, DelegateInfo di) = NewUserAddress(db, user, addr, name, bal, chat?.Id ?? 0);
                    string result = resMgr.Get(Res.AddressAdded, ua) + "\n";

                    result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
                    if (di != null)
                    {
                        ua.FullBalance = di.Bond / 1000000;
                        result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
                        ua.StakingBalance = di.staking_balance / 1000000;
                        ua.Delegators = di.NumDelegators;
                        result += resMgr.Get(Res.StakingInfo, ua) + "\n";
                        //result += FreeSpace(ua);
                    }

                    if (ci.@delegate != null && di == null)
                    {
                        string delname = db.GetDelegateName(ci.@delegate);
                        result += resMgr.Get(Res.Delegate, ua) +
                                  $": <a href='{t.account(ci.@delegate)}'>{delname}</a>\n";
                    }

                    if (!user.HideHashTags)
                        result += "\n#added" + ua.HashTag();
                    if (chat == null)
                    {
                        SendTextMessage(db, user.Id, result, ReplyKeyboards.MainMenu(resMgr, user));
                        NotifyUserActivity(db, $"🔥 User {UserLink(user)} added [{addr}]({t.account(addr)})" +
                                           (!String.IsNullOrEmpty(name)
                                               ? $" as **{name.Replace("_", "__").Replace("`", "'")}**"
                                               : "") + $" (" + bal.TezToString() + ")");
                    }
                    else
                    {
                        SendTextMessage(chat.Id, result);
                        NotifyUserActivity(db, $"🔥 User {UserLink(user)} added [{addr}]({t.account(addr)})" +
                                           (!String.IsNullOrEmpty(name)
                                               ? $" as **{name.Replace("_", "__").Replace("`", "'")}**"
                                               : "") + $" (" + bal.TezToString() + ")" +
                                           (chat != null
                                               ? $" to group [{chat.Title}](https://t.me/{chat.Username})"
                                               : ""));
                    }
                }
                else
                {
                    if (chat == null)
                        SendTextMessage(db, user.Id, resMgr.Get(Res.IncorrectTezosAddress, user),
                            ReplyKeyboards.MainMenu(resMgr, user));
                    else
                        SendTextMessage(chat.Id, resMgr.Get(Res.IncorrectTezosAddress, user));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error on adding \"{msg}\":\n{e.Message}");
                if (chat == null)
                    SendTextMessage(db, user.Id, resMgr.Get(Res.IncorrectTezosAddress, user),
                        ReplyKeyboards.MainMenu(resMgr, user));
                else
                    SendTextMessage(chat.Id, resMgr.Get(Res.IncorrectTezosAddress, user));
            }
        }

        private void LinkAddress()
        {
        }

        /*string FreeSpace(UserAddress ua)
        {
            var c = _currentConstants;
            if (c == null)
                return "";
            //how much tez can be locked in total (by all bakers) as a security deposit
            var totalLocked = (c.blockDeposit + c.endorsementDeposit * c.endorsersPerBlock) *
                              c.blocksPerCycle * (c.preservedCycles + 1);

            //how much of that the baker can cover with his balance
            var bakerBalance = ua.FullBalance * 1000000;
            var bakerShare = bakerBalance / totalLocked;

            //number of rolls, participating in staking
            var totalRolls = _serviceProvider.GetService<ITzKtClient>()
                .GetCycles().Single(c => c.firstLevel <= prevBlock.Level && prevBlock.Level <= c.lastLevel ).totalRolls;

            //how many rolls and staking balance the baker should have in order to lock the whole balance
            var bakerRollsCapacity = totalRolls * bakerShare;
            var bakerStakingCapacity = bakerRollsCapacity * c.tokensPerRoll;

            decimal maxStakingThreshold = 1;
            var maxStakingBalance = bakerStakingCapacity * maxStakingThreshold;
            var freeSpace = (maxStakingBalance - ua.StakingBalance * 1000000) / 1000000M;
            Logger.LogDebug(
                $"FreeSpace calc for {ua.Address}. totalLocked:{totalLocked}; bakerBalance:{bakerBalance}, bakerShare:{bakerShare}, totalRolls:{totalRolls}, bakerStakingCapacity:{bakerStakingCapacity}, maxStakingBalance:{maxStakingBalance}, currentStakingBalance:{ua.StakingBalance}");
            ua.FreeSpace = freeSpace;
            return resMgr.Get(Res.FreeSpace, ua) + "\n";
        }*/

        Action ViewAddress(Storage.TezosDataContext db, long chatId, UserAddress ua, int msgid)
        {
            var user = ua.User;
            var culture = new CultureInfo(user.Language);

            var t = Explorer.FromId(ua.User.Explorer);
            var isDelegate = db.Delegates.Any(o => o.Address == ua.Address);
            var result = chatId == ua.UserId ? "" : $"ℹ️User {ua.User} [{ua.UserId}] address\n";
            var config = db.Set<AddressConfig>().AsNoTracking().FirstOrDefault(x => x.Id == ua.Address);
            result += isDelegate ? $"{config?.Icon ?? "👑"} " : "";
            if (!String.IsNullOrEmpty(ua.Name))
                result += "<b>" + ua.Name + "</b>\n";
            result += $"<a href='{t.account(ua.Address)}'>" + ua.Address + "</a>\n";
            var ci = addrMgr.GetContract(prevBlock?.Hash, ua.Address);
            if (ci != null)
                ua.Balance = ci.balance / 1000000M;

            result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
            if (ci.@delegate != null && !isDelegate)
            {
                string delname = db.GetDelegateName(ci.@delegate);
                result += resMgr.Get(Res.Delegate, ua) + $": <a href='{t.account(ci.@delegate)}'>{delname}</a>\n";
            }
            /*
            var bcd = _serviceProvider.GetService<IBetterCallDevClient>();
            var bcdAcc = bcd.GetAccount(ua.Address);
            if (bcdAcc.balances.Count > 0)
            {
                result += resMgr.Get(Res.Tokens, ua) +
                          String.Join(", ",
                              bcdAcc.balances.Where(t => (t.symbol != null || (t.contract ?? "").Length > 32) && t.Balance > 0).Select(t =>
                                  $"<b>{t.Balance.ToString("###,###,###,###,##0.########", CultureInfo.InvariantCulture)}</b> {(t.symbol ?? t.contract.ShortAddr())}"));
                result += "\n";
            }*/

            if (isDelegate)
            {
                try
                {
                    var di = addrMgr.GetDelegate(prevBlock?.Hash, ua.Address);
                    ua.FullBalance = di.Bond / 1000000;
                    result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
                    ua.StakingBalance = di.staking_balance / 1000000;
                    ua.Delegators = di.NumDelegators;
                    result += resMgr.Get(Res.StakingInfo, ua) + "\n";
                    var tzKtClient = _serviceProvider.GetService<ITzKtClient>();
                    if (currentCycle != null)
					{
                        long rew = 0;
                        long rewMax = 0;
                        for (int i = 0; i < 10; i++)
                        {
                            var r = tzKtClient.GetBakerRewards(ua.Address, currentCycle.index - i);
                            rew += r?.TotalBakerRewards ?? 0;
                            rewMax += (r?.TotalBakerRewardsPlan ?? 0) + (r?.TotalBakerLoss ?? 0);
                        }
                        if (rewMax > 0)
						{
                            ua.AveragePerformance = 100M * rew / rewMax;
                            result += resMgr.Get(Res.AveragePerformance, new ContextObject { Cycle = currentCycle.index - 9, Period = 9, ua = ua, u = ua.User }) + "\n";
                        }
					}
                    
                    //result += FreeSpace(ua);
                    //decimal? perf = addrMgr.GetAvgPerformance(repo, ua.Address);
                    //if (perf.HasValue)
                    //{
                    //    ua.AveragePerformance = perf.Value;
                    //    result += resMgr.Get(Res.AveragePerformance, ua) + "\n";
                    //}
                }
                catch
                {
                }
            }

            // Display information on Tune mode only
            if (msgid != 0)
            {
                var tzkt = _serviceProvider.GetRequiredService<ITzKtClient>();
                var lastSeen = tzkt.GetAccountLastSeen(ua.Address);
                var lastActive = tzkt.GetAccountLastActive(ua.Address);
                if (lastSeen != null)
                    result += Format(Res.LastSeen, (DateTime) lastSeen);

                if (lastActive != null)
                    result += Format(Res.LastActive, (DateTime) lastActive);

                string Format(Res key, DateTime timestamp)
                {
                    var label = resMgr.Get(key, ua);
                    return $"{label}: <b>{timestamp.Humanize(culture: culture)}</b>\n";
                }
            }


            if (ua.ChatId != 0)
            {
                try
                {
                    var chat = Bot.GetChatAsync(ua.ChatId).ConfigureAwait(true).GetAwaiter().GetResult();
                    result += resMgr.Get(Res.NotifyIn, ua.User) +
                              $"<a href=\"https://t.me/{chat.Username}\">{chat.Title}</a>\n";
                }
                catch
                {
                }
            }

            if (msgid == 0)
            {
                result += resMgr.Get(Res.Events, ua);
                if (ua.NotifyTransactions)
                {
                    result += "✅❎";
                    if (ua.AmountThreshold > 0)
                        result += "✂️";
                }

                if (ua.NotifyPayout && !isDelegate)
                    result += "🤑";
                //if (ua.NotifyAwardAvailable && !isDelegate)
                //    result += "🧊";
                if (ua.NotifyDelegateStatus && !isDelegate)
                    result += "🌚";
            }
            else
            {
                result += resMgr.Get(Res.TransactionNotifications, ua) + "\n";
                result += resMgr.Get(Res.AmountThreshold, ua) + "\n";

                if (!isDelegate)
                    result += resMgr.Get(Res.PayoutNotifyStatus, ua) + "\n";
                //if (!isDelegate)
                //    result += resMgr.Get(Res.AwardAvailableNotifyStatus, ua) + "\n";
                if (!isDelegate)
                    result += resMgr.Get(Res.NotifyDelegateInactive, ua) + "\n";
            }

            if (isDelegate)
            {
                if (msgid == 0)
                {
                    if (ua.NotifyDelegations)
                    {
                        result += "🤝👋";
                        if (ua.DelegationAmountThreshold > 0)
                            result += "✂️";
                    }

                    if (ua.NotifyBakingRewards)
                        result += "💰";

                    if (ua.NotifyCycleCompletion)
                        result += "🏁";

                    if (ua.NotifyMisses)
                        result += "🤷🏻‍♂️";

                    if (ua.NotifyDelegatorsBalance && ua.User.Type == 0)
                    {
                        result += "🔺";
                        if (ua.DelegatorsBalanceThreshold > 0)
                            result += "✂️";
                    }
                    if (ua.NotifyRightsAssigned)
                        result += "👉";
                    if (ua.NotifyOutOfFreeSpace)
                        result += "🙅";
                }
                else
                {
                    result += resMgr.Get(Res.DelegationNotifications, ua) + "\n";
                    result += resMgr.Get(Res.DelegationAmountThreshold, ua) + "\n";
                    if (ua.User.Type == 0)
                    {
                        result += resMgr.Get(Res.DelegatorsBalanceNotifyStatus, ua) + "\n";
                        result += resMgr.Get(Res.DelegatorsBalanceThreshold, ua) + "\n";
                    }
                    result += resMgr.Get(Res.RewardNotifications, ua) + "\n";
                    result += resMgr.Get(Res.CycleCompletionNotifications, ua) + "\n";
                    result += resMgr.Get(Res.MissesNotifications, ua) + "\n";
                    result += resMgr.Get(Res.DelegateRightsAssigned, ua) + "\n";
                    result += resMgr.Get(Res.DelegateOutOfFreeSpace, ua) + "\n";
                    result += resMgr.Get(Res.Watchers, ua) + db.GetUserAddresses(ua.Address).Count + "\n";
                }

                if (!ua.User.HideHashTags)
                    // One new line for `address tune` and two for `inline mode`
                    // TODO: Change `result` from string to StringBuilder
                    result += new string('\n', msgid == 0 ? 2 : 1) + ua.HashTag();
                return () => SendTextMessage(db, chatId, result,
                    chatId == ua.UserId
                        ? ReplyKeyboards.AddressMenu(resMgr, ua.User, ua.Id.ToString(), msgid == 0 ? null : ua,
                            Config.Telegram)
                        : ReplyKeyboards.AdminAddressMenu(resMgr, ua), msgid);
            }
            else
            {
                if (!ua.User.HideHashTags)
                    result += new string('\n', msgid == 0 ? 2 : 1) + ua.HashTag();
                string name = "";
                if (ci?.@delegate != null && !db.GetUserAddresses(ua.UserId).Any(o => o.Address == ci.@delegate))
                    name = db.GetDelegateName(ci.@delegate);
                return () => SendTextMessage(db, chatId, result,
                    chatId == ua.UserId
                        ? ReplyKeyboards.AddressMenu(resMgr, ua.User, ua.Id.ToString(), msgid == 0 ? null : ua,
                            new Tuple<string, string>(name, ci?.@delegate))
                        : null, msgid);
            }
        }

        void OnMyAddresses(Storage.TezosDataContext db, long chatId, User user)
        {
            var addresses = db.GetUserAddresses(user.Id);
            if (addresses.Count == 0)
                SendTextMessage(db, user.Id, resMgr.Get(Res.NoAddresses, user), ReplyKeyboards.MainMenu(resMgr, user));
            else

            {
                List<Action> results = new List<Action>();
                foreach (var ua in addresses)
                {
                    Bot.SendChatActionAsync(chatId, ChatAction.Typing);
                    results.Add(ViewAddress(db, chatId, ua, 0));
                }

                foreach (var r in results)
                    r();
            }
        }

        (UserAddress, DelegateInfo) NewUserAddress(Storage.TezosDataContext db, User user, string addr, string name, decimal balance, long chatId)
        {
            var ua = db.AddUserAddress(user, addr, balance, name, chatId);
            DelegateInfo di = null;
            try
            {
                if (addr.StartsWith("tz"))
                {
                    try
                    {
                        di = addrMgr.GetDelegate(prevBlock.Hash, addr);
                        if (di != null)
                        {
                            if (!db.Delegates.Any(o => o.Address == addr))
                            {
                                db.Delegates.Add(new Domain.Delegate {
                                    Address = addr,
                                    Name = addr.ShortAddr()
                                });
                                db.SaveChanges();
                                NotifyUserActivity(db, $"💤 New delegate {addr} monitored");
                            }
                        }
                    }
                    catch (WebException)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }

            return (ua, di);
        }

        #endregion

        public void NotifyDev(Storage.TezosDataContext db, string text, long currentUserID, ParseMode parseMode = ParseMode.Markdown, bool current = false)
        {
            foreach (var devUser in Config.Telegram.DevUsers)
            {
                var user = db.Users.SingleOrDefault(o => o.Username == devUser);
                if (user != null && ((user.Id != currentUserID && !current) || (user.Id == currentUserID && current)))
                {
                    while (text.Length > 4096)
                    {
                        int lastIndexOf = text.Substring(0, 4096).LastIndexOf('\n');
                        SendTextMessage(db, user.Id, text.Substring(0, lastIndexOf), ReplyKeyboards.MainMenu(resMgr, user),
                            parseMode: parseMode);
                        text = text.Substring(lastIndexOf + 1);
                    };

                    if (text != "")
                        SendTextMessage(db, user.Id, text, ReplyKeyboards.MainMenu(resMgr, user), parseMode: parseMode);
                }
            }
        }

        void SendTextMessage(long chatId, string text)
        {
            try
            {
                Logger.LogInformation($"->{chatId}: {text}");
                Bot.SendTextMessageAsync(chatId, text, ParseMode.Html, disableWebPagePreview: true).ConfigureAwait(true).GetAwaiter()
                    .GetResult();
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        void PushTextMessage(Storage.TezosDataContext db, UserAddress ua, string text)
        {
            PushTextMessage(db, ua.UserId, text);
        }
        void PushTextMessage(Storage.TezosDataContext db, long userId, string text)
        {
            db.Add(Domain.Message.Push(userId, text));
            db.SaveChanges();
        }
        void SendTextMessageUA(Storage.TezosDataContext db, UserAddress ua, string text)
        {
            if (ua.ChatId == 0)
                SendTextMessage(db, ua.UserId, text, ReplyKeyboards.MainMenu(resMgr, ua.User));
            else
                SendTextMessage(ua.ChatId, text);
        }

        int msgSent = 0;
        public int SendTextMessage(Storage.TezosDataContext db, long userId, string text, IReplyMarkup keyboard, int replaceId = 0,
            ParseMode parseMode = ParseMode.Html, bool disableNotification = false)
        {
            var u = db.GetUser(userId);
            if (u.Inactive)
                return 0;
            try
            {
                Logger.LogInformation("->" + u.ToString() + ": " + text);
                if (replaceId == 0)
                {
                    Message msg = Bot
                            .SendTextMessageAsync(userId, text, parseMode, disableWebPagePreview: true, disableNotification: disableNotification, replyMarkup: keyboard)
                            .ConfigureAwait(true).GetAwaiter().GetResult();
                        db.LogOutMessage(userId, msg.MessageId, text);
                    Thread.Sleep(50);
                    msgSent++;
					return msg.MessageId;
                }
                else
                {
                    var msg = Bot
                        .EditMessageTextAsync(userId, replaceId, text, parseMode, disableWebPagePreview: true,
                            replyMarkup: (InlineKeyboardMarkup) keyboard).ConfigureAwait(true).GetAwaiter().GetResult();
                    db.LogOutMessage(userId, msg.MessageId, text);
                    return msg.MessageId;
                }
            }
            catch (MessageIsNotModifiedException)
            {
            }
            catch (ChatNotFoundException)
            {
                u.Inactive = true;
                db.SaveChanges();
                NotifyDev(db, "😕 User " + UserLink(u) + " not started chat with bot", userId);
            }
            catch (BadRequestException bre)
			{
                if(bre.Message.Contains("no rights to send"))
				{
                    u.Inactive = true;
                    db.SaveChanges();
                    NotifyDev(db, "😕 Bot have no rights to send a message for " + UserLink(u), userId);
                }
                else
                    LogError(bre);
            }
            catch (ApiRequestException are)
            {
                NotifyDev(db, "🐞 Error while sending message for " + UserLink(u) + ": " + are.Message, userId);
                if (are.Message.StartsWith("Forbidden"))
                {
                    u.Inactive = true;
                    db.SaveChanges();
                }
                else if (are.Message.Contains("group chat was upgraded to a supergroup chat"))
                {
                    u.Inactive = true;
                    db.SaveChanges();
                }
                else
                    LogError(are);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Forbidden: bot was blocked by the user")
                {
                    u.Inactive = true;
                    db.SaveChanges();
                    NotifyDev(db, "😕 Bot was blocked by the user " + UserLink(u), userId);
                }
                else
                    LogError(ex);
            }

            return 0;
        }

        public void NotifyUserActivity(Storage.TezosDataContext db, string text)
        {
            foreach (var userId in Config.Telegram.ActivityChat)
            {
                try
                {
                    if (userId > 0)
                    {
                        var u = db.GetUser((int) userId);
                        Bot.SendTextMessageAsync(userId, text, ParseMode.Markdown, disableWebPagePreview: true,
                                replyMarkup: ReplyKeyboards.MainMenu(resMgr, u)).ConfigureAwait(true).GetAwaiter()
                            .GetResult();
                    }
                    else
                        Bot.SendTextMessageAsync(userId, text, ParseMode.Markdown, disableWebPagePreview: true).ConfigureAwait(true)
                            .GetAwaiter().GetResult();

                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    NotifyDev(db, $"🐞 Error while sending message for chat {userId}: " + ex.Message, 0);
                    LogError(ex);
                }
            }
        }

        void LogError(Exception e)
        {
            string msg = "";
            while (e != null)
            {
                msg += e.GetType().Name + ": ";
                msg += e.Message + Environment.NewLine;
                msg += e.StackTrace + Environment.NewLine + Environment.NewLine;
                e = e.InnerException;
            }

            Logger.LogError(e, msg);
        }

        string UserTitle(Telegram.Bot.Types.User u)
        {
            return (u.FirstName + " " + u.LastName).Trim() +
                   (!String.IsNullOrEmpty(u.Username) ? " @" + u.Username + "" : "");
        }
        string ChatTitle(Telegram.Bot.Types.Chat c)
        {
            return c.Title +
                   (!String.IsNullOrEmpty(c.Username) ? " @" + c.Username + "" : "");
        }

        string UserLink(User u)
        {
            return $"[{(u.Firstname + " " + u.Lastname).Trim()}](tg://user?id={u.Id}) [[{u.Id}]]";
        }

        string ChatLink(User c)
        {
            return $"{c.Title} [[{c.Id}]]";
		}

        Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        async void Upload(Storage.TezosDataContext db, Message message, User user)
        {
            try
            {
                using (var fileStream = new MemoryStream())
                {
                    var fileInfo = await Bot.GetInfoAndDownloadFileAsync(
                        fileId: message.Document.FileId,
                        destination: fileStream
                    );
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.UploadPath ?? "Upload");
                    Directory.CreateDirectory(path);
                    // Распаковать архив
                    if (message.Document.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        string result = "";
                        using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                var destination = Path.Combine(path, entry.FullName);
                                entry.ExtractToFile(destination);
                                result += "\n🔹 " + destination;
                            }

                            NotifyDev(db, "🖇 Files uploaded by " + UserLink(user) + ":" + result, 0);
                        }
                    }
                    else
                    {
                        var destination = Path.Combine(path, message.Document.FileName);
                        File.WriteAllBytes(destination, fileStream.GetBuffer());
                        NotifyDev(db, "📎 File uploaded by " + UserLink(user) + ": " + destination, 0);
                    }
                }
            }
            catch (Exception e)
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, "Данные не загружены: " + e.Message);
            }
        }

        private T GetService<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}