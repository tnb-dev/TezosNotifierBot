using System;
using System.Collections.Generic;
using System.Linq;
using TezosNotifyBot.Model;
using TezosNotifyBot.Domain;
using Google.Api;

namespace TezosNotifyBot
{
    public static class ReplyKeyboards
    {
        public static KeyboardMarkup MainMenu { get; private set; } = KeyboardMarkup.ReplyKeyboard([[CmdNewAddress, CmdMyAddresses], [CmdContacts, CmdSettings]]);
        public static KeyboardMarkup BackMenu { get; private set; } = KeyboardMarkup.ReplyKeyboard([[CmdGoBack]]);

        public static KeyboardMarkup Search { get; private set; } = KeyboardMarkup.SearchInlineButton("🔎 Search");
		
        public static KeyboardMarkup Settings(User u, TelegramOptions options)
        {
			var buttons = new List<List<(string Text, string Callback)>>();
            buttons.Add(u, 0, $"Rate: {(u.CurrencyCode == "USD" ? "💵" : "💶")} {u.CurrencyCode}", "change_currency");
			buttons.Add(u, 0, $"#️⃣ Hashtags: {(u.HideHashTags ? "Off" : "On")}", u.HideHashTags ? "showhashtags" : "hidehashtags");
			buttons.Add(u, 0, $"🐋 Whale alerts", "set_whalealert");
			buttons.Add(u, 0, $"🔈 Voting: {(u.VotingNotify ? "On" : "Off")}", u.VotingNotify ? "hidevotingnotify" : "showvotingnotify");
			buttons.Add(u, 0, $"🦊 Software releases: {(u.ReleaseNotify ? "On" : "Off")}", u.ReleaseNotify ? "tezos_release_off" : "tezos_release_on");

            if (u.IsAdmin(options))
            	buttons.Add(u, 0, $"🖋 Broadcast message", "broadcast");
			buttons.Add(u, 0, $"🎁 Donate", "donate");

			return KeyboardMarkup.InlineKeyboard(buttons);
		}

        public static KeyboardMarkup WhaleAlertSettings(User u)
        {
			var buttons = new List<List<(string Text, string Callback)>>();
			buttons.Add2(u, 0,
                (u.WhaleAlertThreshold == 0 ? "☑️" : "") + " Off", "set_wa_0",
				(u.SmartWhaleAlerts ? "☑️" : "🔲") + " Outflow", "set_swa_" + (u.SmartWhaleAlerts ? "off" : "on"));
			buttons.Add2(u, 0,
				(u.WhaleAlertThreshold == 250000 ? "☑️" : "") + " 250 000 XTZ", "set_wa_250",
				(u.WhaleAlertThreshold == 500000 ? "☑️" : "") + " 500 000 XTZ", "set_wa_500");
			buttons.Add2(u, 0,
				(u.WhaleAlertThreshold == 750000 ? "☑️" : "") + " 750 000 XTZ", "set_wa_750",
				(u.WhaleAlertThreshold == 1000000 ? "☑️" : "") + " 1 000 000 XTZ", "set_wa_1000");
			
			return KeyboardMarkup.InlineKeyboard(buttons);
		}

        public static KeyboardMarkup ExplorerSettings(User u)
        {
			var buttons = new List<List<(string Text, string Callback)>>();
            buttons.Add(u, 3, (u.Explorer == 3 ? "☑️" : "") + " tzkt.io", "set_explorer");
			buttons.Add(u, 1, (u.Explorer == 1 ? "☑️" : "") + " tzstats.io", "set_explorer");

			return KeyboardMarkup.InlineKeyboard(buttons);
		}
        
