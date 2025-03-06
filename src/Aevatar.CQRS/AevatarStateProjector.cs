using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using MediatR;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS;

public class AevatarStateProjector: IStateProjector, ISingletonDependency
{
    
    private readonly IMediator _mediator;
    private readonly ILogger<CQRSProvider> _logger;
    
    public AevatarStateProjector(IMediator mediator, ILogger<CQRSProvider> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    public async Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        var wrapper = state as StateWrapper<StateBase>;
        var grainId = wrapper.GrainId;
        var wrapperState = wrapper.State;
        var command = new SaveStateCommand
        {
            Id = grainId.GetGuidKey().ToString(),
            State = wrapperState
        };
        await _mediator.Send(command);
    }
}