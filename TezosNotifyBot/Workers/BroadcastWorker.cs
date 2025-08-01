using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage;
using Message = TezosNotifyBot.Domain.Message;
using User = Telegram.Bot.Types.User;

namespace TezosNotifyBot.Workers
{
    public class BroadcastWorker : BackgroundService
    {
        private readonly ILogger<BroadcastWorker> _logger;
        private readonly IServiceProvider _provider;
        private ITelegramBotClient Bot { get; }

        public BroadcastWorker(ITelegramBotClient bot, ILogger<BroadcastWorker> logger, IServiceProvider provider)
        {
            Bot = bot;

            _logger = logger;
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested is false)
            {
                var start = DateTime.UtcNow;
                using var scope = _provider.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<TezosDataContext>();

                try
                {
                    DateTime begin = DateTime.UtcNow;
                    var messagesInactive = await db.Set<Message>()
                        .Where(x => x.Kind == MessageKind.Push && x.TelegramMessageId == null && x.Status == MessageStatus.Sending)
                        .Where(x => x.User.Inactive)
                        .OrderBy(x => x.CreateDate)
                        .Take(1000)
                        .ToArrayAsync(stoppingToken);
                    foreach (var message in messagesInactive)
                        message.Status = MessageStatus.SentFailed;
                    await db.SaveChangesAsync();
                    // Выбираем сообщения которые были созданы для отложенной отправки
                    var messages = await db.Set<Message>()
                    .Where(x => x.Kind == MessageKind.Push && x.TelegramMessageId == null && x.Status == MessageStatus.Sending)
                    .Where(x => !x.User.Inactive)
                    .OrderBy(x => x.CreateDate)
                    .Take(3000)
                    .ToArrayAsync(stoppingToken);
                    int count = 0;
                    DateTime startBatch = DateTime.UtcNow;
                    foreach (var message in messages)
                    {
                        count++;
                        try
                        {
                            var id = await Bot.SendMessage(new ChatId(message.UserId), message.Text, parseMode: ParseMode.Html, linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true });

                            message.Sent(id.MessageId);
                            Thread.Sleep(50);
                        }
                        catch (ApiRequestException)
                        {
                            var user = await db.Set<Domain.User>()
                                .SingleOrDefaultAsync(x => x.Id == message.UserId);
                            
                            user.Inactive = true;
                        }
                        catch (Exception e)
                        {
                            message.SentFailed();
                            _logger.LogError(e, "Failed to send push message");
                        }
                        if (count == 30)
                        {
                            begin = DateTime.UtcNow;
                            await db.SaveChangesAsync();
                            count = 0;
                            var ms = (int)DateTime.UtcNow.Subtract(startBatch).TotalMilliseconds;
                            if (ms < 1000)
                                await Task.Delay(1000 - ms, stoppingToken);
                        }
                    }
                    await db.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to read push message stack from database");
                }

                // Wait one second
                if (DateTime.UtcNow.Subtract(start).TotalSeconds < 1)
                    await Task.Delay(1000, stoppingToken);
            }
        }
    }
}