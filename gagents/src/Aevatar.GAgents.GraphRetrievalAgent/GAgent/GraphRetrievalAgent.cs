using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.GraphRetrievalAgent.Common;
using Aevatar.GAgents.GraphRetrievalAgent.GAgent.SEvent;
using Aevatar.GAgents.GraphRetrievalAgent.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Newtonsoft.Json;

namespace Aevatar.GAgents.GraphRetrievalAgent.GAgent;

public interface IGraphRetrievalAgent : IAIGAgent, IGAgent
{
    Task<string?> InvokeLLMWithGraphRetrievalAsync(string prompt);
}

[Description("Advanced graph-based knowledge retrieval agent that performs intelligent semantic searches across connected data structures. Utilizes graph traversal algorithms and AI embeddings for contextual information discovery.")]
public class GraphRetrievalAgent : AIGAgentBase<GraphRetrievalAgentState, GraphRetrievalAgentSEvent, EventBase, GraphRetrievalConfig>, IGraphRetrievalAgent
{
    private readonly ILogger<GraphRetrievalAgent> _logger;
    private readonly IDriver _driver;


    public GraphRetrievalAgent(ILogger<GraphRetrievalAgent> logger)
    {
        _logger = logger;
        _driver = ServiceProvider.GetRequiredService<IDriver>();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Graph Retrieval Agent");
    }
    
    protected override async Task PerformConfigAsync(GraphRetrievalConfig initializationConfig)
    {
        _logger.LogDebug("PerformConfigAsync , schema {schema} example {example}",
            initializationConfig.Schema, initializationConfig.Example);
        RaiseEvent(new SetGraphSchemaSEvent
        {
            Schema = initializationConfig.Schema,
            Example = initializationConfig.Example
        });

        await ConfirmEvents();
    }
    
    public async Task<string?> InvokeLLMWithGraphRetrievalAsync(string prompt)
    {
        List<ChatMessage>? history = null;
        var graphRagData = await GraphRagDataAsync(prompt);
        if (!graphRagData.IsNullOrEmpty())
        {
            _logger.LogDebug("add graph rag data {data}", graphRagData);
            history = new List<ChatMessage>
            {
                new ChatMessage
                {
                    ChatRole = ChatRole.User,
                    Content = graphRagData
                }
            };
        }
        
        var result = await ChatWithHistory(prompt, history);
        return result?[0].Content;
    }
    
    private async Task<List<QueryResult>> QueryAsync(string cypherQuery)
    {
        try
        {
            await using var session = _driver.AsyncSession();
            return await session.ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync(cypherQuery);
                return await result.ToListAsync(r => new QueryResult
                {
                    Data = r.Values.ToDictionary(
                        k => k.Key,
                        v => (object)v.Value)
                });
            });
        }
        catch (ClientException e)
        {
            _logger.LogError("Client error executing Cypher {query}, msg: {msg}, code: {code}", 
                cypherQuery, e.Message, e.Code);
            return new List<QueryResult>();
        }
        catch (AuthenticationException e)
        {
            _logger.LogError("Authentication error executing Cypher {query}, msg: {msg}, code: {code}", 
                cypherQuery, e.Message, e.Code);
            return new List<QueryResult>();
        }
        catch (Exception e)
        {
            _logger.LogError("Error executing Cypher {query}, msg: {msg} ", cypherQuery, e.Message);
            return new List<QueryResult>();
        }
    }
    
    private async Task<string> GraphRagDataAsync(string text)
    {
        _logger.LogDebug("GraphRagDataAsync, text {text}", text);
        var prompt = Prompts.Text2CypherTemplate
            .Replace("{schema}", State.RetrievalSchema)
            .Replace("{examples}", State.RetrievalExample)
            .Replace("{query_text}", text);
        
        var response = await ChatWithHistory(prompt);
        
        if (response.IsNullOrEmpty())
        {
            return string.Empty;
        }
        
        var cypher = response?[0].Content;

        if (cypher.IsNullOrEmpty())
        {
            Logger.LogError("Cannot generate cypher from text {text}.", text);
            return string.Empty;
        }
        
        cypher = Regex.Replace(cypher, @"<think>.*?</think>", "", RegexOptions.Singleline)
            .TrimStart('\r', '\n')
            .TrimEnd('\r', '\n'); ;
        
        _logger.LogDebug("GraphRagDataAsync, get cypher {cypher} from text {text}", cypher, text);
        var result = await QueryAsync(cypher);
        if (!result.Any())
        {
            Logger.LogError("query null for cypher: {cypher}.", cypher);
            return string.Empty;
        }
        
        var resp = result.ToNaturalLanguage();
        Logger.LogDebug("GraphRagDataAsync, get result {result} from cypher {cypher} .", resp, cypher);
        return resp;
    }
    
    protected override void AIGAgentTransitionState(GraphRetrievalAgentState state,
        StateLogEventBase<GraphRetrievalAgentSEvent> @event)
    {
        switch (@event)
        {
            case SetGraphSchemaSEvent setGraphSchemaSEvent:
                state.RetrievalSchema = setGraphSchemaSEvent.Schema;
                state.RetrievalExample = setGraphSchemaSEvent.Example;
                break;
        }
    }
    
}