using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot
{
    public static class ReplyKeyboards
    {
        static Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetMarkup(params string[] buttons)
        {
            return new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
            {
                Keyboard = new[] { buttons.Select(o => new Telegram.Bot.Types.ReplyMarkups.KeyboardButton(o)).ToArray() },
                ResizeKeyboard = true
            };
        }
        static Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup AddRow(this Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup m, params string[] buttons)
        {
            var l = m.Keyboard.ToList();
            l.Add(buttons.Select(o => new Telegram.Bot.Types.ReplyMarkups.KeyboardButton(o)).ToArray());
            m.Keyboard = l.ToArray();
            return m;
        }
        public static Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup MainMenu(ResourceManager resMgr, Model.User u)
        {
            return GetMarkup(CmdNewAddress(resMgr, u), CmdMyAddresses(resMgr, u)).AddRow(CmdContact(resMgr, u), CmdSettings(resMgr, u));
        }
        public static Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup BackMenu(ResourceManager resMgr, Model.User u)
        {
            return GetMarkup(CmdGoBack(resMgr, u));
        }
		
		//public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup Language()
  //      {
  //          return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
  //              new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[][] {
  //                  new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
  //                      new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
  //                      {
  //                          Text = "🇺🇸 English",
  //                          CallbackData = "set_en"
  //                      }
  //                  }/*,
  //                  new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
  //                      new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
  //                      {
  //                          Text = "🇨🇳 中国",
  //                          CallbackData = "set_cn"
  //                      }
  //                  }*/,
  //                  new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
  //                      new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
  //                      {
  //                          Text = "🇷🇺 Русский",
  //                          CallbackData = "set_ru"
  //                      }
  //                  }/*,
  //                  new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
  //                      new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
  //                      {
  //                          Text = "🇫🇷 France",
  //                          CallbackData = "set_ru"
  //                      }
  //                  }*/,
  //                  new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
  //                      new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
  //                      {
  //                          Text = "🇷🇺 Русский",
  //                          CallbackData = ""
  //                      }
  //                  }
  //              }
  //          );
  //      }
		
		public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup Search(ResourceManager resMgr, Model.User u)
		{
			var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
			buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							SwitchInlineQueryCurrentChat = "",
							Text = resMgr.Get(Res.Search, u)
						}
					});			

			return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons.ToArray());
		}
		public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup TweetSettings(int twitterMessageId)
		{
			var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
			Action<string, string> add = (text, data) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text,
							CallbackData = data
						}
					});
			add("Delete", "twdelete " + twitterMessageId.ToString());

			return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons.ToArray());
		}
		public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup Settings(ResourceManager resMgr, Model.User u)
        {
            var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
            Action<string, string> add = (text, data) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
                        new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
                        {
                            Text = text,
                            CallbackData = data
                        }
                    });
			Action<string, string, string, string> add2 = (text1, data1, text2, data2) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text1,
							CallbackData = data1
						},
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text2,
							CallbackData = data2
						}
					});

			add2("🇺🇸 English", "set_en", "🇷🇺 Русский", "set_ru");
			add(resMgr.Get(Res.Explorer, u), "set_explorer");
            if (u.HideHashTags)
                add(resMgr.Get(Res.HashTags, u), "showhashtags");
            else
                add(resMgr.Get(Res.HashTags, u), "hidehashtags");
            
			add(resMgr.Get(Res.WhaleAlerts, u), "set_whalealert");
			add(resMgr.Get(Res.NetworkIssueAlerts, u), "set_nialert");
			if (u.VotingNotify)
				add(resMgr.Get(Res.VotingNotify, u), "hidevotingnotify");
			else
				add(resMgr.Get(Res.VotingNotify, u), "showvotingnotify");

			if (u.IsAdmin())
            {
				add("🖋 Broadcast message", "broadcast");
                add("👫 Get user list", "getuserlist");
                //add("📪 Get user addresses", "getuseraddresses");
                //add("🗂 Get user messages", "getusermessages");
                add("📃 Get logs", "getlog");
                add("💽 Get database", "getdb");
				foreach (var cmd in TezosBot.Commands.Where(o => o.username == u.Username || o.username == u.UserId.ToString()))
                    add(cmd.commandname, "cmd" + TezosBot.Commands.IndexOf(cmd));
            }
			add(resMgr.Get(Res.Donate, u), "donate");
            return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons.ToArray());
        }
		public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup WhaleAlertSettings(ResourceManager resMgr, Model.User u)
		{
			var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
			Action<string, string> add = (text, data) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text,
							CallbackData = data
						}
					});
			Action<string, string, string, string> add2 = (text1, data1, text2, data2) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text1,
							CallbackData = data1
						},
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text2,
							CallbackData = data2
						}
					});
			add((u.WhaleAlertThreshold == 0 ? "☑️" : "" ) + " " + resMgr.Get(Res.Off, u), "set_wa_0");
			add2((u.WhaleAlertThreshold == 250000 ? "☑️" : "") + " 250 000 XTZ", "set_wa_250", (u.WhaleAlertThreshold == 500000 ? "☑️" : "") + " 500 000 XTZ", "set_wa_500");
			add2((u.WhaleAlertThreshold == 750000 ? "☑️" : "") + " 750 000 XTZ", "set_wa_750", (u.WhaleAlertThreshold == 1000000 ? "☑️" : "") + " 1 000 000 XTZ", "set_wa_1000");

			return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons.ToArray());
		}
		public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup ExplorerSettings(Model.User u)
		{
			var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
			Action<string, string> add = (text, data) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text,
							CallbackData = data
						}
					});
			add((u.Explorer == 4 ? "☑️" : "") + " mininax.io", "set_explorer_4");
			add((u.Explorer == 0 ? "☑️" : "") + " tezblock.io", "set_explorer_0");
			add((u.Explorer == 2 ? "☑️" : "") + " tezos.id", "set_explorer_2");
			add((u.Explorer == 3 ? "☑️" : "") + " tzkt.io", "set_explorer_3");
			add((u.Explorer == 1 ? "☑️" : "") + " tzstats.com", "set_explorer_1");

			return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons.ToArray());
		}
		public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup NetworkIssueAlertSettings(ResourceManager resMgr, Model.User u)
		{
			var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
			Action<string, string> add = (text, data) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text,
							CallbackData = data
						}
					});
			Action<string, string, string, string> add2 = (text1, data1, text2, data2) => buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text1,
							CallbackData = data1
						},
						new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
						{
							Text = text2,
							CallbackData = data2
						}
					});
			add((u.NetworkIssueNotify == 0 ? "☑️" : "") + " " + resMgr.Get(Res.Off, u), "set_ni_0");
			add2((u.NetworkIssueNotify == 5 ? "☑️" : "") + " 5", "set_ni_5", (u.NetworkIssueNotify == 10 ? "☑️" : "") + " 10", "set_ni_10");
			add2((u.NetworkIssueNotify == 15 ? "☑️" : "") + " 15", "set_ni_15", (u.NetworkIssueNotify == 20 ? "☑️" : "") + " 20", "set_ni_20");

			return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons.ToArray());
		}
		public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup AddressMenu(ResourceManager resMgr, Model.User u, string id, Model.UserAddress ua, Tuple<string,string> addDelegate)
        {
            var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
			if (ua == null)
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.Delete, u),
						CallbackData = "deleteaddress " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.ManageAddress, u),
						CallbackData = "manageaddress " + id
					}
				});
			else
			{
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.TransactionNotify, ua),
						CallbackData = (ua.NotifyTransactions ? "tranoff" : "tranon") + " " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.SetThreshold, u),
						CallbackData = "setthreshold " + id
					}
				});
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.RenameAddress, u),
						CallbackData = "setname " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.Delete, u),
						CallbackData = "deleteaddress " + id
					}
				});
			}
			if (addDelegate.Item1 != "")
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.AddAddress, u) + " " + addDelegate.Item1,
						CallbackData = "addaddress " + addDelegate.Item2
					}
				});
            return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
        }

        public static Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup AddressMenu(ResourceManager resMgr, Model.User u, string id, Model.UserAddress ua)
        {
			var buttons = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();
			if (ua == null)
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.Delete, u),
						CallbackData = "deleteaddress " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.ManageAddress, u),
						CallbackData = "manageaddress " + id
					}
				});
			else
			{
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.TransactionNotify, ua),
							CallbackData = (ua.NotifyTransactions ? "tranoff" : "tranon") + " " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.SetThreshold, u),
						CallbackData = "setthreshold " + id
					}
				});
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.DelegationNotify, ua),
						CallbackData = (ua.NotifyDelegations ?  "dlgoff" : "dlgon") + " " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.SetDlgThreshold, u),
						CallbackData = "setdlgthreshold " + id
					}
				});
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.RewardNotify, ua),
						CallbackData = (ua.NotifyBakingRewards ? "bakingoff" : "bakingon") + " " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.CycleNotify, ua),
						CallbackData = (ua.NotifyCycleCompletion ? "cycleoff" : "cycleon") + " " + id
					}
				});
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.RenameAddress, u),
						CallbackData = "setname " + id
					},
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.MissesNotify, ua),
						CallbackData = (ua.NotifyMisses ? "missesoff" : "misseson") + " " + id
					}
				});
				buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.Delete, u),
						CallbackData = "deleteaddress " + id
					}
				});
				if (u.IsAdmin())
					buttons.Add(new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[] {
					new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton
					{
						Text = resMgr.Get(Res.NotifyFollowers, u),
						CallbackData = "notifyfollowers " + id
					}
				});
			}
			return new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
        }

        public static string CmdNewAddress(ResourceManager resMgr, Model.User u) => resMgr.Get(Res.NewAddress, u);
        public static string CmdMyAddresses(ResourceManager resMgr, Model.User u) => resMgr.Get(Res.MyAddresses, u);
		public static string CmdContact(ResourceManager resMgr, Model.User u) => resMgr.Get(Res.Contact, u);
		public static string CmdSettings(ResourceManager resMgr, Model.User u) => resMgr.Get(Res.Settings, u);
		public static string CmdGoBack(ResourceManager resMgr, Model.User u) => resMgr.Get(Res.GoBack, u);
	}
}
