﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Model;
using TezosNotifyBot.Tzkt;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Events;
using TezosNotifyBot.Tezos;
using TezosNotifyBot.CryptoCompare;
using Microsoft.AspNetCore.Mvc;

namespace TezosNotifyBot
{
    public partial class TezosProcessing : BackgroundService
	{
		readonly IServiceProvider serviceProvider;
		readonly TelegramBotInvoker telegramBotInvoker;
		readonly ILogger<TezosProcessing> logger;
		readonly TezosBot tezosBot;
		readonly AddressManager addrMgr;
		readonly ResourceManager resMgr;
		readonly BotConfig config;
		static readonly Queue<DateTime> blockProcessings = new Queue<DateTime>();

		public TezosProcessing(ILogger<TezosProcessing> logger, TelegramBotInvoker telegramBotInvoker, IServiceProvider serviceProvider, TezosBot tezosBot, AddressManager addrMgr, ResourceManager resMgr, IOptions<BotConfig> config)
		{
			this.serviceProvider = serviceProvider;
			this.logger = logger;
			this.telegramBotInvoker = telegramBotInvoker;
			this.tezosBot = tezosBot;
			this.addrMgr = addrMgr;
			this.resMgr = resMgr;
			this.config = config.Value;
		}

		//public TezosProcessing(IServiceProvider serviceProvider,

		//	IOptions<BotConfig> config,
		//	ResourceManager resourceManager,
		//	CommandsManager commandsManager,
		//	TezosBotFacade botClient,
		//	TelegramBotInvoker telegramBotInvoker,
		//	TelegramBotHandler telegramBotHandler)
		//{
		//	this.telegramBotInvoker = telegramBotInvoker;
		//	_serviceProvider = serviceProvider;
		//	Logger = logger;
		//	Config = config.Value;

		//	this.commandsManager = commandsManager;
		//	this.botClient = botClient;

		//	addrMgr = _serviceProvider.GetRequiredService<AddressManager>();
		//	resMgr = resourceManager;
		//	this.telegramBotHandler = telegramBotHandler;
		//	this.telegramBotHandler.OnChosenInlineResult = OnChosenInlineResult;
		//	this.telegramBotHandler.OnCallbackQuery = OnCallbackQuery;
		//	this.telegramBotHandler.OnInlineQuery = OnInlineQuery;
		//	this.telegramBotHandler.OnChannelPost = OnChannelPost;
		//	this.telegramBotHandler.OnMessage = OnMessage;
		//}

		static bool paused = false;
		public static bool Paused { set { paused = value; } }

