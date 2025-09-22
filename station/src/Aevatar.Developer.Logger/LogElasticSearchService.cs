using Aevatar.Developer.Logger.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Developer.Logger;

public class LogElasticSearchService : ILogService
{
    // Field constants
    private static readonly string LogCategoryField = "app_log.LogCategory.keyword";
    private static readonly string WorkflowIdField = "app_log.WorkflowId.keyword";
    private static readonly string GrainIdField = "app_log.GrainId.keyword";
    private static readonly string LogLevelField = "app_log.@l";
    private static readonly string MessageField = "app_log.@m";
    private static readonly string TimestampField = "app_log.@t";
    
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
        var mustQueries = new List<Query>();

        // Required: LogCategory must be WORKFLOW
        mustQueries.Add(Query.Term(new TermQuery(new Field(LogCategoryField))
        {
            Value = WorkflowLogCategory
        }));

        // Required: WorkflowId must match exactly
        mustQueries.Add(Query.Term(new TermQuery(new Field(WorkflowIdField))
        {
            Value = workflowId
        }));

        // Optional: RoundId exact match
        if (roundId.HasValue)
        {
            mustQueries.Add(Query.Term(new TermQuery(new Field("app_log.RoundId"))
            {
                Value = roundId.Value
            }));
        }

        // Optional: GrainId exact match
        if (!string.IsNullOrEmpty(grainId))
        {
            mustQueries.Add(Query.Term(new TermQuery(new Field(GrainIdField))
            {
                Value = grainId
            }));
        }

        // Optional: Log level exact match
        if (!string.IsNullOrEmpty(level))
        {
            mustQueries.Add(Query.Term(new TermQuery(new Field(LogLevelField))
            {
                Value = level
            }));
        }

        // Optional: Message pattern match using query_string for text fields
        if (!string.IsNullOrEmpty(messagePattern))
        {
            mustQueries.Add(Query.QueryString(new QueryStringQuery
            {
                Query = $"*{messagePattern}*",
                Fields = new[] { new Field(MessageField) }
            }));
        }

        var sortOptions = new SortOptionsDescriptor<HostLogIndex>()
            .Field(new Field(TimestampField), d => d.Order(SortOrder.Asc)); // Sort by time ascending for workflow tracking

        try
        {
            var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
                .Index($"{indexName}*")  // Use prefix matching for multiple indices
                .Sort(sortOptions)
                .From(from)
                .Size(size)
                .TrackTotalHits(new Elastic.Clients.Elasticsearch.Core.Search.TrackHits(false))
                .Query(new BoolQuery { Must = mustQueries }));

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to query workflow logs: {DebugInfo}", response.DebugInformation);
                throw new Exception($"{WorkflowQueryFailedMessage}: {response.DebugInformation}");
            }

            var results = response.Hits
                .Select(hit => hit.Source!)
                .ToList();

            _logger.LogInformation("Found {Count} workflow logs for WorkflowId: {WorkflowId}, GrainId: {GrainId}, Level: {Level}", 
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