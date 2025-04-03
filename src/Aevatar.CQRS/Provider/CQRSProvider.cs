using System;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MediatR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Aevatar.CQRS.Provider;

public class CQRSProvider : ICQRSProvider, ISingletonDependency
{
    private readonly IMediator _mediator;
    private readonly ILogger<CQRSProvider> _logger;
    private string _projectName = null;

    public CQRSProvider(IMediator mediator, ILogger<CQRSProvider> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<string> QueryStateAsync(string indexName,
        Action<QueryDescriptor<dynamic>> query, int skip, int limit)
    {
        _logger.LogInformation("CQRSProvider QueryStateAsync indexName:{indexName}", indexName);
        var getStateQuery = new GetStateQuery()
        {
            StateName = indexName,
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
            StateName = stateName,
            Query = mustQuery,
            Skip = 0,
            Limit = 1
        };

        var document = await _mediator.Send(getStateQuery);
        return document;
    }
}