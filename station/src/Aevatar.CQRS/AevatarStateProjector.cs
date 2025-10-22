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
    private readonly ConcurrentDictionary<string, SaveStateCommandPlus> _latestCommandsPlus = new();
    private readonly IMediator _mediator;
    private readonly ILogger<AevatarStateProjector> _logger;
    private readonly ProjectorBatchOptions _batchOptions;
    private readonly CancellationTokenSource _shutdownCts = new();
    private int _isProcessing;
    private int _isProcessingPlus;
    private bool _disposed;
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private DateTime _lastFlushTimePlus = DateTime.UtcNow;
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

        // Check if it's a Plus wrapper
        if (IsValidStatePlusWrapper(state))
        {
            return ProjectPlusAsync(state);
        }

        // Otherwise, process as legacy StateWrapper
        if (!IsValidStateWrapper(state))
        {
            _logger.LogDebug("Invalid state wrapper type: {StateType}", state?.GetType().Name);
            return Task.CompletedTask;
        }

        try
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;
            int version = wrapper.Version;
            
            _logger.LogDebug("AevatarStateProjector (Legacy) GrainId {GrainId} Version {Version}", grainId.ToString(), version);
            
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
            _logger.LogError(ex, "Error in ProjectAsync (Legacy) for state type {StateType}", state?.GetType().Name);
        }

        return Task.CompletedTask;
    }

    private Task ProjectPlusAsync<T>(T state) where T : StateWrapperBase
    {
        try
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            CoreStateBase wrapperState = wrapper.State;
            int version = wrapper.Version;
            
            _logger.LogDebug("AevatarStateProjector (Plus) GrainId {GrainId} Version {Version}", grainId.ToString(), version);
            
            var command = new SaveStateCommandPlus
            {
                Id = grainId.ToString(),
                GuidKey = grainId.GetGuidKey().ToString(),
                State = wrapperState,
                Version = version,
                Timestamp = DateTime.UtcNow
            };

            // 更新命令集合
            _latestCommandsPlus.AddOrUpdate(
                command.Id,
                _ => command,
                (_, existing) => command.Version > existing.Version ? command : existing
            );

            // 检查是否需要执行刷新操作
            var shouldFlush = _latestCommandsPlus.Count >= _batchOptions.BatchSize || 
                              (DateTime.UtcNow - _lastFlushTimePlus).TotalSeconds >= _batchOptions.BatchTimeoutSeconds;
            
            if (shouldFlush && Interlocked.CompareExchange(ref _isProcessingPlus, 1, 0) == 0)
            {
                // 非阻塞方式执行刷新
                return FlushInternalPlusAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProjectPlusAsync for state type {StateType}", state?.GetType().Name);
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
                    if (_latestCommands.TryGetValue(cmd.Id, out var curCmd))
                    {
                        if (curCmd.Version == cmd.Version)
                        {
                            _latestCommands.TryRemove(cmd.Id, out _);
                        }
                    }
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

    /// <summary>
    /// 执行内部刷新操作（Plus版本），并重置状态
    /// </summary>
    private async Task FlushInternalPlusAsync()
    {
        try
        {
            await FlushPlusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during flush Plus operation");
        }
        finally
        {
            // 无论成功失败都要重置处理状态
            Interlocked.Exchange(ref _isProcessingPlus, 0);
            _lastFlushTimePlus = DateTime.UtcNow;
        }
    }

    public async Task FlushPlusAsync(CancellationToken cancellationToken = default)
    {
        if (_latestCommandsPlus.IsEmpty)
        {
            return;
        }

        try
        {
            // 计算批处理大小
            int effectiveBatchSize = CalculateEffectiveBatchSize();
            
            // 获取批处理数据
            var currentBatch = _latestCommandsPlus.Values
                .OrderByDescending(c => c.Version)
                .ThenByDescending(c => c.Timestamp)
                .Take(effectiveBatchSize)
                .ToList();

            if (currentBatch.Count > 0)
            {
                _logger.LogInformation("Processing Plus batch: {BatchSize} commands (total pending: {TotalCount})", 
                    currentBatch.Count, _latestCommandsPlus.Count);
                    
                await SendBatchPlusAsync(currentBatch, cancellationToken);
                
                // 处理完成后移除已处理的命令
                foreach (var cmd in currentBatch)
                {
                    if (_latestCommandsPlus.TryGetValue(cmd.Id, out var curCmd))
                    {
                        if (curCmd.Version == cmd.Version)
                        {
                            _latestCommandsPlus.TryRemove(cmd.Id, out _);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plus batch processing failed");
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
        if (!state.GetType().IsGenericType)
        {
            return false;
        }

        var genericType = state.GetType().GetGenericTypeDefinition();
        var argType = state.GetType().GetGenericArguments()[0];
        
        // Only validate legacy StateWrapper<StateBase>
        return genericType == typeof(StateWrapper<>) && typeof(StateBase).IsAssignableFrom(argType);
    }

    private bool IsValidStatePlusWrapper<T>(T state) where T : StateWrapperBase
    {
        if (!state.GetType().IsGenericType)
        {
            return false;
        }

        var genericType = state.GetType().GetGenericTypeDefinition();
        var argType = state.GetType().GetGenericArguments()[0];
        
        // Only validate Plus StateWrapperPlus<CoreStateBase>
        return genericType == typeof(StateWrapperPlus<>) && typeof(CoreStateBase).IsAssignableFrom(argType);
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

    private async Task SendBatchPlusAsync(List<SaveStateCommandPlus> batch, CancellationToken cancellationToken)
    {
        int retryCount = 0;

        while (retryCount < _batchOptions.MaxRetryCount && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var batchCommand = new SaveStateBatchCommandPlus
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
                    _logger.LogError(ex, "Failed to process Plus batch after {RetryCount} attempts", retryCount);
                    throw; // 达到最大重试次数，向上抛出异常
                }
                
                _logger.LogWarning(ex, "Error processing Plus batch, will retry ({RetryCount}/{MaxRetries})", 
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
        
        // 尝试执行最后一次刷新 (Legacy)
        if (_latestCommands.Count > 0 && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
        {
            try
            {
                FlushAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during final legacy flush on dispose");
            }
        }
        
        // 尝试执行最后一次刷新 (Plus)
        if (_latestCommandsPlus.Count > 0 && Interlocked.CompareExchange(ref _isProcessingPlus, 1, 0) == 0)
        {
            try
            {
                FlushPlusAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during final Plus flush on dispose");
            }
        }
        
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        _flushTimer?.Dispose();
    }

    private void FlushTimerCallback(object? state)
    {
        if (_disposed) return;
        
        // Flush Legacy commands
        if (!_latestCommands.IsEmpty && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
        {
            try
            {
                // Non-blocking flush execution
                _ = FlushInternalAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Timer] Exception in Legacy FlushTimerCallback");
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        }
        
        // Flush Plus commands
        if (!_latestCommandsPlus.IsEmpty && Interlocked.CompareExchange(ref _isProcessingPlus, 1, 0) == 0)
        {
            try
            {
                // Non-blocking flush execution
                _ = FlushInternalPlusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Timer] Exception in Plus FlushTimerCallback");
                Interlocked.Exchange(ref _isProcessingPlus, 0);
            }
        }
    }
}