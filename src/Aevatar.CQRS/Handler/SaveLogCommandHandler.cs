using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Volo.Abp.ObjectMapping;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class SaveLogCommandHandler : IRequestHandler<SaveLogCommand>
{
    private readonly IIndexingService  _indexingService ;
    private readonly IObjectMapper _objectMapper;

    public SaveLogCommandHandler(
        IIndexingService indexingService,
        IObjectMapper objectMapper
    )
    {
        _indexingService = indexingService;
        _objectMapper = objectMapper;
    }

    public async Task<Unit> Handle(SaveLogCommand command, CancellationToken cancellationToken)
    {
        var index = _objectMapper.Map<SaveLogCommand, AIChatLogIndex>(command);
        _indexingService.CheckExistOrCreateIndex(index);
        await SaveIndexAsync(index);
        return Unit.Value;
    }

    private async Task SaveIndexAsync(AIChatLogIndex index)
    {
        await _indexingService.SaveOrUpdateChatLogIndexAsync(index);
    }
}