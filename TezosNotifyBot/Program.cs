using System;
using System.Text;
using Gelf.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NornPool.Model;
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

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddJsonFile("Settings/Settings.json");
                    builder.AddJsonFile($"Settings/Settings.{context.HostingEnvironment.EnvironmentName}.json", true);
                    builder.AddJsonFile("Settings/Settings.Local.json", true);
                    builder.AddEnvironmentVariables();
                    builder.AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<TezosDataContext>(builder => builder
                        .UseNpgsql(context.Configuration.GetConnectionString("Default"))
                    );

                    services.Configure<BotConfig>(context.Configuration);
                    
                    services.AddLogging(builder =>
                    {
                        if (context.HostingEnvironment.IsDevelopment() is false)
                        {
                            builder.AddGelf(options => 
                                options.LogSource = $"Tezos {context.HostingEnvironment.EnvironmentName}"
                            );
                        }
                        else
                        {
                            builder.AddConsole();
                        }
                    });

                    services.AddTransient<Repository>();
                    services.AddTransient<TezosBot>();
                    
                    services.AddHostedService<Service>();

                    using var provider = services.BuildServiceProvider();
                    using var database = provider.GetRequiredService<TezosDataContext>();
                    
                    database.Database.Migrate();
                });
    }
}