		DateTime lastReceived = DateTime.UtcNow; //Дата и время получения последнего блока
		DateTime lastWebExceptionNotify = DateTime.MinValue;
		DateTime lastWarn = DateTime.UtcNow;
		static Block prevBlock;
		Constants _currentConstants;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (stoppingToken.IsCancellationRequested is false)
			{
				if (paused)
				{
					Thread.Sleep(5000);
					continue;
				}

				using var scope = serviceProvider.CreateScope();
				var provider = scope.ServiceProvider;
				using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
				var tzkt = serviceProvider.GetRequiredService<ITzKtClient>();
				var md = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>().GetMarketData();
				try
				{
					var block = db.GetLastBlockLevel();
					var blockNum = prevBlock?.Level ?? block.Item1;
					if (!await Client_BlockReceived(db, tzkt, blockNum + 1, md))
						Thread.Sleep(5000);

					if (DateTime.UtcNow.Subtract(lastReceived).TotalMinutes > 5 &&
						DateTime.UtcNow.Subtract(lastWarn).TotalMinutes > 10)
					{
						await tezosBot.NotifyDev(db,
							$"‼️ Last block {block} received {(int)DateTime.UtcNow.Subtract(lastReceived).TotalMinutes} minutes ago, check node‼️",
							0);
						lastWarn = DateTime.UtcNow;
					}
				}
				catch (WebException ex)
				{
					if (DateTime.UtcNow.Subtract(lastWebExceptionNotify).TotalMinutes > 5)
					{
						logger.LogError(ex, ex.Message);
						await tezosBot.NotifyDev(db, $"‼️ WebException: " + ex.Message, 0);
						lastWebExceptionNotify = DateTime.UtcNow;
					}

					Thread.Sleep(1000);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, ex.Message);
					await tezosBot.NotifyDev(db, $"‼️{ex.Message}\n🧱{prevBlock.Level + 1}", 0);
					serviceProvider.GetService<IMemoryCache>().Clear();
					Thread.Sleep(10000);
				}
			}
		}

		static bool lastBlockChanged;
		public static void SetLastBlock(Storage.TezosDataContext db, Block block)
		{
			prevBlock = block;
			lastBlockChanged = true;
			db.SetLastBlockLevel(block.Level, block.blockRound, block.Hash);
		}

		async Task<bool> Client_BlockReceived(Storage.TezosDataContext db, ITzKtClient tzKt, int blockLevel, MarketData md)
		{
			lastReceived = DateTime.UtcNow;
			var tzKtHead = tzKt.GetHead();
			logger.LogDebug($"TzKt level: {tzKtHead.level}, known level: {tzKtHead.knownLevel}");
			if (tzKtHead.level < blockLevel + 1)
				return false;

			var block = tzKt.GetBlock(blockLevel);
			logger.LogDebug($"Block {block.Level} received");

			if ((prevBlock != null && prevBlock.Level != block.Level - 1) || _currentConstants == null)
				_currentConstants = tzKt.GetCurrentProtocol().constants;
			if (prevBlock == null)
				prevBlock = tzKt.GetBlock(blockLevel - 1);

			await ProcessBlockBakingData(db, block, tzKt);

			await ProcessBlockMetadata(db, block, tzKt);

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
			await ProcessTransactions(db, block.Transactions, fromToAmountHash, allUsers, md);
			foreach (var t in fromToAmountHash.Where(o => o.amount >= 10000 && o.token == null))
				db.AddWhaleTransaction(t.from, t.to, block.Level, block.Timestamp, t.amount, t.hash);

			await ProcessDelegations(db, block.Delegations);
			await ProcessOriginations(db, block.Originations);

			var fromGroup = fromToAmountHash.Where(o => o.from != "").GroupBy(o => new { o.from, o.token });
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
						var di = addrMgr.GetDelegate(from.Key.from);
						if (di != null)
							fromBalance = di.Bond / 1000000;
						else
							fromBalance = addrMgr.GetContract(from.Key.from).balance /
										  1000000M;
					}
					else
					{
						fromBalance = addrMgr.GetContract(from.Key.from).balance /
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
							new ContextObject {
								u = ua.User,
								OpHash = from_ua.Single().Item4,
								Block = block.Level,
								Amount = from_ua.Sum(o => o.Item3),
								md = md,
								ua_from = ua,
								ua_to = ua_to,
								Token = from.Key.token
							}) + "\n";
						tags = ua_to.HashTag();
					}
					else
					{
						result = resMgr.Get(Res.OutgoingTransactions,
							new ContextObject {
								u = ua.User,
								ua = ua,
								Block = block.Level,
								Amount = from_ua.Sum(o => o.Item3),
								md = md,
								Token = from.Key.token
							}) + "\n";
						int cnt = 0;
						foreach (var to in from_ua.OrderByDescending(o => o.Item3))
						{
							cnt++;
							var targetAddr = db.GetUserTezosAddress(ua.UserId, to.Item2);
							result += resMgr.Get(Res.To,
										  new ContextObject { u = ua.User, Amount = to.Item3, ua = targetAddr, Token = to.token }) +
									  "\n";
							if (!tags.Contains(targetAddr.HashTag()) && (cnt < 6 || targetAddr.UserId == ua.UserId))
								tags += targetAddr.HashTag();
							if (cnt > 40)
							{
								result += resMgr.Get(Res.NotAllShown,
									new ContextObject { u = ua.User, Block = block.Level }) + "\n";
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
							new ContextObject { u = ua.User, Amount = tokenBalance, Token = from.Key.token }) + "\n";
					}

					if (!ua.User.HideHashTags)
						result += "\n#outgoing" +
								  (from.Key.token != null ? " #" + from.Key.token.Symbol.ToLower() : "") +
								  ua.HashTag() + tags;
					await tezosBot.SendTextMessageUA(db, ua, result);
				}
			}

			var toGroup = fromToAmountHash.GroupBy(o => new { o.to, o.token });
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
						var di = addrMgr.GetDelegate(to.Key.to);
						if (di != null)
							toBalance = (di?.Bond ?? 0) / 1000000;
						else
							toBalance = addrMgr.GetContract(to.Key.to).balance /
										1000000M;
					}
					else
					{
						toBalance = addrMgr.GetContract(to.Key.to).balance / 1000000M;
					}
				}

				var amount = to.Sum(o => o.Item3);

				var contract = addrMgr.GetContract(to.Key.to);
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

					var receiver = new UserAddress {
						Address = receiverAddr,
						Balance = addrMgr.GetBalance(receiverAddr)
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
						var textData = new ContextObject {
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
							text.AppendLine(string.Join(" ", tags.Select(x => x.Trim())));
						}

						tezosBot.PushTextMessage(db, delegateAddress, text.ToString());
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
								new ContextObject {
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
								new ContextObject {
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
							new ContextObject {
								u = ua.User,
								ua = ua,
								Block = block.Level,
								Amount = to_ua.Sum(o => o.Item3),
								md = md,
								Token = to.Key.token
							}) + "\n";
						int cnt = 0;
						foreach (var from in to_ua.OrderByDescending(o => o.Item3))
						{
							cnt++;
							var sourceAddr = db.GetUserTezosAddress(ua.UserId, from.Item1);
							result += resMgr.Get(Res.From,
								new ContextObject { u = ua.User, Amount = from.Item3, ua = sourceAddr, Token = from.token }) + "\n";
							if (!tags.Contains(sourceAddr.HashTag()) && (cnt < 6 || sourceAddr.UserId == ua.UserId))
								tags += sourceAddr.HashTag();
							if (cnt > 40)
							{
								result += resMgr.Get(Res.NotAllShown,
									new ContextObject { u = ua.User, Block = block.Level }) + "\n";
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
							new ContextObject { u = ua.User, Amount = tokenBalance, Token = to.Key.token }) + "\n";
					}

					if (!ua.User.HideHashTags)
						result += $"\n{operationTag}" + (to.Key.token != null ? " #" + to.Key.token.Symbol.ToLower() : "") +
								  ua.HashTag() + tags;
					await tezosBot.SendTextMessageUA(db, ua, result);
				}
			}

			if (prevBlock == null || prevBlock.Level + 1 == block.Level)
				db.SetLastBlockLevel(block.Level, block.blockRound, block.Hash);
			logger.LogInformation($"Block {block.Level} processed");
			//lastHeader = header;
			//lastHash = header.hash;
			if (lastBlockChanged)
			{
				lastBlockChanged = false;
				await tezosBot.NotifyDev(db, $"Switched to block {prevBlock.Level} ({prevBlock.Timestamp})", 0);
				logger.LogInformation($"Switched to block {prevBlock.Level} ({prevBlock.Timestamp})");
			}
			else
				prevBlock = block;
			blockProcessings.Enqueue(DateTime.UtcNow);
			if (blockProcessings.Count > 21)
				blockProcessings.Dequeue();
			
			return true;
		}

		async Task ProcessTransactions(Storage.TezosDataContext db, List<Transaction> ops, List<(string from, string to, decimal amount, string hash, Token token)> fromToAmountHash, List<Domain.User> allUsers, MarketData md)
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
								new ContextObject {
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

							await tezosBot.SendTextMessage(db, u.Id, result, ReplyKeyboards.MainMenu);
						}
					}
				}
			}
		}

		async Task ProcessDelegations(Storage.TezosDataContext db, List<Delegation> ops)
		{
			foreach (var op in ops)
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
							new ContextObject {
								u = ua.User,
								OpHash = op.Hash,
								Amount = op.Amount / 1000000M,
								ua_from = ua,
								ua_to = targetAddr
							});
						if (!ua.User.HideHashTags)
							result += "\n\n#delegation" + targetAddr.HashTag() + ua.HashTag();
						await tezosBot.SendTextMessageUA(db, ua, result);
					}

					foreach (var ua in db.GetUserAddresses(to).Where(o => o.NotifyDelegations))
					{
						if (ua.DelegationAmountThreshold > op.Amount / 1000000M)
							continue;
						var sourceAddr = db.GetUserTezosAddress(ua.UserId, from);
						string result = resMgr.Get(Res.NewDelegation,
							new ContextObject {
								u = ua.User,
								OpHash = op.Hash,
								Amount = op.Amount / 1000000M,
								ua_from = sourceAddr,
								ua_to = ua
							});
						if (!ua.User.HideHashTags)
							result += "\n\n#delegation" + sourceAddr.HashTag() + ua.HashTag();
						await tezosBot.SendTextMessageUA(db, ua, result);
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
							new ContextObject {
								u = ua.User,
								OpHash = op.Hash,
								Amount = op.Amount / 1000000M,
								ua_from = sourceAddr,
								ua_to = ua
							});
						if (!ua.User.HideHashTags)
							result += "\n\n#leave_delegate" + sourceAddr.HashTag() + ua.HashTag();
						await tezosBot.SendTextMessageUA(db, ua, result);
					}
				}
			}
		}
		async Task ProcessOriginations(Storage.TezosDataContext db, List<Origination> ops)
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
						new ContextObject { u = ua.User, OpHash = op.Hash, Amount = amount, ua_from = ua, ua_to = targetAddr });
					if (!ua.User.HideHashTags)
						result += "\n\n#delegation" + targetAddr.HashTag() + ua.HashTag();
					await tezosBot.SendTextMessageUA(db, ua, result);
				}

				foreach (var ua in db.GetUserAddresses(to).Where(o => o.NotifyDelegations))
				{
					if (ua.DelegationAmountThreshold > amount)
						continue;
					var sourceAddr = db.GetUserTezosAddress(ua.UserId, op.OriginatedContract.address);
					string result = resMgr.Get(Res.NewDelegation,
						new ContextObject { u = ua.User, OpHash = op.Hash, Amount = amount, ua_from = sourceAddr, ua_to = ua });
					if (!ua.User.HideHashTags)
						result += "\n\n#delegation " + sourceAddr.HashTag() + ua.HashTag();
					await tezosBot.SendTextMessageUA(db, ua, result);
				}
			}
		}

		async Task ProcessBlockBakingData(Storage.TezosDataContext db, Block block, ITzKtClient tzktClient)
		{
			logger.LogDebug($"ProcessBlockBakingData {block.Level}");

			var missedRights = tzktClient.GetRights(block.Level, "missed");
			foreach (var right in missedRights)
			{
				var uaddrs = db.GetUserAddresses(right.baker.address);

				foreach (var ua in uaddrs.Where(o => o.NotifyMisses))
				{
					if (ua.DownStart == null)
					{
						ua.DownStart = block.Timestamp;
						ua.DownStartLevel = block.Level;
					}
					ua.DownEnd = null;

					if (block.Timestamp >= ua.DownStart.Value.AddMinutes((double)ua.MissesThreshold))
					{
						var result = $"🤷🏻‍♂️ Delegate <a href='{t.account(ua.Address)}'>{ua.DisplayName()}</a> has started missing blocks as of {ua.DownStart.Value.ToString("MMM dd, hh:mm tt")} at block <a href='{t.block(ua.DownStartLevel ?? 0)}'>{ua.DownStartLevel}</a>";
						if (!ua.DownMessageId.HasValue /*|| block.Timestamp.Subtract(ua.LastUpdate).TotalMinutes > 4*/ || block.Timestamp < ua.LastUpdate)
						{
							//if (ua.DownMessageId.HasValue)
							//	result += $"\nContinues missing as of {block.Timestamp.ToString("MMM dd, hh:mm tt")}";
							if (!ua.User.HideHashTags)
								result += "\n\n#missed" + ua.HashTag();
							ua.DownMessageId = await tezosBot.SendTextMessageUA(db, ua, result, 0);
							//result = "! " + result;
							//ua.DownMessageId = await tezosBot.SendTextMessageUA(db, ua, result, ua.DownMessageId ?? 0);
							ua.LastUpdate = block.Timestamp;
						}
					}
					await db.SaveChangesAsync();
				}
			}

			var activeDelegates = block.Endorsements.Select(o => o.@delegate.address).ToList();
			activeDelegates.Add(block.producer.address);
			foreach(var addr in activeDelegates)
			{
				var uaddrs = db.UserAddresses.Include(x => x.User).Where(o => o.Address == addr && !o.IsDeleted && !o.User.Inactive && o.NotifyMisses && o.DownStart.HasValue).ToList();
				foreach (var ua in uaddrs)
				{
					if (ua.DownEnd == null)
					{
						ua.DownEnd = block.Timestamp;
						ua.DownEndLevel = block.Level;
					}

					if (block.Timestamp >= ua.DownEnd.Value.AddMinutes((double)ua.MissesThreshold))
					{
						if (!ua.DownMessageId.HasValue)
						{
							ua.DownStart = null;
							ua.DownEnd = null;
						}
						else
						{
							var result = $"☀️ Delegate <a href='{t.account(ua.Address)}'>{ua.DisplayName()}</a> has resumed block production as of {ua.DownEnd.Value.ToString("MMM dd, hh:mm tt")}, at block <a href='{t.block(ua.DownEndLevel ?? 0)}'>{ua.DownEndLevel}</a>";
							if (!ua.User.HideHashTags)
								result += "\n\n#missed" + ua.HashTag();
							//if (block.Timestamp.Subtract(ua.LastUpdate).TotalMinutes < 4)
							//{
							//	result = $"🤷🏻‍♂️ Delegate <a href='{t.account(ua.Address)}'>{ua.DisplayName()}</a> has started missing blocks as of {ua.DownStart.Value.ToString("MMM dd, hh:mm tt")}\n\n" +
							//		result;
							//	await tezosBot.SendTextMessageUA(db, ua, result, ua.DownMessageId.Value);
							//}
							//else
								await tezosBot.SendTextMessageUA(db, ua, result, 0);
							ua.DownMessageId = null;
						}
					}
					await db.SaveChangesAsync();
				}
			}

			logger.LogInformation($"Block {block.Level} baking data processed");
		}

		class RewardMsg
		{
			public Domain.User User;
			public string Message;
			public string Tags;
			public UserAddress UserAddress;
		}

		Cycle currentCycle;
		async Task ProcessBlockMetadata(Storage.TezosDataContext db, Block block, ITzKtClient tzKtClient)
		{
			logger.LogDebug($"ProcessBlockMetadata {block.Level}");
			var cycles = tzKtClient.GetCycles();
			var cycle = cycles.SingleOrDefault(c => c.firstLevel <= block.Level && block.Level <= c.lastLevel);
			if (cycle == null)
			{
				cycle = tzKtClient.GetCycle(block.Cycle);
			}
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
			//await LoadAddressList(tzKtClient, db);
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
							new ContextObject {
								ua = ua,
								u = ua.User,
								Block = penalty.missedLevel,
								Amount = penalty.TotalLost
							});
						if (!ua.User.HideHashTags)
							result += "\n\n#missed_revelation" + ua.HashTag();
						
						tezosBot.PushTextMessage(db, ua, result);
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

				var dispatcher = serviceProvider.GetService<IEventDispatcher>();

				await dispatcher.Dispatch(new CycleCompletedEvent());

				logger.LogDebug($"Calc delegates performance on {block.Level - 1}");

				var cyclePast = cycles.Single(o => o.index == cycle.index - 1);
				Dictionary<string, Rewards> rewards = new Dictionary<string, Rewards>();
				foreach (var d in uad.Where(o => o.NotifyCycleCompletion).GroupBy(o => o.Address))
					rewards.Add(d.Key, tzKtClient.GetBakerRewards(d.Key, cyclePast.index));
				
				logger.LogDebug($"Calc delegates performance on {block.Level - 1} finished");
				// TODO: TNB-22

				await tezosBot.LoadAddressList(tzKtClient, db);
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

			await VotingNotify(db, block, cycle, tzKtClient);

			logger.LogDebug($"ProcessBlockMetadata {block.Level} completed");
		}

		public static double GetAvgProcessingTime()
		{
			var dtlist = blockProcessings.ToList();
			return dtlist.Count > 1 ? (int)dtlist.Skip(1).Select((o, i) => o.Subtract(dtlist[i]).TotalSeconds).Average() : double.NaN;
		}

		public static int PrevBlockLevel => prevBlock?.Level ?? 0;
	}
}
