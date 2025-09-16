using Aevatar.Developer.Logger.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Developer.Logger;

public class LogElasticSearchService : ILogService
{
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
            .Field(f => f.App_log.Time, d => d.Order(SortOrder.Desc));
        var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
            .Index(indexName)
            .Sort(sortOptions)
            .Size(pageSize)
            .Query(new BoolQuery { Must = mustQueries }));


        if (!response.IsValidResponse)
        {
            throw new Exception($"查询失败: {response.DebugInformation}");
        }

        return response.Hits
            .Select(hit => hit.Source)
            .Where(source => source != null)
            .ToList()!;
    }


    public string GetHostLogIndexAliasName(string nameSpace, string appId, string version)
    {
        return $"{nameSpace}-{appId}-{version}-log-index".ToLower();
    }

    public async Task<List<HostLogIndex>> GetWorkflowLogsAsync(string indexName, string workflowId, string? grainId = null, string? level = null, string? messagePattern = null, int pageSize = 100)
    {
        var mustQueries = new List<Query>();

        // Required: LogCategory must be WORKFLOW
        mustQueries.Add(Query.Term(new TermQuery(new Field("app_log.LogCategory.keyword"))
        {
            Value = "WORKFLOW"
        }));

        // Required: WorkflowId must match exactly
        mustQueries.Add(Query.Term(new TermQuery(new Field("app_log.WorkflowId.keyword"))
        {
            Value = workflowId
        }));

        // Optional: GrainId exact match
        if (!string.IsNullOrEmpty(grainId))
        {
            mustQueries.Add(Query.Term(new TermQuery(new Field("app_log.GrainId.keyword"))
            {
                Value = grainId
            }));
        }

        // Optional: Log level exact match
        if (!string.IsNullOrEmpty(level))
        {
            mustQueries.Add(Query.Term(new TermQuery(new Field("app_log.@l.keyword"))
            {
                Value = level
            }));
        }

        // Optional: Message pattern fuzzy match
        if (!string.IsNullOrEmpty(messagePattern))
        {
            mustQueries.Add(Query.Wildcard(new WildcardQuery(new Field("app_log.@m"))
            {
                Value = $"*{messagePattern}*"
            }));
        }

        var sortOptions = new SortOptionsDescriptor<HostLogIndex>()
            .Field(f => f.App_log.Time, d => d.Order(SortOrder.Asc)); // Sort by time ascending for workflow tracking

        try
        {
            var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
                .Index(indexName)
                .Sort(sortOptions)
                .Size(pageSize)
                .Query(new BoolQuery { Must = mustQueries }));

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to query workflow logs: {DebugInfo}", response.DebugInformation);
                throw new Exception($"查询WorkflowId日志失败: {response.DebugInformation}");
            }

            var results = response.Hits
                .Select(hit => hit.Source)
                .Where(source => source != null)
                .ToList()!;

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