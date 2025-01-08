using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using MediatR;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS.Provider;

public class CQRSProvider : ICQRSProvider, ISingletonDependency
{
    private readonly IMediator _mediator;
    public CQRSProvider(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task PublishAsync(StateBase state, string id)
    {
        var command = new SaveStateCommand
        {
            Id = id,
            State = state
        };
        await _mediator.Send(command);
    }

    public async Task<BaseStateIndex> QueryAsync(string index, string id)
    {
        var getStateQuery = new GetStateQuery()
        {
            Index = index,
            Id = id
        };
        
        var state = await _mediator.Send(getStateQuery);
        return state;
    }

    public async Task SendEventCommandAsync(EventBase eventBase)
    {
        var command = new SendEventCommand
        {
            Event = eventBase
        };
        await _mediator.Send(command);
    }

    public async Task<string> QueryGEventAsync(string eventId,  List<string> grainIds, int pageNumber, int pageSize)
    {
        Func<QueryContainerDescriptor<AgentGEventIndex>, QueryContainer> query;
        if (eventId.IsNullOrEmpty())
        {
            query = q => q.Bool(b => b
                .Must(m => m
                    .Term(f => f.Id, eventId)
                )
            );
        }
        else
        {
            query = q => q.Bool(b => b
                .Must(m => m
                    .Terms(t => t.Field(f => f.GrainId).Terms(grainIds))
                )
            );
        }
        
        Func<SortDescriptor<AgentGEventIndex>, IPromise<IList<ISort>>> sort = s => s
            .Ascending(f => f.Ctime);

        var getStateQuery = new GetGEventQuery()
        {
            Query = query,
            Sort = sort,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        
        var documentContent = await _mediator.Send(getStateQuery);
        return documentContent;
    }

    public async Task PublishAsync(Guid eventId, Guid GrainId, string GrainType, GEventBase eventBase)
    {
        var agentGEventIndex = new AgentGEventIndex()
        {
            Id = eventId,
            GrainId = GrainId,
            GrainType = GrainType,
            Ctime = DateTime.UtcNow,
            EventJson = JsonConvert.SerializeObject(eventBase)
        };
        
        var command = new SaveGEventCommand
        {
            Id = eventId==null?Guid.NewGuid():eventId,
            AgentGEventIndex = agentGEventIndex
        };
        await _mediator.Send(command);
    }
    
    public Task PublishAsync(GEventBase eventBase, string id)
    {
        throw new NotImplementedException();
    }
}