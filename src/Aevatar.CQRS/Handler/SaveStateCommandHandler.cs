using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class SaveStateCommandHandler : IRequestHandler<SaveStateCommand>
{
    private readonly IIndexingService _indexingService;

    public SaveStateCommandHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;
    }

    public async Task Handle(SaveStateCommand request, CancellationToken cancellationToken)
    {
        _indexingService.CheckExistOrCreateStateIndex(request.State);
        await SaveIndexAsync(request);
    }

    private async Task SaveIndexAsync(SaveStateCommand request)
    {
        await _indexingService.SaveOrUpdateStateIndexAsync(request.Id, request.State);
    }
}