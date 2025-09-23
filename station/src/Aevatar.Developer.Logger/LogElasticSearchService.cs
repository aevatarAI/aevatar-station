using Aevatar.Developer.Logger.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
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

    public LogElasticSearchService(ILogger<LogElasticSearchService> logger, ElasticsearchClient elasticClient,
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

            var mustQueries = new List<Query>();

            // Required: LogCategory must be WORKFLOW - 使用Match查询处理text字段
            var logCategoryQuery = Query.Match(new MatchQuery(new Field(LogCategoryField))
            {
                Query = WorkflowLogCategory
            });
            mustQueries.Add(logCategoryQuery);
            _logger.LogInformation("📝 添加LogCategory查询(Match): {Field} = {Value}", LogCategoryField, WorkflowLogCategory);

            // Required: WorkflowId must match exactly - 使用Match查询处理text字段
            var workflowIdQuery = Query.Match(new MatchQuery(new Field(WorkflowIdField))
            {
                Query = workflowId
            });
            mustQueries.Add(workflowIdQuery);
            _logger.LogInformation("📝 添加WorkflowId查询(Match): {Field} = {Value}", WorkflowIdField, workflowId);

            // Optional: RoundId exact match
            if (roundId.HasValue)
            {
                var roundIdQuery = Query.Term(new TermQuery(new Field(RoundIdField))
                {
                    Value = roundId.Value
                });
                mustQueries.Add(roundIdQuery);
                _logger.LogInformation("📝 添加RoundId查询: {Field} = {Value}", RoundIdField, roundId.Value);
            }

            // Optional: GrainId exact match
            if (!string.IsNullOrEmpty(grainId))
            {
                var grainIdQuery = Query.Term(new TermQuery(new Field(GrainIdField))
                {
                    Value = grainId
                });
                mustQueries.Add(grainIdQuery);
                _logger.LogInformation("📝 添加GrainId查询: {Field} = {Value}", GrainIdField, grainId);
            }

            // Optional: Log level exact match
            if (!string.IsNullOrEmpty(level))
            {
                var levelQuery = Query.Term(new TermQuery(new Field(LogLevelField))
                {
                    Value = level
                });
                mustQueries.Add(levelQuery);
                _logger.LogInformation("📝 添加Level查询: {Field} = {Value}", LogLevelField, level);
            }

            // Optional: Message pattern match using query_string for text fields
            if (!string.IsNullOrEmpty(messagePattern))
            {
                var messageQuery = Query.QueryString(new QueryStringQuery
                {
                    Query = $"*{messagePattern}*",
                    Fields = new[] { new Field(MessageField) }
                });
                mustQueries.Add(messageQuery);
                _logger.LogInformation("📝 添加MessagePattern查询: {Field} contains {Pattern}", MessageField, messagePattern);
            }

        var sortOptions = new SortOptionsDescriptor<HostLogIndex>()
            .Field(new Field(TimestampField), d => d.Order(SortOrder.Asc)); // Sort by time ascending for workflow tracking

            _logger.LogInformation("🎯 执行ES查询 - 索引模式: {IndexPattern}, 查询条件数: {QueryCount}", $"{indexName}*", mustQueries.Count);

            try
            {
                // 先尝试简化查询 - 只查询WorkflowId
                _logger.LogInformation("🧪 尝试简化查询 - 只查询WorkflowId");
                var simpleResponse = await _elasticClient.SearchAsync<HostLogIndex>(s => s
                    .Index($"{indexName}*")
                    .From(0)
                    .Size(5)
                    .Query(q => q.Match(m => m.Field(WorkflowIdField).Query(workflowId))));
                
                _logger.LogInformation("📊 简化查询结果: IsValid={IsValid}, Hits={HitCount}", 
                    simpleResponse.IsValidResponse, simpleResponse.Hits?.Count ?? 0);

                if (simpleResponse.Hits?.Count > 0)
                {
                    _logger.LogInformation("✅ 简化查询成功！问题在于LogCategory条件");
                    // 如果简化查询成功，就不加LogCategory条件
                    mustQueries.RemoveAt(0); // 移除LogCategory查询
                    _logger.LogInformation("🔧 移除LogCategory查询条件，只使用WorkflowId查询");
                }
                else
                {
                    _logger.LogWarning("⚠️ 连简化查询都失败，可能是字段映射问题");
                }

                var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
                    .Index($"{indexName}*")  // Use prefix matching for multiple indices
                    .Sort(sortOptions)
                    .From(from)
                    .Size(size)
                    .Query(new BoolQuery { Must = mustQueries }));

            _logger.LogInformation("📊 ES响应状态: IsValid={IsValid}, Took={Took}ms, Hits={HitCount}", 
                response.IsValidResponse, response.Took, response.Hits?.Count ?? 0);

            if (!response.IsValidResponse)
            {
                _logger.LogError("❌ ES查询失败: {DebugInfo}", response.DebugInformation);
                throw new Exception($"{WorkflowQueryFailedMessage}: {response.DebugInformation}");
            }

            if (response.Hits?.Count > 0)
            {
                _logger.LogInformation("📝 ES返回原始文档示例:");
                var firstHit = response.Hits.First();
                _logger.LogInformation("  - 文档ID: {DocId}", firstHit.Id);
                _logger.LogInformation("  - 索引: {Index}", firstHit.Index);
                _logger.LogInformation("  - Source对象: {SourceStatus}", firstHit.Source != null ? "有值" : "null");
                
                if (firstHit.Source?.AppLog != null)
                {
                    _logger.LogInformation("  - AppLog.WorkflowId: {WorkflowId}", firstHit.Source.AppLog.WorkflowId);
                    _logger.LogInformation("  - AppLog.LogCategory: {LogCategory}", firstHit.Source.AppLog.LogCategory);
                    _logger.LogInformation("  - AppLog.Message: {Message}", firstHit.Source.AppLog.Message?.Substring(0, Math.Min(50, firstHit.Source.AppLog.Message.Length)));
                }
                else
                {
                    _logger.LogWarning("⚠️ AppLog对象为null，可能是数据结构不匹配");
                }
            }

            var results = response.Hits?
                .Select(hit => hit.Source!)
                .ToList() ?? new List<HostLogIndex>();

            _logger.LogInformation("✅ 查询完成 - 找到 {Count} 条记录，WorkflowId: {WorkflowId}, GrainId: {GrainId}, Level: {Level}", 
                results.Count, workflowId, grainId ?? "Any", level ?? "Any");
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying workflow logs for WorkflowId: {WorkflowId}, GrainId: {GrainId}", workflowId, grainId);
            throw;
        }
    }

    
}