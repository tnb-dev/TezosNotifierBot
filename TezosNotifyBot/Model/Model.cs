using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;

namespace TezosNotifyBot.Model
{
    public class BotDataContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<LastBlock> LastBlock { get; set; }
        public DbSet<Delegate> Delegates { get; set; }
        public DbSet<UserAddressDelegation> UserAddressDelegations { get; set; }
        public DbSet<DelegateRewards> DelegateRewards { get; set; }
		public DbSet<Proposal> Proposals { get; set; }
		public DbSet<KnownAddress> KnownAddresses { get; set; }
		public DbSet<ProposalVote> ProposalVotes { get; set; }
		public DbSet<BakingRights> BakingRights { get; set; }
		public DbSet<EndorsingRights> EndorsingRights { get; set; }
		public DbSet<BalanceUpdate> BalanceUpdates { get; set; }
		public DbSet<TwitterMessage> TwitterMessages { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
			if (TezosBot.Config != null)
				optionsBuilder.UseSqlite("Data Source=" + TezosBot.Config.DatabasePath);
			else
				optionsBuilder.UseSqlite("Data Source=tezosnotifydata.db");
		}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAddress>()
               .Property(b => b.NotifyTransactions)
               .HasDefaultValue(true);
			modelBuilder.Entity<UserAddress>()
			   .Property(b => b.NotifyDelegations)
			   .HasDefaultValue(true);
			modelBuilder.Entity<UserAddress>()
			   .Property(b => b.NotifyMisses)
			   .HasDefaultValue(true);
			modelBuilder.Entity<UserAddress>()
               .Property(b => b.NotifyCycleCompletion)
               .HasDefaultValue(true);
			modelBuilder.Entity<ProposalVote>()
			   .Property(b => b.Ballot)
			   .HasDefaultValue(0);
			modelBuilder.Entity<ProposalVote>()
			   .Property(b => b.Level)
			   .HasDefaultValue(0);
		}
    }

    public class User
    {
        public int UserId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Username { get; set; }
        public DateTime CreateDate { get; set; }
        public string Language { get; set; }
        public bool Inactive { get; set; }
        public UserState UserState { get; set; }
        public int EditUserAddressId { get; set; }
        public bool HideHashTags { get; set; }
		public int WhaleAlertThreshold { get; set; }
		public bool VotingNotify { get; set; }
		public int NetworkIssueNotify { get; set; }
		public int Explorer { get; set; }

		public override string ToString()
        {
            return Firstname + " " + Lastname + (Username != "" ? " @" + Username : "");
        }

        public bool IsAdmin()
        {
            return TezosBot.Config.DevUserNames.Contains(Username);
        }

		public int WhaleThreshold
		{
			get { return WhaleAlertThreshold; }
		}

		public bool NetworkIssueNotified;
	}

    public enum UserState
    {
        Default = 0,
        Support = 1,
        Broadcast = 2,
        SetAmountThreshold = 3,
		SetDlgAmountThreshold = 4,
		SetName = 5,
		NotifyFollowers = 6
	}

    public class Message
    {
        public int MessageId { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public DateTime CreateDate { get; set; }
        public string Text { get; set; }
        public string CallbackQueryData { get; set; }
        public bool FromUser { get; set; }
        public int TelegramMessageId { get; set; }
    }

    public class UserAddress
    {
        public int UserAddressId { get; set; }
        public string Address { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal Balance { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool IsDeleted { get; set; }
        public string Name { get; set; }
        public bool NotifyBakingRewards { get; set; }
        public decimal AmountThreshold { get; set; }
        public bool NotifyTransactions { get; set; }
        public bool NotifyCycleCompletion { get; set; }
		public bool NotifyDelegations { get; set; }
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

    public class Delegate
    {
        public int DelegateId { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
    }

    public class DelegateRewards
    {
        public int DelegateRewardsId { get; set; }

        public long Rewards { get; set; }
        public long Accured { get; set; }
        public long Delivered { get; set; }

        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }

        public int Cycle { get; set; }
    }

    public class UserAddressDelegation
    {
        public int UserAddressDelegationId { get; set; }
        public UserAddress UserAddress { get; set; }
        public int UserAddressId { get; set; }
        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }
    }

    public class LastBlock
    {
        public int LastBlockID { get; set; }
        public int Level { get; set; }
        public int Priority { get; set; }
		public string Hash { get; set; }
    }

	public class Proposal
	{
		public int ProposalID { get; set; }
		public string Hash { get; set; }
		public string Name { get; set; }
		public int Period { get; set; }
		public Delegate Delegate { get; set; }
		public int DelegateId { get; set; }

		public string HashTag()
		{
			return " #" + (Hash.Substring(0, 7) + Hash.Substring(Hash.Length - 5)).ToLower();
		}
		public int VotedRolls;
		public List<UserAddress> Delegates;
	}

	public class ProposalVote
	{
		public int ProposalVoteID { get; set; }
		public Delegate Delegate { get; set; }
		public int DelegateId { get; set; }
		public Proposal Proposal { get; set; }
		public int ProposalID { get; set; }
		public int Level { get; set; }
		public int VotingPeriod { get; set; }
		public int Ballot { get; set; } //0 - поддержка, 1 - yay, 2 - nay, 3 - pass
	}

	public class KnownAddress
	{
		public int KnownAddressId { get; set; }
		public string Address { get; set; }
		public string Name { get; set; }
	}

	public class BakingRights
	{
		public int BakingRightsID { get; set; }

		public Delegate Delegate { get; set; }
		public int DelegateId { get; set; }

		public int Level { get; set; }
	}

	public class EndorsingRights
	{
		public int EndorsingRightsID { get; set; }

		public Delegate Delegate { get; set; }
		public int DelegateId { get; set; }

		public int Level { get; set; }
		public int SlotCount { get; set; }
	}

	public class BalanceUpdate
	{
		public int BalanceUpdateID { get; set; }

		public Delegate Delegate { get; set; }
		public int DelegateId { get; set; }
		public long Amount { get; set; }

		public int Type { get; set; } // 1 - reward for baking, 2 - reward for endorsing, 3 - missed reward for baking, 4 - missed reward for endorsing
		public int Level { get; set; }
		public int Slots { get; set; }
	}

	public class TwitterMessage
	{
		public int TwitterMessageID { get; set; }

		public string Text { get; set; }
		public DateTime CreateDate { get; set; }
		public string TwitterId { get; set; }
	}
}
