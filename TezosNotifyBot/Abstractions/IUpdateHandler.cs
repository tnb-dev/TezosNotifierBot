using System;
using System.Threading.Tasks;
using Telegram.Bot.Args;

namespace TezosNotifyBot.Abstractions
{
    public interface IUpdateHandler
    {
        Task HandleUpdate(object sender, UpdateEventArgs eventArgs);

        Task HandleException(Exception exception, UpdateEventArgs eventArgs, object sender)
        {
            return Task.CompletedTask;
        }
    }
}