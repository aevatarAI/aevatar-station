using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;
using Volo.Abp.ObjectMapping;

namespace Aevatar.CQRS.Handler;

public class GetDataQueryHandler : IRequestHandler<GetDataQuery, Tuple<long, string>>
{
    private readonly IIndexingService  _indexingService ;
    private readonly IObjectMapper _objectMapper;

    public GetDataQueryHandler(
        IIndexingService indexingService,
        IObjectMapper objectMapper
    )
    {
        _indexingService = indexingService;
        _objectMapper = objectMapper;

    }
    
    public async Task<Tuple<long, string>> Handle(GetDataQuery request, CancellationToken cancellationToken)
    {
        return await _indexingService.GetSortDataDocumentsAsync(request.Index, request.Query, request.Skip, request.Limit);
    }
}