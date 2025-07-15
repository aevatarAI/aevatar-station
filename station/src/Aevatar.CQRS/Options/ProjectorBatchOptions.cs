namespace Aevatar.Options;

public class ProjectorBatchOptions
{
    /// <summary>
    /// 默认批处理大小
    /// </summary>
    public int BatchSize { get; set; } = 15;
    
    /// <summary>
    /// 批处理超时时间（秒）
    /// </summary>
    public int BatchTimeoutSeconds { get; set; } = 1;
    
    /// <summary>
    /// 最大批处理大小（当队列积压时可用）
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;
    
    /// <summary>
    /// 最小批处理大小（即使在内存压力大时也保证的处理数量）
    /// </summary>
    public int MinBatchSize { get; set; } = 5;
    
    /// <summary>
    /// 高内存水位线，超过此值时将减小批大小（字节）
    /// </summary>
    public long HighMemoryThreshold { get; set; } = 1024 * 1024 * 1024; // 1GB
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
    
    /// <summary>
    /// 重试基础延迟（秒）
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 2;
    
    /// <summary>
    /// 最大重试延迟（秒）
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Minimum flush period (ms)
    /// </summary>
    public int FlushMinPeriodInMs { get; set; } = 1000;
}