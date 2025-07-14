using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

{
    private readonly ConcurrentDictionary<string, SaveStateCommand> _latestCommands = new();
    private readonly IMediator _mediator;
    private readonly ILogger<AevatarStateProjector> _logger;
    private readonly ProjectorBatchOptions _batchOptions;

    public AevatarStateProjector(
        IMediator mediator,
        ILogger<AevatarStateProjector> logger,
        IOptionsSnapshot<ProjectorBatchOptions> options)
    {
        _mediator = mediator;
        _logger = logger;
        _batchOptions = options.Value;
    }

    public Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;
            int version = wrapper.Version;
            var command = new SaveStateCommand
            {
                Id = grainId.ToString(),
                GuidKey = grainId.GetGuidKey().ToString(),
                State = wrapperState,
        }

        return Task.CompletedTask;
    }

                .ToList();

            if (currentBatch.Count > 0)
            {
    private bool IsValidStateWrapper<T>(T state) where T : StateWrapperBase
    {
        return state.GetType().IsGenericType &&
               state.GetType().GetGenericTypeDefinition() == typeof(StateWrapper<>) &&
               typeof(StateBase).IsAssignableFrom(state.GetType().GetGenericArguments()[0]);
    }

            }
            catch (Exception ex)
            {
                retryCount++;
    }
}