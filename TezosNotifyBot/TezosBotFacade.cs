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
        private readonly TezosDataContext db;
        private readonly IOptions<BotConfig> options;
        public TelegramBotClient Client { get; }

        public TezosBotFacade(TezosDataContext db, TelegramBotClient client, IOptions<BotConfig> options)
        {
            this.db = db;
            this.options = options;
            Client = client;
        }

        public Task<Message> Reply(Message message, string content)
        {
            return SendText(
                message.Chat,
                content,
                ParseMode.Html,
                replyToMessageId: message.MessageId
            );
        }

        public Task<Message> SendText(
            ChatId chatId, 
            string content, 
            ParseMode parseMode = ParseMode.Markdown,
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            // TODO: Add chunked sending for large texts
            return Client.SendTextMessageAsync(
                chatId,
                content,
                parseMode: parseMode,
                disableWebPagePreview: disableWebPagePreview,
                disableNotification: disableNotification,
                replyToMessageId: replyToMessageId,
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
            return Client.EditMessageTextAsync(
                chatId,
                messageId,
                content,
                parseMode: parseMode,
                disableWebPagePreview: disableWebPagePreview,
                replyMarkup: replyMarkup
            );
        }

        public async Task NotifyAdmins(string content)
        {
            // TODO: Replace with telegram admins
            var nicks = options.Value.Telegram.DevUsers;
            var users = await db.Set<User>().AsNoTracking()
                .Where(x => nicks.Contains(x.Username))
                .Select(x => x.Id)
                .ToArrayAsync();

            var sent = users.Select(userId => SendText(userId, content))
                .AsEnumerable();

            await Task.WhenAll(sent);
        }
    }
}