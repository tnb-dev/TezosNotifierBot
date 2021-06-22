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
            
            var text = _lang.Get(Res.AddressInfoTitle, user.Language, new
            {
                ua = address,
                isDelegate = isDelegate
            });

            var message = new MessageBuilder()
                .AddLine(text)
                .WithHashTag("more_info")
                .WithHashTag("rating")
                .WithHashTag(address);
            
            // TODO: Add new lines based on `isDelegate` variable 

            await Bot.SendText(query.From.Id, message.Build(!user.HideHashTags), ParseMode.Html);
        }
    }
}