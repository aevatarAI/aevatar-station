using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace Aevatar.Developer.Logger;

public class AevatarDeveloperLoggerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<LogElasticSearchOptions>(configuration.GetSection("LogElasticSearch"));

        context.Services.AddSingleton<ElasticsearchClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<LogElasticSearchOptions>>().Value;
            if (options.Uris == null || !options.Uris.Any())
            {
                throw new Exception("The config of [LogElasticSearch] is missing or invalid.");
            }

            var nodes = options.Uris.Select(uri => new Uri(uri)).ToArray();
            var connectionPool = new StaticNodePool(nodes);

            var settings = new ElasticsearchClientSettings(connectionPool)
                .EnableHttpCompression();

            if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
            {
                settings.Authentication(new BasicAuthentication(options.Username, options.Password));
            }

            return new ElasticsearchClient(settings);
        });

        context.Services.AddSingleton<ILogService, LogElasticSearchService>();
    }
}