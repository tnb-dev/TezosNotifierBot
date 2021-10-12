using Google.Cloud.Dialogflow.V2;
using Microsoft.Extensions.DependencyInjection;

namespace TezosNotifyBot.Dialog.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDialogFlow(this IServiceCollection service, string projectId)
        {
            service.AddScoped(provider => new SessionsClientBuilder().Build());
            service.AddTransient(provider => 
                new DialogService(provider.GetService<SessionsClient>(), projectId)    
            );
        }
    }
}