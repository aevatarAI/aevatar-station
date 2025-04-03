using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class GetStateQueryHandler : IRequestHandler<GetStateQuery, string>
{
    private readonly IIndexingService _indexingService;

    public GetStateQueryHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;
    }

    public async Task<string> Handle(GetStateQuery request, CancellationToken cancellationToken)
    {
        return await _indexingService.GetStateIndexDocumentsAsync(request.StateName, request.Query, request.Skip,
            request.Limit);
    }
}