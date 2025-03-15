using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Sports;
using Aevatar.Application.Grains.Agents.Sports.Model;
using Aevatar.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

public interface INeo4JTestService
{
    Task<string> TestConnectionAsync();
    Task StoreDataWithAgentAsync();
}


[RemoteService(IsEnabled = false)]
public class Neo4JTestService : ApplicationService, INeo4JTestService
{
    private readonly IDriver _driver;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Neo4JTestService> _logger;
    private readonly IClusterClient _clusterClient;

    public Neo4JTestService(IDriver driver, IConfiguration configuration, ILogger<Neo4JTestService> logger, IClusterClient clusterClient)
    {
        _driver = driver;
        _configuration = configuration;
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task<string> TestConnectionAsync()
    {
        _logger.LogInformation("Testing Neo4j Connection, uri {url}", _configuration["Neo4j:Uri"]);
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync("RETURN 'Neo4j Connection Successful' AS message");
        var record = await result.SingleAsync();
        return ValueExtensions.As<string>(record["message"]);
    }
    
    public async Task StoreDataWithAgentAsync()
    {
        var gAgent = _clusterClient.GetGrain<ISportsGAgent>(GuidUtil.StringToGuid("test"));
        var club1 = new Node
        {
            Labels = new[] { "Club" },
            Properties = new Dictionary<string, object>
            {
                { "Name", "Liverpool" },
                { "City", "Liverpool" }
            },
            MatchKey = "Name"
        };

        var club2 = new Node
        {
            Labels = new[] { "Club" },
            Properties = new Dictionary<string, object>
            {
                { "Name", "Manchester United" },
                { "City", "Manchester" }
            },
            MatchKey = "Name"
        };

        var player = new Node
        {
            Labels = new[] { "Player" },
            Properties = new Dictionary<string, object>
            {
                { "Name", "Salah" },
                { "Country", "Egypt" }
            },
            MatchKey = "Name"
        };

        var relationships = new[]
        {
            new Relationship
            {
                Type = "PLAYS_FOR",
                StartNode = player,
                EndNode = club1
            },
            new Relationship
            {
                Type = "RIVAL",
                StartNode = club1,
                EndNode = club2
            }
        };

        await gAgent.CreateDataAsync(new[] { club1, club2, player }, relationships);
    }
}