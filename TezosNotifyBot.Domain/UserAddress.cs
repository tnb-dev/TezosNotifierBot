#nullable enable
using System;
using TezosNotifyBot.Shared.Extensions;

namespace TezosNotifyBot.Domain
{
    public class UserAddress
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal Balance { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool IsDeleted { get; set; }
        public string? Name { get; set; }
        public bool NotifyBakingRewards { get; set; }
        public decimal AmountThreshold { get; set; }

        public bool NotifyPayout { get; set; } = true;
        public bool NotifyTransactions { get; set; }
        public bool NotifyCycleCompletion { get; set; }
        public bool NotifyDelegations { get; set; }
        
        /// <summary>
        /// Is user subscribed to notifications about changing delegate's balance 
        /// </summary>
        /// <see cref="DelegatorsBalanceThreshold"/>
        public bool NotifyDelegatorsBalance { get; set; }

        /// <summary>
        /// </summary>
        /// <see cref="NotifyDelegatorsBalance"/>
        public decimal DelegatorsBalanceThreshold { get; set; }
        
        public decimal DelegationAmountThreshold { get; set; }
        public bool NotifyMisses { get; set; }
        public long ChatId { get; set; }

        //public string UsdBalance(decimal price_usd)
        //{
        //    return (Balance * price_usd).ToString("### ### ### ### ##0.00", CultureInfo.InvariantCulture).Trim();
        //}
        //public string BtcBalance(decimal price_btc)
        //{
        //    return (Balance * price_btc).ToString("# ### ##0.####", CultureInfo.InvariantCulture).Trim();
        //}
        //public string TezBalance()
        //{
        //    return Balance.TezToString();
        //}

        public string HashTag()
        {
            if (String.IsNullOrEmpty(Name))
                return " #" + ShortAddr().Replace("…", "").ToLower();
            var ht = System.Text.RegularExpressions.Regex.Replace(Name.ToLower(), "[^a-zа-я0-9]", "");
            if (ht != "")
                return " #" + ht;
            else
                return " #" + ShortAddr().Replace("…", "").ToLower();
        }

        public string ShortAddr()
        {
            return Address.ShortAddr();
        }

        public string DisplayName()
        {
            return String.IsNullOrEmpty(Name) ? ShortAddr() : Name;
        }

        public decimal FullBalance;
        public decimal StakingBalance;
        public int Rolls => (int)StakingBalance / 8000;
        public decimal FreeSpace;
        public int Delegators;
        public decimal AveragePerformance;
        public decimal Performance;
    }
}