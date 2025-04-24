﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Model;
using Microsoft.Extensions.Options;

namespace TezosNotifyBot.Commands.Admin
{
	public class ExcludeWhaleCommand : BaseHandler, IUpdateHandler
	{
        BotConfig Config;
        public ExcludeWhaleCommand(IOptions<BotConfig> config, TezosDataContext db, TezosBotFacade botClient)
          : base(db, botClient)
        {
            Config = config.Value;
        }

        public async Task HandleUpdate(TelegramBotHandler.Chat chat, int messageId, string text)
		{
            if (!Config.Telegram.DevUsers.Contains(chat.Username))
				return;

            var addrs = Regex.Matches(text, "(tz|KT)[a-zA-Z0-9]{34}", RegexOptions.Singleline);
            var result = "";
            foreach (Match m in addrs)
			{
                var addr = Db.KnownAddresses.FirstOrDefault(x => x.Address == m.Value);
                if (addr == null)
				{
                    addr = new KnownAddress(m.Value, m.Value.ShortAddr());
                    Db.KnownAddresses.Add(addr);
                }
                addr.ExcludeWhaleAlert = true;
                result += $"<a href='{t.account(addr.Address)}'>{addr.Name}</a>\n";
            }
            await Db.SaveChangesAsync();
            await Bot.Reply(chat.Id, messageId, $"Excluded from whale alerts:\n{result}");
        }
	}
}
