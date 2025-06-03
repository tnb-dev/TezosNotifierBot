using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Commands.Admin;

namespace TezosNotifyBot.Commands
{
    public class NewFlowProfile: CommandsProfile
    {
        public NewFlowProfile()
        {
            // Commands
            AddCommand<SyncPayoutAddressCommand>("/sync-payout");
            AddCommand<ExcludeWhaleCommand>("/exclude-whale");
        }
    }
}