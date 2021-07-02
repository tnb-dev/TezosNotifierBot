using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NornPool.Model;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Commands;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Model;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Tzkt;
using Delegate = TezosNotifyBot.Domain.Delegate;

namespace TezosNotifyBot.Events.Handlers
{
    public class NotifyDelegateInactive : IEventHandler<CycleCompletedEvent>
    {
        private readonly TezosDataContext _db;
        private readonly ITzKtClient _tzkt;
        private readonly IOptions<BotConfig> _config;
        private readonly ResourceManager _lang;

        public NotifyDelegateInactive(ITzKtClient tzkt, IOptions<BotConfig> config, TezosDataContext db,
            ResourceManager lang)
        {
            _db = db;
            _tzkt = tzkt;
            _lang = lang;
            _config = config;
        }

        public async Task Process(CycleCompletedEvent subject)
        {
            var delegateAddressList = await _db.Set<Delegate>().Select(x => x.Address).ToArrayAsync();

            var delegates = new Dictionary<string, DateTime?>();

            var delegators = await _db.Set<UserAddress>()
                .Include(x => x.User)
                .Where(x => !x.IsDeleted)
                .Where(x => x.NotifyDelegateStatus && !delegateAddressList.Contains(x.Address))
                .ToArrayAsync();


            var inactiveBound = _config.Value.DelegateInactiveTime;

            foreach (var delegator in delegators)
            {
                var account = _tzkt.GetAccount(delegator.Address);
                if (account.Delegate == null) continue;

                var delegateName = account.Delegate.Alias;
                var delegateAddress = account.Delegate.Address;
                if (delegates.ContainsKey(delegateAddress) is false)
                {
                    delegates.Add(delegateAddress, _tzkt.GetAccountLastActive(delegateAddress));
                }

                var delegateLastActive = delegates[delegateAddress];
                if (delegateLastActive == null) continue;

                // Skip if last active recently than `inactiveBound` time span
                if (DateTime.Now.Subtract((DateTime) delegateLastActive) < inactiveBound)
                    continue;

                // Bad hack
                var @delegate = new UserAddress
                {
                    Name = delegateName,
                    Address = delegateAddress
                };

                var user = delegator.User;
                var text = _lang.Get(Res.DelegateInactive, user.Language, new
                {
                    t = Explorer.FromId(user.Explorer), // TODO: Fix it
                    delegateName = @delegate.DisplayName(),
                    delegateAddress,
                    inactiveTime = inactiveBound.Humanize(culture: new CultureInfo(user.Language), toWords: false,
                        maxUnit: TimeUnit.Day),
                    delegatorName = delegator.DisplayName(),
                    delegatorAddress = delegator.Address
                });

                var message = new MessageBuilder()
                    .AddLine(text)
                    .WithHashTag("delegate_inactive")
                    .WithHashTag(@delegate);

                await _db.AddAsync(Message.Push(user.Id, message.Build(!user.HideHashTags)));
                await _db.SaveChangesAsync();
            }
        }
    }
}