        public static KeyboardMarkup AddressMenu(User u, int id, UserAddress ua,
            Tuple<string, string> addDelegate)
        {
			var buttons = new List<List<(string Text, string Callback)>>();
			if (ua == null)
				buttons.Add2(u, id,
					"🗑 Delete", "deleteaddress",
					"🛠 Tune", "manageaddress");
			else
            {
                if (u.Type == 0)
                {
					buttons.Add2(u, id,
							$"{(ua.NotifyTransactions ? "☑️" : "🔲")} Transactions", (ua.NotifyTransactions ? "tranoff" : "tranon"),
							"✂️ Transaction Threshold", "setthreshold");
					buttons.Add2(u, id,
						$"{(ua.NotifyPayout ? "☑️" : "🔲")} Payouts", $"toggle_payout_notify",
                        $"{(ua.NotifyDelegateStatus ? "☑️" : "🔲")} Delegate status", $"toggle-delegate-status"
                        );
					buttons.Add2(u, id,
							 "📝 Rename", "setname",
							 "🗑 Delete", "deleteaddress");
				}
                else
				{
                    buttons.Add2(u, id,
						$"{(ua.NotifyTransactions ? "☑️" : "🔲")} Transactions", (ua.NotifyTransactions ? "tranoff" : "tranon"),
						$"{(ua.NotifyPayout ? "☑️" : "🔲")} Payouts", $"toggle_payout_notify");
                    buttons.Add2(u, id,
						$"{(ua.NotifyDelegateStatus ? "☑️" : "🔲")} Delegate status", $"toggle-delegate-status",
						"🗑 Delete", "deleteaddress");
                }
            }

            if (addDelegate.Item1 != "")
                buttons.Add(u, id, "👀 Monitor delegate " + addDelegate.Item1, "addaddress " + addDelegate.Item2);
			return KeyboardMarkup.InlineKeyboard(buttons);
		}

        public static KeyboardMarkup AddressMenu(User u, int id, UserAddress ua, TelegramOptions options)
        {
			var buttons = new List<List<(string Text, string Callback)>>();
            if (ua == null)
                buttons.Add2(u, id,
					"🗑 Delete", "deleteaddress",
					"🛠 Tune", "manageaddress");
            else
            {
                if (u.Type == 0)
                {
                    buttons.Add2(u, id,
                            $"{(ua.NotifyTransactions ? "☑️" : "🔲")} Transactions", (ua.NotifyTransactions ? "tranoff" : "tranon"),
							"✂️ Transaction Threshold", "setthreshold");
                    buttons.Add2(u, id,
                            $"{(ua.NotifyDelegations ? "☑️" : "🔲")} Delegations", (ua.NotifyDelegations ? "dlgoff" : "dlgon"),
							"✂️ Delegation Threshold", "setdlgthreshold");
                    buttons.Add2(u, id,
							$"{(ua.NotifyDelegatorsBalance ? "☑️" : "🔲")} Delegators balance", $"toggle_delegators_balance",
							"✂️ Balance Threshold", "change_delegators_balance_threshold");
                    buttons.Add2(u, id,
							$"{(ua.NotifyBakingRewards ? "☑️" : "🔲")} Rewards", (ua.NotifyBakingRewards ? "bakingoff" : "bakingon"),
                            $"{(ua.NotifyCycleCompletion ? "☑️" : "🔲")} Cycle completion", (ua.NotifyCycleCompletion ? "cycleoff" : "cycleon"));
                    buttons.Add2(u, id,
							$"{(ua.NotifyOutOfFreeSpace ? "☑️" : "🔲")} Out of free space", (ua.NotifyOutOfFreeSpace ? "outoffreespaceoff" : "outoffreespaceon"),
                            $"{(ua.NotifyMisses ? "☑️" : "🔲")} Misses", "tunemisses");
                    buttons.Add2(u, id,
							 "📝 Rename", "setname",
							 "🗑 Delete", "deleteaddress");
                    if (u.IsAdmin(options) || ua.IsOwner)
                        buttons.Add(u, id, "📣 Notify followers/delegators", "notifyfollowers");
                }
                else
				{
                    buttons.Add2(u, id,
							$"{(ua.NotifyTransactions ? "☑️" : "🔲")} Transactions", (ua.NotifyTransactions ? "tranoff" : "tranon"),
							$"{(ua.NotifyDelegations ? "☑️" : "🔲")} Delegations", (ua.NotifyDelegations ? "dlgoff" : "dlgon"));
                    buttons.Add2(u, id,
							$"{(ua.NotifyBakingRewards ? "☑️" : "🔲")} Rewards", (ua.NotifyBakingRewards ? "bakingoff" : "bakingon"),
							$"{(ua.NotifyCycleCompletion ? "☑️" : "🔲")} Cycle completion", (ua.NotifyCycleCompletion ? "cycleoff" : "cycleon"));
                    buttons.Add2(u, id,
							$"{(ua.NotifyOutOfFreeSpace ? "☑️" : "🔲")} Out of free space", (ua.NotifyOutOfFreeSpace ? "outoffreespaceoff" : "outoffreespaceon"),
							$"{(ua.NotifyMisses ? "☑️" : "🔲")} Misses", (ua.NotifyMisses ? "missesoff" : "misseson"));
                    buttons.Add(u, id, "🗑 Delete", "deleteaddress");
                }
            }

            return KeyboardMarkup.InlineKeyboard(buttons);
        }

