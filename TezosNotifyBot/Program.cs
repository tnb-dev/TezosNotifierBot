using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Gelf.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MihaZupan;
using NornPool.Model;
using Polly;
using Polly.Extensions.Http;
using Telegram.Bot;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.BetterCallDev;
using TezosNotifyBot.Commands.Addresses;
using TezosNotifyBot.Dialog.Extensions;
using TezosNotifyBot.Model;
using TezosNotifyBot.Nodes;
using TezosNotifyBot.Services;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Tzkt;
using TezosNotifyBot.Workers;

namespace TezosNotifyBot
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: It's needed?
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = CreateHostBuilder(args).Build();
            if (args.Contains("--migrate"))
            {
                using var scope = builder.Services.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<TezosDataContext>();
                db.Database.Migrate();
            }

            builder.Run();
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
                        .UseNpgsql(context.Configuration.GetConnectionString("Default"), opt => opt.CommandTimeout(600))
                    );
                    
                    services.Configure<BotConfig>(context.Configuration);
                    services.Configure<TwitterOptions>(context.Configuration.GetSection("Twitter"));
                    services.Configure<ReleasesWorkerOptions>(context.Configuration.GetSection("ReleasesWorker"));
                    services.Configure<MediumOptions>(context.Configuration.GetSection("Medium"));

                    services.AddDialogFlow("tnb-utijmv");
                    
                    services.AddLogging(builder =>
                    {
                        if (context.HostingEnvironment.IsDevelopment())
                            builder.AddConsole();
                        else
                            builder.AddGelf(options =>
                                options.LogSource = $"Tezos {context.HostingEnvironment.EnvironmentName}"
                            );
                    });

                    services.AddScoped<TokenService>();
                    services.AddScoped<AddressService>();
                    services.AddSingleton<AddressTransactionsRepository>();
                    services.AddSingleton<IMemoryCache>(sp => new MemoryCache());
                    services.AddHttpClient<ReleasesClient>();                   
                    

                    services.AddTransient<ITzKtClient>(sp =>
                        new TzKtClient(new HttpClient(), sp.GetService<ILogger<TzKtClient>>(),
                        context.Configuration, sp.GetService<IMemoryCache>()));
                    services.AddTransient<IBetterCallDevClient>(sp =>
                        new BetterCallDevClient(
                            sp.GetService<ILogger<BetterCallDevClient>>(),
                            context.Configuration.GetValue<string>("BetterCallDevUrl")));
                    services.AddTransient<Repository>();
                    services.AddTransient<TezosBot>();
                    services.AddTransient<TezosBotFacade>();
                    services.AddTransient<AddressManager>();

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

                    services.AddSingleton(provider => provider.GetService<IOptions<BotConfig>>()?.Value.Nodes);

                    services.AddHttpClient<NodeManager>(client => { client.Timeout = TimeSpan.FromMinutes(2); })
                        .SetHandlerLifetime(TimeSpan.FromMinutes(1))
                        .AddPolicyHandler(GetRetryPolicy());

                    services.AddSingleton<TwitterClient>();

                    services.AddHostedService<Service>();
                    services.AddHostedService<ReleasesWorker>();
                    services.AddHostedService<BroadcastWorker>();
                    services.AddHostedService<TokensMonitorWorker>();
                    services.AddHostedService<WhaleMonitorWorker>();
                    services.AddHostedService<MediumWorker>();

                    services.Scan(scan => scan
                        .FromCallingAssembly()
                        .AddClasses(classes => classes
                            .AssignableToAny(
                                typeof(IUpdateHandler),
                                typeof(ICallbackHandler)
                            )
                        )
                        .AsSelf()
                        .WithTransientLifetime()
                    );
                    services.Scan(scan => scan
                        .FromCallingAssembly()
                        .AddClasses(classes => classes.AssignableTo<CommandsProfile>())
                        .As<CommandsProfile>()
                        .WithSingletonLifetime()
                    );
                    
                    services.Scan(scan => scan
                        .FromCallingAssembly()
                        .AddClasses(classes => classes
                            .AssignableToAny(
                                typeof(IEventHandler<>),
                                typeof(IEventDispatcher)
                            )
                        )
                        .AsImplementedInterfaces()
                    );

                    services.AddSingleton<CommandsManager>();
                });

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}