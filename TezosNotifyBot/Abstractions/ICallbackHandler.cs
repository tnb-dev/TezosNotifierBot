using System;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace TezosNotifyBot.Abstractions
{
    public interface ICallbackHandler
    {
        Task Handle(string[] args, CallbackQuery query);

        Task HandleException(Exception exception, object sender, CallbackQuery query)
        {
            return Task.CompletedTask;
        }
    }
}