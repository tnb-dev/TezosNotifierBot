using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Commands.Addresses;
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
            
            // Callbacks
            AddCallback<AddressInfoHandler>("address-links");
            AddCallback<AddressTransactionListHandler>("address-transaction-list");
        }
    }
}