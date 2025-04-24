using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TezosNotifyBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TezosNotifyBot.Storage;
using User = TezosNotifyBot.Domain.User;

namespace TezosNotifyBot
{
    public class TezosBotFacade
    {
        private readonly IOptions<BotConfig> options;
        public ITelegramBotClient Client { get; }

        public TezosBotFacade(ITelegramBotClient client, IOptions<BotConfig> options)
        {
            this.options = options;
            Client = client;
        }

        public Task<Message> Reply(long userId, int replyToMessageId, string content)
        {
            return SendText(
				userId,
                content,
                ParseMode.Html,
                replyToMessageId: replyToMessageId
			);
        }

        public Task<Message> SendText(
            ChatId chatId, 
            string content, 
            ParseMode parseMode = ParseMode.Markdown,
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            ReplyMarkup replyMarkup = null)
        {
            // TODO: Add chunked sending for large texts
            return Client.SendMessage(
                chatId,
                content,
                parseMode: parseMode,
				linkPreviewOptions: new LinkPreviewOptions { IsDisabled = disableWebPagePreview },
				disableNotification: disableNotification,
                replyParameters: new ReplyParameters { MessageId = replyToMessageId},
                replyMarkup: replyMarkup
            );
        }

        public Task<Message> EditText(
            ChatId chatId, 
            int messageId,
            string content, 
            ParseMode parseMode = ParseMode.Markdown,
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            InlineKeyboardMarkup replyMarkup = null)
        {
            return Client.EditMessageText(
                chatId,
                messageId,
                content,
                parseMode: parseMode,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = disableWebPagePreview },
                replyMarkup: replyMarkup
            );
        }
    }
}