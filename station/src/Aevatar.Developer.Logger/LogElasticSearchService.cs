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

    private BoolQueryDescriptor<HostLogIndex> BuildChainedBoolQuery(BoolQueryDescriptor<HostLogIndex> b, string workflowId, long? roundId, string? grainId, string? level, string? messagePattern)
    {
        var boolQuery = b.Must(
            // Required: LogCategory must be WORKFLOW
            m => m.Term(t => t
                .Field(f => f.AppLog.LogCategory)
                .Value(WorkflowLogCategory)
            ),
            // Required: WorkflowId must match exactly  
            m => m.Term(t => t
                .Field(f => f.AppLog.WorkflowId)
                .Value(workflowId)
            )
        );

        // Add optional conditions
        AddOptionalQueries(boolQuery, roundId, grainId, level, messagePattern);
        
        return boolQuery;
    }

    private void AddOptionalQueries(BoolQueryDescriptor<HostLogIndex> boolQuery, long? roundId, string? grainId, string? level, string? messagePattern)
    {
        // Optional: RoundId exact match
        if (roundId.HasValue)
        {
            boolQuery.Must(m => m.Term(t => t
                .Field(f => f.AppLog.RoundId)
                .Value(roundId.Value)
            ));
        }

        // Optional: GrainId exact match
        if (!string.IsNullOrEmpty(grainId))
        {
            boolQuery.Must(m => m.Term(t => t
                .Field(f => f.AppLog.GrainId)
                .Value(grainId)
            ));
        }

        // Optional: Log level exact match
        if (!string.IsNullOrEmpty(level))
        {
            boolQuery.Must(m => m.Term(t => t
                .Field(f => f.AppLog.Level)
                .Value(level)
            ));
        }

        // Optional: Message pattern fuzzy match
        if (!string.IsNullOrEmpty(messagePattern))
        {
            boolQuery.Must(m => m.QueryString(qs => qs
                .Query($"*{messagePattern}*")
                .Fields(Fields.FromExpression((HostLogIndex f) => f.AppLog.Message))
            ));
        }
    }

    public async Task<List<HostLogIndex>> GetWorkflowLogsAsync(string indexName, string workflowId, long? roundId = null, string? grainId = null, string? level = null, string? messagePattern = null, int from = 0, int size = 100)
    {
        try
        {
            var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
                .Index($"{indexName}*")
                .From(from)
                .Size(size)
                .Sort(sort => sort
                    .Field(new Field(TimestampField), d => d.Order(SortOrder.Asc)) // Sort by time ascending for workflow tracking
                )
                .Query(q => q
                    .Bool(b => BuildChainedBoolQuery(b, workflowId, roundId, grainId, level, messagePattern))
                )
            );

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to query workflow logs: {DebugInfo}", response.DebugInformation);
                throw new Exception($"{WorkflowQueryFailedMessage}: {response.DebugInformation}");
            }

            var results = response.Hits
                .Select(hit => hit.Source)
                .Where(source => source != null)
                .ToList()!;

            _logger.LogInformation("Found {Count} workflow logs for WorkflowId: {WorkflowId}, RoundId: {RoundId}, GrainId: {GrainId}, Level: {Level}", 
                results.Count, workflowId, roundId?.ToString() ?? "Any", grainId ?? "Any", level ?? "Any");
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying workflow logs for WorkflowId: {WorkflowId}, RoundId: {RoundId}, GrainId: {GrainId}", 
                workflowId, roundId?.ToString() ?? "Any", grainId ?? "Any");
            throw;
        }
    }
}