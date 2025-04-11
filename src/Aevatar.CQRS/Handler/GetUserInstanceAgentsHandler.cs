using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class
    GetUserInstanceAgentsHandler : IRequestHandler<GetUserInstanceAgentsQuery, Tuple<long, string>?>
{
    private readonly IIndexingService _indexingService;

    public GetUserInstanceAgentsHandler(IIndexingService indexingService)
    {
        _indexingService = indexingService;
    }

    public async Task<Tuple<long, string>?> Handle(GetUserInstanceAgentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _indexingService.GetSortDataDocumentsAsync(request.Index, request.Query, skip: request.Skip,
            limit: request.Limit);
    }
}