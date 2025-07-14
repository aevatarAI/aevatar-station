using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Amazon.Runtime.Internal.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aevatar.CQRS.Handler;

public class SaveStateBatchCommandHandler : IRequestHandler<SaveStateBatchCommand>
{
    private readonly IIndexingService _indexingService;
    private readonly ILogger<SaveStateBatchCommandHandler> _logger;

    public SaveStateBatchCommandHandler(
        IIndexingService indexingService, ILogger<SaveStateBatchCommandHandler> logger)
    {
        _indexingService = indexingService;
        _logger = logger;
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
        _logger.LogDebug("[SaveStateBatchCommandHandler][SaveIndicesAsync] start");
        await _indexingService.SaveOrUpdateStateIndexBatchAsync(commands);
    }
}