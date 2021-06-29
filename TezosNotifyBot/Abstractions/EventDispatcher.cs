using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TezosNotifyBot.Abstractions
{
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public EventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task Dispatch<T>(T subject) where T : class
        {
            // using var scope = _serviceProvider.CreateScope();

            var handlers =_serviceProvider.GetServices<IEventHandler<T>>();
            
            foreach (var eventHandler in handlers)
            {
                Task.Run(() => eventHandler.Process(subject));
            }

            return Task.CompletedTask;
        }
    }
}