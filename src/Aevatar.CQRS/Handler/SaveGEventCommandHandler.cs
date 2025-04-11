using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;


namespace Aevatar.CQRS.Handler;

public class SaveGEventCommandHandler : IRequestHandler<SaveGEventCommand>
{
    private readonly IIndexingService _indexingService;

    public SaveGEventCommandHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;
    }

    public async Task Handle(SaveGEventCommand request, CancellationToken cancellationToken)
    {
        _indexingService.CheckExistOrCreateIndex(request.AgentGEventIndex);
        await SaveIndexAsync(request);
    }

    private async Task SaveIndexAsync(SaveGEventCommand request)
    {
        await _indexingService.SaveOrUpdateIndexAsync(request.Id.ToString(), request.AgentGEventIndex);
    }
}