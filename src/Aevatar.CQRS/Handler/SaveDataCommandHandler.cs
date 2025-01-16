using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class SaveDataCommandHandler : IRequestHandler<SaveDataCommand>
{
    private readonly IIndexingService  _indexingService ;

    public SaveDataCommandHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;
    }

    public async Task<Unit> Handle(SaveDataCommand request, CancellationToken cancellationToken)
    {
        _indexingService.CheckExistOrCreateIndex(request.BaseIndex);
        await SaveIndexAsync(request);
        return Unit.Value;
    }

    private async Task SaveIndexAsync(SaveDataCommand request)
    {
        await _indexingService.SaveOrUpdateIndexAsync(request.Id, request.BaseIndex);
    }
}