using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TezosNotifyBot
{
    public class TelegramBotHandler
    {
		TelegramBotClient client;
		public TelegramBotHandler(ITelegramBotClient client)
		{
			this.client = client as TelegramBotClient;
			this.client.OnUpdate += Client_OnUpdate;
			this.client.OnError += Client_OnError;
		}

		async Task Client_OnUpdate(Telegram.Bot.Types.Update update)
		{
			if (update.Type == Telegram.Bot.Types.Enums.UpdateType.ChosenInlineResult)
			{
				if (OnChosenInlineResult != null)
					await OnChosenInlineResult(update.ChosenInlineResult.From.Id, update.ChosenInlineResult.ResultId);
			}
			else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
			{
				if (OnCallbackQuery != null)
				{
					var cq = update.CallbackQuery;
					await OnCallbackQuery(cq.Id, cq.From.Id, cq.Message.Id, cq.Data);
				}
			}
			else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.InlineQuery)
			{
				if (OnInlineQuery != null)
					await OnInlineQuery(update.InlineQuery.Id, update.InlineQuery.Query);
			}
			else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.ChannelPost)
			{
				if (OnChannelPost != null && update.ChannelPost.From != null)
					await OnChannelPost(new Chat(update.ChannelPost.Chat), update.ChannelPost.Id, new User(update.ChannelPost.From), update.ChannelPost.Text);
			}
			else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
			{
				if (!update.Message.From.IsBot && OnMessage != null)
				{
					if (update.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
						await OnMessage(new Chat(update.Message.Chat), update.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private, update.Message.Id, new User(update.Message.From), update.Message.Text);
				}
			}
		}

		async Task Client_OnError(Exception exception, Telegram.Bot.Polling.HandleErrorSource source)
		{
			//throw new NotImplementedException();
		}

		public ChosenInlineResultDelegate OnChosenInlineResult { get; set; }
		public CallbackQueryDelegate OnCallbackQuery { get; set; }
		public InlineQueryDelegate OnInlineQuery { get; set; }
		public MessageDelegate OnMessage { get; set; }
		public ChannelPostDelegate OnChannelPost { get; set; }

		public delegate Task ChosenInlineResultDelegate(long chatId, string resultId);
		public delegate Task CallbackQueryDelegate(string id, long chatId, int messageId, string data);
		public delegate Task InlineQueryDelegate(string id, string query);
		public delegate Task MessageDelegate(Chat chat, bool isPrivate, int id, User from, string text);
		public delegate Task ChannelPostDelegate(Chat chat, int id, User from, string text);

		public class User
		{
			public long Id { get; }
			public string FirstName { get; }
			public string LastName { get; }
			public string Username { get; }
			public User(Telegram.Bot.Types.User u)
			{
				Id = u.Id;
				FirstName = u.FirstName;
				LastName = u.LastName;
				Username = u.Username;
			}
		}

		public class Chat
		{
			public long Id { get; }
			public string Title { get; }
			public string Username { get; }
			public int Type { get; } // Channel - 3			
			public Chat(Telegram.Bot.Types.Chat c)
			{
				Id = c.Id;
				Title = c.Title;
				Username = c.Username;
				Type = (int)c.Type;
			}
		}

		//public class Message
		//{
		//	public int Id { get; }
		//	public string Text { get; }
		//	public long 
		//}
	}
}
