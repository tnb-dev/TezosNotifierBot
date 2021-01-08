using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage;
using Message = TezosNotifyBot.Domain.Message;

namespace TezosNotifyBot.Workers
{
    public class BroadcastWorker : BackgroundService
    {
        private readonly ILogger<BroadcastWorker> _logger;
        private readonly IServiceProvider _provider;
        private TelegramBotClient Bot { get; }

        public BroadcastWorker(TelegramBotClient bot, ILogger<BroadcastWorker> logger, IServiceProvider provider)
        {
            Bot = bot;

            _logger = logger;
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested is false)
            {
                using var scope = _provider.CreateScope();
                await using var db = scope.ServiceProvider.GetRequiredService<TezosDataContext>();

                // Выбираем сообщения которые были созданы для отложенной отправки
                var messages = await db.Set<Message>()
                    .Where(x => x.Kind == MessageKind.Push && x.TelegramMessageId == null &&
                                x.Status == MessageStatus.Queued)
                    .OrderBy(x => x.Id)
                    .Take(30)
                    .ToArrayAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var id = await Bot.SendTextMessageAsync(new ChatId(message.UserId), message.Text,
                            ParseMode.Html, true);
                        message.Sent(id.MessageId);
                    }
                    catch (Exception e)
                    {
                        message.Failed();
                        _logger.LogError($"Failed to send push message for user {message.Id}", e);
                    }

                    await db.SaveChangesAsync();
                }

                // Wait one second
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}