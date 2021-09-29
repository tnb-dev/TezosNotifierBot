using System;
using System.Collections.Generic;
using System.Linq;
using NornPool.Model;
using Telegram.Bot.Types.ReplyMarkups;
using TezosNotifyBot.Domain;

namespace TezosNotifyBot
{
    public static class ReplyKeyboards
    {
        static ReplyKeyboardMarkup GetMarkup(params string[] buttons)
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new[] { buttons.Select(o => new KeyboardButton(o)).ToArray() },
                ResizeKeyboard = true
            };
        }

        static ReplyKeyboardMarkup AddRow(this ReplyKeyboardMarkup m, params string[] buttons)
        {
            var l = m.Keyboard.ToList();
            l.Add(buttons.Select(o => new KeyboardButton(o)).ToArray());
            m.Keyboard = l.ToArray();
            return m;
        }

        public static ReplyKeyboardMarkup MainMenu(ResourceManager resMgr, User u)
        {
            return u.Type == 0 ? GetMarkup(CmdNewAddress(resMgr, u), CmdMyAddresses(resMgr, u))
                .AddRow(CmdContact(resMgr, u), CmdSettings(resMgr, u)) : null;
        }

        public static ReplyKeyboardMarkup BackMenu(ResourceManager resMgr, User u)
        {
            return GetMarkup(CmdGoBack(resMgr, u));
        }

        public static InlineKeyboardMarkup Search(ResourceManager resMgr, User u)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    SwitchInlineQueryCurrentChat = "",
                    Text = resMgr.Get(Res.Search, u)
                }
            });

            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        public static InlineKeyboardMarkup TweetSettings(int twitterMessageId)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            Action<string, string> add = (text, data) => buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = text,
                    CallbackData = data
                }
            });
            add("Delete", "twdelete " + twitterMessageId);

            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        public static InlineKeyboardMarkup Settings(ResourceManager resMgr, User user, TelegramOptions options)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            Action<string, string> add = (text, data) => buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = text,
                    CallbackData = (user.Type != 0 ? $"_{user.Id}_" : "") + data
                }
            });
            Action<string, string, string, string> add2 = (text1, data1, text2, data2) => buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = text1,
                    CallbackData = (user.Type != 0 ? $"_{user.Id}_" : "") + data1
                },
                new InlineKeyboardButton
                {
                    Text = text2,
                    CallbackData = (user.Type != 0 ? $"_{user.Id}_" : "") + data2
                }
            });

            add2("🇺🇸 English", "set_en", "🇷🇺 Русский", "set_ru");
            add(resMgr.Get(Res.UserCurrency, user), "change_currency");
            add(resMgr.Get(Res.Explorer, user), "set_explorer");
            if (user.HideHashTags)
                add(resMgr.Get(Res.HashTags, user), "showhashtags");
            else
                add(resMgr.Get(Res.HashTags, user), "hidehashtags");

            add(resMgr.Get(Res.WhaleAlerts, user), "set_whalealert");
            //add(resMgr.Get(Res.NetworkIssueAlerts, user), "set_nialert");
            if (user.VotingNotify)
                add(resMgr.Get(Res.VotingNotify, user), "hidevotingnotify");
            else
                add(resMgr.Get(Res.VotingNotify, user), "showvotingnotify");

            if (user.ReleaseNotify)
                add(resMgr.Get(Res.ReleaseNotify, user), "tezos_release_off");
            else
                add(resMgr.Get(Res.ReleaseNotify, user), "tezos_release_on");

            if (user.IsAdmin(options))
            {
                add("🖋 Broadcast message", "broadcast");
                add("👫 Get user list", "getuserlist");
                foreach (var cmd in TezosBot.Commands.Where(o =>
                    o.username == user.Username || o.username == user.Id.ToString()))
                    add(cmd.commandname, "cmd" + TezosBot.Commands.IndexOf(cmd));
            }

            add(resMgr.Get(Res.Donate, user), "donate");
            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        public static InlineKeyboardMarkup WhaleAlertSettings(ResourceManager resMgr, User u)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            Action<string, string, string, string> add2 = (text1, data1, text2, data2) => buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = text1,
                    CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data1
                },
                new InlineKeyboardButton
                {
                    Text = text2,
                    CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data2
                }
            });
            add2((u.WhaleAlertThreshold == 0 ? "☑️" : "") + " " + resMgr.Get(Res.Off, u), "set_wa_0",
                (u.SmartWhaleAlerts ? "☑️" : "🔲") + " " + resMgr.Get(Res.SmartWhale, u), "set_swa_" + (u.SmartWhaleAlerts ? "off" : "on"));
            add2((u.WhaleAlertThreshold == 250000 ? "☑️" : "") + " 250 000 XTZ", "set_wa_250",
                (u.WhaleAlertThreshold == 500000 ? "☑️" : "") + " 500 000 XTZ", "set_wa_500");
            add2((u.WhaleAlertThreshold == 750000 ? "☑️" : "") + " 750 000 XTZ", "set_wa_750",
                (u.WhaleAlertThreshold == 1000000 ? "☑️" : "") + " 1 000 000 XTZ", "set_wa_1000");

            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        public static InlineKeyboardMarkup ExplorerSettings(User u)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            Action<string, string> add = (text, data) => buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = text,
                    CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data
                }
            });
            add((u.Explorer == 4 ? "☑️" : "") + " mininax.io", "set_explorer_4");
            add((u.Explorer == 0 ? "☑️" : "") + " tezblock.io", "set_explorer_0");
            add((u.Explorer == 5 ? "☑️" : "") + " teztracker.com", "set_explorer_5");
            add((u.Explorer == 3 ? "☑️" : "") + " tzkt.io", "set_explorer_3");
            add((u.Explorer == 1 ? "☑️" : "") + " tzstats.com", "set_explorer_1");

            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        public static InlineKeyboardMarkup NetworkIssueAlertSettings(ResourceManager resMgr, User u)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            Action<string, string> add = (text, data) => buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = text,
                    CallbackData = data
                }
            });
            Action<string, string, string, string> add2 = (text1, data1, text2, data2) => buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = text1,
                    CallbackData = data1
                },
                new InlineKeyboardButton
                {
                    Text = text2,
                    CallbackData = data2
                }
            });
            add((u.NetworkIssueNotify == 0 ? "☑️" : "") + " " + resMgr.Get(Res.Off, u), "set_ni_0");
            add2((u.NetworkIssueNotify == 5 ? "☑️" : "") + " 5", "set_ni_5",
                (u.NetworkIssueNotify == 10 ? "☑️" : "") + " 10", "set_ni_10");
            add2((u.NetworkIssueNotify == 15 ? "☑️" : "") + " 15", "set_ni_15",
                (u.NetworkIssueNotify == 20 ? "☑️" : "") + " 20", "set_ni_20");

            return new InlineKeyboardMarkup(buttons.ToArray());
        }

        public static InlineKeyboardMarkup AddressMenu(ResourceManager resMgr, User u, string id, UserAddress ua,
            Tuple<string, string> addDelegate)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            if (ua == null)
                buttons.Add3(u,
                    resMgr.Get(Res.AddressInfoButton, u), "address-links " + id,
                    resMgr.Get(Res.Delete, u), "deleteaddress " + id,
                    resMgr.Get(Res.ManageAddress, u), "manageaddress " + id);
            else
            {
                if (u.Type == 0)
                {
                    buttons.Add2(u,
                        resMgr.Get(Res.TransactionNotify, ua), (ua.NotifyTransactions ? "tranoff" : "tranon") + " " + id,
                        resMgr.Get(Res.SetThreshold, u), "setthreshold " + id);
                    buttons.Add2(u,
                        resMgr.Get(Res.PayoutNotifyToggle, ua), $"toggle_payout_notify {id}",
                        resMgr.Get(Res.AwardNotify, u), (ua.NotifyAwardAvailable ? "awardoff" : "awardon") + " " + id);
                    buttons.Add2(u,
                        resMgr.Get(Res.RenameAddress, u), "setname " + id,
                        resMgr.Get(Res.DelegateStatusToggle, ua), $"toggle-delegate-status {id}");
                    buttons.Add(u, resMgr.Get(Res.Delete, u), $"deleteaddress {id}");
                }
                else
				{
                    buttons.Add2(u,
                        resMgr.Get(Res.TransactionNotify, ua), (ua.NotifyTransactions ? "tranoff" : "tranon") + " " + id,
                        resMgr.Get(Res.PayoutNotifyToggle, ua), $"toggle_payout_notify {id}");
                    buttons.Add2(u,
                        resMgr.Get(Res.DelegateStatusToggle, ua), $"toggle-delegate-status {id}",
                        resMgr.Get(Res.Delete, u), $"deleteaddress {id}");
                }
            }

            if (addDelegate.Item1 != "")
                buttons.Add(u, resMgr.Get(Res.AddAddress, u) + " " + addDelegate.Item1, "addaddress " + addDelegate.Item2);
            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup AddressMenu(ResourceManager resMgr, User u, string id, UserAddress ua,
            TelegramOptions options)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            if (ua == null)
                buttons.Add3(u,
                    resMgr.Get(Res.AddressInfoButton, u), "address-links " + id,
                    resMgr.Get(Res.Delete, u), "deleteaddress " + id,
                    resMgr.Get(Res.ManageAddress, u), "manageaddress " + id);
            else
            {
                if (u.Type == 0)
                {
                    buttons.Add2(u,
                            resMgr.Get(Res.TransactionNotify, ua), (ua.NotifyTransactions ? "tranoff" : "tranon") + " " + id,
                            resMgr.Get(Res.SetThreshold, u), "setthreshold " + id);
                    buttons.Add2(u,
                            resMgr.Get(Res.DelegationNotify, ua), (ua.NotifyDelegations ? "dlgoff" : "dlgon") + " " + id,
                            resMgr.Get(Res.SetDlgThreshold, u), "setdlgthreshold " + id);
                    buttons.Add2(u,
                            resMgr.Get(Res.DelegatorsBalanceNotifyToggle, ua), $"toggle_delegators_balance {id}",
                            resMgr.Get(Res.DelegatorsBalanceThresholdButton, u), "change_delegators_balance_threshold " + id);
                    buttons.Add2(u,
                            resMgr.Get(Res.RewardNotify, ua), (ua.NotifyBakingRewards ? "bakingoff" : "bakingon") + " " + id,
                            resMgr.Get(Res.CycleNotify, u), (ua.NotifyCycleCompletion ? "cycleoff" : "cycleon") + " " + id);
                    buttons.Add2(u,
                            resMgr.Get(Res.NotifyRightsAssigned, ua), (ua.NotifyRightsAssigned ? "rightsoff" : "rightson") + " " + id,
                            resMgr.Get(Res.MissesNotify, u), (ua.NotifyMisses ? "missesoff" : "misseson") + " " + id);
                    buttons.Add(u, resMgr.Get(Res.NotifyOutOfFreeSpace, ua), (ua.NotifyOutOfFreeSpace ? "outoffreespaceoff" : "outoffreespaceon") + " " + id);
                    buttons.Add2(u,
                             resMgr.Get(Res.RenameAddress, u), "setname " + id,
                             resMgr.Get(Res.Delete, ua), "deleteaddress " + id);
                    if (u.IsAdmin(options) || ua.IsOwner)
                        buttons.Add(u, resMgr.Get(Res.NotifyFollowers, u), "notifyfollowers " + id);
                }
                else
				{
                    buttons.Add2(u,
                            resMgr.Get(Res.TransactionNotify, ua), (ua.NotifyTransactions ? "tranoff" : "tranon") + " " + id,
                            resMgr.Get(Res.DelegationNotify, ua), (ua.NotifyDelegations ? "dlgoff" : "dlgon") + " " + id);
                    buttons.Add2(u,
                            resMgr.Get(Res.RewardNotify, ua), (ua.NotifyBakingRewards ? "bakingoff" : "bakingon") + " " + id,
                            resMgr.Get(Res.CycleNotify, u), (ua.NotifyCycleCompletion ? "cycleoff" : "cycleon") + " " + id);
                    buttons.Add2(u,
                            resMgr.Get(Res.NotifyRightsAssigned, ua), (ua.NotifyRightsAssigned ? "rightsoff" : "rightson") + " " + id,
                            resMgr.Get(Res.MissesNotify, u), (ua.NotifyMisses ? "missesoff" : "misseson") + " " + id);
                    buttons.Add2(u,
                             resMgr.Get(Res.NotifyOutOfFreeSpace, ua), (ua.NotifyOutOfFreeSpace ? "outoffreespaceoff" : "outoffreespaceon") + " " + id,
                             resMgr.Get(Res.Delete, ua), "deleteaddress " + id);
                }
            }

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup AdminAddressMenu(ResourceManager resMgr, UserAddress ua)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            buttons.Add(new[]
            {
                new InlineKeyboardButton
                {
                    Text = resMgr.Get(Res.IsAddressOwner, ua),
                    CallbackData = (ua.IsOwner ? "owneroff" : "owneron") + " " + ua.Id.ToString()
                }
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static string CmdNewAddress(ResourceManager resMgr, User u) => resMgr.Get(Res.NewAddress, u);
        public static string CmdMyAddresses(ResourceManager resMgr, User u) => resMgr.Get(Res.MyAddresses, u);
        public static string CmdContact(ResourceManager resMgr, User u) => resMgr.Get(Res.Contact, u);
        public static string CmdSettings(ResourceManager resMgr, User u) => resMgr.Get(Res.Settings, u);
        public static string CmdGoBack(ResourceManager resMgr, User u) => resMgr.Get(Res.GoBack, u);
    }

    public static class ReplyKeyboardsExtensions
    {
        public static void Add(this List<InlineKeyboardButton[]> buttons, User u, string text, string data)
        {
            buttons.Add(new[]
                {
                    new InlineKeyboardButton
                    {
                        Text = text,
                        CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data
                    }
                });
        }
        public static void Add2(this List<InlineKeyboardButton[]> buttons, User u, string text1, string data1, string text2, string data2)
		{
            buttons.Add(new[]
                {
                    new InlineKeyboardButton
                    {
                        Text = text1,
                        CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data1
                    },
                    new InlineKeyboardButton
                    {
                        Text = text2,
                        CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data2
                    }
                });
        }
        public static void Add3(this List<InlineKeyboardButton[]> buttons, User u, string text1, string data1, string text2, string data2, string text3, string data3)
        {
            buttons.Add(new[]
                {
                    new InlineKeyboardButton
                    {
                        Text = text1,
                        CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data1
                    },
                    new InlineKeyboardButton
                    {
                        Text = text2,
                        CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data2
                    },
                    new InlineKeyboardButton
                    {
                        Text = text3,
                        CallbackData = (u.Type != 0 ? $"_{u.Id}_" : "") + data3
                    }
                });
        }
    }
}