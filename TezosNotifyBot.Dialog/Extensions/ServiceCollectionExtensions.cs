using Google.Cloud.Dialogflow.V2;
using Microsoft.Extensions.DependencyInjection;

namespace TezosNotifyBot.Dialog.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDialogFlow(this IServiceCollection service, string projectId)
        {
            var client = new SessionsClientBuilder();
            service.AddSingleton(client.Build());
            service.AddTransient(provider => 
                new DialogService(provider.GetService<SessionsClient>(), projectId)    
            );
        }
    }
}