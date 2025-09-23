using Aevatar.Developer.Logger.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Developer.Logger;

public class LogElasticSearchService : ILogService
{
    // Field constants - using nested structure
    private static readonly string LogCategoryField = "app_log.LogCategory";
    private static readonly string WorkflowIdField = "app_log.WorkflowId";
    private static readonly string GrainIdField = "app_log.GrainId";
    private static readonly string LogLevelField = "app_log.@l";
    private static readonly string MessageField = "app_log.@m";
    private static readonly string TimestampField = "app_log.@t";
    private static readonly string RoundIdField = "app_log.RoundId";
    
    // Value constants
    private static readonly string WorkflowLogCategory = "WORKFLOW";
    private static readonly string LogIndexSuffix = "-log-index";
    
    // Error messages
    private static readonly string QueryFailedMessage = "查询失败";
    private static readonly string WorkflowQueryFailedMessage = "查询WorkflowId日志失败";

    private readonly ElasticsearchClient _elasticClient;
    private readonly ILogger<LogElasticSearchService> _logger;
    private readonly LogElasticSearchOptions _logElasticSearchOptions;

    public LogElasticSearchService(ILogger<LogElasticSearchService> logger, 
        [FromKeyedServices("Logger")] ElasticsearchClient elasticClient,
        IOptionsSnapshot<LogElasticSearchOptions> logElasticSearchOptions)
    {
        _logger = logger;
        _elasticClient = elasticClient;
        _logElasticSearchOptions = logElasticSearchOptions.Value;
    }


    public async Task<List<HostLogIndex>> GetHostLatestLogAsync(string indexName, int pageSize)
    {
        var mustQueries = new List<Query>();

        var sortOptions = new SortOptionsDescriptor<HostLogIndex>()
            .Field(new Field(TimestampField), d => d.Order(SortOrder.Desc));
            
        var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
            .Index($"{indexName}*")  // Use prefix matching for multiple indices
            .Sort(sortOptions)
            .Size(pageSize)
            .Query(new BoolQuery { Must = mustQueries }));

        if (!response.IsValidResponse)
        {
            throw new Exception($"{QueryFailedMessage}: {response.DebugInformation}");
        }

        return response.Hits
            .Select(hit => hit.Source)
            .Where(source => source != null)
            .ToList()!;
    }


    public string GetHostLogIndexAliasName(string nameSpace, string appId, string version)
    {
        return $"{nameSpace}-{appId}-{version}{LogIndexSuffix}".ToLower();
    }

    public async Task<List<HostLogIndex>> GetWorkflowLogsAsync(string indexName, string workflowId, long? roundId = null, string? grainId = null, string? level = null, string? messagePattern = null, int from = 0, int size = 100)
    {
        _logger.LogInformation("🔍 开始构建查询 - 索引: {IndexName}, WorkflowId: {WorkflowId}, RoundId: {RoundId}, GrainId: {GrainId}, Level: {Level}, MessagePattern: {MessagePattern}, From: {From}, Size: {Size}", 
            indexName, workflowId, roundId, grainId ?? "Any", level ?? "Any", messagePattern ?? "None", from, size);

        try
        {
            _logger.LogInformation("🎯 使用正确的NEST语法构建查询");
            
            var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
                .Index($"{indexName}*")
                .From(from)
                .Size(size)
                .TrackTotalHits(new Elastic.Clients.Elasticsearch.Core.Search.TrackHits(true)) // 启用总数统计
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            // LogCategory精确匹配
                            m => m.Term(t => t
                                .Field(f => f.AppLog.LogCategory)
                                .Value(WorkflowLogCategory)
                            ),
                            // WorkflowId精确匹配
                            m => m.Term(t => t
                                .Field(f => f.AppLog.WorkflowId)
                                .Value(workflowId)
                            )
                        )
                    )
                )
            );

            // 详细的调试信息
            _logger.LogInformation("📊 ES查询响应详情:");
            _logger.LogInformation("   - IsValidResponse: {IsValid}", response.IsValidResponse);
            _logger.LogInformation("   - Took: {Took}ms", response.Took);
            _logger.LogInformation("   - TimedOut: {TimedOut}", response.TimedOut);
            _logger.LogInformation("   - Hits.Count: {HitCount}", response.Hits?.Count ?? 0);
            _logger.LogInformation("   - Total.Value: {TotalValue}", response.Total.ToString()?? "未知");
            _logger.LogInformation("   - Shards.Total: {ShardsTotal}", response.Shards?.Total);
            _logger.LogInformation("   - Shards.Successful: {ShardsSuccessful}", response.Shards?.Successful);
            _logger.LogInformation("   - Shards.Failed: {ShardsFailed}", response.Shards?.Failed);

            // Debug模式：输出完整的ES调试信息
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("🔍 完整ES调试信息:");
                _logger.LogDebug("{DebugInformation}", response.DebugInformation);
            }

            if (!response.IsValidResponse)
            {
                _logger.LogError("❌ 查询失败: {DebugInfo}", response.DebugInformation);
                throw new Exception($"{WorkflowQueryFailedMessage}: {response.DebugInformation}");
            }

            var results = response.Hits?
                .Select(hit => hit.Source!)
                .ToList() ?? new List<HostLogIndex>();

            _logger.LogInformation("✅ 查询完成 - 找到 {Count} 条记录，WorkflowId: {WorkflowId}", 
                results.Count, workflowId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying workflow logs for WorkflowId: {WorkflowId}, GrainId: {GrainId}", workflowId, grainId ?? "Any");
            throw;
        }
    }
}