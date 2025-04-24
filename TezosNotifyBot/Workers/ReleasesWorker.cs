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
                        var exists = await db.Set<TezosRelease>().AnyAsync(x => x.Tag == release.Tag);
                        if (exists is false)
                        {
                            await db.AddAsync(release);

                            if (IsFakeRelease(release.Tag))
                            {
                                // Skip release notifications
                                continue;
                            }

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
                        var type = release.AnnounceUrl is null ? Res.TezosRelease : Res.TezosReleaseWithLink;
                        var message = _resourceManager.Get(type, (user, release));
                        return Message.Push(user.Id, message);
                    });

                    await db.AddRangeAsync(messages);
                    await db.SaveChangesAsync();
                }
            }
        }
    
        protected static bool IsFakeRelease(string tag) {
            var parts = tag.Trim('v').Split('.', '~', '-').Select(str =>
            {
                if (int.TryParse(str, out var num))
                {
                    return num;
                }
                return int.MinValue;
            }).ToArray<int>();

            return parts[0] >= 100;
        }
    }

    public class ReleasesWorkerOptions
    {
        public string ProjectId { get; set; }

        public TimeSpan RefreshInterval { get; set; }
    }

    public class ReleasesClient
    {
        private readonly HttpClient _client;
        private readonly IOptions<ReleasesWorkerOptions> _options;

        public ReleasesClient(HttpClient client, IOptions<ReleasesWorkerOptions> options)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://gitlab.com");
            _options = options;
        }

        public async Task<TezosRelease[]> GetAll()
        {
            var projectId = _options.Value.ProjectId;
            var responseStream = await _client.GetStreamAsync($"/api/v4/projects/{projectId}/releases");

            var results = await JsonSerializer.DeserializeAsync<ReleaseItem[]>(responseStream);

            return results.Select(json =>
            {
                var announce = json.Assets.Links.FirstOrDefault(x =>
                    x.Url.StartsWith("https://tezos.gitlab.io/releases") || x.Name == "Announcement");

                return new TezosRelease
                {
                    Tag = json.Tag,
                    Url = json.Links.Self,
                    Name = json.Name,
                    Description = json.Description,
                    AnnounceUrl = announce?.Url,
                    ReleasedAt = json.ReleasedAt
                };
            }).ToArray();
        }

        private class ReleaseItem
        {
            [JsonPropertyName("tag_name")] public string Tag { get; set; }

            [JsonPropertyName("name")] public string Name { get; set; }

            [JsonPropertyName("description")] public string Description { get; set; }

            [JsonPropertyName("released_at")] public DateTime ReleasedAt { get; set; }

            [JsonPropertyName("_links")] public ReleaseLinks Links { get; set; }

            [JsonPropertyName("assets")] public ReleaseAssets Assets { get; set; }
        }

        private class ReleaseLinks
        {
            [JsonPropertyName("self")] public string Self { get; set; }
        }

        private class ReleaseAssets
        {
            [JsonPropertyName("links")] public AssetsLink[] Links { get; set; }
        }

        private class AssetsLink
        {
            [JsonPropertyName("url")] public string Url { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; }
        }
    }
}