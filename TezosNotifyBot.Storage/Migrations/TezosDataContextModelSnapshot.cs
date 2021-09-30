﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TezosNotifyBot.Storage;

namespace TezosNotifyBot.Storage.Migrations
{
    [DbContext(typeof(TezosDataContext))]
    partial class TezosDataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.2");

            modelBuilder.Entity("TezosNotifyBot.Domain.AddressConfig", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("Icon")
                        .HasColumnType("text")
                        .HasColumnName("icon");

                    b.HasKey("Id")
                        .HasName("pk_address_config");

                    b.ToTable("address_config");

                    b.HasData(
                        new
                        {
                            Id = "tz1aRoaRhSpRYvFdyvgWLL6TGyRoGF51wDjM",
                            Icon = "💎"
                        },
                        new
                        {
                            Id = "tz1NortRftucvAkD1J58L32EhSVrQEWJCEnB",
                            Icon = "🥨"
                        });
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.Delegate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("Address")
                        .HasColumnType("text")
                        .HasColumnName("address");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_delegate");

                    b.ToTable("delegate");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.DelegateRewards", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<long>("Accured")
                        .HasColumnType("bigint")
                        .HasColumnName("accured");

                    b.Property<int>("Cycle")
                        .HasColumnType("integer")
                        .HasColumnName("cycle");

                    b.Property<int>("DelegateId")
                        .HasColumnType("integer")
                        .HasColumnName("delegate_id");

                    b.Property<long>("Delivered")
                        .HasColumnType("bigint")
                        .HasColumnName("delivered");

                    b.Property<long>("Rewards")
                        .HasColumnType("bigint")
                        .HasColumnName("rewards");

                    b.HasKey("Id")
                        .HasName("pk_delegate_rewards");

                    b.HasIndex("DelegateId")
                        .HasDatabaseName("ix_delegate_rewards_delegate_id");

                    b.ToTable("delegate_rewards");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.KnownAddress", b =>
                {
                    b.Property<string>("Address")
                        .HasColumnType("text")
                        .HasColumnName("address");

                    b.Property<bool>("ExcludeWhaleAlert")
                        .HasColumnType("boolean")
                        .HasColumnName("exclude_whale_alert");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("PayoutFor")
                        .HasColumnType("text")
                        .HasColumnName("payout_for");

                    b.HasKey("Address")
                        .HasName("pk_known_address");

                    b.HasIndex("PayoutFor")
                        .HasDatabaseName("ix_known_address_payout_for");

                    b.ToTable("known_address");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.LastBlock", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("Hash")
                        .HasColumnType("text")
                        .HasColumnName("hash");

                    b.Property<int>("Level")
                        .HasColumnType("integer")
                        .HasColumnName("level");

                    b.Property<int>("Priority")
                        .HasColumnType("integer")
                        .HasColumnName("priority");

                    b.HasKey("Id")
                        .HasName("pk_last_block");

                    b.ToTable("last_block");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.Message", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("CallbackQueryData")
                        .HasColumnType("text")
                        .HasColumnName("callback_query_data");

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("create_date");

                    b.Property<bool>("FromUser")
                        .HasColumnType("boolean")
                        .HasColumnName("from_user");

                    b.Property<int>("Kind")
                        .HasColumnType("integer")
                        .HasColumnName("kind");

                    b.Property<int>("Status")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("status")
                        .HasDefaultValueSql("0");

                    b.Property<int?>("TelegramMessageId")
                        .HasColumnType("integer")
                        .HasColumnName("telegram_message_id");

                    b.Property<string>("Text")
                        .HasColumnType("text")
                        .HasColumnName("text");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_message");

                    b.HasIndex("CreateDate")
                        .HasDatabaseName("ix_message_create_date");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_message_user_id");

                    b.ToTable("message");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.Proposal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("DelegateId")
                        .HasColumnType("integer")
                        .HasColumnName("delegate_id");

                    b.Property<string>("Hash")
                        .HasColumnType("text")
                        .HasColumnName("hash");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<int>("Period")
                        .HasColumnType("integer")
                        .HasColumnName("period");

                    b.HasKey("Id")
                        .HasName("pk_proposal");

                    b.HasIndex("DelegateId")
                        .HasDatabaseName("ix_proposal_delegate_id");

                    b.ToTable("proposal");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.ProposalVote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("Ballot")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("ballot");

                    b.Property<int>("DelegateId")
                        .HasColumnType("integer")
                        .HasColumnName("delegate_id");

                    b.Property<int>("Level")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("level");

                    b.Property<int>("ProposalID")
                        .HasColumnType("integer")
                        .HasColumnName("proposal_id");

                    b.Property<int>("VotingPeriod")
                        .HasColumnType("integer")
                        .HasColumnName("voting_period");

                    b.HasKey("Id")
                        .HasName("pk_proposal_vote");

                    b.HasIndex("DelegateId")
                        .HasDatabaseName("ix_proposal_vote_delegate_id");

                    b.HasIndex("ProposalID")
                        .HasDatabaseName("ix_proposal_vote_proposal_id");

                    b.ToTable("proposal_vote");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.TezosRelease", b =>
                {
                    b.Property<string>("Tag")
                        .HasColumnType("text")
                        .HasColumnName("tag");

                    b.Property<string>("AnnounceUrl")
                        .HasColumnType("text")
                        .HasColumnName("announce_url");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<DateTime>("ReleasedAt")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("released_at");

                    b.Property<string>("Url")
                        .HasColumnType("text")
                        .HasColumnName("url");

                    b.HasKey("Tag")
                        .HasName("pk_tezos_release");

                    b.ToTable("tezos_release");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.Token", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("ContractAddress")
                        .HasColumnType("text")
                        .HasColumnName("contract_address");

                    b.Property<int>("Decimals")
                        .HasColumnType("integer")
                        .HasColumnName("decimals");

                    b.Property<int>("Level")
                        .HasColumnType("integer")
                        .HasColumnName("level");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Symbol")
                        .HasColumnType("text")
                        .HasColumnName("symbol");

                    b.Property<int>("Token_id")
                        .HasColumnType("integer")
                        .HasColumnName("token_id");

                    b.HasKey("Id")
                        .HasName("pk_token");

                    b.ToTable("token");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.TwitterMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("create_date");

                    b.Property<string>("Text")
                        .HasColumnType("text")
                        .HasColumnName("text");

                    b.Property<string>("TwitterId")
                        .HasColumnType("text")
                        .HasColumnName("twitter_id");

                    b.HasKey("Id")
                        .HasName("pk_twitter_message");

                    b.ToTable("twitter_message");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("create_date");

                    b.Property<int>("Currency")
                        .HasColumnType("integer")
                        .HasColumnName("currency");

                    b.Property<int>("EditUserAddressId")
                        .HasColumnType("integer")
                        .HasColumnName("edit_user_address_id");

                    b.Property<int>("Explorer")
                        .HasColumnType("integer")
                        .HasColumnName("explorer");

                    b.Property<string>("Firstname")
                        .HasColumnType("text")
                        .HasColumnName("firstname");

                    b.Property<bool>("HideHashTags")
                        .HasColumnType("boolean")
                        .HasColumnName("hide_hash_tags");

                    b.Property<bool>("Inactive")
                        .HasColumnType("boolean")
                        .HasColumnName("inactive");

                    b.Property<string>("Language")
                        .HasColumnType("text")
                        .HasColumnName("language");

                    b.Property<string>("Lastname")
                        .HasColumnType("text")
                        .HasColumnName("lastname");

                    b.Property<int>("NetworkIssueNotify")
                        .HasColumnType("integer")
                        .HasColumnName("network_issue_notify");

                    b.Property<bool>("ReleaseNotify")
                        .HasColumnType("boolean")
                        .HasColumnName("release_notify");

                    b.Property<bool>("SmartWhaleAlerts")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("smart_whale_alerts");

                    b.Property<string>("Title")
                        .HasColumnType("text")
                        .HasColumnName("title");

                    b.Property<int>("Type")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("type");

                    b.Property<int>("UserState")
                        .HasColumnType("integer")
                        .HasColumnName("user_state");

                    b.Property<string>("Username")
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.Property<bool>("VotingNotify")
                        .HasColumnType("boolean")
                        .HasColumnName("voting_notify");

                    b.Property<int>("WhaleAlertThreshold")
                        .HasColumnType("integer")
                        .HasColumnName("whale_alert_threshold");

                    b.HasKey("Id")
                        .HasName("pk_user");

                    b.ToTable("user");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.UserAddress", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("address");

                    b.Property<decimal>("AmountThreshold")
                        .HasColumnType("numeric")
                        .HasColumnName("amount_threshold");

                    b.Property<decimal>("Balance")
                        .HasColumnType("numeric")
                        .HasColumnName("balance");

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint")
                        .HasColumnName("chat_id");

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("create_date");

                    b.Property<decimal>("DelegationAmountThreshold")
                        .HasColumnType("numeric")
                        .HasColumnName("delegation_amount_threshold");

                    b.Property<decimal>("DelegatorsBalanceThreshold")
                        .HasColumnType("numeric")
                        .HasColumnName("delegators_balance_threshold");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_deleted");

                    b.Property<bool>("IsOwner")
                        .HasColumnType("boolean")
                        .HasColumnName("is_owner");

                    b.Property<int>("LastMessageLevel")
                        .HasColumnType("integer")
                        .HasColumnName("last_message_level");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_update");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<bool>("NotifyAwardAvailable")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_award_available");

                    b.Property<bool>("NotifyBakingRewards")
                        .HasColumnType("boolean")
                        .HasColumnName("notify_baking_rewards");

                    b.Property<bool>("NotifyCycleCompletion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_cycle_completion");

                    b.Property<bool>("NotifyDelegateStatus")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_delegate_status");

                    b.Property<bool>("NotifyDelegations")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_delegations");

                    b.Property<bool>("NotifyDelegatorsBalance")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_delegators_balance");

                    b.Property<bool>("NotifyMisses")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_misses");

                    b.Property<bool>("NotifyOutOfFreeSpace")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_out_of_free_space");

                    b.Property<bool>("NotifyPayout")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_payout");

                    b.Property<bool>("NotifyRightsAssigned")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_rights_assigned");

                    b.Property<bool>("NotifyTransactions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("notify_transactions");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_user_address");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_user_address_user_id");

                    b.ToTable("user_address");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.WhaleTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric")
                        .HasColumnName("amount");

                    b.Property<string>("FromAddress")
                        .HasColumnType("text")
                        .HasColumnName("from_address");

                    b.Property<int>("Level")
                        .HasColumnType("integer")
                        .HasColumnName("level");

                    b.Property<string>("OpHash")
                        .HasColumnType("text")
                        .HasColumnName("op_hash");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("timestamp");

                    b.Property<string>("ToAddress")
                        .HasColumnType("text")
                        .HasColumnName("to_address");

                    b.HasKey("Id")
                        .HasName("pk_whale_transaction");

                    b.ToTable("whale_transaction");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.WhaleTransactionNotify", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.Property<int>("WhaleTransactionId")
                        .HasColumnType("integer")
                        .HasColumnName("whale_transaction_id");

                    b.HasKey("Id")
                        .HasName("pk_whale_transaction_notify");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_whale_transaction_notify_user_id");

                    b.HasIndex("WhaleTransactionId")
                        .HasDatabaseName("ix_whale_transaction_notify_whale_transaction_id");

                    b.ToTable("whale_transaction_notify");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.DelegateRewards", b =>
                {
                    b.HasOne("TezosNotifyBot.Domain.Delegate", "Delegate")
                        .WithMany()
                        .HasForeignKey("DelegateId")
                        .HasConstraintName("fk_delegate_rewards_delegate_delegate_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Delegate");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.Message", b =>
                {
                    b.HasOne("TezosNotifyBot.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("fk_message_users_user_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.Proposal", b =>
                {
                    b.HasOne("TezosNotifyBot.Domain.Delegate", "Delegate")
                        .WithMany()
                        .HasForeignKey("DelegateId")
                        .HasConstraintName("fk_proposal_delegate_delegate_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Delegate");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.ProposalVote", b =>
                {
                    b.HasOne("TezosNotifyBot.Domain.Delegate", "Delegate")
                        .WithMany()
                        .HasForeignKey("DelegateId")
                        .HasConstraintName("fk_proposal_vote_delegate_delegate_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TezosNotifyBot.Domain.Proposal", "Proposal")
                        .WithMany()
                        .HasForeignKey("ProposalID")
                        .HasConstraintName("fk_proposal_vote_proposal_proposal_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Delegate");

                    b.Navigation("Proposal");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.UserAddress", b =>
                {
                    b.HasOne("TezosNotifyBot.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("fk_user_address_user_user_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.WhaleTransactionNotify", b =>
                {
                    b.HasOne("TezosNotifyBot.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("fk_whale_transaction_notify_user_user_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TezosNotifyBot.Domain.WhaleTransaction", "WhaleTransaction")
                        .WithMany("Notifications")
                        .HasForeignKey("WhaleTransactionId")
                        .HasConstraintName("fk_whale_transaction_notify_whale_transaction_whale_transactio~")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");

                    b.Navigation("WhaleTransaction");
                });

            modelBuilder.Entity("TezosNotifyBot.Domain.WhaleTransaction", b =>
                {
                    b.Navigation("Notifications");
                });
#pragma warning restore 612, 618
        }
    }
}
