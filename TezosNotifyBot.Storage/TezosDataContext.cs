using System;
using Microsoft.EntityFrameworkCore;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage.Extensions;
using Delegate = TezosNotifyBot.Domain.Delegate;

namespace TezosNotifyBot.Storage
{
    public class TezosDataContext: DbContext
    {

        #region DbSet's

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
        //public DbSet<BakingRights> BakingRights { get; set; }
        //public DbSet<EndorsingRights> EndorsingRights { get; set; }
        public DbSet<BalanceUpdate> BalanceUpdates { get; set; }
        public DbSet<TwitterMessage> TwitterMessages { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<WhaleTransaction> WhaleTransactions { get; set; }
        #endregion
        public TezosDataContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Add configurations below
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
            
            modelBuilder.Entity<User>(builder =>
                builder.Property(x => x.SmartWhaleAlerts).HasDefaultValue(true));
            modelBuilder.Entity<UserAddress>(builder =>
            {
                builder.Property(x => x.NotifyPayout).HasDefaultValue(true);
                builder.Property(x => x.NotifyDelegatorsBalance).HasDefaultValue(true);
                builder.Property(x => x.NotifyAwardAvailable).HasDefaultValue(true);
                builder.Property(x => x.NotifyRightsAssigned).HasDefaultValue(true);
                builder.Property(x => x.NotifyDelegateStatus).HasDefaultValue(true);
            });
            modelBuilder.Entity<UserAddressDelegation>();
            modelBuilder.Entity<Message>(builder =>
            {
                builder.Property(x => x.Status)
                    .HasDefaultValueSql("0");
                builder.HasIndex(x => x.CreateDate);
            });
            modelBuilder.Entity<Delegate>();
            modelBuilder.Entity<KnownAddress>(builder =>
            {
                builder.HasKey(x => x.Address);
                builder.HasIndex(x => x.PayoutFor);
            });
            modelBuilder.Entity<TwitterMessage>();
            modelBuilder.Entity<BalanceUpdate>();
            modelBuilder.Entity<Proposal>();
            modelBuilder.Entity<ProposalVote>();
            modelBuilder.Entity<Token>();
            modelBuilder.Entity<WhaleTransaction>();
            modelBuilder.Entity<WhaleTransactionNotify>()
                .HasOne(e => e.WhaleTransaction)
                .WithMany(o => o.Notifications)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AddressConfig>()
                .HasData(
                    new AddressConfig("tz1aRoaRhSpRYvFdyvgWLL6TGyRoGF51wDjM", "💎"),
                    new AddressConfig("tz1NortRftucvAkD1J58L32EhSVrQEWJCEnB", "🥨")
                );

            modelBuilder.Entity<TezosRelease>(builder =>
                {
                    builder.HasKey(x => x.Tag);
                    builder.Property(x => x.Tag).ValueGeneratedNever();
                });
            
            // MUST BE BELOW ANY OTHER CONFIGURATIONS
            modelBuilder.ApplyPostgresConventions();
        }
    }
}