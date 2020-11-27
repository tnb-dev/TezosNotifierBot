using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TezosNotifyBot.Storage.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "delegate",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delegate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "known_address",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_known_address", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "last_block",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    level = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_last_block", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "twitter_message",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "text", nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    twitter_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitter_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    firstname = table.Column<string>(type: "text", nullable: true),
                    lastname = table.Column<string>(type: "text", nullable: true),
                    username = table.Column<string>(type: "text", nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    language = table.Column<string>(type: "text", nullable: true),
                    inactive = table.Column<bool>(type: "boolean", nullable: false),
                    user_state = table.Column<int>(type: "integer", nullable: false),
                    edit_user_address_id = table.Column<int>(type: "integer", nullable: false),
                    hide_hash_tags = table.Column<bool>(type: "boolean", nullable: false),
                    whale_alert_threshold = table.Column<int>(type: "integer", nullable: false),
                    voting_notify = table.Column<bool>(type: "boolean", nullable: false),
                    network_issue_notify = table.Column<int>(type: "integer", nullable: false),
                    explorer = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "baking_rights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_baking_rights", x => x.id);
                    table.ForeignKey(
                        name: "fk_baking_rights_delegates_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "balance_update",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    slots = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_balance_update", x => x.id);
                    table.ForeignKey(
                        name: "fk_balance_update_delegates_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delegate_rewards",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rewards = table.Column<long>(type: "bigint", nullable: false),
                    accured = table.Column<long>(type: "bigint", nullable: false),
                    delivered = table.Column<long>(type: "bigint", nullable: false),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    cycle = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delegate_rewards", x => x.id);
                    table.ForeignKey(
                        name: "fk_delegate_rewards_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endorsing_rights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    slot_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_endorsing_rights", x => x.id);
                    table.ForeignKey(
                        name: "fk_endorsing_rights_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proposal",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hash = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    period = table.Column<int>(type: "integer", nullable: false),
                    delegate_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    callback_query_data = table.Column<string>(type: "text", nullable: true),
                    from_user = table.Column<bool>(type: "boolean", nullable: false),
                    telegram_message_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_address",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    balance = table.Column<decimal>(type: "numeric", nullable: false),
                    last_update = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    notify_baking_rewards = table.Column<bool>(type: "boolean", nullable: false),
                    amount_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    notify_transactions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_cycle_completion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_delegations = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    delegation_amount_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    notify_misses = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    chat_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_address", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_address_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proposal_vote",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delegate_id = table.Column<int>(type: "integer", nullable: false),
                    proposal_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    voting_period = table.Column<int>(type: "integer", nullable: false),
                    ballot = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_vote", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_vote_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_proposal_vote_proposal_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "proposal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_address_delegation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_address_id = table.Column<int>(type: "integer", nullable: false),
                    delegate_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_address_delegation", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_address_delegation_delegate_delegate_id",
                        column: x => x.delegate_id,
                        principalTable: "delegate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_address_delegation_user_address_user_address_id",
                        column: x => x.user_address_id,
                        principalTable: "user_address",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_baking_rights_delegate_id",
                table: "baking_rights",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_balance_update_delegate_id",
                table: "balance_update",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_delegate_rewards_delegate_id",
                table: "delegate_rewards",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_endorsing_rights_delegate_id",
                table: "endorsing_rights",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_user_id",
                table: "message",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_delegate_id",
                table: "proposal",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_vote_delegate_id",
                table: "proposal_vote",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_vote_proposal_id",
                table: "proposal_vote",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_address_user_id",
                table: "user_address",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_address_delegation_delegate_id",
                table: "user_address_delegation",
                column: "delegate_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_address_delegation_user_address_id",
                table: "user_address_delegation",
                column: "user_address_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "baking_rights");

            migrationBuilder.DropTable(
                name: "balance_update");

            migrationBuilder.DropTable(
                name: "delegate_rewards");

            migrationBuilder.DropTable(
                name: "endorsing_rights");

            migrationBuilder.DropTable(
                name: "known_address");

            migrationBuilder.DropTable(
                name: "last_block");

            migrationBuilder.DropTable(
                name: "message");

            migrationBuilder.DropTable(
                name: "proposal_vote");

            migrationBuilder.DropTable(
                name: "twitter_message");

            migrationBuilder.DropTable(
                name: "user_address_delegation");

            migrationBuilder.DropTable(
                name: "proposal");

            migrationBuilder.DropTable(
                name: "user_address");

            migrationBuilder.DropTable(
                name: "delegate");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
