using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class TokenUsageQueryHandler: IRequestHandler<TokenUsageQueryCommand, Tuple<long, List<string>>?>
{
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IIndexingService _indexingService;

    public TokenUsageQueryHandler(ICQRSProvider cqrsProvider, IIndexingService indexingService)
    {
        _cqrsProvider = cqrsProvider;
        _indexingService = indexingService;
    }

    public async Task<Tuple<long, List<string>>?> Handle(TokenUsageQueryCommand request, CancellationToken cancellationToken)
    {
        var indexName = _cqrsProvider.GetIndexNameWithHostId(request.HostId,"TokenUsage");
        await _indexingService.TryCreateTokenUsageIndexAsync(indexName);

        return await _indexingService.QueryTokenUsageAsync(indexName, request.SystemLLM, request.StartTime,
            request.EndTime, request.StatisticsAsHour);
    }
}