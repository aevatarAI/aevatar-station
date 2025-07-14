using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
<<<<<<< HEAD
=======
using System.Threading;
>>>>>>> origin/dev
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Dto;
using Aevatar.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

<<<<<<< HEAD
public class AevatarStateProjector : IStateProjector, ISingletonDependency
=======
public class AevatarStateProjector : IStateProjector, ISingletonDependency, IDisposable
>>>>>>> origin/dev
{
    private readonly ConcurrentDictionary<string, SaveStateCommand> _latestCommands = new();
    private readonly IMediator _mediator;
    private readonly ILogger<AevatarStateProjector> _logger;
    private readonly ProjectorBatchOptions _batchOptions;
<<<<<<< HEAD
=======
    private readonly CancellationTokenSource _shutdownCts = new();
    private int _isProcessing;
    private bool _disposed;
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private System.Threading.Timer _flushTimer;
>>>>>>> origin/dev

    public AevatarStateProjector(
        IMediator mediator,
        ILogger<AevatarStateProjector> logger,
        IOptionsSnapshot<ProjectorBatchOptions> options)
    {
        _mediator = mediator;
        _logger = logger;
        _batchOptions = options.Value;
<<<<<<< HEAD
        _ = ProcessCommandsAsync();
=======
        // Initialize timer
        int timerPeriodMs = Math.Max(_batchOptions.FlushMinPeriodInMs, (int)(_batchOptions.BatchTimeoutSeconds * _batchOptions.FlushMinPeriodInMs / 2));
        _flushTimer = new System.Threading.Timer(FlushTimerCallback, null, timerPeriodMs, timerPeriodMs);
>>>>>>> origin/dev
    }

    public Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
<<<<<<< HEAD
        if (IsValidStateWrapper(state))
=======
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
>>>>>>> origin/dev
        {
            dynamic wrapper = state;
            GrainId grainId = wrapper.GrainId;
            StateBase wrapperState = wrapper.State;
            int version = wrapper.Version;
<<<<<<< HEAD
            _logger.LogDebug("AevatarStateProjector GrainId {GrainId} Version {Version}", grainId.ToString(), version);
=======
            
            _logger.LogDebug("AevatarStateProjector GrainId {GrainId} Version {Version}", grainId.ToString(), version);
            
>>>>>>> origin/dev
            var command = new SaveStateCommand
            {
                Id = grainId.ToString(),
                GuidKey = grainId.GetGuidKey().ToString(),
                State = wrapperState,
<<<<<<< HEAD
                Version = version
            };

            _latestCommands.AddOrUpdate(
                command.Id,
                command,
                (id, existing) => command.Version > existing.Version ? command : existing
            );
=======
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
>>>>>>> origin/dev
        }

        return Task.CompletedTask;
    }

<<<<<<< HEAD
    private async Task ProcessCommandsAsync()
    {
        while (true)
        {
            await FlushAsync();
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            if (_latestCommands.Count < _batchOptions.BatchSize)
            {
                await Task.Delay(_batchOptions.BatchTimeoutSeconds * 1000);
            }

            var currentBatch = _latestCommands.Values
                .OrderByDescending(c => c.Version)
                .Take(_batchOptions.BatchSize)
=======
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
>>>>>>> origin/dev
                .ToList();

            if (currentBatch.Count > 0)
            {
<<<<<<< HEAD
                _logger.LogInformation("latestCommands count :{Count} ", _latestCommands.Count);
                await SendBatchAsync(currentBatch);
                CleanProcessedCommands(currentBatch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch processing failed");
        }
    }

    private void CleanProcessedCommands(List<SaveStateCommand> processed)
    {
        foreach (var cmd in processed)
        {
            _latestCommands.TryRemove(cmd.Id, out var current);
            if (current != null && current.Version > cmd.Version)
            {
                _latestCommands.AddOrUpdate(
                    current.Id,
                    current,
                    (id, existing) => current.Version > existing.Version ? current : existing
                );
            }
        }
    }


=======
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

>>>>>>> origin/dev
    private bool IsValidStateWrapper<T>(T state) where T : StateWrapperBase
    {
        return state.GetType().IsGenericType &&
               state.GetType().GetGenericTypeDefinition() == typeof(StateWrapper<>) &&
               typeof(StateBase).IsAssignableFrom(state.GetType().GetGenericArguments()[0]);
    }

<<<<<<< HEAD
    private async Task SendBatchAsync(List<SaveStateCommand> batch)
    {
        const int maxRetries = 3;
        int retryCount = 0;
        List<SaveStateCommand> remainingCommands = new(batch);

        while (retryCount < maxRetries && remainingCommands.Count > 0)
        {
            try
            {
                var batchCommand = new SaveStateBatchCommand { Commands = remainingCommands };
                await _mediator.Send(batchCommand);
                _logger.LogInformation("Successfully sent {Count} commands", remainingCommands.Count);
                return;
=======
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
>>>>>>> origin/dev
            }
            catch (Exception ex)
            {
                retryCount++;
<<<<<<< HEAD
                _logger.LogWarning(ex, "Batch send failed (Attempt {RetryCount}/{MaxRetries})", retryCount, maxRetries);

                remainingCommands = GetValidCommands(remainingCommands);

                if (remainingCommands.Count == 0)
                {
                    _logger.LogInformation("All commands expired or succeeded");
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }

        _logger.LogError("Failed to send {Count} commands after {Retries} retries", remainingCommands.Count,
            maxRetries);
    }

    private List<SaveStateCommand> GetValidCommands(List<SaveStateCommand> commands)
    {
        var validCommands = new List<SaveStateCommand>();
        foreach (var cmd in commands)
        {
            if (_latestCommands.TryGetValue(cmd.Id, out var latest) && latest.Version == cmd.Version)
            {
                validCommands.Add(cmd);
            }
            else
            {
                _logger.LogDebug("Command {Id} v{Version} expired, latest is v{LatestVersion}",
                    cmd.Id, cmd.Version, latest?.Version ?? -1);
            }
        }

        return validCommands;
=======
                
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
>>>>>>> origin/dev
    }
}