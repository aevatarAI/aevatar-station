using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS.Provider;

public class CQRSProvider : ICQRSProvider, ISingletonDependency
{
    private readonly IMediator _mediator;
    private readonly ILogger<CQRSProvider> _logger;
    private readonly IOptions<HostOptions> _options;
    private string _projectName = null;

    public CQRSProvider(IMediator mediator, ILogger<CQRSProvider> logger, IOptions<HostOptions> hostOptions)
    {
        _mediator = mediator;
        _logger = logger;
        _options = hostOptions;
    }

    public Task PublishAsync(string grainId, StateBase state)
    {
        throw new NotImplementedException();
    }

    public async Task PublishAsync(GrainId grainId, StateBase state)
    {
        _logger.LogInformation("CQRSProvider Publish State grainId:{grainId}", grainId);
        var command = new SaveStateCommand
        {
            Id = grainId.GetGuidKey().ToString(),
            State = state
        };
        await _mediator.Send(command);
    }

    public async Task<string> QueryStateAsync(string indexName,
        Func<QueryContainerDescriptor<dynamic>, QueryContainer> query, int skip, int limit)
    {
        _logger.LogInformation("CQRSProvider QueryStateAsync indexName:{indexName}", indexName);
        var getStateQuery = new GetStateQuery()
        {
            Index = indexName,
            Query = query,
            Skip = skip,
            Limit = limit
        };

        var document = await _mediator.Send(getStateQuery);
        return document;
    }

