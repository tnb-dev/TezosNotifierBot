using System;
using System.Net;
using System.Text;
using Gelf.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MihaZupan;
using NornPool.Model;
using Telegram.Bot;
using TezosNotifyBot.Model;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Workers;

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
                    services.Configure<TwitterOptions>(context.Configuration.GetSection("Twitter"));
                    services.Configure<ReleasesWorkerOptions>(context.Configuration.GetSection("ReleasesWorker"));

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

                    services.AddHttpClient<ReleasesClient>();
                    services.AddTransient<Tzkt.ITzKtClient>(sp => new Tzkt.TzKtClient(sp.GetService<ILogger<Tzkt.TzKtClient>>(), context.Configuration.GetValue<string>("TzKtUrl")));
                    services.AddTransient<Repository>();
                    services.AddTransient<TezosBot>();
                    services.AddSingleton(new AddressManager(context.Configuration.GetValue<string>("TzKtUrl")));

                    services.AddSingleton(provider =>
                    {
                        var config = provider.GetService<IOptions<BotConfig>>();

                        IWebProxy proxy = null;
                        if (config.Value.ProxyAddress != null)
                        {
                            if (config.Value.ProxyType == "http")
                                proxy = new WebProxy(config.Value.ProxyAddress, config.Value.ProxyPort);
                            else
                                proxy = new HttpToSocks5Proxy(config.Value.ProxyAddress, config.Value.ProxyPort);
                            if (config.Value.ProxyLogin != null)
                                proxy.Credentials =
                                    new NetworkCredential(config.Value.ProxyLogin, config.Value.ProxyPassword);
                        }

                        return new TelegramBotClient(config.Value.Telegram.BotSecret, proxy);
                    });

                    services.AddSingleton(_ =>
                    {
                        var manager = new ResourceManager();
                        manager.LoadResources("res.txt");

                        return manager;
                    });

                    services.AddSingleton<TwitterClient>();

                    services.AddHostedService<Service>();
                    services.AddHostedService<ReleasesWorker>();
                    services.AddHostedService<BroadcastWorker>();

                    using var provider = services.BuildServiceProvider();
                    using var database = provider.GetRequiredService<TezosDataContext>();

                    database.Database.Migrate();
                });
    }
}