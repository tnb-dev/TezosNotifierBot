using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.CryptoCompare;
using TezosNotifyBot.Dialog;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Model;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Tezos;
using TezosNotifyBot.Tzkt;
using File = System.IO.File;
using User = TezosNotifyBot.Domain.User;

namespace TezosNotifyBot
{
    public partial class TezosBot
    {
        private readonly IServiceProvider _serviceProvider;
        private BotConfig Config { get; set; }
        private ILogger<TezosBot> Logger { get; }
                
        //MarketData md = new MarketData();

        //public MarketData MarketData => md;

        //DateTime mdReceived;

        //DateTime bakersReceived;
        public static List<Command> Commands;

        
        
        AddressManager addrMgr;
        private readonly ResourceManager resMgr;
		//string lastHash;

		
                
        private readonly CommandsManager commandsManager;
        private readonly TezosBotFacade botClient;
        
        //bool paused = false;
        string botUserName;

        TelegramBotInvoker telegramBotInvoker;
		TelegramBotHandler telegramBotHandler;

		public TezosBot(IServiceProvider serviceProvider,
            ILogger<TezosBot> logger,
            IOptions<BotConfig> config,
            ResourceManager resourceManager,
            CommandsManager commandsManager,
            TezosBotFacade botClient,
            TelegramBotInvoker telegramBotDispatcher,
            TelegramBotHandler telegramBotHandler)
        {
            _serviceProvider = serviceProvider;
            Logger = logger;
            Config = config.Value;

            this.commandsManager = commandsManager;
            this.botClient = botClient;

            addrMgr = _serviceProvider.GetRequiredService<AddressManager>();
            resMgr = resourceManager;
            this.telegramBotInvoker = telegramBotDispatcher;
            this.telegramBotHandler = telegramBotHandler;
            this.telegramBotHandler.OnChosenInlineResult = OnChosenInlineResult;
            this.telegramBotHandler.OnCallbackQuery = OnCallbackQuery;
            this.telegramBotHandler.OnInlineQuery = OnInlineQuery;
            this.telegramBotHandler.OnChannelPost = OnChannelPost;
            this.telegramBotHandler.OnMessage = OnMessage;
		}

        private async Task OnChannelPost(TelegramBotHandler.Chat chat, int id, TelegramBotHandler.User from, string text)
        {
            if (text == "/chatid")
                await telegramBotInvoker.SendMessage(chat.Id, $"Chat Id: {chat.Id}");
            else
            {
                using var scope = _serviceProvider.CreateScope();
                var provider = scope.ServiceProvider;
                using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
				var md = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>().GetMarketData();
				try
                {
                    await OnGroupOrChannelMessage(db, md, chat, from, id, text);
                }
                catch (Exception e)
                {
                    LogError(e);
                    try
                    {
                        await NotifyDev(db, "‼️ " + e.Message, 0);
                    }
                    catch
                    {
                    }
                }
            }
        }

		async Task OnInlineQuery(string id, string query)
		{
			using var scope = _serviceProvider.CreateScope();
			var provider = scope.ServiceProvider;
			using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
			var md = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>().GetMarketData();

			var q = query.Trim().ToLower();
			q = q.Replace("'", "").Replace("`", "").Replace(" ", "").Replace("а", "a").Replace("б", "b")
				.Replace("в", "v").Replace("г", "g").Replace("д", "d").Replace("е", "e").Replace("ж", "zh")
				.Replace("з", "z").Replace("и", "i").Replace("й", "y").Replace("к", "k").Replace("л", "l")
				.Replace("м", "m").Replace("н", "n").Replace("о", "o").Replace("п", "p").Replace("р", "r")
				.Replace("с", "s").Replace("т", "t").Replace("у", "u").Replace("ф", "f").Replace("х", "h")
				.Replace("ц", "c").Replace("ч", "ch").Replace("ш", "sh").Replace("щ", "sh").Replace("э", "e")
				.Replace("ю", "u").Replace("я", "ya");
            if (q.Length < 3)
            {
                string result = $"1 ꜩ = ${1M.TezToUsd(md)} ({md.Received.ToString("dd.MM.yyyy HH:mm")} UTC)";
                var results_info = ("info", result, "<b>Tezos blockchain info</b>\n\n" + result + TezosProcessing.PeriodStatus + TezosProcessing.VotingStatus +
                              "\n\n@TezosNotifierBot notifies users about transactions and other events in the Tezos blockchain", (TezosProcessing.PeriodStatus + TezosProcessing.VotingStatus).Trim());
                await telegramBotInvoker.AnswerInlineQuery(id, new List<(string id, string title, string content, string description)> { results_info });
            }
            else
            {
                var ka = db.KnownAddresses.ToList()
                    .Where(o => o.Name.Replace("'", "").Replace("`", "").Replace(" ", "").ToLower().Contains(q))
                    .Select(o => new { o.Address, o.Name });
                var da = db.Delegates.ToList()
                    .Where(o => o.Name != null &&
                                o.Name.Replace("'", "").Replace("`", "").Replace(" ", "").ToLower().Contains(q))
                    .Select(o => new { o.Address, o.Name });
                var results = ka.Union(da).GroupBy(o => o.Address)
                    .Select(o => new { Address = o.Key, Name = o.First().Name }).OrderBy(o => o.Name).Select(o =>
                        (o.Address, o.Name, $"<i>{o.Address}</i>\n<b>{o.Name}</b>", o.Address)).Take(50).ToList();
                await telegramBotInvoker.AnswerInlineQuery(id, results);
            }
		}

		async Task OnChosenInlineResult(long chatId, string resultId)
        {
			if (resultId == "info")
				return;
			using var scope = _serviceProvider.CreateScope();
			var provider = scope.ServiceProvider;
			using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
			var md = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>().GetMarketData();
			await OnNewAddressEntered(db, md, db.GetUser(chatId), resultId);
		}

		async Task OnCallbackQuery(string id, long userId, int messageId, string callbackData)
		{
			using var scope = _serviceProvider.CreateScope();
			var provider = scope.ServiceProvider;
			using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
			var md = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>().GetMarketData();
			try
			{
				if (callbackData.StartsWith("_"))
				{
					userId = long.Parse(callbackData.Substring(1, callbackData.IndexOf('_', 1) - 1));
					callbackData = callbackData.Substring(callbackData.IndexOf('_', 1) + 1);
					var cm = await telegramBotInvoker.GetChatAdministrators(userId);
					if (!cm.Any(m => m == userId))
					{
						await telegramBotInvoker.AnswerCallbackQuery(id, "🤚 Forbidden");
						return;
					}
				}
				var callbackArgs = callbackData.Split(' ').Skip(1).ToArray();

				Func<UserAddress> useraddr = () => {
					var addrId = int.Parse(callbackArgs[0]);
					return db.UserAddresses.FirstOrDefault(x => x.UserId == userId && x.Id == addrId);
				};

				db.LogMessage(userId, messageId, null, callbackData);
				var user = db.Users.SingleOrDefault(x => x.Id == userId);
				Logger.LogInformation(user.ToString() + ": button " + callbackData);
				
				if (callbackData == "donate")
				{
					var file = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "DonateQR.jpg"));
					await telegramBotInvoker.SendPhoto(userId, "🎁 Your donations help us understand that we are moving in the right direction and making a really cool service!", file, ReplyKeyboards.MainMenu);
				}

