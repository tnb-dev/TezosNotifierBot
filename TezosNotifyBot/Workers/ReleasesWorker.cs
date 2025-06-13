using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Storage;

namespace TezosNotifyBot.Workers
{
    public class ReleasesWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReleasesWorker> _logger;
        private readonly IOptions<ReleasesWorkerOptions> _options;
        private readonly ResourceManager _resourceManager;

        public ReleasesWorker(
            IServiceProvider serviceProvider,
            ILogger<ReleasesWorker> logger,
            IOptions<ReleasesWorkerOptions> options,
            ResourceManager resourceManager
        )
        {
            _logger = logger;
            _options = options;
            _resourceManager = resourceManager;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested is false)
            {
                using var scope = _serviceProvider.CreateScope();
                await using var db = scope.ServiceProvider.GetRequiredService<TezosDataContext>();

                var client = scope.ServiceProvider.GetRequiredService<ReleasesClient>();

                try
                {
                    var releases = await client.GetAll();

                    foreach (var release in releases)
                    {
                        var exists = await db.Set<TezosRelease>().AnyAsync(x => x.Name == release.Name);
                        if (exists is false)
                        {
                            await db.AddAsync(release);

                            await BroadcastRelease(db, release);
                        }
                    }

                    await db.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fetch tezos releases");
                }

                await Task.Delay(_options.Value.RefreshInterval);

                async Task BroadcastRelease(TezosDataContext db, TezosRelease release)
                {
                    var subscribers = await db.Set<User>().AsNoTracking()
                        .Where(x => x.ReleaseNotify)
                        .ToArrayAsync();

                    var messages = subscribers.Select(user =>
                    {
                        var message = @$"🦊 Tezos software update {release.Name} released.

Check out the <a href='{release.AnnounceUrl}'>announcement</a>";
                        return Message.Push(user.Id, message);
                    });

                    await db.AddRangeAsync(messages);
                    await db.SaveChangesAsync();
                }
            }
        }
    }

    public class ReleasesWorkerOptions
    {
        public string Url { get; set; }

        public TimeSpan RefreshInterval { get; set; }
    }

    public class ReleasesClient
    {
        private readonly HttpClient _client;
        private readonly IOptions<ReleasesWorkerOptions> _options;

        public ReleasesClient(HttpClient client, IOptions<ReleasesWorkerOptions> options)
        {
            _client = client;
            _options = options;
        }

        public async Task<TezosRelease[]> GetAll()
        {
            var responseStream = await _client.GetStreamAsync(_options.Value.Url);

            var results = await JsonSerializer.DeserializeAsync<ReleaseItem[]>(responseStream);

            return results.Where(r => r.latest == true).Select(json =>
            {
                return new TezosRelease
                {
                    Url = json.announcement,
                    Name = $"v{json.major}.{json.minor}",
                    AnnounceUrl = json.announcement,
                    ReleasedAt = DateTime.Now,
                    Tag = $"v{json.major}.{json.minor}"
				};
            }).ToArray();
        }

        private class ReleaseItem
        {
			public int major { get; set; }
			public int minor { get; set; }
			public int rc { get; set; }
			public string announcement { get; set; }
			public bool? latest { get; set; }
		}
    }
}