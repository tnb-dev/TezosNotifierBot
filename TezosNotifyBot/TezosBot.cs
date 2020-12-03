using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MihaZupan;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NornPool.Model;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Model;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Tezos;
using TezosNotifyBot.Tzkt;
using TezosNotifyBot.TzStats;
using File = System.IO.File;
using Message = Telegram.Bot.Types.Message;
using User = TezosNotifyBot.Domain.User;

namespace TezosNotifyBot
{
	public class TezosBot
    {
	    private BotConfig Config { get; set; }
        private ILogger<TezosBot> Logger { get; }
	    private Repository repo;
        

        TelegramBotClient Bot;
        Client client;
        Client client2;
        MarketData md = new MarketData();
        DateTime mdReceived;
		//DateTime bakersReceived;
		public static List<Command> Commands;
        public static string LogsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
		bool lastBlockChanged = false;
		//Dictionary<string, MyTezosBaker.Baker> bakers = new Dictionary<string, MyTezosBaker.Baker>();
		List<Node> Nodes;
		Node CurrentNode;
		int NetworkIssueMinutes = 2;
		Worker worker;
		RewardsManager rewardsManager;
		AddressManager addrMgr;
		ResourceManager resMgr = new ResourceManager();
		string lastHash;
		Constants _currentConstants;
		Constants GetConstants()
		{
			if (_currentConstants == null)
				_currentConstants = client2.GetConstants(lastHash);
			return _currentConstants;
		}
		TzStatsData tzStats;
		DateTime lastReceived = DateTime.Now;//Дата и время получения последнего блока
		DateTime lastWebExceptionNotify = DateTime.MinValue;
		TwitterClient twitter;
		string twitterAccountName;
		bool twitterNetworkIssueNotified = false;

		public TezosBot(Repository repo, ILogger<TezosBot> logger, IOptions<BotConfig> config)
		{
			Logger = logger;
			Config = config.Value;
			this.repo = repo;
		}

		public void Run(CancellationToken cancelToken)
        {
	        resMgr.LoadResources("res.txt");
			worker = new Worker();
			worker.OnError += Worker_OnError;
			tzStats = new TzStatsData(worker);
			try
			{
				Commands = JsonConvert.DeserializeObject<List<Command>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "commands.json")));
				Nodes = JsonConvert.DeserializeObject<List<Node>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nodes.json")));
				CurrentNode = Nodes[0];

				rewardsManager = new RewardsManager(repo);
				IWebProxy proxy = null;
				if (Config.ProxyAddress != null)
				{
					if (Config.ProxyType == "http")
						proxy = new WebProxy(Config.ProxyAddress, Config.ProxyPort);
					else
						proxy = new HttpToSocks5Proxy(Config.ProxyAddress, Config.ProxyPort);
					if (Config.ProxyLogin != null)
						proxy.Credentials = new NetworkCredential(Config.ProxyLogin, Config.ProxyPassword);
				}

