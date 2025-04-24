using System;
using System.Threading.Tasks;
using Telegram.Bot.Args;

namespace TezosNotifyBot.Abstractions
{
    public interface IUpdateHandler
    {
        Task HandleUpdate(TelegramBotHandler.Chat chat, int messageId, string text);

        Task HandleException(Exception exception, TelegramBotHandler.Chat chat, int messageId)
        {
            return Task.CompletedTask;
        }
    }
}