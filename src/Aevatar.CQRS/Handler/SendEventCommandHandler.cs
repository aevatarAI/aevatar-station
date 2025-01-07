using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Agents;
using Aevatar.Agents.Group;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Sender;
using MediatR;
using Orleans;

namespace Aevatar.CQRS.Handler;

public class SendEventCommandHandler : IRequestHandler<SendEventCommand>
{
    private readonly IGrainFactory _clusterClient;

    public SendEventCommandHandler(IGrainFactory clusterClient
    )
    {
        _clusterClient = clusterClient;
    }

    public async Task<Unit> Handle(SendEventCommand request, CancellationToken cancellationToken)
    {
        var publishingAgent = _clusterClient.GetGrain<IPublishingGAgent>(Guid.NewGuid());
        var groupAgent = _clusterClient.GetGrain<IStateGAgent<GroupAgentState>>(Guid.NewGuid());

        await publishingAgent.RegisterAsync(groupAgent);
        await publishingAgent.PublishEventAsync(request.Event);
        return Unit.Value; 
    }
    
}