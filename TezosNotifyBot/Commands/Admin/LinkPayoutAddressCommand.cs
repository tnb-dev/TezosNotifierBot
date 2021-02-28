using System.Threading.Tasks;
using Telegram.Bot.Args;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Storage;

namespace TezosNotifyBot.Commands.Admin
{
    public class LinkPayoutAddressCommand: IUpdateHandler
    {
        private readonly TezosBotFacade bot;
        private readonly TezosDataContext data;

        public LinkPayoutAddressCommand(TezosBotFacade bot, TezosDataContext data)
        {
            this.bot = bot;
            this.data = data;
        }

        public Task HandleUpdate(object sender, UpdateEventArgs eventArgs)
        {
            throw new System.NotImplementedException();
        }
    }
}