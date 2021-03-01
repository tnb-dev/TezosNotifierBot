using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Commands.Admin;

namespace TezosNotifyBot.Commands
{
    public class AdminCommands: CommandsProfile
    {
        public AdminCommands()
        {
            Handle<SyncPayoutAddressCommand>("/sync-payout");
            // Handle<LinkPayoutAddressCommand>("/link-payout");
        }
    }
}