        public static KeyboardMarkup MissesMenu(User u, int id, UserAddress ua, TelegramOptions options)
		{
			var buttons = new List<List<(string Text, string Callback)>>();
			buttons.Add(u, id, "🔙 Back", "manageaddress");
			buttons.Add(u, id, $"{(ua.NotifyMisses ? "☑️" : "🔲")} Misses "+(ua.NotifyMisses ? "On" : "Off"), (ua.NotifyMisses ? "missesoff" : "misseson"));
			buttons.Add(u, id, (ua.MissesThreshold == 0 ? "☑️" : "") + " No threshold", "set_misses_0");
			buttons.Add(u, id, (ua.MissesThreshold == 30 ? "☑️" : "") + " Threshold 30 min", "set_misses_30");
			buttons.Add(u, id, (ua.MissesThreshold == 60 ? "☑️" : "") + " Threshold 1 hour", "set_misses_60");
			buttons.Add(u, id, (ua.MissesThreshold == 120 ? "☑️" : "") + " Threshold 2 hours", "set_misses_120");
			buttons.Add(u, id, (ua.MissesThreshold == 240 ? "☑️" : "") + " Threshold 4 hours", "set_misses_240");

			return KeyboardMarkup.InlineKeyboard(buttons);
		}


		public static KeyboardMarkup AdminAddressMenu(UserAddress ua)
        {
			var buttons = new List<List<(string Text, string Callback)>>();
            buttons.Add(ua.User, ua.Id, $"{(ua.IsOwner ? "☑️" : "🔲")} Address owner", (ua.IsOwner ? "owneroff" : "owneron"));

			return KeyboardMarkup.InlineKeyboard(buttons);
		}

        public const string CmdNewAddress = "✳️ New Address";
        public const string CmdMyAddresses = "👛 My Addresses";
        public const string CmdContacts = "✉️ Contact us";
        public const string CmdSettings = "⚙️ Settings";
        public const string CmdGoBack = "⬅️ Go back";
    }

    public static class ReplyKeyboardsExtensions
    {
        public static void Add(this List<List<(string Text, string Callback)>> buttons, User u, int id, string text1, string data1)
		{
			buttons.Add(new List<(string Text, string Callback)>
			{
				   (text1, u.CallbackData(data1) + (id > 0 ? " " + id : ""))
			});
		}
        public static void Add2(this List<List<(string Text, string Callback)>> buttons, User u, int id, string text1, string data1, string text2, string data2)
		{
			buttons.Add(new List<(string Text, string Callback)>
			{
				   (text1, u.CallbackData(data1) + (id > 0 ? " " + id : "")),
				   (text2, u.CallbackData(data2) + (id > 0 ? " " + id : ""))
			});
		}
        public static void Add3(this List<List<(string Text, string Callback)>> buttons, User u, int id, string text1, string data1, string text2, string data2, string text3, string data3)
        {
            buttons.Add(new List<(string Text, string Callback)>
            {
                   (text1, u.CallbackData(data1) + (id > 0 ? " " + id : "")),
                   (text2, u.CallbackData(data2) + (id > 0 ? " " + id : "")),
                   (text3, u.CallbackData(data3) + (id > 0 ? " " + id : ""))
            });
        }
        public static string CallbackData(this User u, string data) => (u.Type != 0 ? $"_{u.Id}_" : "") + data;

	}
}