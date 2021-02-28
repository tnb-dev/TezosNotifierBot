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

        public async Task HandleUpdate(object sender, UpdateEventArgs eventArgs)
        {
            var baseAddressList = await DbContext.Set<KnownAddress>().AsNoTracking()
                .Where(x => !EF.Functions.Like(x.Name, "%payout%") && x.Name.Length > 0)
                .ToListAsync();

            await Bot.Reply(eventArgs.Update.Message, $"Starting to sync {baseAddressList.Count} base addresses");

            var updated = 0;
            foreach (var knownAddress in baseAddressList)
            {
                var sql = "UPDATE known_address " +
                          $"SET payout_for = '{knownAddress.Address}' " +
                          $"WHERE name ilike '%{knownAddress.Name.Escape()}%Payout%' and address != '{knownAddress.Address}'";

                updated += await DbContext.Database.ExecuteSqlRawAsync(sql);
            }

            await Bot.Reply(eventArgs.Update.Message, $"Updated {updated} payouts addresses");
        }

        public async Task HandleException(Exception exception, UpdateEventArgs eventArgs, object sender)
        {
            await Bot.Reply(eventArgs.Update.Message, "Failed to process sync.\n\n" +
                                                      $"Error message: <b>{exception.Message}</b>\n");
        }
    }
}