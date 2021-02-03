using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TezosNotifyBot.BetterCallDev;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Storage;

namespace TezosNotifyBot.Workers
{
	public class TokensMonitorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokensMonitorWorker> _logger;
        private readonly IBetterCallDevClient _bcdClient;

        public TokensMonitorWorker(IServiceProvider serviceProvider, ILogger<TokensMonitorWorker> logger, IBetterCallDevClient bcdClient)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _bcdClient = bcdClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested is false)
            {
                using var scope = _serviceProvider.CreateScope();
                await using var db = scope.ServiceProvider.GetRequiredService<TezosDataContext>();

                try
                {
                    var tokensCount = db.Set<Domain.Token>().Select(t => t.ContractAddress).Distinct().Count();

                    foreach (var token in _bcdClient.GetTokens(tokensCount))
                    {
                        if (!db.Set<Domain.Token>().Any(t => t.ContractAddress == token.contract && t.Token_id == token.token_id))
                        {
                            db.Add(new Domain.Token
                            {
                                ContractAddress = token.contract,
                                Decimals = token.decimals,
                                Name = token.name,
                                Symbol = token.symbol,
                                Token_id = token.token_id
                            });
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to load tokens list");
                }

                await Task.Delay(60000);
            }
        }
    }
}
