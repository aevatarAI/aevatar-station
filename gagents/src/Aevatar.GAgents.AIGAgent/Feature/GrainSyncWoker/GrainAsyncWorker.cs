using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.SyncWork;
using Orleans.SyncWork.Enums;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public abstract class GrainAsyncWorker<TRequest, TResponse> : SyncWorker<TRequest, TResponse>,
    IGrainAsyncWorker<TRequest, TResponse>
{
    private IGrainAsyncHandler<TResponse> _grainAsyncHandler;
    private readonly ILogger<GrainAsyncWorker<TRequest, TResponse>> _logger;

    public GrainAsyncWorker(ILogger<GrainAsyncWorker<TRequest, TResponse>> logger,
        LimitedConcurrencyLevelTaskScheduler limitedConcurrencyScheduler) : base(logger, limitedConcurrencyScheduler)
    {
        _logger = logger;
    }

    protected override async Task<TResponse> PerformWork(TRequest request,
        GrainCancellationToken grainCancellationToken)
    {
        _logger.LogDebug($"[StreamAsyncWorker] Performing long run task for request of type {typeof(TRequest).FullName}: {request}");
        try
        {
            var response = await PerformLongRunTask(_grainAsyncHandler, request);
            await _grainAsyncHandler.HandleStreamAsync(response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[StreamAsyncWorker] Error performing long run task: {ex.Message}");
            throw;
        }
    }

    protected abstract Task<TResponse> PerformLongRunTask(IGrainAsyncHandler<TResponse> grainAsyncHandler, TRequest request);

    public Task SetLongRunTaskAsync(GrainId grainId)
    {
        _grainAsyncHandler = GrainFactory.GetGrain<IGrainAsyncHandler<TResponse>>(grainId);
        return Task.CompletedTask;
    }
}