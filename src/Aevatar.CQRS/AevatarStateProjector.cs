using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

public class AevatarStateProjector : IStateProjector, ISingletonDependency, IDisposable
{
    private readonly ConcurrentDictionary<string, SaveStateCommand> _latestCommands = new();
    private readonly IMediator _mediator;
    private readonly ILogger<AevatarStateProjector> _logger;
    private readonly ProjectorBatchOptions _batchOptions;
    private readonly CancellationTokenSource _shutdownCts = new();
    private int _isProcessing;
    private bool _disposed;
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private System.Threading.Timer _flushTimer;

    public AevatarStateProjector(
        IMediator mediator,
        ILogger<AevatarStateProjector> logger,
        IOptionsSnapshot<ProjectorBatchOptions> options)
    {
        _mediator = mediator;
        _logger = logger;
        _batchOptions = options.Value;
        // Initialize timer
        int timerPeriodMs = Math.Max(_batchOptions.FlushMinPeriodInMs, (int)(_batchOptions.BatchTimeoutSeconds * _batchOptions.FlushMinPeriodInMs / 2));
        _flushTimer = new System.Threading.Timer(FlushTimerCallback, null, timerPeriodMs, timerPeriodMs);
    }

    public Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        if (_disposed)
        {
            _logger.LogWarning("ProjectAsync called after disposal");
            return Task.CompletedTask;
        }

        if (!IsValidStateWrapper(state))
        {
            return Task.CompletedTask;
        }

        try
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;
            int version = wrapper.Version;
            
            _logger.LogDebug("AevatarStateProjector GrainId {GrainId} Version {Version}", grainId.ToString(), version);
            
            var command = new SaveStateCommand
            {
                Id = grainId.ToString(),
                GuidKey = grainId.GetGuidKey().ToString(),
                State = wrapperState,
                Version = version,
                Timestamp = DateTime.UtcNow
            };

            // 更新命令集合
            _latestCommands.AddOrUpdate(
                command.Id,
                _ => command,
                (_, existing) => command.Version > existing.Version ? command : existing
            );

            // 检查是否需要执行刷新操作
            var shouldFlush = _latestCommands.Count >= _batchOptions.BatchSize || 
                              (DateTime.UtcNow - _lastFlushTime).TotalSeconds >= _batchOptions.BatchTimeoutSeconds;
            
            if (shouldFlush && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
            {
                // 非阻塞方式执行刷新
                return FlushInternalAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProjectAsync");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行内部刷新操作，并重置状态
    /// </summary>
    private async Task FlushInternalAsync()
    {
        try
        {
            await FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during flush operation");
        }
        finally
        {
            // 无论成功失败都要重置处理状态
            Interlocked.Exchange(ref _isProcessing, 0);
            _lastFlushTime = DateTime.UtcNow;
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_latestCommands.IsEmpty)
        {
            return;
        }

        try
        {
            // 计算批处理大小
            int effectiveBatchSize = CalculateEffectiveBatchSize();
            
            // 获取批处理数据
            var currentBatch = _latestCommands.Values
                .OrderByDescending(c => c.Version)
                .ThenByDescending(c => c.Timestamp)
                .Take(effectiveBatchSize)
                .ToList();

            if (currentBatch.Count > 0)
            {
                _logger.LogInformation("Processing batch: {BatchSize} commands (total pending: {TotalCount})", 
                    currentBatch.Count, _latestCommands.Count);
                    
                await SendBatchAsync(currentBatch, cancellationToken);
                
                // 处理完成后移除已处理的命令
                foreach (var cmd in currentBatch)
                {
                    _latestCommands.TryRemove(cmd.Id, out _);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch processing failed");
            throw;
        }
    }

    private int CalculateEffectiveBatchSize()
    {
        // 默认使用配置的批大小
        int size = _batchOptions.BatchSize;
        
        // 如果队列较大，增加批大小以加快处理
        if (_latestCommands.Count > _batchOptions.BatchSize * 5)
        {
            size = Math.Min(_batchOptions.BatchSize * 2, _batchOptions.MaxBatchSize);
        }
        
        // 检查内存压力
        if (GC.GetTotalMemory(false) > _batchOptions.HighMemoryThreshold)
        {
            size = Math.Max(_batchOptions.MinBatchSize, size / 2);
        }
        
        return size;
    }

    private bool IsValidStateWrapper<T>(T state) where T : StateWrapperBase
    {
        return state.GetType().IsGenericType &&
               state.GetType().GetGenericTypeDefinition() == typeof(StateWrapper<>) &&
               typeof(StateBase).IsAssignableFrom(state.GetType().GetGenericArguments()[0]);
    }

    private async Task SendBatchAsync(List<SaveStateCommand> batch, CancellationToken cancellationToken)
    {
        int retryCount = 0;

        while (retryCount < _batchOptions.MaxRetryCount && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var batchCommand = new SaveStateBatchCommand
                {
                    Commands = batch
                };
                
                await _mediator.Send(batchCommand, cancellationToken);
                return; // 成功发送后直接返回
            }
            catch (Exception ex)
            {
                retryCount++;
                
                if (retryCount >= _batchOptions.MaxRetryCount)
                {
                    _logger.LogError(ex, "Failed to process batch after {RetryCount} attempts", retryCount);
                    throw; // 达到最大重试次数，向上抛出异常
                }
                
                _logger.LogWarning(ex, "Error processing batch, will retry ({RetryCount}/{MaxRetries})", 
                    retryCount, _batchOptions.MaxRetryCount);
                
                // 指数退避策略
                int delayMs = (int)(_batchOptions.RetryBaseDelaySeconds * 1000 * Math.Pow(2, retryCount - 1));
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // 尝试执行最后一次刷新
        if (_latestCommands.Count > 0 && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
        {
            try
            {
                FlushAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during final flush on dispose");
            }
        }
        
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        _flushTimer?.Dispose();
    }

    private void FlushTimerCallback(object? state)
    {
        if (_disposed) return;
        if (_latestCommands.IsEmpty) return;
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0) return;
        try
        {
            // Non-blocking flush execution
            _ = FlushInternalAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Timer] Exception in FlushTimerCallback");
            Interlocked.Exchange(ref _isProcessing, 0);
        }
    }
}