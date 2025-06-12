using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace TezosNotifyBot
{
    public class TelegramBotInvoker
    {
		ITelegramBotClient bot;
		ILogger<TelegramBotInvoker> logger;
		public TelegramBotInvoker(ITelegramBotClient client, ILogger<TelegramBotInvoker> logger)
		{
			bot = client;
			this.logger = logger;
		}

		public async Task DeleteMessage(long chatId, int messageId) => await bot.DeleteMessage(chatId, messageId);

		public async Task AnswerCallbackQuery(string callbackQueryId, string text) => await bot.AnswerCallbackQuery(callbackQueryId, text);

		public async Task<int> SendMessage(long chatId, string text, KeyboardMarkup keyboardMarkup = null)
		{
			logger.LogInformation($"->{chatId}: {text}");
			var msg = await bot.SendMessage(chatId, text, ParseMode.Html, replyParameters: null, replyMarkup: keyboardMarkup, new LinkPreviewOptions { IsDisabled = true });
			Thread.Sleep(50);
			return msg.Id;
		}
		
		public async Task<int> EditMessage(long chatId, int messageId, string text, KeyboardMarkup keyboardMarkup = null)
		{
			if ((InlineKeyboardMarkup)keyboardMarkup == null)
				keyboardMarkup = null;
			var msg = await bot.EditMessageText(chatId, messageId, text, ParseMode.Html, linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true }, replyMarkup: keyboardMarkup);
			Thread.Sleep(50);
			return msg.Id;
		}

		public async Task<IEnumerable<long>> GetChatAdministrators(long chatId)
		{
			var cm = await bot.GetChatAdministrators(chatId);
			return cm.Select(o => o.User.Id);
		}

		public async Task SendChatActionTyping(long chatId) => await bot.SendChatAction(chatId, ChatAction.Typing);

		public async Task<string> GetChatLink(long chatId)
		{
			var chat = await bot.GetChat(chatId);
			return $"<a href=\"https://t.me/{chat.Username}\">{chat.Title}</a>";
		}
                              
		public async Task AnswerInlineQuery(string inlineQueryId, List<(string id, string title, string content, string description)> results)
		{
			var articles = results.Select(o => new InlineQueryResultArticle(o.id,
				o.title,
				new InputTextMessageContent(o.content) {
					ParseMode = ParseMode.Html,
					LinkPreviewOptions = new LinkPreviewOptions { IsDisabled = true }
				}) {
				Description = o.description
			});
			await bot.AnswerInlineQuery(inlineQueryId, articles, cacheTime: 20);
		}

		public async Task<int> SendPhoto(long chatId, string caption, Stream data, KeyboardMarkup keyboardMarkup = null)
		{
			var file = InputFile.FromStream(data);
			var msg = await bot.SendPhoto(chatId, file, caption, replyMarkup: keyboardMarkup);
			return msg.Id;
		}

		public async Task<int> SendFile(long chatId, string caption, Stream data)
		{
			var file = InputFile.FromStream(data);
			var msg = await bot.SendDocument(chatId, file, caption);
			return msg.Id;
		}

		public async Task<int> CopyMessage(long targetChatId, long sourceChatId, int messageId) => await bot.CopyMessage(targetChatId, sourceChatId, messageId);

		public async Task<string> GetBotUsername() => (await bot.GetMe()).Username;
	}
}
