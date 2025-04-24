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

        public bool HasUpdateHandler(string text)
        {
            return profiles.Any(profile => profile.UpdateHandlers
                .Keys.Any(command => text.StartsWith(command))
            );
        }
        
        public bool HasCallbackHandler(string callbackData)
        {
            return profiles.Any(profile => profile.CallbackHandlers
                .Keys.Any(command => callbackData.StartsWith(command))
            );
        }

        public async Task ProcessCallbackHandler(long userId, int messageId, string callbackData)
        {
            var data = callbackData.Split(' ');
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
                await handlerInstance.Handle(args, userId, messageId);
            }
            catch (Exception e)
            {
                await handlerInstance.HandleException(e);
                
                logger.LogError($"Failed to process callback handler for message: {name}\n" +
                                $"With exception message: {e.Message}", e);
            }
        }

        public async Task ProcessUpdateHandler(TelegramBotHandler.Chat chat, int messageId, string text)
        {
            // step 1: find handler
            var profile = profiles.FirstOrDefault(p => p.UpdateHandlers.Keys.Any(c => text.StartsWith(c)));

            var handler = profile?.UpdateHandlers.FirstOrDefault(pair => text.StartsWith(pair.Key));

            if (handler is null)
                throw new Exception("Handler for update event not found.\n" +
                                    $"Message to handle: {text}");

            // step 2: create scope for using scoped services in handlers
            using var scope = provider.CreateScope();
            
            var handlerType = handler.Value.Value;
            // step 3: resolve handler instance
            var handlerInstance = (IUpdateHandler) scope.ServiceProvider.GetRequiredService(handlerType);

            try
            {
                // step 4: awaiting handler result
                await handlerInstance.HandleUpdate(chat, messageId, text);
            }
            catch (Exception e)
            {
                await handlerInstance.HandleException(e, chat, messageId);
                
                logger.LogError($"Failed to process update handler for message: {text}\nWith exception message: {e.Message}", e);
            }
        }
    }
}