				client = CreateTezosClient();
				client2 = CreateTezosClient();
				{
					var block = repo.GetLastBlockLevel();
					lastHash = block.Item3;
					if (lastHash == null)
						lastHash = client.GetBlockHeader(block.Item1).hash;
				}
				twitter = new TwitterClient(
					Config.TwitterConsumerKey, 
					Config.TwitterConsumerKeySecret,
					Config.TwitterAccessToken, 
					Config.TwitterAccessTokenSecret, LogsPath
				);
				twitter.OnTwit += Twitter_OnTwit;
				twitter.OnTwitResponse += Twitter_OnTwitResponse;
				Bot = new TelegramBotClient(Config.Telegram.BotSecret, proxy);
				Logger.LogDebug("Connecting to Telegram Server...");
				Bot.SetWebhookAsync("").ConfigureAwait(true).GetAwaiter().GetResult();
				Logger.LogDebug("Connected to Telegram Server.");
				Bot.OnCallbackQuery += OnCallbackQuery;
				Bot.OnUpdate += OnUpdate;
				addrMgr = new AddressManager();
				Bot.StartReceiving();
				var me = Bot.GetMeAsync().ConfigureAwait(true).GetAwaiter().GetResult();
				Logger.LogInformation("Старт обработки сообщений @" + me.Username);
				client.BlockReceived += Client_BlockReceived;
				NotifyDev(me.Username + " v2.0 started, last block: " + repo.GetLastBlockLevel().ToString(), 0);
				tzStats.LoadCycle(repo.GetLastBlockLevel().Item1);
				var c = tzStats.GetCycle(repo.GetLastBlockLevel().Item1);
				// TODO: Check why `snapshot_cycle` is null
				NotifyDev($"Current cycle: {c.cycle}, rolls: {c.snapshot_cycle?.rolls}", 0);
				if (Config.TwitterConsumerKey != null)
				{
					var settings = twitter.GetSettings().ConfigureAwait(true).GetAwaiter().GetResult();
					twitterAccountName = (string)JObject.Parse(settings)["screen_name"];
					if (Config.TwitterChatId != 0)
						Bot.SendTextMessageAsync(Config.TwitterChatId, $"🚀 Bot started using twitter account: https://twitter.com/{twitterAccountName}");
					//twitter.TweetAsync("🚀 Bot started!");
				}
				//NotifyDev("🐦 Twitter response: " + twresult, 0, Telegram.Bot.Types.Enums.ParseMode.Default);
				LoadAddressList();
				DateTime lastBlock;
				DateTime lastWarn = DateTime.Now;
				do
				{
					try
					{
						if (DateTime.Now.Subtract(mdReceived).TotalMinutes > 5)
						{
							try
							{
								md = client.GetMarketData();
							}
							catch
							{
							}
							mdReceived = DateTime.Now;
						}

						var block = repo.GetLastBlockLevel();
						lastHash = block.Item3;
						if (lastHash == null)
							lastHash = client.GetBlockHeader(block.Item1).hash;
						lastBlock = client.Run(block.Item1);
						if (repo.GetLastBlockLevel().Item1 != block.Item1)
						{
							lastReceived = DateTime.Now;
							foreach (var user1 in repo.GetUsers().Where(o => o.NetworkIssueNotified))
								user1.NetworkIssueNotified = false;
							twitterNetworkIssueNotified = false;
						}
						if (DateTime.Now.Subtract(lastReceived).TotalMinutes > 5 && DateTime.Now.Subtract(lastWarn).TotalMinutes > 10)
						{
							NotifyDev($"‼️ Last block {block} received {(int)DateTime.Now.Subtract(lastReceived).TotalMinutes} minutes ago, check node‼️", 0);
							lastWarn = DateTime.Now;
						}
					}
					catch (WebException ex)
					{
						if (DateTime.Now.Subtract(lastWebExceptionNotify).TotalMinutes > 5)
						{
							LogError(ex);
							NotifyDev($"‼️ Node {CurrentNode.Name} issue: " + ex.Message, 0);
							lastWebExceptionNotify = DateTime.Now;
						}
						Thread.Sleep(5000);
					}
					catch (Exception ex)
					{
						LogError(ex);
						NotifyDev("‼️" + ex.Message, 0);
					}
					if (DateTime.Now.Subtract(lastReceived).TotalMinutes > NetworkIssueMinutes)
					{
						List<BlockHeader> blockHeaders = new List<BlockHeader>();
						string checkNodeResults = $"Last block {repo.GetLastBlockLevel().Item1} received {(int)DateTime.Now.Subtract(lastReceived).TotalMinutes} minutes ago. Network issue? Checking nodes...\n";
						foreach(var node in Nodes)
						{
							try
							{
								var bh = new Client(node.Url, Logger).GetBlockHeader("head");
								if (bh != null)
								{
									blockHeaders.Add(bh);
									checkNodeResults += $"{node.Name}: {bh.level} ({bh.timestamp})\n";
								}
								else
								{
									checkNodeResults += $"{node.Name}: block not received\n";
								}
							}
							catch (Exception ex)
							{
								checkNodeResults += $"{node.Name}: {ex.Message}\n";
							}
						}
						if (blockHeaders.Count == 0 || blockHeaders.Max(o => o.level) < repo.GetLastBlockLevel().Item1 + 4)
						{
							checkNodeResults += "Network issue!";
							Logger.LogWarning(checkNodeResults);
							//NotifyDev(checkNodeResults, 0);
							var lastBH = blockHeaders.OrderByDescending(o => o.level).FirstOrDefault();
							foreach (var user1 in repo.GetUsers().Where(o => o.NetworkIssueNotify > 0 && !o.NetworkIssueNotified))
							{
								if (DateTime.Now.Subtract(lastReceived).TotalMinutes < user1.NetworkIssueNotify)
									continue;
								string result = resMgr.Get(Res.NetworkIssue, new ContextObject { u = user1, Block = lastBH?.level ?? repo.GetLastBlockLevel().Item1, Minutes = (int)DateTime.Now.Subtract(lastReceived).TotalMinutes });
								if (!user1.HideHashTags)
									result += "\n\n#network_issue";
								SendTextMessage(user1.Id, result, ReplyKeyboards.MainMenu(resMgr, user1));
								user1.NetworkIssueNotified = true;
							}
							// Twitter
							{
								if (!twitterNetworkIssueNotified && DateTime.Now.Subtract(lastReceived).TotalMinutes > Config.TwitterNetworkIssueNotify)
								{
									string twText = resMgr.Get(Res.NetworkIssue, new ContextObject { Block = lastBH?.level ?? repo.GetLastBlockLevel().Item1, Minutes = (int)DateTime.Now.Subtract(lastReceived).TotalMinutes });
									twitter.TweetAsync(twText);
									twitterNetworkIssueNotified = true;
								}
							}
						}
						else
						{
							checkNodeResults += "Not now. 🤨";
						}
					}
					int wait = 60000 - (int)DateTime.Now.Subtract(lastReceived).TotalMilliseconds;
					if (wait > 10)
					{
						Logger.LogDebug($"Waiting {wait} milliseconds");
						Thread.Sleep(wait);
					}
				}
				while (cancelToken.IsCancellationRequested is false);
			}
			catch(Exception fe)
			{
				Logger.LogCritical(fe, "Fatal error");
			}
        }

		private void Twitter_OnTwitResponse(int twitId, string response)
		{
			var tw = repo.GetTwitterMessage(twitId);
			var jObject = JObject.Parse(response);
			if (jObject["errors"] != null)
			{
				if (Config.TwitterChatId != 0)
					Bot.SendTextMessageAsync(Config.TwitterChatId, $"Twitter Errors: {response}", ParseMode.Default);
			}
			else
			{
				tw.TwitterId = (string)jObject["id"];
				repo.UpdateTwitterMessage(tw);
				if (Config.TwitterChatId != 0)
					Bot.SendTextMessageAsync(Config.TwitterChatId, $"Published: https://twitter.com/{twitterAccountName}/status/{tw.TwitterId}", ParseMode.Default,
						replyMarkup: ReplyKeyboards.TweetSettings(twitId));
			}
		}

		private int Twitter_OnTwit(string text)
		{
			if (Config.TwitterChatId != 0)
				Bot.SendTextMessageAsync(Config.TwitterChatId, text + $"\n\n📏 {Regex.Replace(text, "http(s)?://([\\w-]+.)+[\\w-]+(/[\\w- ./?%&=])?", "01234567890123456789123").Length}", ParseMode.Default);
			return repo.CreateTwitterMessage(text).Id;
		}

		private void Worker_OnError(object sender, ErrorEventArgs e)
		{
			NotifyDev("‼️" + sender.ToString() + ": " + e.GetException().Message, 0);
		}

		BlockHeader lastHeader;
		BlockMetadata lastMetadata;
        private bool Client_BlockReceived(BlockHeader header, BlockMetadata blockMetadata, Operation[] operations)
		{
			if (lastWebExceptionNotify != DateTime.MinValue)
			{
				NotifyDev($"✅ Node {CurrentNode.Name} continue working", 0);
				lastWebExceptionNotify = DateTime.MinValue;
			}
			if (lastHeader != null && header.predecessor != lastHeader.hash)
			{
				NotifyDev($"🤖 Last processed block {lastHeader.level} ({lastHeader.hash}), current received block {header.level} (predecessor {header.predecessor}). Address cache cleared", 0);
				addrMgr.Clear();
			}
			if (lastHeader != null && header.level % 100 == 0 && lastHeader.level + 1 == header.level)
			{
				string hash = lastHash;
				int level = lastHeader.level;
				var contracts = addrMgr.GetContractsBalances();
				worker.Run("Cache validation", wcb => 
				{
					//NotifyDev($"🤖 Starting cache validation of {contracts.Count} addresses on block {level}", 0);
					int errors = 0;
					using (var tc = new Client(CurrentNode.Url, Logger))
					{
						foreach (var contract in contracts)
						{
							var ci = tc.GetContractInfo(hash, contract.Item1);
							if (ci.balance != contract.Item2)
							{
								NotifyDev($"🤖🛑 Address {contract.Item1} at block {level}: cached {contract.Item2 / 1000000M}XTZ, node value {ci.balance / 1000000M}XTZ. Address removed from cache.", 0);
								addrMgr.Remove(contract.Item1);
								errors++;
							}
						}
					}
					//NotifyDev($"🤖 Finished cache validation of {contracts.Count} addresses on block {level}: {errors} errors", 0);
				});
			}
			lastReceived = DateTime.Now;
			Logger.LogDebug($"Block {header.level} received");
			addrMgr.LoadQueue(client, lastHash);
			//if (blockMetadata.level.cycle_position == 0)
			//{
			//	var delegates = client.GetDelegates(header.hash);
			//	var constants = client.GetConstants(header.hash);
			//	int rolls = 0;
			//	decimal totalstake = 0;
			//	foreach (var d in delegates)
			//	{
			//		var s = client.GetDelegateStake(header.hash, d);
			//		rolls += (int)decimal.Floor(s / constants.tokens_per_roll);
			//		totalstake += s;
			//	}
			//	NotifyDev($"Total rolls for cycle {blockMetadata.level.cycle + 5}: {rolls}, total stake: {totalstake / 1000000}", 0);
			//}
			var prevHeader = lastHeader?.hash == header.predecessor ? lastHeader : client.GetBlockHeader((header.level - 1).ToString());
			var prevMD = lastMetadata?.level?.level == header.level - 1 ? lastMetadata : client.GetBlockMetadata((header.level - 1).ToString());
			if (!ProcessBlockBakingData(prevHeader, prevMD, operations))
				return false;

			HashSet<string> addrUpdated = new HashSet<string>();
			Action<string, long> addrUpdate = (addr, bal) =>
			 {
				 if (!addrUpdated.Contains(addr))
					 addrUpdated.Add(addr);
				 Logger.LogDebug($"Balance of {addr} updated to {bal / 1000000M} XTZ at block {header.level}");
			 };

			if (addrMgr.LastHash != header.hash)
			{
				foreach (var op in operations)
				{
					foreach (var content in op.contents)
					{
						if (content.kind == "activate_account" || content.kind == "reveal" || content.kind == "delegation" || content.kind == "origination" || content.kind == "transaction")
						{
							addrMgr.UpdateBalance(content.metadata.balance_updates, header.hash, addrUpdate);
							if (content.metadata.operation_result?.status == "applied")
								addrMgr.UpdateBalance(content.metadata.operation_result?.balance_updates, header.hash, addrUpdate);
							if (content.metadata.internal_operation_results != null)
							{
								foreach (var ior in content.metadata.internal_operation_results)
								{
									if (ior.result.status == "applied")
										addrMgr.UpdateBalance(ior.result.balance_updates, header.hash, addrUpdate);
								}
							}
						}
					}
				}
				addrMgr.UpdateBalance(blockMetadata.balance_updates, header.hash, addrUpdate);
				addrMgr.UpdateBalance(operations.SelectMany(o => o.contents.Where(o1 => o1.metadata.balance_updates != null && o1.kind == "endorsement").SelectMany(o1 => o1.metadata.balance_updates)), header.hash, addrUpdate);
				addrMgr.LastHash = header.hash;
			}

			/*foreach(var addr in addrUpdated)
			{
				var ci = client.GetContractInfo(header.hash, addr);
				var bal = addrMgr.GetContract(client, header.hash, addr).balance;
				if (ci.balance != bal)
				{
					logger.Verbose($"Actual balance of {addr} is {ci.balance / 1000000M} XTZ at block {header.level}");
					NotifyDev($"🤖🛑 Address {addr} at block {header.level}: cached {bal / 1000000M}XTZ, node value {ci.balance / 1000000M}XTZ. Block {header.hash}", 0);
				}
			}*/

			if (blockMetadata.level.voting_period_position == 32767 && blockMetadata.voting_period_kind == "testing")
			{
				var hash = client.GetCurrentProposal(prevHeader.hash);
				var p = repo.GetProposal(hash);
				foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
				{
					var result = resMgr.Get(Res.TestingVoteSuccess, new ContextObject { p = p, u = u, Block = blockMetadata.level.level });
					if (!u.HideHashTags)
						result += "\n\n#proposal" + p.HashTag();
					SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
				}
				// Делегат не проголосовал
				var listings = client.GetVoteListings(prevHeader.hash);
				var votes = client.GetBallotList(prevHeader.hash);
				foreach (var listing in listings)
				{
					if (!votes.Any(o => o.pkh == listing.pkh))
					{
						foreach (var ua in repo.GetUserAddresses(listing.pkh))
						{
							if (ua.User.VotingNotify)
							{
								var result = resMgr.Get(Res.DelegateDidNotVoted, (ua, p));
								if (!ua.User.HideHashTags)
									result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
								SendTextMessage(ua.UserId, result, ReplyKeyboards.MainMenu(resMgr, ua.User));
							}
						}
					}
				}
			}

			if (blockMetadata.level.voting_period_position == 32767 && blockMetadata.voting_period_kind == "proposal" && prevMD.voting_period_kind == "testing_vote")
			{
				var hash = client.GetCurrentProposal(prevHeader.hash);
				var p = repo.GetProposal(hash);
				foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
				{
					var result = resMgr.Get(Res.TestingVoteFailed, new ContextObject { p = p, u = u, Block = blockMetadata.level.level });
					if (!u.HideHashTags)
						result += "\n\n#proposal" + p.HashTag();
					SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
				}
			}

			if (blockMetadata.level.voting_period_position == 32767 && blockMetadata.voting_period_kind == "proposal" && prevMD.voting_period_kind == "promotion_vote")
			{
				var hash = client.GetCurrentProposal(prevHeader.hash);
				var p = repo.GetProposal(hash);
				foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
				{
					var result = blockMetadata.next_protocol == hash ?
							resMgr.Get(Res.PromotionVoteSuccess, new ContextObject { p = p, u = u, Block = blockMetadata.level.level }) :
							resMgr.Get(Res.PromotionVoteFailed, new ContextObject { p = p, u = u, Block = blockMetadata.level.level });
					if (!u.HideHashTags)
						result += "\n\n#proposal" + p.HashTag();
					SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
				}
			}

			List<Tuple<string, string, decimal, string>> fromToAmountHash = new List<Tuple<string, string, decimal, string>>();
			var allUsers = repo.GetUsers();
			foreach (var op in operations)
            {
                foreach(var content in op.contents)
                {
					if (content.kind == "double_baking_evidence" || content.kind == "double_endorsement_evidence")
					{
						// Двойная выпечка.
						var offend = content.metadata.balance_updates.FirstOrDefault(o => o.change < 0);
						if (offend != null)
						{
							var offender = offend.@delegate;
							decimal lost = -content.metadata.balance_updates.Where(o => o.change < 0).Sum(o => o.change);
							decimal rewards = content.metadata.balance_updates.Where(o => o.change > 0).Sum(o => o.change);

							var offenderAddresses = repo.GetUserAddresses(offender);
							foreach (var ua in offenderAddresses)
							{
								string result = resMgr.Get(Res.DoubleBakingOccured, new ContextObject {u = ua.User, ua = ua, Amount = lost / 1000000, Block = header.level, md = md, OpHash = op.hash });
								if (!ua.User.HideHashTags)
									result += "\n\n#doublebaking" + ua.HashTag();
								SendTextMessageUA(ua, result);
							}

							var bakerAddresses = repo.GetUserAddresses(blockMetadata.baker);
							foreach (var ua in bakerAddresses)
							{
								string result = resMgr.Get(Res.DoubleBakingEvidence, new ContextObject { u = ua.User, ua = ua, Amount = rewards / 1000000, Block = header.level, md = md, OpHash = op.hash });
								if (!ua.User.HideHashTags)
									result += "\n\n#doublebaking" + ua.HashTag();
								SendTextMessageUA(ua, result);
							}
						}
					}
					if (content.kind == "proposals")
					{
						var from = content.source;
						var props = content.proposals;
						var listings = client.GetVoteListings(header.hash);
						int rolls = listings.Single(o => o.pkh == from).rolls;
						int allrolls = listings.Sum(o => o.rolls);
						var votes = client.GetProposals(header.hash);
						foreach (var hash in props)
						{
							var votedrolls = votes[hash];
							var p = repo.GetProposal(hash);
							if (p == null)
							{
								p = repo.AddProposal(hash, from, content.period);
								p.VotedRolls = votedrolls;
								foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
								{
									var ua = repo.GetUserAddresses(u.Id).FirstOrDefault(o => o.Address == from);
									if (ua == null)
										ua = new UserAddress { Address = from, Name = repo.GetDelegateName(from) };
									ua.StakingBalance = rolls * 8000;
									var result = resMgr.Get(Res.NewProposal, new ContextObject { ua = ua, p = p, u = u, OpHash = op.hash, Block = blockMetadata.level.level });
									if (!u.HideHashTags)
										result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
									SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
								}

								//Twitter
								{
									var twText = resMgr.Get(Res.TwitterNewProposal, new ContextObject { ua = new UserAddress { Address = from, Name = repo.GetDelegateName(from), StakingBalance = rolls * 8000 }, p = p, OpHash = op.hash });
									twitter.TweetAsync(twText);
								}
							}
							else
							{
								p.VotedRolls = votedrolls;
								foreach (var ua in repo.GetUserAddresses(from))
								{
									if (ua.User.VotingNotify)
									{
										ua.StakingBalance = rolls * 8000;
										var result = resMgr.Get(Res.SupplyProposal, new ContextObject { ua = ua, p = p, u = ua.User, OpHash = op.hash, TotalRolls = allrolls, Block = blockMetadata.level.level });
										if (!ua.User.HideHashTags)
											result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
										SendTextMessage(ua.UserId, result, ReplyKeyboards.MainMenu(resMgr, ua.User));
									}
								}
							}
							repo.AddProposalVote(p, from, blockMetadata.level.voting_period, header.level, 0);
						}
					}
					if (content.kind == "ballot")
					{
						var from = content.source;
						var hash = content.proposal;

						var listings = client.GetVoteListings(header.hash);
						int rolls = listings.Single(o => o.pkh == from).rolls;
						int allrolls = listings.Sum(o => o.rolls);

						var p = repo.GetProposal(hash);
						if (p == null)
						{
							p = repo.AddProposal(hash, from, content.period);
							NotifyDev($"⚠️ Proposal {p.Name} missed in database, added impicitly", 0);
						}

						// Записать Vote в базу данных
						repo.AddProposalVote(p, from, blockMetadata.level.voting_period, header.level, content.ballot == "yay" ? 1 : (content.ballot == "nay" ? 2 : 3));

						foreach (var ua in repo.GetUserAddresses(from))
						{
							if (ua.User.VotingNotify)
							{
								ua.StakingBalance = rolls * 8000;
								Res bp = Res.BallotProposal_pass;
								if (content.ballot == "yay")
									bp = Res.BallotProposal_yay;
								if (content.ballot == "nay")
									bp = Res.BallotProposal_nay;
								var result = resMgr.Get(bp, new ContextObject { ua = ua, p = p, u = ua.User, OpHash = op.hash, TotalRolls = allrolls, Block = blockMetadata.level.level });
								if (!ua.User.HideHashTags)
									result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
								SendTextMessage(ua.UserId, result, ReplyKeyboards.MainMenu(resMgr, ua.User));
							}
						}

						//foreach (var devUser in Config.DevUserNames)
						//{
						//	var user = repo.GetUser(devUser);
						//	if (user != null)
						//	{
						//		var result = TextStrings.BallotProposal(user, op.hash, from, repo.GetDelegateName(from), p.Hash, p.Name, rolls, allrolls, content.ballot);
						//		if (!user.HideHashTags)
						//			result += "\n#proposal #" + (hash.Substring(0, 7) + hash.Substring(hash.Length - 5)).ToLower();
						//		SendTextMessage(user.UserId, result, ReplyKeyboards.MainMenu(resMgr, user));
						//	}
						//}
						//Проверка кворума
						var ballots = client.GetBallots(header.hash);
						var participation = ballots.yay + ballots.nay + ballots.pass;

						var quorum = client.GetQuorum(header.hash);

						//NotifyDev($"<b>Voting process status</b>\n\nYay: {ballots.yay} rolls, {(100M * ballots.yay / participation).ToString("n2")}%\nNay: {ballots.nay} rolls, {(100M * ballots.nay / participation).ToString("n2")}%\nPass: {ballots.pass} rolls, {(100M * ballots.pass / participation).ToString("n2")}%\n\nTotal participation: {participation} rolls, {(100M * participation/allrolls).ToString("n2")}%, current quorum: {(quorum/100M).ToString("n2")}%", 0, Telegram.Bot.Types.Enums.ParseMode.Html);
						if (participation * 10000 / allrolls >= quorum && (participation - rolls) * 10000 / allrolls < quorum)
						{
							foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
							{
								var result = resMgr.Get(Res.QuorumReached, new ContextObject { u = u, p = p, Block = blockMetadata.level.level });
								if (!u.HideHashTags)
									result += "\n\n#proposal" + p.HashTag();
								SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
							}
							{
								var twText = resMgr.Get(Res.TwitterQuorumReached, new ContextObject { p = p, Block = blockMetadata.level.level });
								twitter.TweetAsync(twText);
							}
						}
					}
                    if (content.metadata?.operation_result?.status != "applied")
                        continue;
                    if (content.kind == "transaction")
                    {
                        var from = content.source;
                        var to = content.destination;
                        var amount = decimal.Parse(content.amount) / 1000000M;
						if (amount == 0 && content.metadata.internal_operation_results != null && content.metadata.internal_operation_results.Count > 0 && content.metadata.internal_operation_results[0].kind == "transaction")
						{
							from = content.metadata.internal_operation_results[0].source;
							to = content.metadata.internal_operation_results[0].destination;
							amount = decimal.Parse(content.metadata.internal_operation_results[0].amount) / 1000000M;
						}
						if (amount == 0 && content.metadata.internal_operation_results != null && content.metadata.internal_operation_results.Count > 0 && content.metadata.internal_operation_results[0].kind == "delegation")
						{
							from = content.metadata.internal_operation_results[0].source;
							to = content.metadata.internal_operation_results[0].@delegate;
							var fromAddresses = repo.GetUserAddresses(from);
							if (to != null)
							{
								var toAddresses = repo.GetUserAddresses(to);
								foreach (var ua in repo.GetUserAddresses(from).Where(o => o.NotifyDelegations))
								{
									amount = addrMgr.GetContract(client, header.hash, from).balance / 1000000M;
									var targetAddr = repo.GetUserTezosAddress(ua.UserId, to);
									string result = resMgr.Get(Res.NewDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = ua, ua_to = targetAddr });
									if (!ua.User.HideHashTags)
										result += "\n\n#delegation" + targetAddr.HashTag() + ua.HashTag();
									SendTextMessageUA(ua, result);
								}
								foreach (var ua in repo.GetUserAddresses(to).Where(o => o.NotifyDelegations))
								{
									amount = addrMgr.GetContract(client, header.hash, from).balance / 1000000M;
									if (ua.DelegationAmountThreshold > amount)
										continue;
									var sourceAddr = repo.GetUserTezosAddress(ua.UserId, from);
									string result = resMgr.Get(Res.NewDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = sourceAddr, ua_to = ua });
									if (!ua.User.HideHashTags)
										result += "\n\n#delegation" + sourceAddr.HashTag() + ua.HashTag();
									SendTextMessageUA(ua, result);
								}
							}
							addrMgr.UpdateDelegate(from, to);
							var prevdelegate = client.GetContractInfo(header.hash + "~1", from)?.@delegate;
							if (prevdelegate != null)
							{
								foreach (var ua in repo.GetUserAddresses(prevdelegate).Where(o => o.NotifyDelegations))
								{
									amount = addrMgr.GetContract(client, header.hash, from).balance / 1000000M;
									if (ua.DelegationAmountThreshold > amount)
										continue;
									var sourceAddr = repo.GetUserTezosAddress(ua.UserId, from);
									string result = resMgr.Get(Res.UnDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = sourceAddr, ua_to = ua });
									if (!ua.User.HideHashTags)
										result += "\n\n#leave_delegate" + sourceAddr.HashTag() + ua.HashTag();
									SendTextMessageUA(ua, result);
								}
							}
							continue;
						}
						if (amount == 0)
							continue;
                        fromToAmountHash.Add(new Tuple<string, string, decimal, string>(from, to, amount, op.hash));
						// Уведомления о китах
						foreach(var u in allUsers.Where(o => !o.Inactive && o.WhaleThreshold > 0 && o.WhaleThreshold <= amount))
						{
							var ua_from = repo.GetUserTezosAddress(u.Id, from);
							var ua_to = repo.GetUserTezosAddress(u.Id, to);
							string result = resMgr.Get(Res.WhaleTransaction, new ContextObject { u = u, OpHash = op.hash, Amount = amount, md = md, ua_from = ua_from, ua_to = ua_to });
							if (!u.HideHashTags)
							{
								result += "\n\n#whale" + ua_from.HashTag() + ua_to.HashTag();
							}
							SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
						}
						// Уведомления о китах для твиттера
						if (amount >= 500000)
						{
							var ua_from = repo.GetUserTezosAddress(0, from);
							var ua_to = repo.GetUserTezosAddress(0, to);
							string result = resMgr.Get(Res.TwitterWhaleTransaction, new ContextObject { OpHash = op.hash, Amount = amount, md = md, ua_from = ua_from, ua_to = ua_to });
							twitter.TweetAsync(result);
						}
                    }
                    if (content.kind == "delegation")
                    {
                        var from = content.source;
                        var to = content.@delegate;
                        var fromAddresses = repo.GetUserAddresses(from);
						addrMgr.UpdateDelegate(from, to);
						if (to != null)
						{
							var toAddresses = repo.GetUserAddresses(to);
							foreach (var ua in repo.GetUserAddresses(from).Where(o => o.NotifyDelegations))
							{
								var amount = addrMgr.GetContract(client, header.hash, from).balance / 1000000M;
								var targetAddr = repo.GetUserTezosAddress(ua.UserId, to);
								string result = resMgr.Get(Res.NewDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = ua, ua_to = targetAddr });
								if (!ua.User.HideHashTags)
									result += "\n\n#delegation" + targetAddr.HashTag() + ua.HashTag();
								SendTextMessageUA(ua, result);
							}
							foreach (var ua in repo.GetUserAddresses(to).Where(o => o.NotifyDelegations))
							{
								var amount = addrMgr.GetContract(client, header.hash, from).balance / 1000000M;
								if (ua.DelegationAmountThreshold > amount)
									continue;
								var sourceAddr = repo.GetUserTezosAddress(ua.UserId, from);
								string result = resMgr.Get(Res.NewDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = sourceAddr, ua_to = ua });
								if (!ua.User.HideHashTags)
									result += "\n\n#delegation" + sourceAddr.HashTag() + ua.HashTag();
								SendTextMessageUA(ua, result);
							}
						}
                        var prevdelegate = client.GetContractInfo(header.hash + "~1", from)?.@delegate;
                        if (prevdelegate != null)
                        {
                            foreach (var ua in repo.GetUserAddresses(prevdelegate).Where(o => o.NotifyDelegations))
							{
								var amount = addrMgr.GetContract(client, header.hash, from).balance / 1000000M;
								if (ua.DelegationAmountThreshold > amount)
									continue;
								var sourceAddr = repo.GetUserTezosAddress(ua.UserId, from);
								string result = resMgr.Get(Res.UnDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = sourceAddr, ua_to = ua });
								if (!ua.User.HideHashTags)
                                    result += "\n\n#leave_delegate" + sourceAddr.HashTag() + ua.HashTag();
								SendTextMessageUA(ua, result);
                            }
                        }
                    }
                    if (content.kind == "origination")
                    {						
						var from = content.metadata.operation_result.originated_contracts.FirstOrDefault();
                        var to = content.@delegate;
                        if (to == null)
                            continue;
                        var amount = content.balance / 1000000M;
                        string tezAmount = amount.TezToString();
                        var toAddresses = repo.GetUserAddresses(to);
                        var fromAddresses = repo.GetUserAddresses(from);
						addrMgr.UpdateDelegate(from, to);
                        foreach (var ua in repo.GetUserAddresses(content.source))
                        {
                            var targetAddr = repo.GetUserTezosAddress(ua.UserId, to);
                            string result = resMgr.Get(Res.NewDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = ua, ua_to = targetAddr });
                            if (!ua.User.HideHashTags)
                                result += "\n\n#delegation" + targetAddr.HashTag() + ua.HashTag();
							SendTextMessageUA(ua, result);
                        }
                        foreach (var ua in repo.GetUserAddresses(to).Where(o => o.NotifyDelegations))
                        {
							if (ua.DelegationAmountThreshold > amount)
								continue;
							var sourceAddr = repo.GetUserTezosAddress(ua.UserId, from);
							string result = resMgr.Get(Res.NewDelegation, new ContextObject { u = ua.User, OpHash = op.hash, Amount = amount, ua_from = sourceAddr, ua_to = ua });
							if (!ua.User.HideHashTags)
                                result += "\n\n#delegation " + sourceAddr.HashTag() + ua.HashTag();
							SendTextMessageUA(ua, result);
                        }
                    }
				}
            }

            var fromGroup = fromToAmountHash.GroupBy(o => o.Item1);
            foreach (var from in fromGroup)
            {
                //decimal total = from.Sum(o => o.Item3);
                var fromAddresses = repo.GetUserAddresses(from.Key).Where(o => o.NotifyTransactions).ToList();
                decimal fromBalance = 0;
                bool fromDelegate = false;
                if (fromAddresses.Count > 0)
                {
                    if (repo.IsDelegate(from.Key))
                    {
                        fromDelegate = true;
						var di = addrMgr.GetDelegate(client, header.hash, from.Key, true);
						if (di != null)
						{
							decimal bond = di.balance - di.frozen_balance + (di.frozen_balance_by_cycle.Count > 0 ? di.frozen_balance_by_cycle.Sum(o => o.deposit) : 0);
							fromBalance = bond / 1000000;
						}
						else
							throw new ApplicationException("GetDelegateInfo error (" + CurrentNode.Name + ")");
                    }
                    else
                    {
                        fromBalance = addrMgr.GetContract(client, header.hash, from.Key).balance / 1000000M;
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
                        var ua_to = repo.GetUserTezosAddress(ua.UserId, to);
						result = resMgr.Get(Res.OutgoingTransaction, new ContextObject { u = ua.User, OpHash = from_ua.Single().Item4, Block = header.level, Amount = from_ua.Sum(o => o.Item3), md = md, ua_from = ua, ua_to = ua_to });
						tags = ua_to.HashTag();
                    }
                    else
                    {
						result = resMgr.Get(Res.OutgoingTransactions, new ContextObject { u = ua.User, ua = ua, Block = header.level, Amount = from_ua.Sum(o => o.Item3), md = md }) + "\n";
						int cnt = 0;
                        foreach(var to in from_ua.OrderByDescending(o => o.Item3))
                        {
							cnt++;
                            var targetAddr = repo.GetUserTezosAddress(ua.UserId, to.Item2);
							result += resMgr.Get(Res.To, new ContextObject { u = ua.User, Amount = to.Item3, ua = targetAddr }) + "\n";
							if (!tags.Contains(targetAddr.HashTag()) && (cnt < 6 || targetAddr.UserId == ua.UserId))
                                tags += targetAddr.HashTag();
							if (cnt > 40)
							{
								result += resMgr.Get(Res.NotAllShown, new ContextObject { u = ua.User, Block = header.level }) + "\n";
								break;
							}
                        }
					}
					result += "\n\n";
					if (fromDelegate)
                        result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
					else
                        result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
                    if (!ua.User.HideHashTags)
                        result += "\n#outgoing" + ua.HashTag() + tags;
					SendTextMessageUA(ua, result);
                    repo.UpdateBalance(ua);
                }
            }

            var toGroup = fromToAmountHash.GroupBy(o => o.Item2);
            foreach (var to in toGroup)
            {
                //decimal total = to.Sum(o => o.Item3);
                var toAddresses = repo.GetUserAddresses(to.Key).Where(o => o.NotifyTransactions).ToList();
                decimal toBalance = 0;
                bool toDelegate = false;
                if (toAddresses.Count > 0)
                {
                    if (repo.IsDelegate(to.Key))
                    {
                        toDelegate = true;
                        var di = addrMgr.GetDelegate(client, header.hash, to.Key, true);
						decimal bond = di.balance - di.frozen_balance + (di.frozen_balance_by_cycle.Count > 0 ? di.frozen_balance_by_cycle.Sum(o => o.deposit) : 0);
                        toBalance = bond / 1000000;
                    }
                    else
                    {
                        toBalance = addrMgr.GetContract(client, header.hash, to.Key).balance / 1000000M;
                    }            
                }
                foreach (var ua in toAddresses)
                {
                    if (ua.AmountThreshold > to.Sum(o => o.Item3))
                        continue;
					if (!toDelegate)
						ua.Balance = toBalance;
					else
						ua.FullBalance = toBalance;
                    //string usdBalance = ua.UsdBalance(md.price_usd);
                    //string btcBalance = ua.BtcBalance(md.price_btc);
                    //string balance = ua.TezBalance();
                    string result = "";
                    string tags = "";
					var to_ua = to.Where(o => ua.User.WhaleThreshold == 0 || o.Item3 < ua.User.WhaleThreshold);
					if (to_ua.Count() == 0)
						continue;
					if (to_ua.Count() == 1)
                    {
                        var from = to_ua.Single().Item1;
                        var ua_from = repo.GetUserTezosAddress(ua.UserId, from);
						result = resMgr.Get(Res.IncomingTransaction, new ContextObject { u = ua.User, OpHash = to_ua.Single().Item4, Block = header.level, Amount = to_ua.Sum(o => o.Item3), md = md, ua_from = ua_from, ua_to = ua });
						tags = ua_from.HashTag();
                    }
                    else
                    {
                        result = resMgr.Get(Res.IncomingTransactions, new ContextObject { u = ua.User, ua = ua, Block = header.level, Amount = to_ua.Sum(o => o.Item3), md = md }) + "\n";
						int cnt = 0;
						foreach (var from in to_ua.OrderByDescending(o => o.Item3))
                        {
							cnt++;
							var sourceAddr = repo.GetUserTezosAddress(ua.UserId, from.Item1);
							result += resMgr.Get(Res.From, new ContextObject { u = ua.User, Amount = from.Item3, ua = sourceAddr }) + "\n";
                            if (!tags.Contains(sourceAddr.HashTag()) && (cnt < 6 || sourceAddr.UserId == ua.UserId))
                                tags += sourceAddr.HashTag();
							if (cnt > 40)
							{
								result += resMgr.Get(Res.NotAllShown, new ContextObject { u = ua.User, Block = header.level }) + "\n";
								break;
							}
						}
                    }
					result += "\n\n";
					if (toDelegate)
                        result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
					else
                        result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
                    if (!ua.User.HideHashTags)
                        result += "\n#incoming" + ua.HashTag() + tags;
					SendTextMessageUA(ua, result);
                    repo.UpdateBalance(ua);
                }
            }

			if (!lastBlockChanged)
				repo.SetLastBlockLevel(header.level, header.priority, header.hash);
			Logger.LogInformation("Block " + header.level.ToString() + " operations processed");
			lastHeader = header;
			lastMetadata = blockMetadata;
			lastHash = header.hash;
			if (lastBlockChanged)
			{
				lastBlockChanged = false;
				return false;
			}
			return true;
        }

		bool ProcessBlockBakingData(BlockHeader header, BlockMetadata blockMetadata, Operation[] operations)
		{
			Logger.LogDebug($"ProcessBlockBakingData {header.level}");
			if (!repo.IsRightsLoaded(blockMetadata.level.cycle))
				LoadBakingEndorsingRights(header.hash, blockMetadata.level.cycle);
			if (!repo.IsRightsLoaded(blockMetadata.level.cycle + 1))
			{
				LoadBakingEndorsingRights(header.hash, blockMetadata.level.cycle + 1);
				//worker.Run($"LoadBakingEndorsingRights({blockMetadata.level.cycle + 1})", () =>
				//{
				//	if (!repo.IsRightsLoaded(blockMetadata.level.cycle + 1))
				//});				
			}
			var baking_rights = client.GetBakingRights(header.predecessor);
			//var endorsing_rights = client.GetEndorsingRights(header.predecessor);
			// Проверка baking rights
			Logger.LogDebug($"Baking rights processing {header.level + 1}");
			int slots = operations.Where(o => o.contents.Any(o1 => o1.kind == "endorsement")).SelectMany(o => o.contents).Sum(o => o.metadata.slots.Count);
			if (header.level <= 655360)
				slots = 32;
			foreach (var baking_right in baking_rights)
			{
				if (baking_right.priority == 0 && baking_right.@delegate != blockMetadata.baker)
				{
					long rewards = (16000000 * (8 + 2 * slots / 32))/10;
					if (baking_right.level >= Config.CarthageStart)
						rewards = 80000000L * slots / 32 / 2;
					rewardsManager.BalanceUpdate(baking_right.@delegate, RewardsManager.RewardType.MissedBaking, header.level + 1, rewards);
				}
				if (baking_right.@delegate != blockMetadata.baker)
				{
					var uaddrs = repo.GetUserAddresses(baking_right.@delegate);
					ContractInfo info = null;
					if (uaddrs.Count > 0)
						info = addrMgr.GetContract(client, header.hash, baking_right.@delegate);

					long rewards = (16000000 * (8 + 2 * slots / 32)) / 10;
					if (baking_right.level >= Config.CarthageStart)
						rewards = 80000000L * slots / 32 / 2;

					foreach (var ua in uaddrs.Where(o => o.NotifyMisses))
					{
						ua.Balance = info.balance;
						var result = resMgr.Get(Res.MissedBaking, new ContextObject { u = ua.User, ua = ua, Block = header.level, Amount = rewards / 1000000M });
						
						if (!ua.User.HideHashTags)
							result += "\n\n#missed_baking" + ua.HashTag();
						SendTextMessageUA(ua, result);
					}
				}
				else
				{
					if (baking_right.priority > 0)
					{
						var uaddrs = repo.GetUserAddresses(baking_right.@delegate);
						foreach (var ua in uaddrs)
						{
							var result = resMgr.Get(Res.StoleBaking, new ContextObject { u = ua.User, ua = ua, Block = header.level, Priority = baking_right.priority, Amount = (blockMetadata.balance_updates.Where(o => o.kind == "freezer" && o.category == "rewards" && (o.level ?? o.cycle) == blockMetadata.level.cycle).Sum(o => o.change) / 1000000M) });
							if (!ua.User.HideHashTags)
								result += "\n\n#stole_baking" + ua.HashTag();
							SendTextMessageUA(ua, result);
						}
					}
					break;
				}
			}
			
			var priority = header.priority;
			Logger.LogDebug($"Endorsements processing {header.level + 1}");
			var endorsing_rights = repo.GetEndorsingRights(header.level);
			if (operations.Any(o => o.contents.Any(o1 => o1.kind == "endorsement")))
			{
				foreach (var d in endorsing_rights)
				{
					//repo.UpdateDelegateRewards(d.@delegate, blockMetadata.level.cycle, (uint)(2000000 / (priority + 1)) * (uint)d.slots.Count, 0);
					if (!operations.Any(o => o.contents.Any(o1 => o1.kind == "endorsement" && o1.metadata.@delegate == d.Item1)))
					{
						long rewards = (uint)(2000000 / (priority + 1)) * (uint)d.Item2;
						if (header.level >= Config.CarthageStart)
						{
							rewards = 80000000 * d.Item2 / 32 / 2;
							if (priority > 0)
								rewards = rewards * 2 / 3;
						}
						rewardsManager.BalanceUpdate(d.Item1, RewardsManager.RewardType.MissedEndorsing, header.level + 1, rewards, d.Item2);
						var uaddrs = repo.GetUserAddresses(d.Item1);
						ContractInfo info = null;
						if (uaddrs.Count > 0)
							info = addrMgr.GetContract(client, header.hash, d.Item1);
						foreach (var ua in uaddrs.Where(o => o.NotifyMisses))
						{
							ua.Balance = info.balance;
							var result = resMgr.Get(Res.MissedEndorsing, new ContextObject { u = ua.User, ua = ua, Block = header.level, Amount = rewards / 1000000M });
							if (!ua.User.HideHashTags)
								result += "\n\n#missed_endorsing" + ua.HashTag();
							SendTextMessageUA(ua, result);
						}
					}
				}
			}
			else
			{
				foreach (var d in endorsing_rights)
				{
					var uaddrs = repo.GetUserAddresses(d.Item1);
					foreach (var ua in uaddrs.Where(o => o.NotifyMisses))
					{
						var result = resMgr.Get(Res.SkippedEndorsing, new ContextObject { u = ua.User, ua = ua, Block = header.level});
						if (!ua.User.HideHashTags)
							result += "\n\n#notendorsed" + ua.HashTag();
						SendTextMessageUA(ua, result);
					}
				}
			}
			Logger.LogDebug($"Updating rewards {header.level}");
			foreach (var bu in blockMetadata.balance_updates.Where(o => o.kind == "freezer" && o.category == "rewards" && (o.level ?? o.cycle) == blockMetadata.level.cycle))
				rewardsManager.BalanceUpdate(bu.@delegate, header.priority == 0 ? RewardsManager.RewardType.Baking : RewardsManager.RewardType.StolenBaking, header.level + 1, bu.change);
			foreach (var bu in operations.SelectMany(o => o.contents.Where(o1 => o1.metadata.balance_updates != null).SelectMany(o1 => o1.metadata.balance_updates)).Where(o => o.kind == "freezer" && o.category == "rewards" && (o.level ?? o.cycle) == blockMetadata.level.cycle))
				rewardsManager.BalanceUpdate(bu.@delegate, RewardsManager.RewardType.Endorsing, header.level + 1, bu.change, endorsing_rights.SingleOrDefault(o => o.Item1 == bu.@delegate)?.Item2 ?? 0);
			
			ProcessBlockMetadata(blockMetadata, header.hash);
			Logger.LogInformation("Block " + (header.level + 1).ToString() + " baking data processed");
			return true;
		}

		class RewardMsg
		{
			public User User;
			public string Message;
			public string Tags;
			public UserAddress UserAddress;
		}
        void ProcessBlockMetadata(BlockMetadata blockMetadata, string hash)
        {
	        Logger.LogDebug($"ProcessBlockMetadata {blockMetadata.level.level}");
			var bakingRewards = blockMetadata.balance_updates.Where(o => o.kind == "contract" || (o.kind == "freezer" && o.category == "deposits"))
                .GroupBy(o => o.contract ?? o.@delegate).Select(o => new { Delegate = o.Key, Change = o.Sum(o1 => o1.change) }).ToList();
			//User,rewards,tags,lang
			List<RewardMsg> msgList = new List<RewardMsg>();
            foreach (var d in bakingRewards)
            {
                if (d.Change > 0)
                {
                    var ualist = repo.GetUserAddresses(d.Delegate).Where(o => o.NotifyBakingRewards).ToList();
                    if (ualist.Count > 0)
                    {
						DelegateInfo di;
						try
						{
							di = addrMgr.GetDelegate(client, hash, d.Delegate, true);
						}
						catch
						{
							continue;
						}
                        foreach (var ua in ualist)
                        {
							var t = Explorer.FromId(ua.User.Explorer);
							string result = resMgr.Get(Res.RewardDeliveredItem, new ContextObject { u = ua.User, ua = ua, Amount = d.Change / 1000000M }) + " ";
                            decimal bond = di.balance - di.frozen_balance + (di.frozen_balance_by_cycle.Count > 0 ? di.frozen_balance_by_cycle.Sum(o => o.deposit) : 0);
                            bond /= 1000000;
							ua.FullBalance = bond;
                            result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n\n";

							var reward = msgList.LastOrDefault(o => o.User == ua.User && o.UserAddress.ChatId == ua.ChatId);
							if (reward == null || reward.Message.Length > 3500)
							{
								reward = new RewardMsg { User = ua.User, Message = "", Tags = "", UserAddress = ua};
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
				SendTextMessageUA(msg.UserAddress, resMgr.Get(Res.RewardDelivered, new ContextObject { u = msg.User, Cycle = blockMetadata.level.cycle - 5 }) + "\n\n" + msg.Message + (msg.Tags != "" ? "\n#reward" + msg.Tags : ""));					
			}
			if (blockMetadata.level.cycle_position == 0)
            {
                var uad = repo.GetUserDelegates();
                foreach (var d in uad.Select(o => o.Address).Distinct())
                {
					DelegateInfo di;
					try
					{
						di = addrMgr.GetDelegate(client, hash, d, true);
					}
					catch
					{
						continue;
					}
					if (di != null)
					{
						decimal accured = di.frozen_balance_by_cycle.Where(o => o.cycle == (blockMetadata.level.cycle - 1)).Sum(o => o.rewards + o.fees);
						repo.UpdateDelegateAccured(d, (blockMetadata.level.cycle - 1), (long)accured);
					}
				}
				foreach (var usr in uad.Where(o => o.NotifyCycleCompletion).GroupBy(o => new { o.UserId, o.ChatId }))
                {
					string perf = resMgr.Get(Res.CycleCompleted, new ContextObject { u = usr.First().User, Cycle = blockMetadata.level.cycle - 1 });
                    foreach (var dr in usr)
                    {
                        var rew = rewardsManager.GetLastActualRewards(dr.Address);
						var rewMax = rewardsManager.GetLastMaxRewards(dr.Address);
						if (rewMax > 0)
                        {
							dr.Performance = 100M * rew / rewMax;
							perf += "\n\n" + resMgr.Get(Res.Performance, dr);
                            perf += "\n" + resMgr.Get(Res.Accrued, new ContextObject { u = usr.First().User, Cycle = blockMetadata.level.cycle - 1, Amount = rew / 1000000M });
                        }
                    }
                    if (!usr.First().User.HideHashTags)
                        perf += "\n\n#cycle" + String.Join("", usr.Select(o => o.HashTag()));
                    SendTextMessageUA(usr.First(), perf);
                }
				LoadAddressList();
				//for (int i = 1; i < 4; i++)
				//{
				//	if (!repo.IsRightsLoaded(blockMetadata.level.cycle + i))
				//		LoadBakingEndorsingRights(hash, blockMetadata.level.cycle + i);
				//}
			}
			//Завершен период подачи предложений
			if (blockMetadata.level.voting_period_position == 0 && blockMetadata.voting_period_kind == "testing_vote")
			{
				var proposals = client.GetProposals(hash + "~2");
				Dictionary<string, List<string>> supporters = proposals.Select(o => new { Hash = o.Key, Delegates = repo.GetProposalVotes(o.Key, blockMetadata.level.voting_period - 1) }).ToDictionary(o => o.Hash, o => o.Delegates);
				foreach(var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
				{
					var t = Explorer.FromId(u.Explorer);
					if (proposals.Count == 1)
					{
						string propHash = proposals.Single().Key;
						var p = repo.GetProposal(propHash);
						if (p == null)
							p = repo.AddProposal(propHash, null, blockMetadata.level.voting_period - 1);
						
						var delegateList = supporters[propHash];
						// Список поддержавших делегатов, которые мониторит юзер
						var addrList = repo.GetUserAddresses(u.Id).Where(o => delegateList.Contains(o.Address)).ToList();
						p.Delegates = addrList;
						p.VotedRolls = proposals.Single().Value;
						string tags = "";
						string result = resMgr.Get(Res.ProposalSelectedForVotingOne, new ContextObject { p = p, u = u, Block = blockMetadata.level.level });
						if (addrList.Count() > 0)
						{
							tags = String.Join("", addrList.Select(o => o.HashTag()));
							result += String.Join(", ", p.Delegates.Select(o => "<a href='" + t.account(o.Address) + "'>" + o.DisplayName() + "</a>").ToArray());
							result += "\n";
						}
						result += resMgr.Get(Res.ProposalSelectedForVoting, new ContextObject { p = p, u = u, Block = blockMetadata.level.level });

						if (!u.HideHashTags)
							result += "\n\n#proposal" + p.HashTag() + tags;
						SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
					}
					else
					{
						string propItems = "";
						string tags = "";
						foreach (var prop in proposals)
						{
							string propHash = prop.Key;
							var delegateList = supporters[propHash];
							var addrList = repo.GetUserAddresses(u.Id).Where(o => delegateList.Contains(o.Address)).ToList();
							string delegateListString = "";
							if (addrList.Count() > 0)
							{
								delegateListString = String.Join(", ", addrList.Select(o => $"<a href='{t.account(o.Address)}'>{o.DisplayName()}</a>").ToArray());
								tags += String.Join("", addrList.Select(o => o.HashTag()));
							}
							var p = repo.GetProposal(propHash);
							if (p == null)
								p = repo.AddProposal(propHash, null, blockMetadata.level.voting_period - 1);
							p.VotedRolls = prop.Value;
							propItems += resMgr.Get(Res.ProposalSelectedItem, new ContextObject { p = p, u = u, Block = blockMetadata.level.level }) + "\n" + delegateListString;
						}
						{
							var prop = proposals.OrderByDescending(o => o.Value).First();
							string propHash = prop.Key;
							var p = repo.GetProposal(propHash);
							p.VotedRolls = prop.Value;
							string result = resMgr.Get(Res.ProposalSelectedMany, new ContextObject { p = p, u = u, Block = blockMetadata.level.level }) + "\n" + propItems + "\n\n" +
								resMgr.Get(Res.ProposalSelectedForVoting, new ContextObject { p = p, u = u, Block = blockMetadata.level.level });
							if (!u.HideHashTags)
								result += "\n\n#proposal" + p.HashTag() + tags;
							SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
						}
					}
				}
			}
			Logger.LogDebug($"ProcessBlockMetadata {blockMetadata.level.level} completed");
		}

		private void LoadAddressList()
		{
			try
			{
				var t = Explorer.FromId(0);
				var delegates = repo.GetDelegates();
				var knownNames = repo.GetKnownAddresses();
				string result = "";
				int cnt = 0;
				string url = "https://api.tzkt.io/v1/accounts?type=delegate&limit=10000";
				string txt = client2.Download(url);
				List<string> updated = new List<string>();

				foreach (var a in JsonConvert.DeserializeObject<Account[]>(txt))
				{
					var addr = a.address;
					var name = a.alias;
					if (string.IsNullOrEmpty(a.alias))
						continue;
					updated.Add(addr);
					if (delegates.Any(o => o.Address == addr))
					{
						if (delegates.Any(o => o.Address == addr && o.Name != name))
						{
							repo.SetDelegateName(addr, name);
							result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
							cnt++;
						}
						continue;
					}
					if (knownNames.Any(o => o.Address == addr))
					{
						if (knownNames.Any(o => o.Address == addr && o.Name != name))
						{
							repo.SetKnownAddress(addr, name);
							result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
							cnt++;
						}
						continue;
					}
					repo.SetKnownAddress(addr, name);
					result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
					cnt++;
				}

				url = "https://raw.githubusercontent.com/blockwatch-cc/tzstats/master/src/config/aliases.js";
				txt = client2.Download(url);

				var matches = Regex.Matches(txt, "([tK][zT][a-zA-Z0-9]{34}):\\s{\\sname: ('|\")(.*?)\\2");
				if (matches.Count == 0)
					NotifyDev($"Parsing failed: {url}", 0);
				foreach (Match m in matches)
				{
					var addr = m.Groups[1].Value;
					if (updated.Contains(addr))
						continue;
					var name = m.Groups[3].Value;
					if (delegates.Any(o => o.Address == addr))
					{
						if (delegates.Any(o => o.Address == addr && o.Name != name))
						{
							repo.SetDelegateName(addr, name);
							result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
							cnt++;
						}
						continue;
					}
					if (knownNames.Any(o => o.Address == addr))
					{
						if (knownNames.Any(o => o.Address == addr && o.Name != name))
						{
							repo.SetKnownAddress(addr, name);
							result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
							cnt++;
						}
						continue;
					}
					repo.SetKnownAddress(addr, name);
					result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
					cnt++;
				}				

				result = "Updated names: " + cnt + "\n" + result;
				NotifyDev(result, 0, ParseMode.Html);
			}
			catch (Exception e)
			{
				LogError(e);
				NotifyDev("Fail to update address list from GitHub: " + e.Message, 0);
			}
		}

		async void OnCallbackQuery(object sc, CallbackQueryEventArgs ev)
        {
            try
            {
                var message = ev.CallbackQuery.Message;
                repo.LogMessage(ev.CallbackQuery.From, message.MessageId, null, ev.CallbackQuery.Data);
                Logger.LogInformation(UserTitle(ev.CallbackQuery.From) + ": button " + ev.CallbackQuery.Data);
                var u = repo.GetUser(ev.CallbackQuery.From.Id);
				var t = Explorer.FromId(u.Explorer);
				if (ev.CallbackQuery.Data == "donate")
				{
					InputOnlineFile file = new InputOnlineFile(File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tnb-qr.jpg")));
					await Bot.SendPhotoAsync(ev.CallbackQuery.From.Id, file, resMgr.Get(Res.DonateInfo, u), ParseMode.Html, replyMarkup: ReplyKeyboards.MainMenu(resMgr, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("twdelete "))
				{
					var twitterMessageId = int.Parse(ev.CallbackQuery.Data.Substring("twdelete ".Length));
					var twm = repo.GetTwitterMessage(twitterMessageId);
					if (twm != null && twm.TwitterId != null)
					{
						await twitter.DeleteTweetAsync(twm.TwitterId);
						repo.DeleteTwitterMessage(twm);
						await Bot.DeleteMessageAsync(ev.CallbackQuery.Message.Chat.Id, message.MessageId);
					}
				}
				if (ev.CallbackQuery.Data.StartsWith("deleteaddress"))
                {
                    var ua = repo.RemoveAddr(ev.CallbackQuery.From.Id, ev.CallbackQuery.Data.Substring("deleteaddress ".Length));
                    if (ua != null)
                    {
                        string result = resMgr.Get(Res.AddressDeleted, ua);
                        if (!u.HideHashTags)
                            result += "\n\n#deleted" + ua.HashTag();
                        SendTextMessage(u.Id, result, null, ev.CallbackQuery.Message.MessageId);
						NotifyUserActivity($"User {UserLink(u)} deleted [{ua.Address}]({t.account(ua.Address)})");
                    }
                    else
                        SendTextMessage(u.Id, resMgr.Get(Res.AddressNotExist, u), null, ev.CallbackQuery.Message.MessageId);
                }
                if (ev.CallbackQuery.Data.StartsWith("addaddress"))
                {
                    var addr = ev.CallbackQuery.Data.Substring("addaddress ".Length);
                    OnNewAddressEntered(u, addr);
                }
                if (ev.CallbackQuery.Data.StartsWith("setthreshold"))
                {
                    var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("setthreshold ".Length));
                    if (ua != null)
                    {
                        u.UserState = UserState.SetAmountThreshold;
                        u.EditUserAddressId = ua.Id;
                        repo.UpdateUser(u);
						string result = resMgr.Get(Res.EnterAmountThreshold, ua);
                        if (!u.HideHashTags)
                            result += "\n\n#txthreshold" + ua.HashTag();
                        SendTextMessage(u.Id, result, ReplyKeyboards.BackMenu(resMgr, u));
                    }
                    else
                        SendTextMessage(u.Id, resMgr.Get(Res.AddressNotExist, u), null, ev.CallbackQuery.Message.MessageId);
				}
				if (ev.CallbackQuery.Data.StartsWith("setname"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("setname ".Length));
					if (ua != null)
					{
						u.UserState = UserState.SetName;
						u.EditUserAddressId = ua.Id;
						repo.UpdateUser(u);
						string result = resMgr.Get(Res.EnterNewName, ua);
						if (!u.HideHashTags)
							result += "\n\n#rename" + ua.HashTag();
						SendTextMessage(u.Id, result, ReplyKeyboards.BackMenu(resMgr, u));
					}
					else
						SendTextMessage(u.Id, resMgr.Get(Res.AddressNotExist, u), null, ev.CallbackQuery.Message.MessageId);
				}
				if (ev.CallbackQuery.Data.StartsWith("notifyfollowers"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("notifyfollowers ".Length));
					if (ua != null)
					{
						u.UserState = UserState.NotifyFollowers;
						u.EditUserAddressId = ua.Id;
						repo.UpdateUser(u);
						string result = resMgr.Get(Res.EnterMessageForAddressFollowers, ua);
						foreach (User u1 in GetFollowers(ua.Address))
							result += "\n" + u1.ToString() + $" [{u1.Id}]";
						SendTextMessage(u.Id, result, ReplyKeyboards.BackMenu(resMgr, u));
					}
					else
						SendTextMessage(u.Id, resMgr.Get(Res.AddressNotExist, u), null, ev.CallbackQuery.Message.MessageId);
				}
				if (ev.CallbackQuery.Data.StartsWith("setdlgthreshold"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("setdlgthreshold ".Length));
					if (ua != null)
					{
						u.UserState = UserState.SetDlgAmountThreshold;
						u.EditUserAddressId = ua.Id;
						repo.UpdateUser(u);
						string result = resMgr.Get(Res.EnterDlgAmountThreshold, ua);
						if (!u.HideHashTags)
							result += "\n\n#dlgthreshold" + ua.HashTag();
						SendTextMessage(u.Id, result, ReplyKeyboards.BackMenu(resMgr, u));
					}
					else
						SendTextMessage(u.Id, resMgr.Get(Res.AddressNotExist, u), null, ev.CallbackQuery.Message.MessageId);
				}
				if (ev.CallbackQuery.Data == "set_explorer")
				{
					SendTextMessage(u.Id, resMgr.Get(Res.ChooseExplorer, u), ReplyKeyboards.ExplorerSettings(u), ev.CallbackQuery.Message.MessageId);
				}
				else if (ev.CallbackQuery.Data.StartsWith("set_explorer_"))
				{
					int exp = int.Parse(ev.CallbackQuery.Data.Substring("set_explorer_".Length));
					if (u.Explorer != exp)
					{
						u.Explorer = exp;
						repo.UpdateUser(u);
						SendTextMessage(u.Id, resMgr.Get(Res.ChooseExplorer, u), ReplyKeyboards.ExplorerSettings(u), ev.CallbackQuery.Message.MessageId);
					}
				}
				else if (ev.CallbackQuery.Data.StartsWith("set_whalealert"))
				{					
					SendTextMessage(u.Id, resMgr.Get(Res.WhaleAlertsTip, u), ReplyKeyboards.WhaleAlertSettings(resMgr, u), ev.CallbackQuery.Message.MessageId);
				}
				else if (ev.CallbackQuery.Data.StartsWith("set_wa_"))
				{
					int wat = int.Parse(ev.CallbackQuery.Data.Substring("set_wa_".Length));
					u.WhaleAlertThreshold = wat * 1000;
					repo.UpdateUser(u);
					SendTextMessage(u.Id, resMgr.Get(Res.WhaleAlertSet, u), null, ev.CallbackQuery.Message.MessageId);
				}
				else if (ev.CallbackQuery.Data.StartsWith("set_nialert"))
				{
					SendTextMessage(u.Id, resMgr.Get(Res.NetworkIssueAlertsTip, u), ReplyKeyboards.NetworkIssueAlertSettings(resMgr, u), ev.CallbackQuery.Message.MessageId);
				}
				else if (ev.CallbackQuery.Data.StartsWith("set_ni_"))
				{
					int nin = int.Parse(ev.CallbackQuery.Data.Substring("set_ni_".Length));
					u.NetworkIssueNotify = nin;
					repo.UpdateUser(u);
					SendTextMessage(u.Id, resMgr.Get(Res.NetworkIssueAlertSet, u), null, ev.CallbackQuery.Message.MessageId);
				}
				else if (ev.CallbackQuery.Data.StartsWith("set_"))
                {
                    u.Language = ev.CallbackQuery.Data.Substring("set_".Length);
                    repo.UpdateUser(u);
                    SendTextMessage(u.Id, resMgr.Get(Res.Welcome, u), ReplyKeyboards.MainMenu(resMgr, u));
                }
                if (ev.CallbackQuery.Data.StartsWith("bakingon"))
                {
                    var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("bakingon ".Length));
                    if (ua != null)
                    {
                        ua.NotifyBakingRewards = true;
                        repo.UpdateBalance(ua);
                        ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                    else
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("manageaddress"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("manageaddress ".Length));
					if (ua != null)
					{
						ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
					}
					else
						await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("bakingoff"))
                {
                    var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("bakingoff ".Length));
                    if (ua != null)
                    {
                        ua.NotifyBakingRewards = false;
                        repo.UpdateBalance(ua);
                        ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                    else
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));

				}
                if (ev.CallbackQuery.Data.StartsWith("cycleon"))
                {
                    var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("cycleon ".Length));
                    if (ua != null)
                    {
                        ua.NotifyCycleCompletion = true;
                        repo.UpdateBalance(ua);
                        ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                    else
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));

				}
                if (ev.CallbackQuery.Data.StartsWith("cycleoff"))
                {
                    var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("cycleoff ".Length));
                    if (ua != null)
                    {
                        ua.NotifyCycleCompletion = false;
                        repo.UpdateBalance(ua);
                        ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                    else
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));

				}
                if (ev.CallbackQuery.Data.StartsWith("tranon"))
                {
                    var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("tranon ".Length));
                    if (ua != null)
                    {
                        ua.NotifyTransactions = true;
                        repo.UpdateBalance(ua);
                        ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                    else
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));

				}
                if (ev.CallbackQuery.Data.StartsWith("tranoff"))
                {
                    var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("tranoff ".Length));
                    if (ua != null)
                    {
                        ua.NotifyTransactions = false;
                        repo.UpdateBalance(ua);
                        ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
                    }
                    else
                        await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("dlgon"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("dlgon ".Length));
					if (ua != null)
					{
						ua.NotifyDelegations = true;
						repo.UpdateBalance(ua);
						ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
					}
					else
						await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("dlgoff"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("dlgoff ".Length));
					if (ua != null)
					{
						ua.NotifyDelegations = false;
						repo.UpdateBalance(ua);
						ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
					}
					else
						await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("misseson"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("misseson ".Length));
					if (ua != null)
					{
						ua.NotifyMisses = true;
						repo.UpdateBalance(ua);
						ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
					}
					else
						await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("missesoff"))
				{
					var ua = repo.GetUserAddresses(ev.CallbackQuery.From.Id).FirstOrDefault(o => o.Id.ToString() == ev.CallbackQuery.Data.Substring("missesoff ".Length));
					if (ua != null)
					{
						ua.NotifyMisses = false;
						repo.UpdateBalance(ua);
						ViewAddress(u.Id, ua, ev.CallbackQuery.Message.MessageId)();
					}
					else
						await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, resMgr.Get(Res.AddressNotExist, u));
				}
				if (ev.CallbackQuery.Data.StartsWith("hidehashtags"))
                {
                    u.HideHashTags = true;
                    repo.UpdateUser(u);
                    SendTextMessage(u.Id, "Settings", ReplyKeyboards.Settings(resMgr, u, Config.Telegram), ev.CallbackQuery.Message.MessageId);
                }
                if (ev.CallbackQuery.Data.StartsWith("showhashtags"))
                {
                    u.HideHashTags = false;
                    repo.UpdateUser(u);
                    SendTextMessage(u.Id, "Settings", ReplyKeyboards.Settings(resMgr, u, Config.Telegram), ev.CallbackQuery.Message.MessageId);
				}
				if (ev.CallbackQuery.Data.StartsWith("showvotingnotify"))
				{
					u.VotingNotify = true;
					repo.UpdateUser(u);
					SendTextMessage(u.Id, "Settings", ReplyKeyboards.Settings(resMgr, u, Config.Telegram), ev.CallbackQuery.Message.MessageId);
				}
				if (ev.CallbackQuery.Data.StartsWith("hidevotingnotify"))
				{
					u.VotingNotify = false;
					repo.UpdateUser(u);
					SendTextMessage(u.Id, "Settings", ReplyKeyboards.Settings(resMgr, u, Config.Telegram), ev.CallbackQuery.Message.MessageId);
				}
				if (Config.DevUserNames.Contains(u.Username))
                {
                    if (ev.CallbackQuery.Data.StartsWith("broadcast"))
                    {
                        u.UserState = UserState.Broadcast;
                        SendTextMessage(u.Id, "Enter your message for [" + u.Language + "] bot users", ReplyKeyboards.BackMenu(resMgr, u));
                    }
                    if (ev.CallbackQuery.Data.StartsWith("getuserlist"))
                    {
                        OnSql(u, "select * from user");
                    }
                    if (ev.CallbackQuery.Data.StartsWith("getuseraddresses"))
                    {
                        OnSql(u, "select * from user_address");
                    }
                    if (ev.CallbackQuery.Data.StartsWith("getusermessages"))
                    {
                        OnSql(u, "select * from message");
                    }
                    if (ev.CallbackQuery.Data.StartsWith("getlog"))
                    {
                        var dir = new DirectoryInfo(LogsPath);
                        var files = dir.GetFiles("*.log");
                        MemoryStream outputMemStream = new MemoryStream();
                        var zipStream = new ZipOutputStream(outputMemStream);
                        zipStream.SetLevel(9);
                        foreach (var f in files)
                        {
                            if (DateTime.Now.Subtract(f.LastWriteTime).TotalDays > 14)
                                continue;
                            var newEntry = new ZipEntry(f.Name);
                            newEntry.DateTime = f.LastWriteTime;
                            zipStream.PutNextEntry(newEntry);
                            using (var fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
								StreamUtils.Copy(fs, zipStream, new byte[4096]);
                            zipStream.CloseEntry();                            
                        }
                        zipStream.IsStreamOwner = false;
                        zipStream.Close();
                        outputMemStream.Position = 0;
                        var tf = new InputOnlineFile(outputMemStream, "logs.zip");
                        Bot.SendDocumentAsync(u.Id, tf).ConfigureAwait(true).GetAwaiter().GetResult();
                    }
                    if (ev.CallbackQuery.Data.StartsWith("getdb"))
                    {
                        MemoryStream outputMemStream = new MemoryStream();
                        var zipStream = new ZipOutputStream(outputMemStream);
                        zipStream.SetLevel(9);
                        var newEntry = new ZipEntry("tezosnotifydata.db");
                        newEntry.DateTime = DateTime.Now;
                        zipStream.PutNextEntry(newEntry);
                        using (var fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tezosnotifydata.db"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
							StreamUtils.Copy(fs, zipStream, new byte[4096]);
                        zipStream.CloseEntry();
                        zipStream.IsStreamOwner = false;
                        zipStream.Close();
                        outputMemStream.Position = 0;
                        var tf = new InputOnlineFile(outputMemStream, "tezosnotifydata.zip");
                        Bot.SendDocumentAsync(u.Id, tf).ConfigureAwait(true).GetAwaiter().GetResult();
                    }
                    if (ev.CallbackQuery.Data.StartsWith("cmd"))
                    {
                        var cmd = Commands[int.Parse(ev.CallbackQuery.Data.Substring("cmd".Length))];
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
                        Logger.LogInformation(UserTitle(ev.CallbackQuery.From) + " started: "+ cmd.filepath + " " + cmd.arguments);
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
                                SendTextMessage(u.Id, resultSplit, ReplyKeyboards.MainMenu(resMgr, u));
                            } while (pos < result.Length);
                        }
                        catch(Exception ex)
                        {
                            LogError(ex);
                            SendTextMessage(u.Id, "❗️" + ex.Message, ReplyKeyboards.MainMenu(resMgr, u));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

		List<User> GetFollowers(string addr)
		{
			var di = client2.GetDelegateInfo(addr);
			var results = repo.GetUserAddresses(addr).Select(o => o.User).ToList();
			foreach(var d in di.delegated_contracts)
			{
				var users = repo.GetUserAddresses(d);
				foreach (var u in users)
					if (!results.Contains(u.User))
						results.Add(u.User);
			}
			return results;
		}

        void OnUpdate(object su, UpdateEventArgs evu)
        {
			if (evu.Update.ChosenInlineResult != null)
			{
				OnNewAddressEntered(repo.GetUser(evu.Update.ChosenInlineResult.From.Id), evu.Update.ChosenInlineResult.ResultId);
				return;
			}
			if (evu.Update.InlineQuery != null)
			{
				var q = evu.Update.InlineQuery.Query.Trim().ToLower();
				q = q.Replace("'", "").Replace("`", "").Replace(" ", "").Replace("а", "a").Replace("б", "b").Replace("в", "v").Replace("г", "g").Replace("д", "d").Replace("е", "e").Replace("ж", "zh").
					Replace("з", "z").Replace("и", "i").Replace("й", "y").Replace("к", "k").Replace("л", "l").Replace("м", "m").Replace("н", "n").
					Replace("о", "o").Replace("п", "p").Replace("р", "r").Replace("с", "s").Replace("т", "t").Replace("у", "u").Replace("ф", "f").
					Replace("х", "h").Replace("ц", "c").Replace("ч", "ch").Replace("ш", "sh").Replace("щ", "sh").Replace("э", "e").Replace("ю", "u").Replace("я", "ya");
				if (q.Length < 3)
					return;
				var ka = repo.GetKnownAddresses().Where(o => o.Name.Replace("'", "").Replace("`", "").Replace(" ", "").ToLower().Contains(q)).Select(o => new { o.Address, o.Name });
				var da = repo.GetDelegates().Where(o => o.Name != null && o.Name.Replace("'", "").Replace("`", "").Replace(" ", "").ToLower().Contains(q)).Select(o => new { o.Address, o.Name });
				var results = ka.Union(da).GroupBy(o => o.Address).Select(o => new { Address = o.Key, Name = o.First().Name }).OrderBy(o => o.Name).
					Select(o => new InlineQueryResultArticle(o.Address, o.Name, new InputTextMessageContent($"<i>{o.Address}</i>\n<b>{o.Name}</b>") { ParseMode = ParseMode.Html }) { Description = o.Address });
				//evu.Update.InlineQuery.
				Bot.AnswerInlineQueryAsync(evu.Update.InlineQuery.Id, results.Take(50), 10);
				return;
			}
            if (evu.Update.CallbackQuery != null || evu.Update.InlineQuery != null) return;
            var update = evu.Update;
            var message = update.Message;
            //if (update.ChannelPost != null && update.ChannelPost.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            //{
            //    var user = repo.GetUser(update.ChannelPost.From.Id);
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
            if (message == null) return;
            try
            {
                if (message.From.IsBot)
                    return;
				var u = repo.UserExists(message.From.Id) ? repo.GetUser(message.From.Id) : null;

				if (message.Type == MessageType.Photo &&
					message.Chat.Type == ChatType.Private &&
					(u?.UserState == UserState.Broadcast ||
					u?.UserState == UserState.NotifyFollowers))
				{
					int count = 0;
					using (var fileStream = new MemoryStream())
					{
						var fileInfo = Bot.GetInfoAndDownloadFileAsync(
							 fileId: message.Photo.OrderByDescending(o => o.Width).First().FileId,
							 destination: fileStream
						).ConfigureAwait(true).GetAwaiter().GetResult();
						fileStream.Seek(0, SeekOrigin.Begin);

						List<User> users = repo.GetUsers().Where(o => !o.Inactive && o.Language == u.Language).ToList();
						if (u.UserState == UserState.NotifyFollowers)
						{
							var ua = repo.GetUserAddresses(u.Id).FirstOrDefault(o => o.Id == u.EditUserAddressId);
							users = GetFollowers(ua.Address);
						}
						var ifo = new InputOnlineFile(fileStream);
						foreach (var user1 in users)
						{
							try
							{
								var caption = ApplyEntities(message.Caption, message.CaptionEntities);
								Bot.SendPhotoAsync(user1.Id, ifo, caption: caption, parseMode: ParseMode.Html, replyMarkup: ReplyKeyboards.MainMenu(resMgr, user1)).ConfigureAwait(true).GetAwaiter().GetResult();
								Thread.Sleep(50);
								count++;
							}
							catch (ChatNotFoundException)
							{
								user1.Inactive = true;
								repo.UpdateUser(user1);
								NotifyUserActivity("😕 User " + UserLink(user1) + " not started chat with bot");
							}
							catch (ApiRequestException are)
							{
								NotifyUserActivity("🐞 Error while sending message for " + UserLink(u) + ": " + are.Message);
								if (are.Message.StartsWith("Forbidden"))
								{
									u.Inactive = true;
									repo.UpdateUser(u);
								}
								else
									LogError(are);
							}
							catch (Exception ex)
							{
								if (ex.Message == "Forbidden: bot was blocked by the user")
								{
									u.Inactive = true;
									repo.UpdateUser(u);
									NotifyUserActivity("😕 Bot was blocked by the user " + UserLink(u));
								}
								else
									LogError(ex);
							}
						}
					}
					SendTextMessage(u.Id, resMgr.Get(u.UserState == UserState.Broadcast ? Res.MessageDelivered : Res.MessageDeliveredForUsers, u) + " (" + count.ToString() + ")", ReplyKeyboards.MainMenu(resMgr, u));
					u.UserState = UserState.Default;

				}

				if (message.Type == MessageType.Text &&
                    message.Chat.Type == ChatType.Private)
                {
                    bool newUser = !repo.UserExists(message.From.Id);

                    repo.LogMessage(message.From, message.MessageId, message.Text, null);
					u = repo.GetUser(message.From.Id);
					Logger.LogInformation(UserTitle(message.From) + ": " + message.Text);
                    if (newUser)
						NotifyUserActivity("🔅 New user: " + UserLink(u));
                    bool welcomeBack = false;
                    if (u.Inactive)
                    {
                        u.Inactive = false;
                        repo.UpdateUser(u);
                        welcomeBack = true;
						NotifyUserActivity("🤗 User " + UserLink(u) + " is back");
					}
					if (message.Text.StartsWith("/start"))
					{						
						if (newUser || welcomeBack)
							SendTextMessage(u.Id, welcomeBack ? resMgr.Get(Res.WelcomeBack, u) : resMgr.Get(Res.Welcome, u), ReplyKeyboards.MainMenu(resMgr, u));
						var cmd = message.Text.Substring("/start".Length).Replace("_", " ").Trim();
						if (Regex.IsMatch(cmd, "(tz|KT)[a-zA-Z0-9]{34}"))
						{
							var explorer = Explorer.FromStart(message.Text);
							u.Explorer = explorer.id;
							repo.UpdateUser(u);
							var addr = cmd.Replace(explorer.buttonprefix + " ", "");
							OnNewAddressEntered(u, addr);							
						}
						else if (!newUser && !welcomeBack)
							SendTextMessage(u.Id, resMgr.Get(Res.Welcome, u), ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (Config.DevUserNames.Contains(message.From.Username) && message.ReplyToMessage != null && message.ReplyToMessage.Entities.Length > 0 &&
						message.ReplyToMessage.Entities[0].User != null)
					{
						var replyUser = repo.GetUser(message.ReplyToMessage.Entities[0].User.Id);
						SendTextMessage(replyUser.Id, resMgr.Get(Res.SupportReply, replyUser) + "\n\n" + message.Text, ReplyKeyboards.MainMenu(resMgr, replyUser));
						NotifyDev("📤 Message for " + UserLink(replyUser) + " from " + UserLink(u) + ":\n\n" + message.Text.Replace("_", "__").Replace("`", "'").Replace("*", "**").Replace("[", "(").Replace("]", ")") + "\n\n#outgoing", u.Id);
					}
					/*else if (u.UserState == Model.UserState.Default && Regex.IsMatch(message.Text.Replace(" ", "").Replace("\r", "").Replace("\n", ""), "^[a-fA-F0-9]*$"))
                    {
                        var result = client2.Inject(message.Text.Replace(" ", "").Replace("\r", "").Replace("\n", ""));
                        if (result.StartsWith("\"o") && result.Length == 54)
                        {
                            result = JsonConvert.DeserializeObject<string>(result);
                            string url = $"https://tzscan.io/{result}";
                            NotifyDev("💉 User " + UserLink(u) + " injected operation: " + url, u.UserId);
                            SendTextMessage(u.UserId, "✔️ " + url, ReplyKeyboards.MainMenu(resMgr, u));
                        }
                        else
                        {
                            NotifyDev("💉 User " + UserLink(u) + " injected operation failed: " + result, u.UserId);
                            SendTextMessage(u.UserId, result, ReplyKeyboards.MainMenu(resMgr, u));
                        }
                    }*/
					else if (message.Text == ReplyKeyboards.CmdNewAddress(resMgr, u))
					{
						OnNewAddress(u);
					}
					else if (message.Text.StartsWith("/forward") && u.IsAdmin(Config.Telegram))
					{
						var msgid = int.Parse(message.Text.Substring("/forward".Length).Trim());
						var msg = repo.GetMessage(msgid);
						var m = Bot.ForwardMessageAsync(msg.UserId, msg.UserId, msg.TelegramMessageId).ConfigureAwait(true).GetAwaiter().GetResult();
						SendTextMessage(u.Id, $"Message forwarded for user {UserLink(repo.GetUser(msg.UserId))}", ReplyKeyboards.MainMenu(resMgr, u), parseMode: ParseMode.Markdown);
						Bot.ForwardMessageAsync(u.Id, msg.UserId, msg.TelegramMessageId);
					}
					else if (message.Text.StartsWith("/help") && Config.DevUserNames.Contains(message.From.Username))
					{
						SendTextMessage(u.Id, @"Administrator commands:
/sql {query} - run sql query
/block - show current block
/setblock {number} - set last processed block
/names
{addr1} {name1}
{addr2} {name2}
...etc - add public known addresses
/names - show public known addresses
/defaultnode - switch to localnode
/node - list and check nodes
/setdelegatename {addr} {name} - set delegate name
/addrlist {userid} - view user addresses
/msglist {userid} - view user messages (last month)
/userinfo {userid} - view user info and settings
/set_ru - switch tu russian
/set_en - switch to english
/clearcache - clear address cache
/forward messageid - forward message to user and caller
/twclean - clean published twitter messages", ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text.StartsWith("/sql") && Config.DevUserNames.Contains(message.From.Username))
					{
						OnSql(u, message.Text.Substring("/sql".Length));
					}
					else if (message.Text.StartsWith("/clearcache") && Config.DevUserNames.Contains(message.From.Username))
					{
						addrMgr.Clear();
						NotifyDev("ℹ️ Cache is cleared", 0);
					}
					else if (message.Text == "/twclean")
					{
						int cnt = 0;
						foreach(var twm in repo.GetTwitterMessages(DateTime.Now.AddDays(-7)))
						{
							if (twm.TwitterId != null)
							{
								twitter.DeleteTweetAsync(twm.TwitterId).ConfigureAwait(true).GetAwaiter().GetResult();
								repo.DeleteTwitterMessage(twm);
								cnt++;
							}
						}
						SendTextMessage(u.Id, $"Deleted tweets: {cnt}", ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text == ReplyKeyboards.CmdMyAddresses(resMgr, u))
					{
						OnMyAddresses(message.From.Id, u);
					}
					else if (message.Text.StartsWith("/addrlist") && Config.DevUserNames.Contains(message.From.Username))
					{
						if (int.TryParse(message.Text.Substring("/addrlist".Length).Trim(), out int userid))
						{
							var u1 = repo.GetUser(userid);
							if (u1 != null)
								OnMyAddresses(message.From.Id, u1);
							else
								SendTextMessage(u.Id, $"User not found: {userid}", ReplyKeyboards.MainMenu(resMgr, u));
						}
						else
							SendTextMessage(u.Id, "Command syntax:\n/addrlist {userid}", ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text.StartsWith("/msglist") && Config.DevUserNames.Contains(message.From.Username))
					{
						if (int.TryParse(message.Text.Substring("/msglist".Length).Trim(), out int userid))
						{
							OnSql(u, $"select * from Messages where UserId = {userid} and CreateDate >= DATE('now', '-1 month') order by CreateDate");
						}
						else
							SendTextMessage(u.Id, "Command syntax:\n/msglist {userid}", ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text.StartsWith("/userinfo") && Config.DevUserNames.Contains(message.From.Username))
					{
						if (int.TryParse(message.Text.Substring("/userinfo".Length).Trim(), out int userid))
						{
							var u1 = repo.GetUser(userid);
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
								SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
							}
							else
								SendTextMessage(u.Id, $"User not found: {userid}", ReplyKeyboards.MainMenu(resMgr, u));
						}
						else
							SendTextMessage(u.Id, "Command syntax:\n/addrlist {userid}", ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if(message.Text == "/res" && Config.DevUserNames.Contains(message.From.Username))
					{
						var ua1 = repo.GetUserAddresses(u.Id)[0];
						ua1.AveragePerformance = 92.321M;
						ua1.Delegators = 123;
						ua1.FreeSpace = 190910312.123M;
						ua1.FullBalance = 1392103913.1451M;
						ua1.Performance = 12.32M;
						ua1.StakingBalance = 178128312.23M;

						var ua2 = repo.GetUserAddresses(u.Id)[1];
						var ua3 = repo.GetUserAddresses(u.Id)[2];
						var p = repo.GetProposal("PsDELPH1Kxsxt8f9eWbxQeRxkjfbxoqM52jvs5Y5fBxWWh4ifpo");
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
							u = u,
							ua = ua1,
							ua_from = ua2,
							ua_to = ua3
						};
						foreach (Res res in Enum.GetValues(typeof(Res)))
						{
							SendTextMessage(u.Id, $"#{res.ToString()}\n\n" + resMgr.Get(res, contextObject), ReplyKeyboards.MainMenu(resMgr, u));
						}
					}
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
								q = q.Replace("'", "").Replace("`", "").Replace(" ", "").Replace("а", "a").Replace("б", "b").Replace("в", "v").Replace("г", "g").Replace("д", "d").Replace("е", "e").Replace("ж", "zh").
									Replace("з", "z").Replace("и", "i").Replace("й", "y").Replace("к", "k").Replace("л", "l").Replace("м", "m").Replace("н", "n").
									Replace("о", "o").Replace("п", "p").Replace("р", "r").Replace("с", "s").Replace("т", "t").Replace("у", "u").Replace("ф", "f").
									Replace("х", "h").Replace("ц", "c").Replace("ч", "ch").Replace("ш", "sh").Replace("щ", "sh").Replace("э", "e").Replace("ю", "u").Replace("я", "ya");
								var da = repo.GetDelegates().Where(o => o.Name != null && o.Name.Replace("'", "").Replace("`", "").Replace(" ", "").ToLower().Contains(q)).Select(o => new { o.Address, o.Name });
								addr = da.Select(o => o.Address).FirstOrDefault();
							}
						}
						if (addr != null && cycle > 0)
						{
							var t = Explorer.FromId(u.Explorer);
							var d = repo.GetDelegateName(addr);
							string result = $"Baking statistics for <a href='{t.account(addr)}'>{d}</a> in cycle {cycle}:\n";
							var bu = repo.GetBalanceUpdates(addr, cycle);
							result += $"Baked / Missed: <b>{bu.Count(o => o.Type == 1)} ({(bu.Where(o => o.Type == 1).Sum(o => o.Amount) / 1000000M).TezToString()}) / {bu.Count(o => o.Type == 3)} ({(bu.Where(o => o.Type == 3).Sum(o => o.Amount) / 1000000M).TezToString()})</b>\n";
							result += $"Endorsed / Missed: <b>{bu.Where(o => o.Type == 2).Sum(o => o.Slots)} ({(bu.Where(o => o.Type == 2).Sum(o => o.Amount) / 1000000M).TezToString()}) / {bu.Where(o => o.Type == 4).Sum(o => o.Slots)} ({(bu.Where(o => o.Type == 4).Sum(o => o.Amount) / 1000000M).TezToString()})</b>\n";
							result += $"Actual / Maximum rewards: <b>{(bu.Where(o => o.Type < 3).Sum(o => o.Amount) / 1000000M).TezToString()} / {(bu.Sum(o => o.Amount) / 1000000M).TezToString()}</b>\n";
							result += "\nAverage 10-cycle performance calculation\n";
							result += "<pre>Cycle      Actual      Maximum</pre>\n";
							decimal rew = 0;
							decimal max = 0;
							for (int i = 0; i < 10; i++)
							{
								var rew1 = repo.GetRewards(addr, cycle - i, false);
								var max1 = repo.GetRewards(addr, cycle - i, true);
								rew += rew1;
								max += max1;
								result += $"<pre> {cycle - i} {(rew1 / 1000000).ToString("0.00").PadLeft(12, ' ')} {(max1 / 1000000).ToString("0.00").PadLeft(12, ' ')}</pre>\n";
							}
							if (max > 0)
							{
								result += $"Average 10-cycle performance: {(100M * rew / max).ToString("#0.000")}%\n";
							}

							SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
						}
						else
						{
							SendTextMessage(u.Id, "Command syntax:\n/perf <i>cycle</i> <i>delegate</i>", ReplyKeyboards.MainMenu(resMgr, u));
						}
					}
					else if (message.Text == "/info")
					{
						Info(update);
					}
					else if (message.Text == "/stat")
					{
						Stat(update);
					}
					else if (message.Text == "/set_ru")
					{
						u.Language = "ru";
						repo.UpdateUser(u);
						SendTextMessage(u.Id, resMgr.Get(Res.Welcome, u), ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text == "/set_en")
					{
						u.Language = "en";
						repo.UpdateUser(u);
						SendTextMessage(u.Id, resMgr.Get(Res.Welcome, u), ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text == "/block")
					{
						int l = repo.GetLastBlockLevel().Item1;
						int c = (l - 1) / 4096;
						int p = l - c * 4096 - 1;
						SendTextMessage(u.Id, $"Last block processed: {l}, cycle {c}, position {p}", ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text.StartsWith("/setblock") && Config.DevUserNames.Contains(message.From.Username))
					{
						if (int.TryParse(message.Text.Substring("/setblock ".Length), out int num))
						{
							var h = client2.GetBlockHeader(num);
							repo.SetLastBlockLevel(num, h.priority, h.hash);
							lastBlockChanged = true;
							var c = tzStats.GetCycle(repo.GetLastBlockLevel().Item1);
							NotifyDev($"Last block processed changed: {repo.GetLastBlockLevel().Item1}, {repo.GetLastBlockLevel().Item3}\nCurrent cycle: {c.cycle}, rolls: {c.snapshot_cycle.rolls}", 0);
							_currentConstants = null;
						}
					}
					else if (message.Text.StartsWith("/names") && Config.DevUserNames.Contains(message.From.Username))
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
								repo.SetKnownAddress(addr, name);
								result += $"<a href='{t.account(addr)}'>{addr.ShortAddr()}</a> {name}\n";
							}
						}
						if (result != "")
							NotifyDev("Установлены названия:\n\n" + result, 0, ParseMode.Html);
						else
						{
							foreach (var item in repo.GetKnownAddresses())
							{
								result += $"<a href='{t.account(item.Address)}'>{item.Address.ShortAddr()}</a> {item.Name}";
							}
							SendTextMessage(u.Id, "Известные адреса:\n" + result, ReplyKeyboards.MainMenu(resMgr, u));
						}
					}
					else if (Config.DevUserNames.Contains(message.From.Username) && message.Text.StartsWith("/processmd"))
					{
						var md = client2.GetBlockMetadata(message.Text.Substring("/processmd ".Length));
						ProcessBlockMetadata(md, message.Text.Substring("/processmd ".Length));
					}
					else if (Config.DevUserNames.Contains(message.From.Username) && message.Text == "/defaultnode")
					{
						client.SetNodeUrl(Nodes[0].Url);
						client2.SetNodeUrl(Nodes[0].Url);
						NotifyDev("Switched to " + Nodes[0].Name, 0);
					}
					else if (Config.DevUserNames.Contains(message.From.Username) && message.Text.StartsWith("/node"))
					{
						var n = message.Text.Substring(5);
						if (int.TryParse(n, out int n_i) && n_i < Nodes.Count)
						{
							var node = Nodes[n_i];
							client.SetNodeUrl(node.Url);
							client2.SetNodeUrl(node.Url);
							NotifyDev("Switched to " + node.Name, 0);
							CurrentNode = node;
						}
						else
						{
							string result = $"Current node: {CurrentNode.Name}\n\n";
							for (int i = 0; i < Nodes.Count; i++)
							{
								result += $"/node{i} = {Nodes[i].Name}\n<b>Status:</b> {Nodes[i].CheckStatus()}\n\n";
							}
							SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
						}
					}
					else if (Config.DevUserNames.Contains(message.From.Username) && message.Text.StartsWith("/loaddelegatelist"))
					{
						//LoadDelegateList();
						SendTextMessage(u.Id, "Not implemented", ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (Config.DevUserNames.Contains(message.From.Username) && message.Text.StartsWith("/setdelegatename"))
					{
						var dn = message.Text.Substring("/setdelegatename ".Length);
						var dnMatch = Regex.Match(dn, "(tz[a-zA-Z0-9]{34})(.*)");
						if (dnMatch.Success)
						{
							string addr = dnMatch.Groups[1].Value.Trim();
							string name = dnMatch.Groups[2].Value.Trim();
							repo.SetDelegateName(addr, name);
							SendTextMessage(u.Id, $"👑 {addr}: <b>{name}</b>", ReplyKeyboards.MainMenu(resMgr, u));
						}
					}
					else if (Regex.IsMatch(message.Text, "(tz|KT)[a-zA-Z0-9]{34}") && u.UserState != UserState.Broadcast && u.UserState != UserState.Support && u.UserState != UserState.NotifyFollowers)
					{
						OnNewAddressEntered(u, message.Text);
					}
					else if (message.Text == ReplyKeyboards.CmdGoBack(resMgr, u))
					{
						SendTextMessage(u.Id, resMgr.Get(Res.SeeYou, u), ReplyKeyboards.MainMenu(resMgr, u));
					}
					else if (message.Text == ReplyKeyboards.CmdContact(resMgr, u))
					{
						u.UserState = UserState.Support;
						SendTextMessage(u.Id, resMgr.Get(Res.WriteHere, u), ReplyKeyboards.BackMenu(resMgr, u));
						return;
					}
					else if (message.Text == ReplyKeyboards.CmdSettings(resMgr, u))
					{
						SendTextMessage(u.Id, "Settings", ReplyKeyboards.Settings(resMgr, u, Config.Telegram));
					}
					else
					{
						if (u.UserState == UserState.Support)
						{
							u.UserState = UserState.Default;
							SendTextMessage(u.Id, resMgr.Get(Res.MessageSentToSupport, u), ReplyKeyboards.MainMenu(resMgr, u));
							NotifyDev("💌 Message from " + UserLink(u) + ":\n" + message.Text.Replace("_", "__").Replace("`", "'").Replace("*", "**").Replace("[", "(").Replace("]", ")") + "\n\n#inbox", 0);
						}
						else if (u.UserState == UserState.SetAmountThreshold)
						{
							var ua = repo.GetUserAddresses(u.Id).FirstOrDefault(o => o.Id == u.EditUserAddressId);
							if (ua != null && decimal.TryParse(message.Text.Replace(" ", "").Replace(",", "."), out decimal amount) && amount >= 0)
							{
								ua.AmountThreshold = amount;
								repo.UpdateBalance(ua);
								SendTextMessage(u.Id, resMgr.Get(Res.ThresholdEstablished, ua), ReplyKeyboards.MainMenu(resMgr, u));
							}
							else
								SendTextMessage(u.Id, resMgr.Get(Res.UnrecognizedCommand, u), ReplyKeyboards.MainMenu(resMgr, u));
						}
						else if (u.UserState == UserState.SetDlgAmountThreshold)
						{
							var ua = repo.GetUserAddresses(u.Id).FirstOrDefault(o => o.Id == u.EditUserAddressId);
							if (ua != null && decimal.TryParse(message.Text.Replace(" ", "").Replace(",", "."), out decimal amount) && amount >= 0)
							{
								ua.DelegationAmountThreshold = amount;
								repo.UpdateBalance(ua);
								SendTextMessage(u.Id, resMgr.Get(Res.DlgThresholdEstablished, ua), ReplyKeyboards.MainMenu(resMgr, u));
							}
							else
								SendTextMessage(u.Id, resMgr.Get(Res.UnrecognizedCommand, u), ReplyKeyboards.MainMenu(resMgr, u));
						}
						else if (u.UserState == UserState.SetName)
						{
							var ua = repo.GetUserAddresses(u.Id).FirstOrDefault(o => o.Id == u.EditUserAddressId);
							if (ua != null)
							{
								ua.Name = message.Text.Trim();
								repo.UpdateBalance(ua);
								string result = resMgr.Get(Res.AddressRenamed, ua);
								if (!ua.User.HideHashTags)
									result += "\n\n#rename" + ua.HashTag();
								SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
							}
							else
								SendTextMessage(u.Id, resMgr.Get(Res.UnrecognizedCommand, u), ReplyKeyboards.MainMenu(resMgr, u));
						}
						else if (u.UserState == UserState.NotifyFollowers)
						{
							var ua = repo.GetUserAddresses(u.Id).FirstOrDefault(o => o.Id == u.EditUserAddressId);
							string text = ApplyEntities(message.Text, message.Entities);
							foreach (var u1 in GetFollowers(ua.Address))
								SendTextMessage(u1.Id, text, ReplyKeyboards.MainMenu(resMgr, u));
							SendTextMessage(u.Id, resMgr.Get(Res.MessageDeliveredForUsers, u), ReplyKeyboards.MainMenu(resMgr, u));
						}
						else if (u.UserState == UserState.Broadcast)
						{
							int count = 0;
							string text = ApplyEntities(message.Text, message.Entities);
							foreach (var user1 in repo.GetUsers())
							{
								if (user1.Language == u.Language)
								{
									SendTextMessage(user1.Id, text, ReplyKeyboards.MainMenu(resMgr, user1), disableNotification: true);
									count++;
								}
							}
							u.UserState = UserState.Default;
							SendTextMessage(u.Id, resMgr.Get(Res.MessageDelivered, u) + "(" + count.ToString() + ")", ReplyKeyboards.MainMenu(resMgr, u));
						}
						else
						{
							SendTextMessage(u.Id, resMgr.Get(Res.UnrecognizedCommand, u) + ": " + message.Text, ReplyKeyboards.MainMenu(resMgr, u));
						}
					}
                    u.UserState = UserState.Default;
				}
				else if (message.Type == MessageType.Text)
				{
					if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
					{
						if (message.Text.StartsWith("/info"))
						{
							Info(update);
						}
						/*
						var chatAdmins = Bot.GetChatAdministratorsAsync(message.Chat.Id).ConfigureAwait(true).GetAwaiter().GetResult();
						if (Regex.IsMatch(message.Text, "(tz|KT)[a-zA-Z0-9]{34}"))
						{
							if (chatAdmins.Any(o => o.User.Id == message.From.Id))
							{
								var u = repo.GetUser(message.From.Id);
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
				}
				if (message.Type == MessageType.Document && Config.DevUserNames.Contains(message.From.Username))
				{
					Upload(message, repo.GetUser(message.From.Id));
				}
			}
            catch (Exception e)
            {
                LogError(e);
                try
                {
                    NotifyDev("‼️ " + e.Message, 0);
                }
                catch { }
            }
        }

		string ApplyEntities(string text, MessageEntity[] entities)
		{
			if (entities == null)
				return text;
			string result = "";
			int currentIndex = 0;
			foreach(var e in entities)
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
			string result = $"1 <b>ꜩ</b> = ${1M.TezToUsd(md)} ({mdReceived.ToString("dd.MM.yyyy HH:mm")})\n";
			var bh = client2.GetBlockHeader(lastHash);
			var bm = client2.GetBlockMetadata(lastHash);
			result += $"#{bh.level} ({bh.timestamp.ToString("dd.MM.yyyy HH:mm:ss")})\n";
			if (bm.voting_period_kind == "proposal")
			{
				result += "Голосование: период подачи предложений\n";
				var proposals = client2.GetProposals(lastHash);
				if (proposals.Count == 0)
					result += "Ни одного предложения не поступило";
				else
					result += "Текущие предложения: " + String.Join("; ", proposals.Select(o => (repo.GetProposal(o.Key)?.Name ?? o.Key) + $" - {o.Value} rolls").ToArray());
			}
			Bot.SendTextMessageAsync(chatId, result, ParseMode.Html).ConfigureAwait(true).GetAwaiter().GetResult();
		}
		void Stat(Update update)
		{
			var chatId = update.Message.Chat?.Id ?? update.Message.From.Id;
			string result = $"Active users: {repo.GetUsers().Count(o => !o.Inactive)}\n";
			result += $"Monitored addresses: {repo.GetUserAddresses().Select(o => o.Address).Distinct().Count()}\n";
			
			Bot.SendTextMessageAsync(chatId, result, ParseMode.Html).ConfigureAwait(true).GetAwaiter().GetResult();
		}
		

		#region Commands
		void OnNewAddress(User user)
        {
            SendTextMessage(user.Id, resMgr.Get(Res.NewAddressHint, user), ReplyKeyboards.Search(resMgr, user));
        }
        void OnSql(User u, string sql)
        {
            try
            {
                var res = repo.RunSql(sql);
                string allData = String.Join("\r\n", res.Select(o => String.Join(';', o)).ToArray());
                if (res[0].Length <= 3 && res.Count <= 20)
                    SendTextMessage(u.Id, allData, ReplyKeyboards.MainMenu(resMgr, u));
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
                SendTextMessage(u.Id, e.Message, ReplyKeyboards.MainMenu(resMgr, u));
            }
        }
        void OnNewAddressEntered(User user, string msg, Chat chat = null)
        {
			Bot.SendChatActionAsync(chat?.Id ?? user.Id, ChatAction.Typing);
			string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
            var nameMatch = Regex.Match(msg, "([^ ]* )?.*(tz|KT)[a-zA-Z0-9]{34}[^a-zA-Z0-9а-яА-Я]*(.*)");
            var name = nameMatch.Success ? (nameMatch.Groups[3].Value.Trim() != "" ? nameMatch.Groups[3].Value.Trim() : nameMatch.Groups[1].Value.Trim()) : "";
            if (name == addr)
                name = addr.ShortAddr().Replace("…", "");
			if (String.IsNullOrEmpty(name))
				name = repo.GetKnownAddressName(addr);
			if (String.IsNullOrEmpty(name))
				name = repo.GetDelegateName(addr).Replace("…", "");
			try
			{
				var ci = addrMgr.GetContract(client2, lastHash, addr, true);
				var t = Explorer.FromId(user.Explorer);
				if (ci != null)
				{
					decimal bal = ci.balance / 1000000M;
					(UserAddress ua, DelegateInfo di) = NewUserAddress(user.Id, addr, name, bal, chat?.Id ?? 0);
					string result = resMgr.Get(Res.AddressAdded, ua) + "\n";
					
					result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
					if (di != null)
					{
						decimal bond = di.balance - di.frozen_balance + (di.frozen_balance_by_cycle.Count > 0 ? di.frozen_balance_by_cycle.Sum(o => o.deposit) : 0);
						bond /= 1000000;
						ua.FullBalance = bond;
						result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
						var sb = di.staking_balance / 1000000;
						ua.StakingBalance = sb;
						ua.Delegators = di.delegated_contracts.Count;
						result += resMgr.Get(Res.StakingInfo, ua) + "\n";
						result += FreeSpace(ua);
					}
					if (ci.@delegate != null && di == null)
					{
						string delname = repo.GetDelegateName(ci.@delegate);
						result += resMgr.Get(Res.Delegate, ua) + $": <a href='{t.account(ci.@delegate)}'>{delname}</a>\n";
					}
					if (!user.HideHashTags)
						result += "\n#added" + ua.HashTag();
					if (chat == null)
					{
						SendTextMessage(user.Id, result, ReplyKeyboards.MainMenu(resMgr, user));
						NotifyUserActivity($"🔥 User {UserLink(user)} added [{addr}]({t.account(addr)})" + (!String.IsNullOrEmpty(name) ? $" as **{name.Replace("_", "__").Replace("`", "'")}**" : "") + $" (" + bal.TezToString() + ")");
					}
					else
					{
						SendTextMessage(chat.Id, result);
						NotifyUserActivity($"🔥 User {UserLink(user)} added [{addr}]({t.account(addr)})" + (!String.IsNullOrEmpty(name) ? $" as **{name.Replace("_", "__").Replace("`", "'")}**" : "") + $" (" + bal.TezToString() + ")" + (chat != null ? $" to group [{chat.Title}](https://t.me/{chat.Username})" : ""));
					}
				}
				else
				{
					if (chat == null)
						SendTextMessage(user.Id, resMgr.Get(Res.IncorrectTezosAddress, user), ReplyKeyboards.MainMenu(resMgr, user));
					else
						SendTextMessage(chat.Id, resMgr.Get(Res.IncorrectTezosAddress, user));
				}

			}
			catch(Exception)
			{
				if (chat == null)
					SendTextMessage(user.Id, resMgr.Get(Res.IncorrectTezosAddress, user), ReplyKeyboards.MainMenu(resMgr, user));
				else
					SendTextMessage(chat.Id, resMgr.Get(Res.IncorrectTezosAddress, user));
			}
		}
		string FreeSpace(UserAddress ua)
		{
			var c = GetConstants();
			//how much tez can be locked in total (by all bakers) as a security deposit
			var totalLocked = (c.block_security_deposit + c.endorsement_security_deposit * c.endorsers_per_block) * c.blocks_per_cycle * (c.preserved_cycles + 1);

			//how much of that the baker can cover with his balance
			var bakerBalance = ua.FullBalance * 1000000;
			var bakerShare = bakerBalance / totalLocked;

			//number of rolls, participating in staking
			var totalRolls = tzStats.GetCycle(repo.GetLastBlockLevel().Item1).rolls;

			//how many rolls and staking balance the baker should have in order to lock the whole balance
			var bakerRollsCapacity = totalRolls * bakerShare;
			var bakerStakingCapacity = bakerRollsCapacity * c.tokens_per_roll;

			decimal maxStakingThreshold = 1;
			var maxStakingBalance = bakerStakingCapacity * maxStakingThreshold;
			var freeSpace = (maxStakingBalance - ua.StakingBalance * 1000000) / 1000000M;
			Logger.LogDebug($"FreeSpace calc for {ua.Address}. totalLocked:{totalLocked}; bakerBalance:{bakerBalance}, bakerShare:{bakerShare}, totalRolls:{totalRolls}, bakerStakingCapacity:{bakerStakingCapacity}, maxStakingBalance:{maxStakingBalance}, currentStakingBalance:{ua.StakingBalance}");
			ua.FreeSpace = freeSpace;
			return resMgr.Get(Res.FreeSpace, ua) + "\n";
		}
        Action ViewAddress(long chatId, UserAddress ua, int msgid)
		{
			var t = Explorer.FromId(ua.User.Explorer);
			var isDelegate = repo.IsDelegate(ua.Address);
			string result = chatId == ua.UserId ? "" : $"ℹ️User {ua.User} [{ua.UserId}] address\n";
			result += isDelegate ? "👑 " : "";
            if (!String.IsNullOrEmpty(ua.Name))
                result += "<b>" + ua.Name + "</b>\n";
            result += $"<a href='{t.account(ua.Address)}'>" + ua.Address + "</a>\n";
            var ci = addrMgr.GetContract(client2, lastHash, ua.Address, true);
            if (ci != null)
                ua.Balance = ci.balance / 1000000M;

            result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
			if (ci.@delegate != null && !isDelegate)
            {
                string delname = repo.GetDelegateName(ci.@delegate);
                result += resMgr.Get(Res.Delegate, ua) + $": <a href='{t.account(ci.@delegate)}'>{delname}</a>\n";
            }
            if (isDelegate)
            {
                try
                {
                    var di = addrMgr.GetDelegate(client2, lastHash, ua.Address, enqueue: true);
                    decimal bond = di.balance - di.frozen_balance + (di.frozen_balance_by_cycle.Count > 0 ? di.frozen_balance_by_cycle.Sum(o => o.deposit) : 0);
                    bond /= 1000000;
					ua.FullBalance = bond;
                    result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
                    var sb = di.staking_balance / 1000000;
					ua.StakingBalance = sb;
					ua.Delegators = di.delegated_contracts.Count;
					result += resMgr.Get(Res.StakingInfo, ua) + "\n";
					result += FreeSpace(ua);
					decimal? perf = addrMgr.GetAvgPerformance(repo, ua.Address);
					if (perf.HasValue)
                    {
						ua.AveragePerformance = perf.Value;
                        result += resMgr.Get(Res.AveragePerformance, ua) + "\n";
                    }
                }
                catch { }
            }
			if (ua.ChatId != 0)
			{
				try
				{
					var chat = Bot.GetChatAsync(ua.ChatId).ConfigureAwait(true).GetAwaiter().GetResult();
					result += resMgr.Get(Res.NotifyIn, ua.User) + $"<a href=\"https://t.me/{chat.Username}\">{chat.Title}</a>\n";
				}
				catch { }
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
			}
			else
			{
				result += resMgr.Get(Res.TransactionNotifications, ua) + "\n";
				result += resMgr.Get(Res.AmountThreshold, ua);
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
				}
				else
				{
					result += "\n";
					result += resMgr.Get(Res.DelegationNotifications, ua) + "\n";
					result += resMgr.Get(Res.DelegationAmountThreshold, ua) + "\n";
					result += resMgr.Get(Res.RewardNotifications, ua) + "\n";
					result += resMgr.Get(Res.CycleCompletionNotifications, ua) + "\n";
					result += resMgr.Get(Res.MissesNotifications, ua) + "\n";
					result += resMgr.Get(Res.Watchers, ua) + repo.GetUserAddresses(ua.Address).Count;
				}
				if (!ua.User.HideHashTags)
                    result += "\n\n" + ua.HashTag();
				return () => SendTextMessage(chatId, result,
					chatId == ua.UserId
						? ReplyKeyboards.AddressMenu(resMgr, ua.User, ua.Id.ToString(), msgid == 0 ? null : ua,
							Config.Telegram)
						: null, msgid);
            }
            else
            {
				if (!ua.User.HideHashTags)
                    result += "\n\n" + ua.HashTag();
                string name = "";
                if (ci?.@delegate != null && !repo.GetUserAddresses(ua.UserId).Any(o => o.Address == ci.@delegate))
                    name = repo.GetDelegateName(ci.@delegate);
				return () => SendTextMessage(chatId, result, chatId == ua.UserId ? ReplyKeyboards.AddressMenu(resMgr, ua.User, ua.Id.ToString(), msgid == 0 ? null : ua, new Tuple<string, string>(name, ci?.@delegate)) : null, msgid);
            }
		}
		void OnMyAddresses(long chatId, User user)
        {
			var addresses = repo.GetUserAddresses(user.Id);
            if (addresses.Count == 0)
                SendTextMessage(user.Id, resMgr.Get(Res.NoAddresses, user), ReplyKeyboards.MainMenu(resMgr, user));
            else

			{
				List<Action> results = new List<Action>();
				foreach (var ua in addresses)
				{
					Bot.SendChatActionAsync(chatId, ChatAction.Typing);
					results.Add(ViewAddress(chatId, ua, 0));
				}
				foreach (var r in results)
					r();
			}
        }

        (UserAddress, DelegateInfo) NewUserAddress(int userId, string addr, string name, decimal balance, long chatId)
        {
            var ua = repo.AddUserAddress(userId, addr, balance, name, chatId);
            DelegateInfo di = null;
            try
            {
                if (addr.StartsWith("tz"))
                {
                    try
                    {
                        di = addrMgr.GetDelegate(client2, lastHash, addr, enqueue: true);
                        if (di != null)
                        {
                            if (!repo.IsDelegate(addr))
                            {
                                repo.AddDelegate(addr, addr.ShortAddr());
								NotifyUserActivity($"💤 New delegate {addr} monitored");
                            }
                        }
                    }
                    catch(WebException)
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

        void NotifyDev(string text, int currentUserID, ParseMode parseMode = ParseMode.Markdown)
        {
            foreach (var devUser in Config.DevUserNames)
            {
                var user = repo.GetUser(devUser);
				if (user != null && user.Id != currentUserID)
				{
					while (text.Length > 4096)
					{
						int lastIndexOf = text.Substring(0, 4096).LastIndexOf('\n');
						SendTextMessage(user.Id, text.Substring(0, lastIndexOf), ReplyKeyboards.MainMenu(resMgr, user), parseMode: parseMode);
						text = text.Substring(lastIndexOf + 1);
					};
					if (text != "")
						SendTextMessage(user.Id, text, ReplyKeyboards.MainMenu(resMgr, user), parseMode: parseMode);
				}
            }
        }

		void SendTextMessage(long chatId, string text)
		{			
			try
			{
				Logger.LogInformation("->" + chatId.ToString() + ": " + text);
				var msg = Bot.SendTextMessageAsync(chatId, text, ParseMode.Html, true).ConfigureAwait(true).GetAwaiter().GetResult();
				Thread.Sleep(50);
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}

		void SendTextMessageUA(UserAddress ua, string text)
		{
			if (ua.ChatId == 0)
				SendTextMessage(ua.UserId, text, ReplyKeyboards.MainMenu(resMgr, ua.User));
			else
				SendTextMessage(ua.ChatId, text);
		}

		int SendTextMessage(long userId, string text, IReplyMarkup keyboard, int replaceId = 0, ParseMode parseMode = ParseMode.Html, bool disableNotification = false)
        {
            var u = repo.GetUser((int)userId);
            if (u.Inactive)
                return 0;
            try
            {
	            Logger.LogInformation("->" + u.ToString() + ": " + text);
                if (replaceId == 0)
                {
                    var msg = Bot.SendTextMessageAsync(userId, text, parseMode, true, disableNotification, replyMarkup: keyboard).ConfigureAwait(true).GetAwaiter().GetResult();
                    repo.LogOutMessage((int)userId, msg.MessageId, text);
                    Thread.Sleep(50);
					return msg.MessageId;
                }
                else
                {
                    var msg = Bot.EditMessageTextAsync(userId, replaceId, text, parseMode, true, replyMarkup: (InlineKeyboardMarkup)keyboard).ConfigureAwait(true).GetAwaiter().GetResult();
                    repo.LogOutMessage((int)userId, msg.MessageId, text);
					return msg.MessageId;
				}
            }
			catch(MessageIsNotModifiedException)
			{ }
            catch(ChatNotFoundException)
            {
                u.Inactive = true;
                repo.UpdateUser(u);
                NotifyDev("😕 User " + UserLink(u) + " not started chat with bot", (int)userId);
            }
			catch(ApiRequestException are)
			{
				NotifyDev("🐞 Error while sending message for " + UserLink(u) + ": " + are.Message, (int)userId);
				if (are.Message.StartsWith("Forbidden"))
				{
					u.Inactive = true;
					repo.UpdateUser(u);
				}
				else
					LogError(are);
			}
            catch (Exception ex)
            {
                if (ex.Message == "Forbidden: bot was blocked by the user")
                {
                    u.Inactive = true;
                    repo.UpdateUser(u);
                    NotifyDev("😕 Bot was blocked by the user " + UserLink(u), (int)userId);
                }
                else
                    LogError(ex);
			}
			return 0;
		}

		void NotifyUserActivity(string text)
		{
			foreach (var userId in Config.Telegram.ActivityChat)
			{
				try
				{
					if (userId > 0)
					{
						var u = repo.GetUser((int)userId);
						Bot.SendTextMessageAsync(userId, text, ParseMode.Markdown, true, replyMarkup: ReplyKeyboards.MainMenu(resMgr, u)).ConfigureAwait(true).GetAwaiter().GetResult();
					}
					else
						Bot.SendTextMessageAsync(userId, text, ParseMode.Markdown, true).ConfigureAwait(true).GetAwaiter().GetResult();
					Thread.Sleep(50);
				}
				catch (Exception ex)
				{
					NotifyDev($"🐞 Error while sending message for chat {userId}: " + ex.Message, 0);
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

		Client CreateTezosClient()
		{
			return new Client(CurrentNode.Url, Logger);
		}
        
        string UserTitle(Telegram.Bot.Types.User u)
        {
            return (u.FirstName + " " + u.LastName).Trim() + (!String.IsNullOrEmpty(u.Username) ? " @" + u.Username + "" : "");
        }

        string UserLink(User u)
        {
            return $"[{(u.Firstname + " " + u.Lastname).Trim()}](tg://user?id={u.Id}) [[{u.Id}]]";
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

		async void Upload(Message message, User user)
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
							NotifyDev("🖇 Files uploaded by " + UserLink(user) + ":" + result, 0);
						}
					}
					else
					{
						var destination = Path.Combine(path, message.Document.FileName);
						File.WriteAllBytes(destination, fileStream.GetBuffer());
						NotifyDev("📎 File uploaded by " + UserLink(user) + ": " + destination, 0);
					}					
				}				
			}
			catch (Exception e)
			{
				await Bot.SendTextMessageAsync(message.Chat.Id, "Данные не загружены: " + e.Message);
			}
		}

		void LoadBakingEndorsingRights(string hash, int cycle)
		{
			Logger.LogInformation($"Loading rights for cycle {cycle}");
			var br = client2.GetBakingRights(hash, cycle);
			var er = client2.GetEndorsingRights(hash, cycle);
			repo.SaveBakingEndorsingRights(br, er);
			Logger.LogInformation($"Rights for cycle {cycle} loaded");
		}
	}
}