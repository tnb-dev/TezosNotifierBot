using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Model;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Storage;

namespace TezosNotifyBot.Commands.Addresses
{
    public class AddressLinksCallbackHandler : BaseHandler, ICallbackHandler
    {
        private readonly Repository _repo;
        private readonly ResourceManager _lang;

        public AddressLinksCallbackHandler(Repository repo, ResourceManager lang, TezosDataContext db,
            TezosBotFacade botClient)
            : base(db, botClient)
        {
            _repo = repo;
            _lang = lang;
        }

        public async Task Handle(string[] args, CallbackQuery query)
        {
            var user = await Db.Users
                .SingleOrDefaultAsync(x => x.Id == query.From.Id);
            var address = await Db.UserAddresses
                .SingleOrDefaultAsync(x => x.Id == args.GetInt(0));

            if (address is null || address.UserId != query.From.Id)
            {
                // TODO: Throw an error
                return;
            }

            var isDelegate = _repo.IsDelegate(address.Address);

            var lang = user.Language;
            var title = _lang.Get(Res.AddressInfoTitle, lang, new {ua = address});

            var message = new MessageBuilder()
                .AddLine(title)
                .AddEmptyLine()
                .WithHashTag("more_info")
                .WithHashTag("rating")
                .WithHashTag(address);

            var linkData = new {address = address.Address};

            if (isDelegate)
            {
                message.AddLine(_lang.Get(Res.AddressLinkTezosNode, lang, linkData));
                message.AddLine(_lang.Get(Res.AddressLinkTzKt, lang, linkData));
            }
            else
            {
                message.AddLine(_lang.Get(Res.AddressLinkBackingBad, lang, linkData));
            }

            await Bot.SendText(query.From.Id, message.Build(!user.HideHashTags), ParseMode.Html,
                disableWebPagePreview: true);
        }
    }
}