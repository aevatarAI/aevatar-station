using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using MediatR;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS.Provider;

public class CQRSProvider : ICQRSProvider, ISingletonDependency
{
    private readonly IMediator _mediator;

    public CQRSProvider(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task PublishAsync(StateBase state, string grainId)
    {
        throw new NotImplementedException();
    }

    public async Task PublishAsync(StateBase state, GrainId grainId)
    {
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

    public async Task<string> QueryAgentStateAsync(string indexName, Guid primaryKey)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>
        {
            q => q.Term(i =>
                i.Field("_id").Value(primaryKey))
        };
        
        QueryContainer Filter(QueryContainerDescriptor<dynamic> f) => f.Bool(b => b.Must(mustQuery));
        
        var getStateQuery = new GetStateQuery()
        {
            Index = indexName,
            Query = Filter,
            Skip = 0,
            Limit = 1
        };
    
        var document = await _mediator.Send(getStateQuery);
        return document;
    }
    

    public async Task PublishAsync(Guid eventId, GrainId grainId, StateLogEventBase eventBase)
    {
        var agentGrainId = Guid.Parse(grainId.Key.ToString());
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
    
}