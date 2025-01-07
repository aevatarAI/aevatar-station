using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class GetGEventQueryHandler : IRequestHandler<GetGEventQuery, string>
{
    private readonly IIndexingService  _indexingService ;

    public GetGEventQueryHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;

    }

    public async Task<string> Handle(GetGEventQuery request, CancellationToken cancellationToken)
    {
        return await _indexingService.QueryEventIndexAsync(request.Id, request.Index);
    }
}