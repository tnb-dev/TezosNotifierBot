using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Tzkt;

namespace TezosNotifyBot.Services
{
    public class AddressService
    {
        private readonly TezosDataContext data;
        private readonly ITzKtClient tzKtClient;

        public AddressService(TezosDataContext data, ITzKtClient tzKtClient)
        {
            this.data = data;
            this.tzKtClient = tzKtClient;
        }

        public async Task<DateTime?> GetDelegateLastActive(string address)
        {
            var payoutAddresses = await data.KnownAddresses
                .Where(x => EF.Functions.ILike(x.Name, $"{address}%"))
                .ToArrayAsync();

            var lastActive = tzKtClient.GetAccountLastActive(address);
            foreach (var payoutAddress in payoutAddresses)
            {
                var result = tzKtClient.GetAccountLastActive(payoutAddress.Address);
                if (result > lastActive)
                    lastActive = result;
            }

            return lastActive;
        }
    }
}