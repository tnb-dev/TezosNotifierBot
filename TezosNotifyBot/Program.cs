using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Gelf.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MihaZupan;
using Polly;
using Polly.Extensions.Http;
using Telegram.Bot;
using TezosNotifyBot.Abstractions;
using TezosNotifyBot.Commands.Addresses;
using TezosNotifyBot.CryptoCompare;
using TezosNotifyBot.Dialog.Extensions;
using TezosNotifyBot.Model;
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
                    services.AddEntityFrameworkNpgsql();
                    services.AddDbContext<TezosDataContext>((sp, builder) => builder
                        .UseNpgsql(context.Configuration.GetConnectionString("Default"), opt => opt.CommandTimeout(600))
                        .UseInternalServiceProvider(sp)
                        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
					);
                    
                    services.Configure<BotConfig>(context.Configuration);
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

                    services.AddScoped<AddressService>();
                    services.AddSingleton<AddressTransactionsRepository>();
                    services.AddSingleton<IMemoryCache>(sp => new MemoryCache());
                    services.AddHttpClient<ReleasesClient>();                   
                    

                    services.AddTransient<ITzKtClient>(sp =>
                        new TzKtClient(new HttpClient(), sp.GetService<ILogger<TzKtClient>>(),
                        context.Configuration, sp.GetService<IMemoryCache>()));

                    services.AddTransient<IMarketDataProvider>(sp => {
                        var config = sp.GetService<IOptions<BotConfig>>();
                        return new CryptoCompareClient(config.Value.CryptoCompareToken, new HttpClient(), sp.GetService<ILogger<CryptoCompareClient>>());
                    });
                    services.AddSingleton<TezosBot>();
                    services.AddSingleton<TezosBotFacade>();
                    services.AddSingleton<AddressManager>();

                    services.AddSingleton<ITelegramBotClient>(provider =>
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

                        return new TelegramBotClient(config.Value.Telegram.BotSecret);
                    });

                    services.AddSingleton<TelegramBotInvoker>();
					services.AddSingleton<TelegramBotHandler>();

					services.AddSingleton(_ =>
                    {
                        var manager = new ResourceManager();
                        manager.LoadResources("res.txt");

                        return manager;
                    });

                    services.AddHostedService<Service>();
                    services.AddHostedService<ReleasesWorker>();
                    services.AddHostedService<BroadcastWorker>();
                    services.AddHostedService<WhaleMonitorWorker>();
                    services.AddHostedService<TezosProcessing>();

                    services.Scan(scan => scan
                        .FromEntryAssembly()
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
                        .FromEntryAssembly()
                        .AddClasses(classes => classes.AssignableTo<CommandsProfile>())
                        .As<CommandsProfile>()
                        .WithSingletonLifetime()
                    );
                    
                    services.Scan(scan => scan
                        .FromEntryAssembly()
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