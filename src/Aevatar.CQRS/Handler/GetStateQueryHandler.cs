using System.Threading;
using System.Threading.Tasks;
using Aevatar.Agents;
using Aevatar.CQRS.Dto;
using MediatR;
using Nest;

namespace Aevatar.CQRS.Handler;

public class GetStateQueryHandler : IRequestHandler<GetStateQuery, BaseStateIndex>
{
    private readonly IIndexingService  _indexingService ;

    public GetStateQueryHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;

    }
    
    public async Task<BaseStateIndex> Handle(GetStateQuery request, CancellationToken cancellationToken)
    {
        return await _indexingService.QueryStateIndexAsync(request.Id, request.Index);
    }
}