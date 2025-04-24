using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Model;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Storage;
using System.Linq;

namespace TezosNotifyBot.Commands.Addresses
{
    public class AddressInfoHandler : BaseHandler, ICallbackHandler
    {
        private readonly ResourceManager _lang;

        public AddressInfoHandler(ResourceManager lang, TezosDataContext db,
            TezosBotFacade botClient)
            : base(db, botClient)
        {
            _lang = lang;
        }

        public async Task Handle(string[] args, long userId, int messageId)
        {
            var user = await Db.Users.SingleOrDefaultAsync(x => x.Id == userId);
            var address = await Db.UserAddresses.SingleOrDefaultAsync(x => x.Id == args.GetInt(0));

            if (address is null || address.UserId != userId)
            {
                // TODO: Throw an error
                return;
            }

            var isDelegate = Db.Delegates.Any(o => o.Address == address.Address);

            var lang = user.Language;
            var title = _lang.Get(Res.AddressInfoTitle, lang, new { ua = address });

            var message = new MessageBuilder()
                .AddLine(title)
                .AddEmptyLine()
                .WithHashTag("more_info")
                .WithHashTag("rating")
                .WithHashTag(address);

            var linkData = new { address = address.Address };

            if (isDelegate)
            {
                message.AddLine(_lang.Get(Res.AddressLinkTzKt, lang, linkData));
            }
            else
            {
                message.AddLine(_lang.Get(Res.AddressLinkBackingBad, lang, linkData));
            }

            var buttons = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData(
                    _lang.Get(Res.AddressTransactionListLink, user.Language, new {}),
                    $"address-transaction-list {address.Address} 1"
                )
            );

            await Bot.SendText(
				userId,
                message.Build(!user.HideHashTags),
                ParseMode.Html,
                disableWebPagePreview: true,
                replyMarkup: buttons
            );
        }
    }
}