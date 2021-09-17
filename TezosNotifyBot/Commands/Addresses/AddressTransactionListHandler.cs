using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Model;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Tzkt;

namespace TezosNotifyBot.Commands.Addresses
{
    public class AddressTransactionListHandler : BaseHandler, ICallbackHandler
    {
        private readonly ITzKtClient tzKtClient;
        private readonly ResourceManager langManager;

        public AddressTransactionListHandler(TezosDataContext db, TezosBotFacade botClient, ITzKtClient tzKtClient,
            ResourceManager langManager)
            : base(db, botClient)
        {
            this.tzKtClient = tzKtClient;
            this.langManager = langManager;
        }

        public async Task Handle(string[] args, CallbackQuery query)
        {
            var page = args.GetInt(1);
            var userAddress = await Db.UserAddresses
                .Include(x => x.User)
                .SingleOrDefaultAsync(x => x.Address == args[0]);

            if (userAddress == null)
                throw new ArgumentException("Bla bla");

            var user = userAddress.User;

            var transactions = tzKtClient.GetAccountOperations<Transaction>(
                userAddress.Address,
                $"type=transaction&limit=10&sender={userAddress.Address}"
            );

            var message = new MessageBuilder()
                .AddLine(
                    langManager.Get(
                        Res.AddressTransactionListTitle,
                        user.Language,
                        new { addressName = userAddress.DisplayName() }
                    )
                )
                .AddEmptyLine()
                .WithHashTag("transaction_list")
                .WithHashTag(userAddress);

            foreach (var tx in transactions)
            {
                var row = langManager.Get(Res.AddressTransactionListItem, user.Language, new
                {
                    hash = tx.Hash,
                    amount = tx.Amount,
                    targetName = tx.Target.DisplayName(),
                    targetAddress = tx.Target.address,
                    timestamp = tx.Timestamp.ToLocaleString(user.Language),
                    t = Explorer.FromId(user.Explorer),
                });
                message.AddLine(row);
            }

            await Bot.EditText(
                query.From.Id,
                query.Message.MessageId,
                message.Build(!userAddress.User.HideHashTags),
                parseMode: ParseMode.Html
            );
        }

        public async Task HandleException(Exception exception, object sender, CallbackQuery query)
        {
            await Bot.NotifyAdmins(exception.Message);
        }
    }
}