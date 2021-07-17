using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;
using TezosNotifyBot.Model;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Tzkt;

namespace TezosNotifyBot.Workers
{
    public class MediumWorker : BackgroundService
    {
        private readonly ILogger<MediumWorker> _logger;
        private readonly IServiceProvider _provider;
        private readonly MediumOptions _options;
        private readonly ITzKtClient _tzKtClient;
        private int lastCycle = 0;
        private int lastLevel = 0;

        public MediumWorker(ITzKtClient tzKtClient, IOptions<MediumOptions> options, ILogger<MediumWorker> logger, IServiceProvider provider)
        {
            _options = options.Value;
            _tzKtClient = tzKtClient;
            _logger = logger;
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested is false)
            {
                using var scope = _provider.CreateScope();
                var bot = scope.ServiceProvider.GetRequiredService<TezosBot>();
                using var db = scope.ServiceProvider.GetRequiredService<TezosDataContext>();
                var repo = scope.ServiceProvider.GetRequiredService<Repository>();
                lastLevel = db.LastBlock.Single().Level;
                var cycles = _tzKtClient.GetCycles();
                var currentCycle = cycles.FirstOrDefault(c => c.firstLevel <= lastLevel && lastLevel <= c.lastLevel);
                if (lastCycle == 0 && currentCycle != null)
                {
                    lastCycle = currentCycle.index;
                    bot.NotifyDev($"MediumWorker started on cycle {lastCycle}", 0);
                }
                if (currentCycle != null && lastCycle != currentCycle.index)
                {
                    try
                    {
                        var prevCycle = cycles.Single(c => c.index == currentCycle.index - 1);
                        var result = CreatePost(repo, bot.MarketData, prevCycle, currentCycle);
                        bot.NotifyUserActivity($"New Medium post: [{result.data.title}]({result.data.url})");
                        lastCycle = currentCycle.index;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to create medium post");
                        bot.NotifyDev("🛑 Failed to create medium post: \n" + e.Message + "\n" + e.StackTrace, 0);
                    }
                }
                // Wait one minute
                await Task.Delay(60000, stoppingToken);
            }
        }