				if (commandsManager.HasCallbackHandler(callbackData))
				{
					await commandsManager.ProcessCallbackHandler(userId, messageId, callbackData);
					return;
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
						await SendTextMessage(db, user.Id, result, null, messageId);
						await NotifyUserActivity(db, $"User {UserLink(user)} deleted {ua.Address}");
					}
					else
						await SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null, messageId);
				}

				if (callbackData.StartsWith("addaddress"))
				{
					var addr = callbackData.Substring("addaddress ".Length);
					await OnNewAddressEntered(db, md, user, addr);
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
						await SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu);
					}
					else
						await SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null,	messageId);
				}
				if (callbackData.StartsWith("misseson"))
				{
					var ua = useraddr();
					if (ua != null)
					{
                        ua.NotifyMisses = true;
						db.SaveChanges();
                        await ViewAddress(db, md, user.Id, ua, messageId, true)();
					}
					else
						await telegramBotInvoker.AnswerCallbackQuery(id, resMgr.Get(Res.AddressNotExist, user));
                    return;
				}
				if (callbackData.StartsWith("missesoff"))
				{
					var ua = useraddr();
					if (ua != null)
					{
						ua.NotifyMisses = false;
                        ua.DownStart = null;
                        ua.DownEnd = null;
                        ua.DownMessageId = null;
						db.SaveChanges();
						await ViewAddress(db, md, user.Id, ua, messageId, true)();
					}
					else
						await telegramBotInvoker.AnswerCallbackQuery(id, resMgr.Get(Res.AddressNotExist, user));
					return;
				}
				if (callbackData.StartsWith("set_misses_"))
                {
					var ua = useraddr();
					if (ua != null)
					{
						var cd = callbackData.Split(' ').First();
						int mt = int.Parse(cd.Substring("set_misses_".Length));
						ua.MissesThreshold = mt;
						db.SaveChanges();
						await ViewAddress(db, md, user.Id, ua, messageId, true)();
					}
					else
						await SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null, messageId);
                    return;
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
						await SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu);
					}
					else
						await SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null, messageId);
				}

				if (callbackData.StartsWith("notifyfollowers"))
				{
					var ua = useraddr();
					if (ua != null)
					{
						var cycles = _serviceProvider.GetService<ITzKtClient>().GetCycles();
						if (ua.IsOwner && !user.IsAdmin(Config.Telegram) &&
							cycles.Single(c => c.firstLevel <= TezosProcessing.PrevBlockLevel && TezosProcessing.PrevBlockLevel <= c.lastLevel) ==
							cycles.Single(c => c.firstLevel <= ua.LastMessageLevel && ua.LastMessageLevel <= c.lastLevel))
						{
							await SendTextMessage(user.Id, resMgr.Get(Res.OwnerLimitReached, user));
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
						await SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu);
					}
					else
						await SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null, messageId);
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
						await SendTextMessage(db, user.Id, result, ReplyKeyboards.BackMenu);
					}
					else
						await SendTextMessage(db, user.Id, resMgr.Get(Res.AddressNotExist, user), null,	messageId);
				}

				if (callbackData.StartsWith("change_delegators_balance_threshold "))
				{
					var ua = useraddr();
					if (ua == null)
					{
						await SendTextMessage(db, userId, resMgr.Get(Res.AddressNotExist, user), null, messageId);
					}
					else
					{
						user.UserState = UserState.SetDelegatorsBalanceThreshold;
						user.EditUserAddressId = ua.Id;
						db.SaveChanges();
						var text = resMgr.Get(Res.EnterDelegatorsBalanceThreshold, ua);
						await SendTextMessage(db, user.Id, text, ReplyKeyboards.BackMenu);
					}
				}

				if (callbackData.StartsWith("toggle_payout_notify"))
				{
					var ua = useraddr();
					if (ua != null)
					{
						ua.NotifyPayout = !ua.NotifyPayout;
						db.SaveChanges();
						await ViewAddress(db, md, user.Id, ua, messageId)();
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
						await ViewAddress(db, md, user.Id, ua, messageId)();
					}
				}

				if (callbackData.StartsWith("set_whalealert"))
				{
					await SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertsTip, user), ReplyKeyboards.WhaleAlertSettings(user), messageId);
				}
				else if (callbackData.StartsWith("set_wa_"))
				{
					int wat = int.Parse(callbackData.Substring("set_wa_".Length));
					user.WhaleAlertThreshold = wat * 1000;
					db.SaveChanges();
					await SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertSet, user), null, messageId);
				}
				else if (callbackData.StartsWith("set_swa_off"))
				{
					user.SmartWhaleAlerts = false;
					db.SaveChanges();
					await SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertsTip, user), ReplyKeyboards.WhaleAlertSettings(user), messageId);
				}
				else if (callbackData.StartsWith("set_swa_on"))
				{
					user.SmartWhaleAlerts = true;
					db.SaveChanges();
					await SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleAlertsTip, user), ReplyKeyboards.WhaleAlertSettings(user), messageId);
				}

				if (callbackData.StartsWith("manageaddress"))
				{
					var ua = useraddr();
					if (ua != null)
					{
                        await ViewAddress(db, md, user.Id, ua, messageId)();
					}
					else
						await telegramBotInvoker.AnswerCallbackQuery(id, resMgr.Get(Res.AddressNotExist, user));
				}
                if (callbackData.StartsWith("tunemisses"))
                {
					var ua = useraddr();
					if (ua != null)
					{
						await ViewAddress(db, md, user.Id, ua, messageId, true)();
					}
					else
						await telegramBotInvoker.AnswerCallbackQuery(id, resMgr.Get(Res.AddressNotExist, user));
				}

				Action<string, Action<UserAddress>> editUA = async (cmd, action) => {
					if (callbackData.StartsWith(cmd))
					{
						var ua = useraddr();
						if (ua != null)
						{
							action(ua);
							db.SaveChanges();
							await ViewAddress(db, md, user.Id, ua, messageId)();
						}
						else
							await telegramBotInvoker.AnswerCallbackQuery(id, resMgr.Get(Res.AddressNotExist, user));
					}
				};

				editUA("bakingon", ua => ua.NotifyBakingRewards = true);
				editUA("bakingoff", ua => ua.NotifyBakingRewards = false);
				editUA("cycleon", ua => ua.NotifyCycleCompletion = true);
				editUA("cycleoff", ua => ua.NotifyCycleCompletion = false);
				editUA("owneron", ua => ua.IsOwner = true);
				editUA("owneroff", ua => ua.IsOwner = false);				
				editUA("outoffreespaceon", ua => ua.NotifyOutOfFreeSpace = true);
				editUA("outoffreespaceoff", ua => ua.NotifyOutOfFreeSpace = false);
				editUA("tranon", ua => ua.NotifyTransactions = true);
				editUA("tranoff", ua => ua.NotifyTransactions = false);
				editUA("awardon", ua => ua.NotifyAwardAvailable = true);
				editUA("awardoff", ua => ua.NotifyAwardAvailable = false);
				editUA("dlgon", ua => ua.NotifyDelegations = true);
				editUA("dlgoff", ua => ua.NotifyDelegations = false);
				editUA("toggle-delegate-status", ua => ua.NotifyDelegateStatus = !ua.NotifyDelegateStatus);

				if (callbackData.StartsWith("hidehashtags"))
				{
					user.HideHashTags = true;
					db.SaveChanges();
					await SendTextMessage(user.Id, resMgr.Get(Res.HashTagsOff, user));
					await SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(user, Config.Telegram), messageId);
				}

				if (callbackData.StartsWith("showhashtags"))
				{
					user.HideHashTags = false;
					db.SaveChanges();
					await SendTextMessage(user.Id, resMgr.Get(Res.HashTagsOn, user));
					await SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(user, Config.Telegram), messageId);
				}

				if (callbackData.StartsWith("change_currency"))
				{
					user.Currency = user.Currency == UserCurrency.Usd ? UserCurrency.Eur : UserCurrency.Usd;
					db.SaveChanges();
					await SendTextMessage(user.Id, resMgr.Get(Res.UserCurrencyChanged, user));
					await SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(user, Config.Telegram), messageId);
				}

				if (callbackData.StartsWith("showvotingnotify"))
				{
					user.VotingNotify = true;
					db.SaveChanges();
					await SendTextMessage(user.Id, resMgr.Get(Res.VotingNotifyChanged, user));
					await SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(user, Config.Telegram), messageId);
				}

				if (callbackData.StartsWith("hidevotingnotify"))
				{
					user.VotingNotify = false;
					db.SaveChanges();
					await SendTextMessage(user.Id, resMgr.Get(Res.VotingNotifyChanged, user));
					await SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(user, Config.Telegram), messageId);
				}

				if (callbackData.StartsWith("tezos_release_on"))
				{
					user.ReleaseNotify = true;
					db.SaveChanges();
					await SendTextMessage(user.Id, resMgr.Get(Res.ReleaseNotifyChanged, user));
					await SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(user, Config.Telegram), messageId);
				}

				if (callbackData.StartsWith("tezos_release_off"))
				{
					user.ReleaseNotify = false;
					db.SaveChanges();
					await SendTextMessage(user.Id, resMgr.Get(Res.ReleaseNotifyChanged, user));
					await SendTextMessage(db, user.Id, "Settings", ReplyKeyboards.Settings(user, Config.Telegram), messageId);
				}

				if (Config.Telegram.DevUsers.Contains(user.Username))
				{
					if (callbackData.StartsWith("broadcast"))
					{
						user.UserState = UserState.Broadcast;
						await SendTextMessage(db, user.Id, $"Enter your message for bot users", ReplyKeyboards.BackMenu);
					}

					if (callbackData.StartsWith("getuseraddresses"))
					{
						await OnSql(db, user, "select * from user_address");
					}

					if (callbackData.StartsWith("getusermessages"))
					{
						await OnSql(db, user, "select * from message");
					}
				}
			}
			catch (Exception e)
			{
				LogError(e);
                await NotifyDev(db, e.Message, 0);
			}
		}

		public async Task Run(CancellationToken cancelToken)
        {
            var version = GetType().Assembly.GetName().Version?.ToString(3);

            try
            {
                botUserName = await telegramBotInvoker.GetBotUsername();
                Logger.LogInformation("Старт обработки сообщений @" + botUserName);
                                
                var message = new StringBuilder();
                message.AppendLine($"{botUserName} v{version} started");
                message.AppendLine();
                message.AppendLine($"Using TzKt api: {(Config.TzKtUrl.Contains("localhost") ? "local" : Config.TzKtUrl)}");
                {
                    using var scope = _serviceProvider.CreateScope();
                    var provider = scope.ServiceProvider;
                    using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
                    await NotifyDev(db, message.ToString(), 0);
                }

                do
                {
     //               using var scope = _serviceProvider.CreateScope();
     //               var provider = scope.ServiceProvider;
     //               using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
     //               if (paused)
					//{
                        Thread.Sleep(1000);
     //                   continue;
					//}
                    
                } while (cancelToken.IsCancellationRequested is false);
            }
            catch (Exception fe)
            {
                Logger.LogCritical(fe, "Fatal error");
            }
        }

        //BlockHeader lastHeader;
        //BlockMetadata lastMetadata;
        

        
        public async Task LoadAddressList(ITzKtClient tzKt, Storage.TezosDataContext db)
        {
            try
            {
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
				await NotifyDev(db, result, 0);
            }
            catch (Exception e)
            {
                LogError(e);
				await NotifyDev(db, "Fail to update address list from GitHub: " + e.Message, 0);
            }
        }

        List<User> GetFollowers(Storage.TezosDataContext db, string addr)
        {
            var di = addrMgr.GetDelegate(addr);
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


		private async Task OnMessage(TelegramBotHandler.Chat chat, bool isPrivate, int id, TelegramBotHandler.User from, string text)
		{
			using var scope = _serviceProvider.CreateScope();
			var provider = scope.ServiceProvider;
			using var db = scope.ServiceProvider.GetRequiredService<Storage.TezosDataContext>();
			var md = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>().GetMarketData();

			try
            {
				var user = db.GetUser(from.Id);

				if (isPrivate && (user?.UserState == UserState.Broadcast || user?.UserState == UserState.NotifyFollowers))
				{
					var count = 0;
					
					var users = db.Users.Where(o => !o.Inactive).ToList();
					if (user.UserState == UserState.NotifyFollowers)
					{
						var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
						users = GetFollowers(db, ua.Address);
						if (!user.IsAdmin(Config.Telegram))
						{
							ua.LastMessageLevel = TezosProcessing.PrevBlockLevel;
							db.SaveChanges();
						}
					}

					foreach (var user1 in users)
					{
						try
						{
                            await telegramBotInvoker.CopyMessage(user1.Id, chat.Id, id);
							count++;
						}
						//catch (ChatNotFoundException)
						//{
						//	user1.Inactive = true;
						//	db.SaveChanges();
						//	await NotifyUserActivity(db, "😕 User " + UserLink(user1) + " not started chat with bot");
						//}
						catch (ApiRequestException are)
						{
							await NotifyUserActivity(db, "🐞 Error while sending message for " + UserLink(user1) + ": " + are.Message);
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
								await NotifyUserActivity(db, "😕 Bot was blocked by the user " + UserLink(user1));
							}
							else
								LogError(ex);
						}
					}

					await SendTextMessage(db, user.Id, $"Message delivered ({count})", ReplyKeyboards.MainMenu);
					user.UserState = UserState.Default;
				}
                else if (isPrivate)
                {
					bool newUser = user == null;

					db.LogMessage(from, id, text, null);
					user = db.GetUser(from.Id);
					Logger.LogInformation(UserTitle(from) + ": " + text);
					if (newUser)
						await NotifyUserActivity(db, "🔅 New user: " + UserLink(user));
					bool welcomeBack = false;
					if (user.Inactive)
					{
						user.Inactive = false;
						db.SaveChanges();
						welcomeBack = true;
						await NotifyUserActivity(db, "🤗 User " + UserLink(user) + " is back");
					}
					if (text.StartsWith("/start"))
					{
						if (newUser || welcomeBack)
							await SendTextMessage(db, user.Id,
								welcomeBack ? resMgr.Get(Res.WelcomeBack, user) : resMgr.Get(Res.Welcome, user),
								ReplyKeyboards.MainMenu);
						var cmd = text.Substring("/start".Length).Replace("_", " ").Trim();
						if (Regex.IsMatch(cmd, "(tz|KT)[a-zA-Z0-9]{34}"))
						{
							db.SaveChanges();
							await OnNewAddressEntered(db, md, user, text);
						}
						else if (!newUser && !welcomeBack)
							await SendTextMessage(db, user.Id, resMgr.Get(Res.Welcome, user),	ReplyKeyboards.MainMenu);
					}
					else if (text.Contains("Tezos blockchain info"))
					{
						return;
					}
					//else if (Config.Telegram.DevUsers.Contains(from.Username) &&
					//		 message.ReplyToMessage != null &&
					//		 message.ReplyToMessage.Entities.Length > 0 &&
					//		 message.ReplyToMessage.Entities[0].User != null)
					//{
					//	var replyUser = db.GetUser(message.ReplyToMessage.Entities[0].User.Id);
					//	await SendTextMessage(db, replyUser.Id, resMgr.Get(Res.SupportReply, replyUser) + "\n\n" + text, ReplyKeyboards.MainMenu);
					//	await NotifyDev(db,
					//		"📤 Message for " + UserLink(replyUser) + " from " + UserLink(user) + ":\n\n" +
					//		text.Replace("_", "__").Replace("`", "'").Replace("*", "**").Replace("[", "(")
					//			.Replace("]", ")") + "\n\n#outgoing", user.Id);
					//}

					else if (text == ReplyKeyboards.CmdNewAddress)
					{
						await OnNewAddress(db, user);
					}
					else if (text == "/outflow_off")
					{
						user.SmartWhaleAlerts = false;
						db.SaveChanges();
						await SendTextMessage(db, user.Id, resMgr.Get(Res.WhaleOutflowOff, user), ReplyKeyboards.MainMenu);
					}
					else if (text.StartsWith("/medium ") && user.IsAdmin(Config.Telegram))
					{
						var str = text.Substring("/medium ".Length);
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
							await NotifyUserActivity(db, $"New Medium post: [{result.data.title}]({result.data.url})");
						}
						catch (Exception e)
						{
							await NotifyDev(db, "Failed to create medium post: " + e.Message + "\nAuth token:" + _serviceProvider.GetService<IOptions<MediumOptions>>().Value.AuthToken + "\n" + e.StackTrace, user.Id);
						}
					}
					else if (text == "/stop" && user.IsAdmin(Config.Telegram))
					{
						TezosProcessing.Paused = true;
						await NotifyDev(db, "Blockchain processing paused", 0);
					}
					else if (text == "/resume" && user.IsAdmin(Config.Telegram))
					{
						TezosProcessing.Paused = false;
						await NotifyDev(db, "Blockchain processing resumed", 0);
					}
					else if (text.StartsWith("/help") && Config.Telegram.DevUsers.Contains(from.Username))
					{
						await SendTextMessage(db, user.Id, @"Administrator commands:
/sql {query} - run sql query
/block - show current block
/setblock {number} - set last processed block
/names
{addr1} {name1}
{addr2} {name2}
...etc - add public known addresses
/setdelegatename {addr} {name} - set delegate name
/addrlist {userid} - view user addresses
/msglist {userid} - view user messages (last month)
/userinfo {userid} - view user info and settings
/stop - stop processing blockchain
/resume - resume processing blockchain
/medium {cycle} - post medium article about cycle {cycle}", ReplyKeyboards.MainMenu);
					}
					else if (text.StartsWith("/sql") && Config.Telegram.DevUsers.Contains(from.Username))
					{
						await OnSql(db, user, text.Substring("/sql".Length));
					}
					else if (text == ReplyKeyboards.CmdMyAddresses || text.StartsWith("/list"))
					{
						await OnMyAddresses(db, md, from.Id, user);
					}
					else if (text.StartsWith("/addrlist") && Config.Telegram.DevUsers.Contains(from.Username))
					{
						if (int.TryParse(text.Substring("/addrlist".Length).Trim(), out int userid))
						{
							var u1 = db.GetUser(userid);
							if (u1 != null)
								await OnMyAddresses(db, md, from.Id, u1);
							else
								await SendTextMessage(db, user.Id, $"User not found: {userid}",
									ReplyKeyboards.MainMenu);
						}
						else
							await SendTextMessage(db, user.Id, "Command syntax:\n/addrlist {userid}",
								ReplyKeyboards.MainMenu);
					}
					else if (text.StartsWith("/msglist") && Config.Telegram.DevUsers.Contains(from.Username))
					{
						if (int.TryParse(text.Substring("/msglist".Length).Trim(), out int userid))
						{
							await OnSql(db, user,
								$"select * from message where user_id = {userid} and create_date >= 'now'::timestamp - '1 month'::interval order by create_date");
						}
						else
							await SendTextMessage(db, user.Id, "Command syntax:\n/msglist {userid}",
								ReplyKeyboards.MainMenu);
					}
					else if (text.StartsWith("/userinfo") && Config.Telegram.DevUsers.Contains(from.Username))
					{
						if (int.TryParse(text.Substring("/userinfo".Length).Trim(), out int userid))
						{
							var u1 = db.GetUser(userid);
							if (u1 != null)
							{
								string result = $"User {u1.ToString()} [{u1.Id}]\n";
								result += $"Created: {u1.CreateDate.ToString("dd.MM.yyyy HH:mm")}\n";
								result += $"Hashtags: {(u1.HideHashTags ? "off" : "on")}\n";
								result += $"Inactive: {(u1.Inactive ? "yes" : "no")}\n";
								result += $"Voting Notify: {(u1.VotingNotify ? "on" : "off")}\n";
								result += $"Whale Alert Threshold: {u1.WhaleAlertThreshold}\n";
								await SendTextMessage(db, user.Id, result, ReplyKeyboards.MainMenu);
							}
							else
								await SendTextMessage(db, user.Id, $"User not found: {userid}", ReplyKeyboards.MainMenu);
						}
						else
							await SendTextMessage(db, user.Id, "Command syntax:\n/addrlist {userid}", ReplyKeyboards.MainMenu);
					}
					else if (text == "/info" || text == "info")
					{
						await Info(chat.Id, md);
					}
					else if (text == "/stat")
					{
						await Stat(db, chat.Id);
					}
					else if (text == "/block")
					{
						int l = db.GetLastBlockLevel().Item1;
						//int c = (l - 1) / 4096;
						//int p = l - c * 4096 - 1;
						var avg = TezosProcessing.GetAvgProcessingTime();
						var cs = ((MemoryCache)_serviceProvider.GetService<IMemoryCache>()).Count;
						await SendTextMessage(db, user.Id, $"Last block processed: {l}, msh sent: {msgSent}\nAvg. processing time: {avg}\nCache size: {cs}", ReplyKeyboards.MainMenu);
					}
					else if (text.StartsWith("/setblock") && Config.Telegram.DevUsers.Contains(from.Username))
					{
						if (int.TryParse(text.Substring("/setblock ".Length).Replace(",", "").Replace(" ", ""), out int num))
						{
							var tzKt = _serviceProvider.GetService<ITzKtClient>();
							var b = tzKt.GetBlock(num);
							var lbl = db.LastBlock.Single();
							lbl.Level = num;
							lbl.Priority = b.blockRound;
							lbl.Hash = b.Hash;
							db.SaveChanges();
                            TezosProcessing.SetLastBlock(db, tzKt.GetBlock(num));
							var c = tzKt.GetCycles().Single(c => c.firstLevel <= num && num <= c.lastLevel);
							await NotifyDev(db,
								$"Last block processed changed: {db.GetLastBlockLevel().Item1}, {db.GetLastBlockLevel().Item3}\nCurrent cycle: {c.index}, totalStaking: {c.totalStaking}",
								0);
						}
					}
					else if (text.StartsWith("/names") && Config.Telegram.DevUsers.Contains(from.Username))
					{
						var data = text.Substring("/names".Length).Trim().Split('\n');
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
							await NotifyDev(db, "Names assigned:\n\n" + result, 0);
					}
					else if (Config.Telegram.DevUsers.Contains(from.Username) && text.StartsWith("/setdelegatename"))
					{
						var dn = text.Substring("/setdelegatename ".Length);
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
							await SendTextMessage(db, user.Id, $"👑 {addr}: <b>{name}</b>", ReplyKeyboards.MainMenu);
						}
					}
					else if (commandsManager.HasUpdateHandler(text))
					{
						await commandsManager.ProcessUpdateHandler(chat, id, text);
					}
					else if (text.StartsWith("/add") && !Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}"))
					{
						await OnNewAddress(db, user);
					}
					else if (text.StartsWith("/trnthreshold"))
					{
						if (Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}"))
						{
							var msg = text.Substring($"/trnthreshold ".Length);
							string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
							string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
							if (long.TryParse(threshold, out long t))
							{
								var ua = db.GetUserTezosAddress(user.Id, addr);
								if (ua.Id != 0)
								{
									ua.AmountThreshold = t;
									db.SaveChanges();
									await SendTextMessage(db, user.Id, resMgr.Get(Res.ThresholdEstablished, ua), null);
									return;
								}
							}
						}

						await SendTextMessage(user.Id, $"Use <b>trnthreshold</b> command with Tezos address and the transaction amount (XTZ) threshold for this address. For example::\n/trnthreshold <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
					}
					else if (text.StartsWith("/dlgthreshold"))
					{
						if (Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}"))
						{
							var msg = text.Substring($"/dlgthreshold ".Length);
							string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
							string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
							if (long.TryParse(threshold, out long t))
							{
								var ua = db.GetUserTezosAddress(user.Id, addr);
								if (ua.Id != 0)
								{
									ua.DelegationAmountThreshold = t;
									db.SaveChanges();
									await SendTextMessage(db, user.Id, resMgr.Get(Res.DlgThresholdEstablished, ua), null);
									return;
								}
							}
						}

						await SendTextMessage(user.Id, $"Use <b>dlgthreshold</b> command with Tezos address and the delegation amount (XTZ) threshold for this address. For example::\n/dlgthreshold <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
					}
					else if (Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}") &&
							 user.UserState != UserState.Broadcast && user.UserState != UserState.Support &&
							 user.UserState != UserState.NotifyFollowers)
					{
						await OnNewAddressEntered(db, md, user, text.Replace("/add", ""));
					}
					else if (text == ReplyKeyboards.CmdGoBack)
					{
						await SendTextMessage(db, user.Id, resMgr.Get(Res.SeeYou, user), ReplyKeyboards.MainMenu);
					}
					else if (text == ReplyKeyboards.CmdContacts)
					{
						user.UserState = UserState.Support;
						await SendTextMessage(db, user.Id, resMgr.Get(Res.WriteHere, user), ReplyKeyboards.BackMenu);
						return;
					}
					else if (text == ReplyKeyboards.CmdSettings || text.StartsWith("/settings"))
					{
						await SendTextMessage(db, user.Id, resMgr.Get(Res.Settings, user).Substring(2), ReplyKeyboards.Settings(user, Config.Telegram));
					}
					else
					{
						if (user.UserState == UserState.Support)
						{
							user.UserState = UserState.Default;

							var dialog = _serviceProvider.GetRequiredService<DialogService>();
							var (action, answer) = dialog.Intent(user.Id.ToString(), text, CultureInfo.GetCultureInfo("en"));
							// TODO: Add `action == input.unknown` handling
							await SendTextMessage(db, user.Id, answer, ReplyKeyboards.MainMenu);

							var messageBuilder = new MessageBuilder();

							messageBuilder.AddLine("💌 Message from " + UserLink(user) + ":\n" + text
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

							await NotifyDev(db, messageBuilder.Build(), 0);
						}
						else if (user.UserState == UserState.SetAmountThreshold)
						{
							var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
							if (ua != null && decimal.TryParse(text.Replace(" ", "").Replace(",", "."),
								out decimal amount) && amount >= 0)
							{
								ua.AmountThreshold = amount;
								db.SaveChanges();
								await SendTextMessage(db, user.Id, resMgr.Get(Res.ThresholdEstablished, ua), ReplyKeyboards.MainMenu);
							}
							else
								await SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user), ReplyKeyboards.MainMenu);
						}
						else if (user.UserState == UserState.SetDlgAmountThreshold)
						{
							var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
							if (ua != null && decimal.TryParse(text.Replace(" ", "").Replace(",", "."),
								out decimal amount) && amount >= 0)
							{
								ua.DelegationAmountThreshold = amount;
								db.SaveChanges();
								await SendTextMessage(db, user.Id, resMgr.Get(Res.DlgThresholdEstablished, ua), ReplyKeyboards.MainMenu);
							}
							else
								await SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user), ReplyKeyboards.MainMenu);
						}
						else if (user.UserState == UserState.SetDelegatorsBalanceThreshold)
						{
							var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
							if (ua != null && decimal.TryParse(text.Replace(" ", "").Replace(",", "."),
								out var amount) && amount >= 0)
							{
								ua.DelegatorsBalanceThreshold = amount;
								db.SaveChanges();
								await SendTextMessage(db, user.Id, resMgr.Get(Res.ChangedDelegatorsBalanceThreshold, ua), ReplyKeyboards.MainMenu);
							}
							else
								await SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user),	ReplyKeyboards.MainMenu);
						}
						else if (user.UserState == UserState.SetName)
						{
							var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
							if (ua != null)
							{
								ua.Name = Regex.Replace(text.Trim(), "<.*?>", "");
								db.SaveChanges();
								string result = resMgr.Get(Res.AddressRenamed, ua);
								if (!ua.User.HideHashTags)
									result += "\n\n#rename" + ua.HashTag();
								await SendTextMessage(db, user.Id, result, ReplyKeyboards.MainMenu);
							}
							else
								await SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user), ReplyKeyboards.MainMenu);
						}
						else if (user.UserState == UserState.NotifyFollowers)
						{
							var ua = db.GetUserAddresses(user.Id).FirstOrDefault(o => o.Id == user.EditUserAddressId);
							if (!user.IsAdmin(Config.Telegram))
							{
								ua.LastMessageLevel = TezosProcessing.PrevBlockLevel;
								db.SaveChanges();
								text = resMgr.Get(Res.DelegateMessage, ua) + "\n\n" + text;
							}
							int count = 0;
							foreach (var u1 in GetFollowers(db, ua.Address))
							{
								var tags = !u1.HideHashTags ? "\n\n#delegate_message" + ua.HashTag() : "";
								await SendTextMessage(db, u1.Id, text + tags, ReplyKeyboards.MainMenu);
								count++;
							}
							await SendTextMessage(db, user.Id, resMgr.Get(Res.MessageDeliveredForUsers, new ContextObject { u = user, Amount = count }), ReplyKeyboards.MainMenu);
						}
						else if (user.UserState == UserState.Broadcast)
						{
							int count = 0;
                            foreach (var user1 in db.Users.Where(o => !o.Inactive).ToList())
                            {
                                await CopyMessage(db, user1.Id, chat.Id, id, text);
                                count++;
                            }

							user.UserState = UserState.Default;
							await SendTextMessage(db, user.Id, resMgr.Get(Res.MessageDelivered, user) + "(" + count.ToString() + ")", ReplyKeyboards.MainMenu);
						}
						else
						{
							await SendTextMessage(db, user.Id, resMgr.Get(Res.UnrecognizedCommand, user) + ": " + text, ReplyKeyboards.MainMenu);
						}
					}

					user.UserState = UserState.Default;
				}
                await db.SaveChangesAsync();

                if (!isPrivate)
                {
                    await OnGroupOrChannelMessage(db, md, chat, from, id, text);
                }
			}
            catch(Exception e)
            {
				LogError(e);
				try
				{
					await NotifyDev(db, "‼️ " + e.Message, 0);
				}
				catch
				{
				}
			}
		}

        async Task OnGroupOrChannelMessage(Storage.TezosDataContext db, MarketData md, TelegramBotHandler.Chat chat, TelegramBotHandler.User from, int messageId, string text)
        {
            var chatUser = db.GetUser(chat.Id);
            bool newChat = chatUser == null;

            text = text.Replace($"@{botUserName}", "");
            if (!text.StartsWith("/info") &&
                !text.StartsWith("/settings") &&
                !text.StartsWith("/add") &&
                !text.StartsWith("/list") &&
                !text.StartsWith("/trnthreshold") &&
                !text.StartsWith("/dlgthreshold") &&
                !Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}"))
                return;
            db.LogMessage(chat, messageId, text, null);
            if (chatUser == null)
                chatUser = db.GetUser(chat.Id);
            Logger.LogInformation(chat.Title + ": " + text);
            if (newChat)
                await NotifyUserActivity(db, "👥 New chat: " + ChatLink(chatUser));

            if (text.StartsWith("/info"))
            {
                await Info(chat.Id, md);
                return;
            }

            if (chat.Type != 3 && !(await telegramBotInvoker.GetChatAdministrators(chat.Id)).Contains(from.Id))
                return;

            if (text.StartsWith("/settings"))
            {
                await SendTextMessage(db, chatUser.Id, "Settings", ReplyKeyboards.Settings(chatUser, Config.Telegram));
            }

            if (text.StartsWith("/add") || (Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}") && chat.Type != 3))
            {
                if (Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}"))
                {
                    string addr = Regex.Matches(text, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                    await OnNewAddressEntered(db, md, chatUser, text.Substring(text.IndexOf(addr)));
                }
                else
                    await SendTextMessage(chatUser.Id, $"Use <b>add</b> command with Tezos address and the title for this address (optional). For example::\n/add@{botUserName} <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf Аrthur</i>");
            }

            if (text.StartsWith("/list"))
            {
                await OnMyAddresses(db, md, chat.Id, chatUser);
            }

            if (text.StartsWith("/trnthreshold"))
            {
                if (Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}"))
                {
                    var msg = text.Substring($"/trnthreshold ".Length);
                    string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                    string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
                    if (long.TryParse(threshold, out long t))
                    {
                        var ua = db.GetUserTezosAddress(chat.Id, addr);
                        if (ua.Id != 0)
                        {
                            ua.AmountThreshold = t;
                            db.SaveChanges();
                            await SendTextMessage(db, chat.Id, resMgr.Get(Res.ThresholdEstablished, ua), null);
                            return;
                        }
                    }
                }

                await SendTextMessage(chatUser.Id, $"Use <b>trnthreshold</b> command with Tezos address and the transaction amount (XTZ) threshold for this address. For example::\n/trnthreshold@{botUserName} <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
            }

            if (text.StartsWith("/dlgthreshold"))
            {
                if (Regex.IsMatch(text, "(tz|KT)[a-zA-Z0-9]{34}"))
                {
                    var msg = text.Substring($"/dlgthreshold ".Length);
                    string addr = Regex.Matches(msg, "(tz|KT)[a-zA-Z0-9]{34}").First().Value;
                    string threshold = msg.Substring(msg.IndexOf(addr) + addr.Length).Trim();
                    if (long.TryParse(threshold, out long t))
                    {
                        var ua = db.GetUserTezosAddress(chat.Id, addr);
                        if (ua.Id != 0)
                        {
                            ua.DelegationAmountThreshold = t;
                            db.SaveChanges();
                            await SendTextMessage(db, chat.Id, resMgr.Get(Res.DlgThresholdEstablished, ua), null);
                            return;
                        }
                    }
                }

                await SendTextMessage(chatUser.Id, $"Use <b>dlgthreshold</b> command with Tezos address and the delegation amount (XTZ) threshold for this address. For example::\n/dlgthreshold@{botUserName} <i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf 1000</i>");
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

		async Task Info(long chatId, MarketData md)
        {
            string result = $"1 <b>ꜩ</b> = ${1M.TezToUsd(md)} ({md.Received.ToString("dd.MM.yyyy HH:mm")})" + TezosProcessing.PeriodStatus + TezosProcessing.VotingStatus;

			await telegramBotInvoker.SendMessage(chatId, result);			
        }

		async Task Stat(Storage.TezosDataContext db, long chatId)
        {
            string result = $"Active users: {db.Users.Count(o => !o.Inactive)}\n";
            result += $"Monitored addresses: {db.UserAddresses.Where(o => !o.IsDeleted && !o.User.Inactive).Select(o => o.Address).Distinct().Count()}\n";

            await telegramBotInvoker.SendMessage(chatId, result);
        }


        #region Commands

        async Task OnNewAddress(Storage.TezosDataContext db, User user)
        {
            await SendTextMessage(db, user.Id, resMgr.Get(Res.NewAddressHint, user), ReplyKeyboards.Search);
        }

		async Task OnSql(Storage.TezosDataContext db, User u, string sql)
        {
            try
            {
                var res = db.RunSql(sql);
                string allData = String.Join("\r\n", res.Select(o => String.Join(';', o)).ToArray());
                if (res[0].Length <= 3 && res.Count <= 20)
                    await SendTextMessage(db, u.Id, allData, ReplyKeyboards.MainMenu);
                else
                {
                    Stream s = GenerateStreamFromString(allData);
                    string fileName = "result.txt";
                    if (allData.Length > 100000)
                    {
                        s = Utils.CreateZipToMemoryStream(s, "result.txt");
                        fileName = "result.zip";
                    }
                    await telegramBotInvoker.SendFile(u.Id, fileName, s);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
				await SendTextMessage(db, u.Id, e.Message, ReplyKeyboards.MainMenu);
            }
        }

        async Task OnNewAddressEntered(Storage.TezosDataContext db, MarketData md, User user, string msg, Telegram.Bot.Types.Chat chat = null)
        {
            await telegramBotInvoker.SendChatActionTyping(chat?.Id ?? user.Id);
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
                var ci = addrMgr.GetContract(addr);
                if (ci != null)
                {
                    decimal bal = ci.balance / 1000000M;
                    (UserAddress ua, DelegateInfo di) = await NewUserAddress(db, user, addr, name, bal, chat?.Id ?? 0);
                    string result = resMgr.Get(Res.AddressAdded, ua) + "\n";

                    result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
                    if (di != null)
                    {
                        ua.FullBalance = di.Bond / 1000000;
                        result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
                        ua.StakingBalance = di.staking_balance / 1000000;
                        ua.Delegators = di.NumDelegators;
                        result += $"Staking Balance: <b>{ua.StakingBalance.TezToString()}</b> (<b>{ua.Delegators}</b> delegators)\n";
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
						await SendTextMessage(db, user.Id, result, ReplyKeyboards.AddressMenu(ua.User, ua.Id, null, Config.Telegram));
                        await NotifyUserActivity(db, $"🔥 User {UserLink(user)} added [{addr}]({t.account(addr)})" +
                                           (!String.IsNullOrEmpty(name)
                                               ? $" as **{name.Replace("_", "__").Replace("`", "'")}**"
                                               : "") + $" (" + bal.TezToString() + ")");
                    }
                    else
                    {
						await SendTextMessage(chat.Id, result);
                        await NotifyUserActivity(db, $"🔥 User {UserLink(user)} added [{addr}]({t.account(addr)})" +
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
                        await SendTextMessage(db, user.Id, resMgr.Get(Res.IncorrectTezosAddress, user), ReplyKeyboards.MainMenu);
                    else
						await SendTextMessage(chat.Id, resMgr.Get(Res.IncorrectTezosAddress, user));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error on adding \"{msg}\":\n{e.Message}");
                if (chat == null)
                    await SendTextMessage(db, user.Id, resMgr.Get(Res.IncorrectTezosAddress, user), ReplyKeyboards.MainMenu);
                else
                    await SendTextMessage(chat.Id, resMgr.Get(Res.IncorrectTezosAddress, user));
            }
        }

        Func<Task> ViewAddress(Storage.TezosDataContext db, MarketData md, long chatId, UserAddress ua, int msgid, bool tuneMisses = false)
        {
            var isDelegate = db.Delegates.Any(o => o.Address == ua.Address);
            var result = chatId == ua.UserId ? "" : $"ℹ️User {ua.User} [{ua.UserId}] address\n";
            var config = db.Set<AddressConfig>().AsNoTracking().FirstOrDefault(x => x.Id == ua.Address);
            result += isDelegate ? $"{config?.Icon ?? "👑"} " : "";
            if (!String.IsNullOrEmpty(ua.Name))
                result += "<b>" + ua.Name + "</b>\n";
            result += $"<a href='{t.account(ua.Address)}'>" + ua.Address + "</a>\n";
            var ci = addrMgr.GetContract(ua.Address);
            if (ci != null)
                ua.Balance = ci.balance / 1000000M;

            result += resMgr.Get(Res.CurrentBalance, (ua, md)) + "\n";
            if (ci.@delegate != null && !isDelegate)
            {
                string delname = db.GetDelegateName(ci.@delegate);
                result += resMgr.Get(Res.Delegate, ua) + $": <a href='{t.account(ci.@delegate)}'>{delname}</a>\n";
            }            

            if (isDelegate)
            {
                try
                {
                    var di = addrMgr.GetDelegate(ua.Address);
                    ua.FullBalance = di.Bond / 1000000;
                    result += resMgr.Get(Res.ActualBalance, (ua, md)) + "\n";
                    ua.StakingBalance = di.staking_balance / 1000000;
                    ua.Delegators = di.NumDelegators;
                    result += $"Staking Balance: <b>{ua.StakingBalance.TezToString()}</b> (<b>{ua.Delegators}</b> delegators)\n";
                    var tzKtClient = _serviceProvider.GetService<ITzKtClient>();
                    /*todo
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
                    */
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
                    return $"{label}: <b>{timestamp.ToString()}</b>\n";
                }
            }


            if (ua.ChatId != 0)
            {
                try
                {
                    var chatLink = telegramBotInvoker.GetChatLink(ua.ChatId).ConfigureAwait(false).GetAwaiter().GetResult();
                    result += $"Group: {chatLink}\n";
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
                result += resMgr.Get(Res.TransactionNotifications, ua);
                if (ua.NotifyTransactions && ua.AmountThreshold > 0)
                    result += ", > " + ua.AmountThreshold.TezToString();
                result += "\n";
                
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

                    if (ua.NotifyMisses)
                        result += "🤷🏻‍♂️";

                    if (ua.NotifyDelegatorsBalance && ua.User.Type == 0)
                    {
                        result += "🔺";
                        if (ua.DelegatorsBalanceThreshold > 0)
                            result += "✂️";
                    }
                }
                else
                {
                    result += resMgr.Get(Res.DelegationNotifications, ua);
					if (ua.NotifyDelegations && ua.DelegationAmountThreshold > 0)
						result += ", > " + ua.DelegationAmountThreshold.TezToString();
					result += "\n";

                    if (ua.User.Type == 0)
                    {
                        result += resMgr.Get(Res.DelegatorsBalanceNotifyStatus, ua);
						if (ua.NotifyDelegatorsBalance && ua.DelegatorsBalanceThreshold > 0)
							result += ", > " + ua.DelegatorsBalanceThreshold.TezToString();
						result += "\n";
                    }
                    result += resMgr.Get(Res.MissesNotifications, ua) + ua.MissesThresholdText + "\n";
                    result += resMgr.Get(Res.DelegateOutOfFreeSpace, ua) + "\n";
                    result += resMgr.Get(Res.Watchers, ua) + db.GetUserAddresses(ua.Address).Count + "\n";
                }

                if (!ua.User.HideHashTags)
                    // One new line for `address tune` and two for `inline mode`
                    // TODO: Change `result` from string to StringBuilder
                    result += new string('\n', msgid == 0 ? 2 : 1) + ua.HashTag();
                return () => SendTextMessage(db, chatId, result, tuneMisses ? ReplyKeyboards.MissesMenu(ua.User, ua.Id, ua, Config.Telegram) :
					chatId == ua.UserId
                        ? ReplyKeyboards.AddressMenu(ua.User, ua.Id, msgid == 0 ? null : ua, Config.Telegram)
                        : ReplyKeyboards.AdminAddressMenu(ua), msgid);
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
                        ? ReplyKeyboards.AddressMenu(ua.User, ua.Id, msgid == 0 ? null : ua, new Tuple<string, string>(name, ci?.@delegate))
                        : null, msgid);
            }
        }

        async Task OnMyAddresses(Storage.TezosDataContext db, MarketData md, long chatId, User user)
        {
            var addresses = db.GetUserAddresses(user.Id);
            if (addresses.Count == 0)
                await SendTextMessage(db, user.Id, resMgr.Get(Res.NoAddresses, user), ReplyKeyboards.MainMenu);
            else

            {
                var results = new List<Func<Task>>();
                foreach (var ua in addresses)
                {
                    await telegramBotInvoker.SendChatActionTyping(chatId);
                    results.Add(ViewAddress(db, md, chatId, ua, 0));
                }

                foreach (var r in results)
                    await r();
            }
        }

        async Task<(UserAddress, DelegateInfo)> NewUserAddress(Storage.TezosDataContext db, User user, string addr, string name, decimal balance, long chatId)
        {
            var ua = db.AddUserAddress(user, addr, balance, name, chatId);
            DelegateInfo di = null;
            try
            {
                if (addr.StartsWith("tz"))
                {
                    try
                    {
                        di = addrMgr.GetDelegate(addr);
                        if (di != null)
                        {
                            if (!db.Delegates.Any(o => o.Address == addr))
                            {
                                db.Delegates.Add(new Domain.Delegate {
                                    Address = addr,
                                    Name = addr.ShortAddr()
                                });
                                db.SaveChanges();
                                await NotifyUserActivity(db, $"💤 New delegate {addr} monitored");
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

        public async Task NotifyDev(Storage.TezosDataContext db, string text, long currentUserID, bool current = false)
        {
            foreach (var devUser in Config.Telegram.DevUsers)
            {
                var user = db.Users.SingleOrDefault(o => o.Username == devUser);
                if (user != null && ((user.Id != currentUserID && !current) || (user.Id == currentUserID && current)))
                {
                    while (text.Length > 4096)
                    {
                        int lastIndexOf = text.Substring(0, 4096).LastIndexOf('\n');
                        await SendTextMessage(db, user.Id, text.Substring(0, lastIndexOf), ReplyKeyboards.MainMenu);
                        text = text.Substring(lastIndexOf + 1);
                    };

                    if (text != "")
                        await SendTextMessage(db, user.Id, text, ReplyKeyboards.MainMenu);
                }
            }
        }

        async Task<int> SendTextMessage(long chatId, string text, int replaceId = 0)
        {
            if (replaceId == 0)
                return await telegramBotInvoker.SendMessage(chatId, text);
            else
				return await telegramBotInvoker.EditMessage(chatId, replaceId, text);
		}

		public void PushTextMessage(Storage.TezosDataContext db, UserAddress ua, string text)
        {
            PushTextMessage(db, ua.UserId, text);
        }
		public void PushTextMessage(Storage.TezosDataContext db, long userId, string text)
        {
            db.Add(Domain.Message.Push(userId, text));
            db.SaveChanges();
        }
        public async Task<int> SendTextMessageUA(Storage.TezosDataContext db, UserAddress ua, string text, int replaceId = 0)
        {
            if (ua.ChatId == 0)
                return await SendTextMessage(db, ua.UserId, text, ReplyKeyboards.MainMenu, replaceId);
            else
				return await SendTextMessage(ua.ChatId, text, replaceId);
        }

        int msgSent = 0;
		internal async Task<int> SendTextMessage(Storage.TezosDataContext db, long userId, string text, KeyboardMarkup keyboard, int replaceId = 0)
        {
            var u = db.GetUser(userId);
            if (u?.Inactive ?? true)
                return 0;
            try
            {
                Logger.LogInformation("->" + u.ToString() + ": " + text);
                if (replaceId == 0)
                {
                    var msgId = await telegramBotInvoker.SendMessage(userId, text, keyboard);
                    db.LogOutMessage(userId, msgId, text);
                    msgSent++;
					return msgId;
                }
                else
                {
					var msgId = await telegramBotInvoker.EditMessage(userId, replaceId, text, keyboard);
					db.LogOutMessage(userId, replaceId, text);
                    return msgId;
                }
            }
   //         catch (MessageIsNotModifiedException)
   //         {
   //         }
   //         catch (ChatNotFoundException)
   //         {
   //             u.Inactive = true;
   //             db.SaveChanges();
   //             await NotifyDev(db, "😕 User " + UserLink(u) + " not started chat with bot", userId);
   //         }
   //         catch (BadRequestException bre)
			//{
   //             if(bre.Message.Contains("no rights to send"))
			//	{
   //                 u.Inactive = true;
   //                 db.SaveChanges();
			//		await NotifyDev(db, "😕 Bot have no rights to send a message for " + UserLink(u), userId);
   //             }
   //             else
   //                 LogError(bre);
   //         }
            catch (ApiRequestException are)
            {
				await NotifyDev(db, "🐞 Error while sending message for " + UserLink(u) + ": " + are.Message + $"\n\n/{replaceId}/\n" + text, 0);
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
					await NotifyDev(db, "😕 Bot was blocked by the user " + UserLink(u), userId);
                }
                else
                    LogError(ex);
            }

            return 0;
        }

		async Task<int> CopyMessage(Storage.TezosDataContext db, long userId, long chatId, int messageId, string text)
		{
			var u = db.GetUser(userId);
			if (u.Inactive)
				return 0;
            try
            {
                Logger.LogInformation("->" + u.ToString() + ": " + text);
                var msgId = await telegramBotInvoker.CopyMessage(userId, chatId, messageId);
                db.LogOutMessage(userId, msgId, text);
                msgSent++;
                return msgId;
            }
            //         catch (MessageIsNotModifiedException)
            //         {
            //         }
            //         catch (ChatNotFoundException)
            //         {
            //             u.Inactive = true;
            //             db.SaveChanges();
            //             await NotifyDev(db, "😕 User " + UserLink(u) + " not started chat with bot", userId);
            //         }
            //         catch (BadRequestException bre)
            //{
            //             if(bre.Message.Contains("no rights to send"))
            //	{
            //                 u.Inactive = true;
            //                 db.SaveChanges();
            //		await NotifyDev(db, "😕 Bot have no rights to send a message for " + UserLink(u), userId);
            //             }
            //             else
            //                 LogError(bre);
            //         }
            catch (ApiRequestException are)
            {
                await NotifyDev(db, "🐞 Error while copying message for " + UserLink(u) + ": " + are.Message, userId);
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
                    await NotifyDev(db, "😕 Bot was blocked by the user " + UserLink(u), userId);
                }
                else
                    LogError(ex);
            }

			return 0;
		}

		internal async Task NotifyUserActivity(Storage.TezosDataContext db, string text)
        {
            foreach (var userId in Config.Telegram.ActivityChat)
            {
                try
                {
                    if (userId > 0)
                    {
                        var u = db.GetUser((int)userId);
						await telegramBotInvoker.SendMessage(userId, text, ReplyKeyboards.MainMenu);						
                    }
                    else
                        await telegramBotInvoker.SendMessage(userId, text);
                }
                catch (Exception ex)
                {
					await NotifyDev(db, $"🐞 Error while sending message for chat {userId}: " + ex.Message, 0);
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

        string UserTitle(TelegramBotHandler.User u)
        {
            return (u.FirstName + " " + u.LastName).Trim() +
                   (!String.IsNullOrEmpty(u.Username) ? " @" + u.Username + "" : "");
        }
        //string ChatTitle(Telegram.Bot.Types.Chat c)
        //{
        //    return c.Title +
        //           (!String.IsNullOrEmpty(c.Username) ? " @" + c.Username + "" : "");
        //}

        string UserLink(User u)
        {
            return $"<a href='tg://user?id={u.Id}'>{(u.Firstname + " " + u.Lastname).Trim()}</a> [{u.Id}]";
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

        /*async void Upload(Storage.TezosDataContext db, Message message, User user)
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
                await telegramBotDispatcher.SendMessage(message.Chat.Id, "Данные не загружены: " + e.Message);
            }
        }*/

        private T GetService<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}