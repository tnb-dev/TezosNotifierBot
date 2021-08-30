using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NornPool.Model;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Model;
using TezosNotifyBot.Nodes;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Tzkt;

namespace TezosNotifyBot.Workers
{
	public class WhaleMonitorWorker : BackgroundService
	{
		private readonly ILogger<WhaleMonitorWorker> _logger;
		private readonly IServiceProvider _provider;
        private readonly ResourceManager resMgr;
        private readonly BotConfig _config;
        private readonly NodeManager _nodeManager;
        int lastBlock = 0;
        public WhaleMonitorWorker(IOptions<BotConfig> config, ILogger<WhaleMonitorWorker> logger, ResourceManager resourceManager, IServiceProvider provider, NodeManager nodeManager)
		{
			_logger = logger;
			_provider = provider;
            _config = config.Value;
            _nodeManager = nodeManager;
            resMgr = resourceManager;
		}
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			System.Threading.Thread.Sleep(10000);
			while (stoppingToken.IsCancellationRequested is false)
			{
				try
				{
                    await Run(stoppingToken);
				}
                catch(Exception e)
				{
                    _logger.LogError(e, e.Message);
				}
			}
		}

        async Task Run(CancellationToken stoppingToken)
		{
            using var scope = _provider.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<TezosDataContext>();
            var bot = scope.ServiceProvider.GetRequiredService<TezosBot>();

            var block = db.LastBlock.Single();
            if (block.Level > lastBlock)
            {
                var tzKt = _provider.GetService<ITzKtClient>();
                var tzKtBlock = tzKt.GetBlock(block.Level);
                var repo = _provider.GetRequiredService<Repository>();
                var wtlist = repo.GetWhaleTransactions();
                var allUsers = repo.GetUsers();
                var md = _nodeManager.Client.GetMarketData();

                foreach (var address in wtlist.GroupBy(o => o.FromAddress).Where(o => o.Sum(o1 => o1.Amount) >= 250000))
                {
                    var minLevel = address.Min(a => a.Level);
                    var maxLevel = address.Max(a => a.Level);
                    if (maxLevel < block.Level)
                        continue;
                    var timeStamp = address.Min(a => a.Timestamp);
                    var from_start = tzKt.GetBalance(address.Key, minLevel - 1);
                    var from_end = tzKt.GetBalance(address.Key, block.Level);
                    var amount = (from_start - from_end);
                    foreach (var u in allUsers.Where(o =>
                         !o.Inactive && o.WhaleThreshold > 0 && o.WhaleThreshold <= amount && o.SmartWhaleAlerts))
                    {
                        var ua_from = repo.GetUserTezosAddress(u.Id, address.Key);
                        var listFiltered = address.Where(o => !o.Notifications.Any(n => n.UserId == u.Id) && o.Amount < u.WhaleThreshold);

                        if (listFiltered.Count() <= 1 || listFiltered.Sum(o => o.Amount) < u.WhaleThreshold) continue;

                        string result = resMgr.Get(Res.WhaleOutflow,
                            new ContextObject
                            {
                                u = u,
                                Amount = from_start - from_end,
                                md = md,
                                ua = ua_from,
                                Period = (int)Math.Ceiling(tzKtBlock.Timestamp.Subtract(timeStamp).TotalDays)
                            });
                        string tags = "";
                        foreach (var op in listFiltered.OrderByDescending(o => o.Amount).Take(10).OrderBy(o => o.Level))
                        {
                            var ua_to = repo.GetUserTezosAddress(u.Id, op.ToAddress);
                            result += "\n" + resMgr.Get(Res.WhaleOutflowItem,
                            new ContextObject
                            {
                                u = u,
                                Amount = op.Amount,
                                md = md,
                                ua = ua_to,
                                Block = op.Level,
                                OpHash = op.OpHash
                            });
                            repo.AddWhaleTransactionNotify(op.Id, u.Id);
                            tags += ua_to.HashTag();
                        }
                        result += "\n" + resMgr.Get(Res.TurnOff, u) + ": /outflow_off";
                        if (!u.HideHashTags)
                        {
                            result += "\n\n#whale" + ua_from.HashTag() + tags;
                        }

                        bot.SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
                    }
                }

                var minDate = tzKtBlock.Timestamp.AddDays(-_config.WhaleSeriesLength);
                repo.CleanWhaleTransactions(minDate);

                lastBlock = block.Level;
            }

            await Task.Delay(1000, stoppingToken);
        }
	}
}
