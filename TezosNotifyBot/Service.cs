using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace TezosNotifyBot
{
    public class Service : BackgroundService
    {
        private readonly TezosBot bot;

        public Service(TezosBot bot)
        {
            this.bot = bot;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO: This is temporary solution 
            bot.Run(stoppingToken);
            
            return Task.CompletedTask;
        }
    }
}