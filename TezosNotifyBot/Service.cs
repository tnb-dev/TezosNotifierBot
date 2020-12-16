using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TezosNotifyBot
{
    public class Service : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public Service(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested is false)
            {
                using var scope = _serviceProvider.CreateScope();

                var bot = scope.ServiceProvider.GetRequiredService<TezosBot>();
            
                // TODO: This is temporary solution 
                await bot.Run(stoppingToken);
            }
        }
    }
}