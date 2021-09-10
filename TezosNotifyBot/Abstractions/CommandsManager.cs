using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace TezosNotifyBot.Abstractions
{
    public class CommandsManager
    {
        private readonly IServiceProvider provider;
        private readonly ILogger<CommandsManager> logger;
        private readonly IEnumerable<CommandsProfile> profiles;

        public CommandsManager(IServiceProvider provider, ILogger<CommandsManager> logger,
            IEnumerable<CommandsProfile> profiles)
        {
            this.logger = logger;
            this.provider = provider;
            this.profiles = profiles;
        }

        public bool HasUpdateHandler(UpdateEventArgs eventArgs)
        {
            if (eventArgs.Update.Type != UpdateType.Message)
                return false;

            return profiles.Any(profile => profile.UpdateHandlers
                .Keys.Any(command => eventArgs.Update.Message.Text.StartsWith(command))
            );
        }
        
        public bool HasCallbackHandler(CallbackQueryEventArgs eventArgs)
        {
            return profiles.Any(profile => profile.CallbackHandlers
                .Keys.Any(command => eventArgs.CallbackQuery.Data.StartsWith(command))
            );
        }

        public async Task ProcessCallbackHandler(object sender, CallbackQueryEventArgs eventArgs)
        {
            var data = eventArgs.CallbackQuery.Data.Split(' ');
            var name = data.First();
            var args = data.Skip(1).ToArray();
            
            // step 1: find handler
            var profile = profiles.FirstOrDefault(p =>
                p.CallbackHandlers.Keys.Any(c => name == c));

            var handler = profile?.CallbackHandlers.FirstOrDefault(pair => name == pair.Key);

            if (handler is null)
                throw new Exception("Handler for callback event not found.\n" +
                                    $"Callback name to handle: {name}");
            
            // step 2: create scope for using scoped services in handlers
            using var scope = provider.CreateScope();
            
            var handlerType = handler.Value.Value;
            // step 3: resolve handler instance
            var handlerInstance = (ICallbackHandler) scope.ServiceProvider.GetService(handlerType);
            if (handlerInstance is null)
                throw new ArgumentException($"Handler for callback {name} not found");
            
            try
            {
                // step 4: awaiting handler result
                await handlerInstance.Handle(args, eventArgs.CallbackQuery);
            }
            catch (Exception e)
            {
                await handlerInstance.HandleException(e, sender, eventArgs.CallbackQuery);
                
                logger.LogError($"Failed to process callback handler for message: {name}\n" +
                                $"With exception message: {e.Message}", e);
            }
        }

        public async Task ProcessUpdateHandler(object sender, UpdateEventArgs eventArgs)
        {
            if (eventArgs.Update.Type != UpdateType.Message)
                return;

            var message = eventArgs.Update.Message.Text;
            // step 1: find handler
            var profile = profiles.FirstOrDefault(p =>
                p.UpdateHandlers.Keys.Any(c => message.StartsWith(c)));

            var handler = profile?.UpdateHandlers.FirstOrDefault(pair => message.StartsWith(pair.Key));

            if (handler is null)
                throw new Exception("Handler for update event not found.\n" +
                                    $"Message to handle: {message}");

            // step 2: create scope for using scoped services in handlers
            using var scope = provider.CreateScope();
            
            var handlerType = handler.Value.Value;
            // step 3: resolve handler instance
            var handlerInstance = (IUpdateHandler) scope.ServiceProvider.GetRequiredService(handlerType);

            try
            {
                // step 4: awaiting handler result
                await handlerInstance.HandleUpdate(sender, eventArgs);
            }
            catch (Exception e)
            {
                await handlerInstance.HandleException(e, eventArgs, sender);
                
                logger.LogError($"Failed to process update handler for message: {message}\n" +
                                $"With exception message: {e.Message}", e);
            }
        }
    }
}