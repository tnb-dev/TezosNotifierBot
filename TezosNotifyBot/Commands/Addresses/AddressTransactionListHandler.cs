using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Model;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Shared.Models;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Storage.Extensions;
using TezosNotifyBot.Tzkt;

namespace TezosNotifyBot.Commands.Addresses
{
    public class AddressTransactionListHandler : BaseHandler, ICallbackHandler
    {
        private AddressTransactionsRepository TransactionsRepository { get; }
        private const int takePerPage = 10;

        private readonly ITzKtClient tzKtClient;
        private readonly ResourceManager lang;

        public AddressTransactionListHandler(
            TezosDataContext db,
            TezosBotFacade botClient,
            ResourceManager lang,
            AddressTransactionsRepository transactionsRepository
        )
            : base(db, botClient)
        {
            TransactionsRepository = transactionsRepository;
            this.lang = lang;
        }

        public async Task Handle(string[] args, CallbackQuery query)
        {
            var page = args.GetInt(1);

            var address = await Db.Set<UserAddress>()
                .SingleOrDefaultAsync(x => x.Address == args[0]);

            if (address == null)
                throw new ArgumentException("Bla bla");

            var user = await Db.Users.ByIdAsync(address.UserId);

            var pager = TransactionsRepository.GetPage(address.Address, page, takePerPage);

            var message = new MessageBuilder()
                .AddLine(
                    lang.Get(
                        Res.AddressTransactionListTitle,
                        user.Language,
                        new { addressName = address.DisplayName() }
                    )
                )
                .AddEmptyLine()
                .WithHashTag("transaction_list")
                .WithHashTag(address);

            foreach (var tx in pager.Items)
            {
                var row = lang.Get(Res.AddressTransactionListItem, user.Language, new
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
                message.Build(!address.User.HideHashTags),
                parseMode: ParseMode.Html,
                disableWebPagePreview: true,
                replyMarkup: BuildKeyboard()
            );

            InlineKeyboardMarkup BuildKeyboard()
            {
                var callback = $"address-transaction-list {address.Address}";
                if (pager.Page == 1 && pager.Pages == 1)
                    return new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData(
                            lang.Get(Res.AddressTransactionListRefresh, user.Language), $"{callback} 1")
                    );

                var older = InlineKeyboardButton.WithCallbackData(
                    lang.Get(Res.AddressTransactionListOlder, user.Language), $"{callback} {pager.Page + 1}");
                var newer = InlineKeyboardButton.WithCallbackData(
                    lang.Get(Res.AddressTransactionListNewer, user.Language), $"{callback} {pager.Page - 1}");
                var latest =
                    InlineKeyboardButton.WithCallbackData(lang.Get(Res.AddressTransactionListLatest, user.Language),
                        $"{callback} 1");

                if (pager.Page == 1)
                    return new InlineKeyboardMarkup(older);
                if (pager.Page == pager.Pages)
                    return new InlineKeyboardMarkup(new[] { newer, latest, });

                return new InlineKeyboardMarkup(new[] { newer, latest, older, });
            }
        }

        public async Task HandleException(Exception exception, object sender, CallbackQuery query)
        {
            await Bot.NotifyAdmins(exception.Message);
        }
    }

    public class AddressTransactionsRepository
    {
        private const string Space = "address:tx:";
        private IMemoryCache Cache { get; }
        private ITzKtClient TzKtClient { get; }

        public AddressTransactionsRepository(IMemoryCache cache, ITzKtClient tzKtClient)
        {
            Cache = cache;
            TzKtClient = tzKtClient;
        }

        public Paginated<Transaction> GetPage(string address, int page, int take)
        {
            var key = Space + address;

            if (page == 1)
                Cache.Remove(key);

            var transactions = Cache.GetOrCreate(key, entry =>
            {
                var filter = new QueryBuilder
                {
                    { "type", "transaction" },
                    { "limit", "100" },
                    { "sender", address },
                    { "sort.desc", "id" }
                };

                entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));

                return TzKtClient.GetAccountOperations<Transaction>(address, filter.ToQueryString()).ToArray();
            });

            return transactions.Paginate(page, take);
        }
    }
}