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

    public async Task<List<HostLogIndex>> GetWorkflowLogsAsync(string indexName, string workflowId, int pageSize = 100)
    {
        return await GetWorkflowLogsWithFilterAsync(indexName, workflowId, null, null, pageSize);
    }

    public async Task<List<HostLogIndex>> GetWorkflowLogsWithFilterAsync(string indexName, string workflowId, string? workflowStep = null, string? workflowAction = null, int pageSize = 100)
    {
        var mustQueries = new List<Query>();

        // Search for workflow logs containing the specific WorkflowId
        var workflowIdQuery = Query.Wildcard(new WildcardQuery(new Field("app_log.@m"))
        {
            Value = $"*WORKFLOW:*[WorkflowId={workflowId}]*"
        });
        mustQueries.Add(workflowIdQuery);

        // Add optional step filter
        if (!string.IsNullOrEmpty(workflowStep))
        {
            var stepQuery = Query.Wildcard(new WildcardQuery(new Field("app_log.@m"))
            {
                Value = $"*[Step={workflowStep}]*"
            });
            mustQueries.Add(stepQuery);
        }

        // Add optional action filter
        if (!string.IsNullOrEmpty(workflowAction))
        {
            var actionQuery = Query.Wildcard(new WildcardQuery(new Field("app_log.@m"))
            {
                Value = $"*{workflowAction}:*"
            });
            mustQueries.Add(actionQuery);
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

            _logger.LogInformation("Found {Count} workflow logs for WorkflowId: {WorkflowId}", results.Count, workflowId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying workflow logs for WorkflowId: {WorkflowId}", workflowId);
            throw;
        }
    }
}