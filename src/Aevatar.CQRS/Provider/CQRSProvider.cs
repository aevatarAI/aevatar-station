using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using MediatR;
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

    public async Task<string> QueryGEventAsync(string index, string id)
    {
        var getStateQuery = new GetGEventQuery()
        {
            Index = index,
            Id = id
        };
        
        var documentContent = await _mediator.Send(getStateQuery);
        return documentContent;
    }

    public async Task PublishAsync(GEventBase eventBase, string id)
    {
        var command = new SaveGEventCommand
        {
            Id = id,
            GEvent = eventBase
        };
        await _mediator.Send(command);
    }
}