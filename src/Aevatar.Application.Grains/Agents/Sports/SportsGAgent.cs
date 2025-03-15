using Aevatar.Application.Grains.Agents.Sports.Common;
using Aevatar.Application.Grains.Agents.Sports.Model;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Aevatar.Application.Grains.Agents.Sports;


[GenerateSerializer]
public class SportsGAgentState : StateBase
{
}

[GenerateSerializer]
public class SportsStateLogEvent : StateLogEventBase<SportsStateLogEvent>;


[GenerateSerializer]
public class SportsConfiguration : ConfigurationBase
{
    [Id(0)] public string Content { get; set; }
}

public interface ISportsGAgent : IStateGAgent<SportsGAgentState>
{
    Task CreateDataAsync(IEnumerable<Node> nodes, IEnumerable<Relationship> relationships);
    Task<List<string>?> GetClubPlayers(string clubName);
    Task<List<(string, string)>?> GetClubRivals(string clubName);
}

public class SportsGAgent : GAgentBase<SportsGAgentState, SportsStateLogEvent,  EventBase, SportsConfiguration>, ISportsGAgent
{

    private readonly ILogger<SportsGAgent> _logger;
    private readonly IDriver _driver;

    public SportsGAgent(ILogger<SportsGAgent> logger, IDriver driver)
    {
        _logger = logger;
        _driver = driver;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("SportsAgent is a Neo4j graph database agent designed for sports domain data management");
    }
    
    protected override async Task PerformConfigAsync(SportsConfiguration configuration)
    {
        var club1 = new Node
        {
            Labels = new[] { "Club" },
            Properties = new Dictionary<string, object>
            {
                { "Name", "Real Madrid" },
                { "City", "Madrid" }
            },
            MatchKey = "Name"
        };

        var club2 = new Node
        {
            Labels = new[] { "Club" },
            Properties = new Dictionary<string, object>
            {
                { "Name", "Barcelona" },
                { "City", "Barcelona" }
            },
            MatchKey = "Name"
        };

        var player = new Node
        {
            Labels = new[] { "Player" },
            Properties = new Dictionary<string, object>
            {
                { "Name", "Luka Modric" },
                { "Country", "Croatia" }
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

        await CreateDataAsync(new[] { club1, club2, player }, relationships);
    }
    
    
    public async Task CreateDataAsync(IEnumerable<Node> nodes, IEnumerable<Relationship> relationships)
    {
        try
        {
            var builder = new CypherQueryBuilder();
            var (query, parameters) = builder.Build(nodes, relationships);
            
            await using var session = _driver.AsyncSession();
            await session.ExecuteWriteAsync(async tx => 
            {
                await tx.RunAsync(query, parameters);
            });
        }
        catch (ClientException e)
        {
            _logger.LogError("Error storing, msg: {msg}, code: {code}", e.Message, e.Code);
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Error authentication, msg: {msg}, code: {code}", e.Message, e.Code);
        }
    }
    
    public async Task<List<string>?> GetClubPlayers(string clubName)
    {
        try
        {
            var query = @"
        MATCH (p:Player)-[:PLAYS_FOR]->(c:Club {Name: $clubName})
        RETURN p.Name AS PlayerName
        ORDER BY PlayerName";

            var parameters = new { clubName };

            await using var session = _driver.AsyncSession();
            return await session.ExecuteReadAsync(async tx => 
            {
                var result = await tx.RunAsync(query, parameters);
                return await result.ToListAsync(r => 
                    Neo4j.Driver.ValueExtensions.As<string>(r["PlayerName"])); 
            });
        }
        catch (ClientException e)
        {
            _logger.LogError("Error Quering, msg: {msg}, code: {code}", e.Message, e.Code);
            return null;
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Error authentication, msg: {msg}, code: {code}", e.Message, e.Code);
            return null;
        }
    }

    public async Task<List<(string, string)>?> GetClubRivals(string clubName)
    {
        try
        {
            var query = @"
        MATCH (c:Club {Name: $clubName})-[:RIVAL]-(rival:Club)
        RETURN c.Name AS Club, rival.Name AS RivalClub
        ORDER BY RivalClub";

            var parameters = new { clubName };
        
            await using var session = _driver.AsyncSession();
            return await session.ExecuteReadAsync(async tx => 
            {
                var result = await tx.RunAsync(query, parameters);
                return await result.ToListAsync(r => 
                (
                    Neo4j.Driver.ValueExtensions.As<string>(r["Club"]),   
                    Neo4j.Driver.ValueExtensions.As<string>(r["RivalClub"]) 
                ));
            });
        }
        catch (ClientException e)
        {
            _logger.LogError("Error Querying, msg: {msg}, code: {code}", e.Message, e.Code);
            return null;
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Error authentication, msg: {msg}, code: {code}", e.Message, e.Code);
            return null;
        }
        
    }
    
}