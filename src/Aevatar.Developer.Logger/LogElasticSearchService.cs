using Aevatar.Developer.Logger.Entities;
using Aevetar.Developer.Logger.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Aevatar.Developer.Logger;

public class LogElasticSearchService:ILogService
{
    private readonly ElasticClient _elasticClient;
    private readonly ILogger<LogElasticSearchService> _logger;
    private readonly LogElasticSearchOptions _logElasticSearchOptions;

    public LogElasticSearchService(ILogger<LogElasticSearchService> logger, ElasticClient elasticClient,
        IOptionsSnapshot<LogElasticSearchOptions> logElasticSearchOptions)
    {
        _logger = logger;
        _elasticClient = elasticClient;
        _logElasticSearchOptions = logElasticSearchOptions.Value;
    }

    public async Task<List<HostLogIndex>> GetHostLatestLogAsync(string indexName, int pageSize)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<HostLogIndex>, QueryContainer>>();
        QueryContainer Filter(QueryContainerDescriptor<HostLogIndex> f) 
        {
            var boolQuery = new BoolQueryDescriptor<HostLogIndex>()
                .Must(mustQuery);
            return f.Bool(b => boolQuery);
        }
        
        var response = await _elasticClient.SearchAsync<HostLogIndex>(s => s
            .Index(indexName)
            .Sort(so => so
                .Descending(f => f.App_log.Time))
            .Size(pageSize)
            .Query(Filter));

        return response.Documents.ToList();
    }
   

    public string GetHostLogIndexAliasName(string nameSpace, string appId, string version)
    {
        return $"{nameSpace}-{appId}-{version}-log-index".ToLower();
    }

    public async Task CreateFileBeatLogILMPolicyAsync(string policyName)
    {
        if (_logElasticSearchOptions == null || _logElasticSearchOptions.Username.IsNullOrEmpty())
        {
            return;
        }

        var putPolicyResponse = await _elasticClient.IndexLifecycleManagement.PutLifecycleAsync(policyName, p => p
            .Policy(pd => pd
                .Phases(ph => ph
                    .Hot(h => h
                        .MinimumAge("0ms")
                        .Actions(a => a
                            .Rollover(ro => ro
                                .MaximumSize(_logElasticSearchOptions.ILMPolicy.HotMaxSize)
                                .MaximumAge(_logElasticSearchOptions.ILMPolicy.HotMaxAge))
                            .SetPriority(pp => pp.Priority(100))
                        )
                    )
                    .Cold(c => c
                        .MinimumAge(_logElasticSearchOptions.ILMPolicy.ColdMinAge)
                        .Actions(a => a
                            .Freeze(f => f)
                            .SetPriority(pp => pp.Priority(50))
                        )
                    )
                    .Delete(d => d
                        .MinimumAge(_logElasticSearchOptions.ILMPolicy.DeleteMinAge)
                        .Actions(a => a
                            .Delete(de => de)
                        )
                    )
                )
            )
        );

        if (putPolicyResponse.IsValid)
        {
            _logger.LogInformation("ILM policy is created successfully. ");
        }
        else
        {
            _logger.LogError($"Failed to create an ILM policy: {putPolicyResponse.DebugInformation}");
        }
    }
}