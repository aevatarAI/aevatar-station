using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class SaveStateBatchCommandHandler : IRequestHandler<SaveStateBatchCommand>
{
    private readonly IIndexingService _indexingService;

    public SaveStateBatchCommandHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;
    }

    public async Task Handle(SaveStateBatchCommand request, CancellationToken cancellationToken)
    {
        foreach (var stateCommand in request.Commands)
        {
            // Check or create the necessary index for each state
            _indexingService.CheckExistOrCreateStateIndex(stateCommand.State);
        }

        // Save all indices in a single operation for batch processing
        await SaveIndicesAsync(request.Commands);
    }

    private async Task SaveIndicesAsync(IEnumerable<SaveStateCommand> commands)
    {
        await _indexingService.SaveOrUpdateStateIndexBatchAsync(commands);
    }
}