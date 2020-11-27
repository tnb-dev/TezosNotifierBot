using System;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TezosNotifyBot.Model;
using TezosNotifyBot.Storage;

namespace TezosNotifyBot
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: It's needed?
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            CreateHostBuilder(args).Build().Run();
			// Console.WriteLine("Бот запущен " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
   //          var npb = new TezosBot();
   //          npb.Run();
        }
        
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddJsonFile("Settings/Settings.json");
                    builder.AddJsonFile($"Settings/Settings.{context.HostingEnvironment.EnvironmentName}.json", true);
                    builder.AddJsonFile("Settings/Settings.Local.json", true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContextPool<TezosDataContext>(builder => builder
                        .UseNpgsql(context.Configuration.GetConnectionString("Default"))
                    );

                    // TODO: Make this transient 
                    services.AddSingleton<Repository>();
                    services.AddSingleton<TezosBot>();
                    
                    services.AddHostedService<Service>();
                });
    }
}
