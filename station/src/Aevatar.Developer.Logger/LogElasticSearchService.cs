using Aevatar.Developer.Logger.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.Developer.Logger;

public class LogElasticSearchService : ILogService
{
    // Constants
    private const string TimestampField = "app_log.@t";
    private const string WorkflowLogCategory = "WORKFLOW";
    private const string LogIndexSuffix = "-log-index";
    private const string QueryFailedMessage = "查询失败";
    private const string WorkflowQueryFailedMessage = "查询WorkflowId日志失败";

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

    private QueryDescriptor<HostLogIndex> BuildWorkflowLogsQuery(QueryDescriptor<HostLogIndex> q, string workflowId, long? roundId, string? grainId, string? level, string? messagePattern)
    {
        // Build must clauses list
        var mustClauses = new List<Action<QueryDescriptor<HostLogIndex>>>
        {
            // Required conditions
            m => m.Term(t => t.Field(f => f.AppLog!.LogCategory).Value(WorkflowLogCategory)),
            m => m.Term(t => t.Field(f => f.AppLog!.WorkflowId).Value(workflowId))
        };

        // Add optional conditions
        if (roundId.HasValue)
        {
            mustClauses.Add(m => m.Term(t => t.Field(f => f.AppLog!.RoundId).Value(roundId.Value)));
        }

        if (!string.IsNullOrEmpty(grainId))
        {
            mustClauses.Add(m => m.Term(t => t.Field(f => f.AppLog!.GrainId).Value(grainId)));
        }

        if (!string.IsNullOrEmpty(level))
        {
            mustClauses.Add(m => m.Term(t => t.Field(f => f.AppLog!.Level).Value(level)));
        }

        if (!string.IsNullOrEmpty(messagePattern))
        {
            mustClauses.Add(m => m.QueryString(qs => qs
                .Query($"*{messagePattern}*")
                .Fields(Fields.FromExpression((HostLogIndex f) => f.AppLog!.Message))
            ));
        }

        // Apply all conditions
        return q.Bool(b => b.Must(mustClauses.ToArray()));
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
                .Query(q => BuildWorkflowLogsQuery(q, workflowId, roundId, grainId, level, messagePattern))
            );

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to query workflow logs: {DebugInfo}", response.DebugInformation);
                throw new Exception($"{WorkflowQueryFailedMessage}: {response.DebugInformation}");
            }

            var results = response.Hits
                .Select(hit => hit.Source)
                .Where(source => source != null)
                .ToList();

            _logger.LogInformation(
                "Found {Count} workflow logs for WorkflowId: {WorkflowId}, RoundId: {RoundId}, GrainId: {GrainId}, Level: {Level}", 
                results.Count, 
                workflowId,
                roundId?.ToString() ?? "Any",
                grainId ?? "Any",
                level ?? "Any"
            );
            
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