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
using Microsoft.Extensions.Options;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage;

namespace TezosNotifyBot.Workers
{
    public class ReleasesWorker: BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<ReleasesWorkerOptions> _options;

        public ReleasesWorker(IServiceProvider serviceProvider, IOptions<ReleasesWorkerOptions> options)
        {
            _options = options;
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

                    foreach (var tezosRelease in releases)
                    {
                        var exists = await db.Set<TezosRelease>().AnyAsync(x => x.Tag == tezosRelease.Tag);
                        if (exists is false)
                        {
                            db.Add(tezosRelease);
                        }
                    }

                    await db.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
                await Task.Delay(_options.Value.RefreshInterval);
            }
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
                var announce = json.Assets.Links.FirstOrDefault();
                
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
            [JsonPropertyName("tag_name")]
            public string Tag { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("released_at")]
            public DateTime ReleasedAt { get; set; }

            [JsonPropertyName("_links")]
            public ReleaseLinks Links { get; set; }

            [JsonPropertyName("assets")]
            public ReleaseAssets Assets { get; set; }
        }

        private class ReleaseLinks
        {
            [JsonPropertyName("self")]
            public string Self { get; set; }
        }

        private class ReleaseAssets
        {
            [JsonPropertyName("links")]
            public AssetsLink[] Links { get; set; }
        }

        private class AssetsLink
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
    }
}