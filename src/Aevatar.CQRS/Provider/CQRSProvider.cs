using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Aevatar.Options;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS.Provider;

public class CQRSProvider : ICQRSProvider, ISingletonDependency
{
    private readonly IMediator _mediator;
    private readonly ILogger<CQRSProvider> _logger;
    private readonly IOptions<HostOptions> _options;
    private string _projectName = null;

    public CQRSProvider(IMediator mediator, ILogger<CQRSProvider> logger, IOptions<HostOptions> hostOptions)
    {
        _mediator = mediator;
        _logger = logger;
        _options = hostOptions;
    }

    public async Task<string> QueryStateAsync(string indexName,
        Action<QueryDescriptor<dynamic>> query, int skip, int limit)
    {
        _logger.LogInformation("CQRSProvider QueryStateAsync indexName:{indexName}", indexName);
        var getStateQuery = new GetStateQuery()
        {
            Index = indexName,
            Query = query,
            Skip = skip,
            Limit = limit
        };

        var document = await _mediator.Send(getStateQuery);
        return document;
    }


    public async Task<string> QueryAgentStateAsync(string stateName, Guid primaryKey)
    {
        _logger.LogInformation("CQRSProvider QueryAgentStateAsync stateName:{stateName}, primaryKey:{primaryKey}",
            stateName, primaryKey);
        var mustQuery = new Action<QueryDescriptor<dynamic>>(q =>
            q.Term(t => t
                .Field("_id")
                .Value(primaryKey.ToString())
            ));
        var getStateQuery = new GetStateQuery()
        {
            Index = GetIndexName(stateName),
            Query = mustQuery,
            Skip = 0,
            Limit = 1
        };

        var document = await _mediator.Send(getStateQuery);
        return document;
    }

    public async Task<Tuple<long, List<TargetT>>> GetUserInstanceAgent<SourceT, TargetT>(Guid userId, int pageIndex,
        int pageSize)
    {
        _logger.LogInformation("CQRSProvider query user instance agents,UserId:{userId}", userId);
        var mustQuery = new Action<QueryDescriptor<dynamic>>(q =>
            q.Term(t => t
                    .Field("userId.keyword") // Specify the field
                    .Value(userId.ToString()) // Specify the value
            )
        );
        var index = GetIndexName(typeof(SourceT).Name.ToLower());
        var queryResponse = await _mediator.Send(new GetUserInstanceAgentsQuery()
        {
            Index = index,
            Skip = pageIndex * pageSize,
            Query = mustQuery,
            Limit = pageSize,
        });

        if (queryResponse == null || queryResponse.Item2.IsNullOrWhiteSpace())
        {
            return new Tuple<long, List<TargetT>>(0, new List<TargetT>());
        }

        var documentList = JsonConvert.DeserializeObject<List<TargetT>>(queryResponse.Item2);
        if (documentList != null)
        {
            return new Tuple<long, List<TargetT>>(queryResponse.Item1, documentList);
        }

        _logger.LogWarning(
            "CQRSProvider query user instance agents documentList == null, UserId:{userId}, document string:{documents}",
            userId, queryResponse.Item2);

        return new Tuple<long, List<TargetT>>(0, new List<TargetT>());
    }

    public string GetIndexName(string index)
    {
        return $"{CqrsConstant.IndexPrefix}-{_options.Value.HostId}-{index}{CqrsConstant.IndexSuffix}".ToLower();
    }
}