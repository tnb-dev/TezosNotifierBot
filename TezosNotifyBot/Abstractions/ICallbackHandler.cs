using System;
using System.Threading.Tasks;

namespace TezosNotifyBot.Abstractions
{
    public interface ICallbackHandler
    {
        Task Handle(string[] args, long userId, int messageId);

        Task HandleException(Exception exception)
        {
            return Task.CompletedTask;
        }
    }
}