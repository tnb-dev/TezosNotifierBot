using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Args;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Storage.Extensions;

namespace TezosNotifyBot.Commands.Admin
{
    public class SyncPayoutAddressCommand : IUpdateHandler
    {
        private TezosDataContext DbContext { get; }
        private TezosBotFacade Bot { get; }

        public SyncPayoutAddressCommand(TezosDataContext dbContext, TezosBotFacade botClient)
        {
            DbContext = dbContext;
            Bot = botClient;
        }

        public async Task HandleUpdate(TelegramBotHandler.Chat chat, int messageId, string text)
        {
            var baseAddressList = await DbContext.Set<KnownAddress>().AsNoTracking()
                .Where(x => !EF.Functions.Like(x.Name, "%payout%") && x.Name.Length > 0)
                .ToListAsync();

            await Bot.Reply(chat.Id, messageId, $"Starting to sync {baseAddressList.Count} base addresses");

            var updated = 0;
            foreach (var knownAddress in baseAddressList)
            {
                var sql = "UPDATE known_address " +
                          $"SET payout_for = '{knownAddress.Address}' " +
                          $"WHERE name ilike '{knownAddress.Name.Escape()} Payout%' and address != '{knownAddress.Address}'";

                updated += await DbContext.Database.ExecuteSqlRawAsync(sql);
            }

            await Bot.Reply(chat.Id, messageId, $"Updated {updated} payouts addresses");
        }

        public async Task HandleException(Exception exception, TelegramBotHandler.Chat chat, int messageId)
        {
            await Bot.Reply(chat.Id, messageId, $"Failed to process sync.\n\nError message: <b>{exception.Message}</b>\n");
        }
    }
}