    public async Task<Tuple<long, List<AgentGEventIndex>>> QueryGEventAsync(string eventId, List<string> grainIds,
        int pageNumber, int pageSize)
    {
        _logger.LogInformation("CQRSProvider QueryGEventAsync eventId:{eventId}, grainIds:{grainIds}", eventId,
            grainIds);
        var mustQuery = new List<Func<QueryContainerDescriptor<AgentGEventIndex>, QueryContainer>>();
        if (!eventId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Id).Value(eventId)));
        }

        if (!grainIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.AgentPrimaryKey).Terms(grainIds)));
        }

        QueryContainer Filter(QueryContainerDescriptor<AgentGEventIndex> f) => f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<AgentGEventIndex>, IPromise<IList<ISort>>>(s =>
            s.Ascending(t => t.Ctime));

        var getStateQuery = new GetGEventQuery()
        {
            Query = Filter,
            Sort = sorting,
            Skip = (pageNumber - 1) * pageSize,
            Limit = pageSize
        };

        var tuple = await _mediator.Send(getStateQuery);
        return tuple;
    }

    public async Task<Tuple<long, List<AgentGEventIndex>>> QueryAgentGEventAsync(Guid? primaryKey, string agentType,
        int pageNumber, int pageSize)
    {
        _logger.LogInformation("CQRSProvider QueryAgentGEventAsync primaryKey:{primaryKey}, agentType:{agentType}",
            primaryKey, agentType);
        var mustQuery = new List<Func<QueryContainerDescriptor<AgentGEventIndex>, QueryContainer>>();

        if (primaryKey != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.AgentPrimaryKey).Value(primaryKey)));
        }

        if (!agentType.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.AgentGrainType).Value(agentType)));
        }

        QueryContainer Filter(QueryContainerDescriptor<AgentGEventIndex> f) => f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<AgentGEventIndex>, IPromise<IList<ISort>>>(s =>
            s.Ascending(t => t.Ctime));

        var getStateQuery = new GetGEventQuery()
        {
            Query = Filter,
            Sort = sorting,
            Skip = pageNumber * pageSize,
            Limit = pageSize
        };

        var tuple = await _mediator.Send(getStateQuery);
        return tuple;
    }

    public async Task<string> QueryAgentStateAsync(string stateName, Guid primaryKey)
    {
        _logger.LogInformation("CQRSProvider QueryAgentStateAsync stateName:{stateName}, primaryKey:{primaryKey}",
            stateName, primaryKey);
        var mustQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>
        {
            q => q.Term(i =>
                i.Field("_id").Value(primaryKey.ToString().Replace("-", "")))
        };

        QueryContainer Filter(QueryContainerDescriptor<dynamic> f) => f.Bool(b => b.Must(mustQuery));

        var getStateQuery = new GetStateQuery()
        {
            Index = GetIndexName(stateName),
            Query = Filter,
            Skip = 0,
            Limit = 1
        };

        var document = await _mediator.Send(getStateQuery);
        return document;
    }

    public async Task<Tuple<long, List<TargetT>>> GetUserInstanceAgent<SourceT, TargetT>(Guid userId, int pageIndex,
        int pageSize)
    {
        _logger.LogInformation("CQRSProvider query user instance agents,UserId:{userId}", userId);
        var mustQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>
        {
            q => q.Term(i =>
                i.Field("userId").Value(userId.ToString()))
        };

        var index = GetIndexName(typeof(SourceT).Name.ToLower());
        QueryContainer Filter(QueryContainerDescriptor<dynamic> f) => f.Bool(b => b.Must(mustQuery));
        var queryResponse = await _mediator.Send(new GetUserInstanceAgentsQuery()
        {
            Index = index,
            Skip = pageIndex * pageSize,
            Query = Filter,
            Limit = pageSize,
        });

        if (queryResponse.Item2.IsNullOrWhiteSpace())
        {
            return new Tuple<long, List<TargetT>>(0, new List<TargetT>());
        }

        var documentList = JsonConvert.DeserializeObject<List<TargetT>>(queryResponse.Item2);
        if (documentList != null)
        {
            return new Tuple<long, List<TargetT>>(queryResponse.Item1, documentList);
        }

        _logger.LogWarning(
            "CQRSProvider query user instance agents documentList == null, UserId:{userId}, document string:{documents}",
            userId, queryResponse.Item2);

        return new Tuple<long, List<TargetT>>(0, new List<TargetT>());
    }


    public async Task PublishAsync(Guid eventId, GrainId grainId, StateLogEventBase eventBase)
    {
        _logger.LogInformation("CQRSProvider Publish event grainId:{grainId}", grainId);
        var grainType = grainId.Type;
        if (eventId == Guid.Empty)
        {
            eventId = Guid.NewGuid();
        }

        if (eventBase.Ctime == DateTime.MinValue)
        {
            eventBase.Ctime = DateTime.UtcNow;
        }

        if (eventBase.Id == Guid.Empty)
        {
            eventBase.Id = eventId;
        }

        var agentGEventIndex = new AgentGEventIndex()
        {
            Id = eventId,
            AgentPrimaryKey = grainId.GetGuidKey(),
            AgentGrainType = grainType.ToString(),
            Ctime = DateTime.UtcNow,
            EventJson = JsonConvert.SerializeObject(eventBase),
            EventName = eventBase.GetType().Name
        };
        
        var command = new SaveGEventCommand
        {
            Id = eventId == null ? Guid.NewGuid() : eventId,
            AgentGEventIndex = agentGEventIndex
        };
        await _mediator.Send(command);
    }

    public Task PublishAsync(Guid eventId, string grainId, StateLogEventBase eventBase)
    {
        throw new NotImplementedException();
    }

    public string GetIndexName(string index)
    {
        return $"{CqrsConstant.IndexPrefix}-{_options.Value.HostId}-{index}{CqrsConstant.IndexSuffix}".ToLower();
    }

    public Task PublishAsync<TState>(GrainId grainId, TState state) where TState : StateBase
    {
        throw new NotImplementedException();
    }

    public Task PublishAsync<TStateLogEvent>(Guid eventId, GrainId grainId, TStateLogEvent stateLogEvent)
        where TStateLogEvent : StateLogEventBase
    {
        throw new NotImplementedException();
    }
}