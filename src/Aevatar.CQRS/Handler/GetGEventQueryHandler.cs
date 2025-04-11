using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class GetGEventQueryHandler : IRequestHandler<GetGEventQuery, Tuple<long, List<AgentGEventIndex>>>
{
    private readonly IIndexingService  _indexingService ;

    public GetGEventQueryHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;

    }

    public async Task<Tuple<long, List<AgentGEventIndex>>> Handle(GetGEventQuery request, CancellationToken cancellationToken)
    {
        return await _indexingService.GetSortListAsync<AgentGEventIndex>(request.Query,sortFunc:request.Sort,skip:request.Skip, limit: request.Limit);
    }
}