        public Medium.Response CreatePost(Repository repo, Tezos.MarketData md, Cycle prevCycle, Cycle currentCycle)
		{
            StringBuilder post = new StringBuilder();

            post.AppendLine("<p><i>This post was generated fully automatically by the <a href='https://tzsnt.fr/'>TezosNotifierBot</a>.</i></p>");
            post.AppendLine("<p><img src='https://tzsnt.fr/img/tezos-notifier-horizontal.png' /></p>");
            post.AppendLine("<h1>General cycle stats</h1>");
            post.AppendLine($"<p>Cycle {prevCycle.index} is completed in {prevCycle.Length.Days} days, {prevCycle.Length.Hours} hours, {prevCycle.Length.Minutes} minutes!</p>");
            post.AppendLine($"<p>Next {currentCycle.index} cycle will end on {currentCycle.endTime.ToString("MMMMM d a\\t HH:mm", CultureInfo.GetCultureInfo("en"))} UTC.</p>");
            post.AppendLine("<h1>Transaction stats</h1>");
            post.AppendLine($"<p>In the {prevCycle.index} cycle was made {_tzKtClient.GetTransactionsCount(prevCycle.firstLevel, prevCycle.lastLevel).ToString("###,###,###,###")} transactions.</p>");

            FillRates(post, prevCycle, currentCycle);

            post.AppendLine("<h1>Whale transactions</h1>");

            var whaleTransactions = _tzKtClient.GetTransactions($"level.ge={prevCycle.firstLevel}&level.le={prevCycle.lastLevel}&status=applied&amount.ge=500000000000");
            if (whaleTransactions.Count == 0)
                post.AppendLine("<p>Not a single whale transaction was made.</p>");
            else if (whaleTransactions.Count == 0)
                post.AppendLine("<p>There was 1 whale transaction with amount &gt; 500 000 XTZ:</p>");
            else
                post.AppendLine($"<p>There were {whaleTransactions.Count} whale transactions with amount &gt; 500 000 XTZ:</p>");
            post.AppendLine("<ul>");
            foreach (var wt in whaleTransactions)
            {
                var sender = repo.GetUserTezosAddress(0, wt.Sender.address);
                var target = repo.GetUserTezosAddress(0, wt.Target.address);
                post.AppendLine($"<li><a target='_blank' href='https://tzkt.io/{wt.Hash}?utm_source=tezosnotifierbot'>transaction</a> of {(wt.Amount / 1000000M).TezToString()} ({(wt.Amount / 1000000M).TezToUsd(md)} USD) from <a href='https://tzkt.io/{wt.Sender.address}?utm_source=tezosnotifierbot'>{sender.DisplayName()}</a> to <a href='https://tzkt.io/{wt.Target.address}?utm_source=tezosnotifierbot'>{target.DisplayName()}</a></li>");
            }
            post.AppendLine("</ul>");
            post.AppendLine($"<p>So more then {whaleTransactions.Sum(o => o.Amount / 1000000M).TezToString()} was transferred by whales!</p>");
            FillGovernance(post, prevCycle, currentCycle);
            post.AppendLine("<p><hr /></p>");
            FillLinks(post);

            var result = Publish($"Tezos Blockchain cycle {prevCycle.index} stats", post.ToString());
            return result;
        }

        void FillGovernance(StringBuilder post, Cycle cycle_prev, Cycle cycle_cur)
		{
            post.AppendLine("<h1>Governance</h1>");

            var periods = _tzKtClient.GetVotingPeriods();
            var current = periods.FirstOrDefault(c => c.firstLevel <= cycle_cur.lastLevel && cycle_cur.lastLevel <= c.lastLevel);
            var prev = periods.FirstOrDefault(c => c.firstLevel <= cycle_prev.lastLevel && cycle_prev.lastLevel <= c.lastLevel);

            if (prev.kind == "adoption" && current.kind == "proposal")
			{
                var proposals = _tzKtClient.GetProposals(prev.epoch);
                post.AppendLine($"<p>Adoption period finished. Blockchain switched to new protocol {proposals[0].DisplayLink}. New proposal period begins.</p>");
            }
            if (prev.kind == "proposal" && current.kind == "proposal")
			{
                var proposals = _tzKtClient.GetProposals(current.epoch);
                if (proposals.Count == 0)
                    post.AppendLine($"<p>Now it is proposal period in the Tezos blockchain. No proposals have been submitted.</p>");
                else
				{
                    post.AppendLine($"<p>Now it is proposal period in the Tezos blockchain. Proposals have been submitted:</p>");
                    post.AppendLine("<ul>");
                    foreach (var p in proposals)
                        post.AppendLine($"<li>{p.DisplayLink} - {p.rolls} rolls</li>");
                    post.AppendLine("</ul>");
                    if (current.upvotesQuorum <= (100D * current.topRolls / current.totalRolls))
                        post.AppendLine($"<p>Quorum of {current.upvotesQuorum:##.0}% reached.</p>");
                    else
                        post.AppendLine($"<p>Quorum of {current.upvotesQuorum:##.0}% is not reached.</p>");
                    post.AppendLine($"<p>Proposal period will end on {current.endTime.ToString("MMMMM d a\\t HH:mm", CultureInfo.GetCultureInfo("en"))} UTC.</p>");
                }
            }
            if (prev.kind == "proposal" && current.kind == "exploration")
            {
                var proposals = _tzKtClient.GetProposals(current.epoch);
                post.AppendLine($"<p>Proposal period finished. New exploration period begins with proposal {proposals[0].DisplayLink}.</p>");
            }
            if (prev.kind == "exploration" && current.kind == "exploration")
			{
                var proposals = _tzKtClient.GetProposals(current.epoch);
                post.AppendLine($"<p>Now it is exploration period for proposal {proposals[0].DisplayLink}.</p>");
                post.AppendLine($"<p>{current.yayBallots + current.nayBallots + current.passBallots} delegates ({(100D * (current.yayBallots + current.nayBallots + current.passBallots) / current.totalBakers) ?? 0:###.0}%) with total of {current.totalRolls} rolls ({(100D * (current.yayRolls + current.nayRolls + current.passRolls) / current.totalRolls) ?? 0:###.0}%) voted so far:</p>");
                FillBallots(post, current);
                BallotQuorum(post, current);
                post.AppendLine($"<p>Exploration period will end on {current.endTime.ToString("MMMMM d a\\t HH:mm", CultureInfo.GetCultureInfo("en"))} UTC.</p>");
            }
            if (prev.kind == "exploration" && current.kind == "proposal")
			{
                var proposals = _tzKtClient.GetProposals(prev.epoch);
                post.AppendLine($"<p>Exploration period for proposal {proposals[0].DisplayLink} finished with results:</p>");
                FillBallots(post, prev);
                BallotQuorum(post, prev);
                post.AppendLine($"<p>Proposal {proposals[0].DisplayLink} not accepted. New proposal period begins.</p>");
            }
            if (prev.kind == "exploration" && current.kind == "testing")
            {
                var proposals = _tzKtClient.GetProposals(prev.epoch);
                post.AppendLine($"<p>Exploration period for proposal {proposals[0].DisplayLink} finished with results:</p>");
                FillBallots(post, prev);
                post.AppendLine($"<p>Proposal {proposals[0].DisplayLink} accepted. Testing period begins.</p>");
            }
            if (prev.kind == "testing" && current.kind == "testing")
			{
                var proposals = _tzKtClient.GetProposals(current.epoch);
                post.AppendLine($"<p>Now it is testing period for proposal {proposals[0].DisplayLink}.</p>");
                post.AppendLine($"<p>Testing period will end on {current.endTime.ToString("MMMMM d a\\t HH:mm", CultureInfo.GetCultureInfo("en"))} UTC.</p>");
            }
            if (prev.kind == "testing" && current.kind == "promotion")
            {
                var proposals = _tzKtClient.GetProposals(current.epoch);
                post.AppendLine($"<p>Testing period finished. Promotion period begins with proposal {proposals[0].DisplayLink}.</p>");
            }
            if (prev.kind == "promotion" && current.kind == "promotion")
            {
                var proposals = _tzKtClient.GetProposals(current.epoch);
                post.AppendLine($"<p>Now it is promotion period for proposal {proposals[0].DisplayLink}.</p>");
                post.AppendLine($"<p>{current.yayBallots + current.nayBallots + current.passBallots} delegates ({(100D * (current.yayBallots + current.nayBallots + current.passBallots) / current.totalBakers) ?? 0:###.0}%) with total of {current.totalRolls} rolls ({(100D * (current.yayRolls + current.nayRolls + current.passRolls) / current.totalRolls) ?? 0:###.0}%) voted so far:</p>");
                FillBallots(post, current);
                BallotQuorum(post, current);
                post.AppendLine($"<p>Promotion period will end on {current.endTime.ToString("MMMMM d a\\t HH:mm", CultureInfo.GetCultureInfo("en"))} UTC.</p>");
            }
            if (prev.kind == "promotion" && current.kind == "proposal")
            {
                var proposals = _tzKtClient.GetProposals(prev.epoch);
                post.AppendLine($"<p>Promotion period for proposal {proposals[0].DisplayLink} finished with results:</p>");
                FillBallots(post, prev);
                BallotQuorum(post, prev);
                post.AppendLine($"<p>Proposal {proposals[0].DisplayLink} not accepted. New proposal period begins.</p>");
            }
            if (prev.kind == "promotion" && current.kind == "adoption")
            {
                var proposals = _tzKtClient.GetProposals(prev.epoch);
                post.AppendLine($"<p>Promotion period for proposal {proposals[0].DisplayLink} finished with results:</p>");
                FillBallots(post, prev);
                post.AppendLine($"<p>Proposal {proposals[0].DisplayLink} accepted. Adoption period begins.</p>");
            }
            if (prev.kind == "adoption" && current.kind == "adoption")
            {
                var proposals = _tzKtClient.GetProposals(current.epoch);
                post.AppendLine($"<p>Now it is adoption period for proposal {proposals[0].DisplayLink}.</p>");
                post.AppendLine($"<p>Adoption period will end on {current.endTime.ToString("MMMMM d a\\t HH:mm", CultureInfo.GetCultureInfo("en"))} UTC.</p>");
            }
        }

        void BallotQuorum(StringBuilder post, VotingPeriod vp)
		{
            if (vp.ballotsQuorum < (100D * (vp.yayRolls + vp.nayRolls + vp.passRolls) / vp.totalRolls))
                post.AppendLine($"<p>Quorum of {vp.ballotsQuorum:#0.0}% reached</p>");
            else
                post.AppendLine($"<p>Quorum of {vp.ballotsQuorum:#0.0}% not reached</p>");
        }

        void FillBallots(StringBuilder post, VotingPeriod vp)
        {
            post.AppendLine("<ul>");
            post.AppendLine($"<li>Yay - {vp.yayRolls} rolls ({(100D * vp.yayRolls / (vp.yayRolls + vp.nayRolls + vp.passRolls)) ?? 0:##0.0}%)</li>");
            post.AppendLine($"<li>Nay - {vp.nayRolls} rolls ({(100D * vp.nayRolls / (vp.yayRolls + vp.nayRolls + vp.passRolls)) ?? 0:##0.0}%)</li>");
            post.AppendLine($"<li>Pass - {vp.passRolls} rolls ({(100D * vp.passRolls / (vp.yayRolls + vp.nayRolls + vp.passRolls)) ?? 0:##0.0}%)</li>");
            post.AppendLine("</ul>");
        }

        void FillRates(StringBuilder post, Cycle prevCycle, Cycle cycle)
		{
            var histUrl = $"https://min-api.cryptocompare.com/data/v2/histohour?fsym=XTZ&tsym=USD&limit=72&toTs={new DateTimeOffset(cycle.startTime).ToUnixTimeSeconds()}&api_key=378ecd1eb63001a82b202939e2c731e12b65b4854d308b580e9b5c448565a54f";
            WebClient wc = new WebClient();
            var histDataStr = wc.DownloadString(histUrl);
            var histData = JsonSerializer.Deserialize<CryptoCompare.HistohourResult>(histDataStr);
            
            string strPrices = wc.DownloadString("https://min-api.cryptocompare.com/data/price?fsym=XTZ&tsyms=BTC,USD,EUR,ETH&api_key=378ecd1eb63001a82b202939e2c731e12b65b4854d308b580e9b5c448565a54f");
            var dtoPrice = JsonSerializer.Deserialize<Tezos.CryptoComparePrice>(strPrices);

            post.AppendLine("<h1>Market data</h1>");
            post.AppendLine("<p>Current XTZ price:</p>");
            post.AppendLine("<ul>");
            post.AppendLine($"<li>{dtoPrice.USD} USD</li>");
            post.AppendLine($"<li>{dtoPrice.EUR} EUR</li>");
            post.AppendLine($"<li>{dtoPrice.BTC} BTC</li>");
            post.AppendLine($"<li>{dtoPrice.ETH} ETH</li>");
            post.AppendLine("</ul>");
            post.AppendLine($"<p>Price change in the {prevCycle.index} cycle:</p>");
            post.AppendLine("<ul>");
            post.AppendLine($"<li>low {histData.Data.Data.Where(d => d.Timestamp >= prevCycle.startTime).Min(d => d.low)} USD</li>");
            post.AppendLine($"<li>high {histData.Data.Data.Where(d => d.Timestamp >= prevCycle.startTime).Max(d => d.high)} USD</li>");
            post.AppendLine("</ul>");

            var stat = _tzKtClient.GetCycleStats(prevCycle.index);
            post.AppendLine($"<p>Current Market Cap is {(dtoPrice.USD * (stat.totalSupply / 1000000)).ToString("###,###,###,###,###,###")} USD</p>");
            post.AppendLine($"<p>Current Supply is {(stat.totalSupply / 1000000).ToString("###,###,###,###,###,###")} XTZ</p>");
        }

        void FillLinks(StringBuilder post)
        {
            post.AppendLine("<p><strong>Links:</strong></p>");
            post.AppendLine("<ul>");
            post.AppendLine("<li>Telegram Bot: https://t.me/TezosNotifierBot</li>");
            post.AppendLine("<li>Twitter Bot: https://twitter.com/NotifierTezos</li>");
            post.AppendLine("<li>Twitter Announcements: https://twitter.com/TezosNotifier</li>");
            post.AppendLine("<li>Medium: https://medium.com/@tezosnotifier</li>");
            post.AppendLine("<li>Github: https://github.com/tnb-dev/TezosNotifierBot</li>");
            post.AppendLine("<li>Web: https://tzsnt.fr</li>");
            post.AppendLine("</ul>");
        }

        Medium.Response Publish(string title, string post)
        {
            WebClient wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _options.AuthToken);
            wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            wc.Headers.Add(HttpRequestHeader.Accept, "application/json");
            wc.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");

            var obj = new
            {
                title = title,
                contentFormat = "html",
                content = post,
                publishStatus = _options.PublishStatus,
                tags = new string[] { "tezos", "blockchain", "stats" }
            };
            var data = JsonSerializer.Serialize(obj);

            var str = wc.UploadString($"{_options.ApiUrl}v1/users/{_options.UserId}/posts", data);
            return JsonSerializer.Deserialize<Medium.Response>(str);
        }
    }
}