using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using MediatR;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS;

public class AevatarStateProjector : IStateProjector
{
    private readonly IMediator _mediator;
    private readonly ILogger<AevatarStateProjector> _logger;

    public AevatarStateProjector(IMediator mediator, ILogger<AevatarStateProjector> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        if (state.GetType().IsGenericType &&
            state.GetType().GetGenericTypeDefinition() == typeof(StateWrapper<>) &&
            typeof(StateBase).IsAssignableFrom(state.GetType().GetGenericArguments()[0]))
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;

            var command = new SaveStateCommand
            {
                Id = grainId.GetGuidKey().ToString(),
                State = wrapperState
            };
            _ = _mediator.Send(command)
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _logger.LogError(task.Exception, "_mediator sender error");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid state type: {state.GetType().Name}. Expected StateWrapper<T> where T : StateBase.");
        }
    }
}