using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;
using Newtonsoft.Json;

namespace Aevatar.CQRS.Handler;

public class SaveStateCommandHandler : IRequestHandler<SaveStateCommand>
{
    private readonly IIndexingService  _indexingService ;

    public SaveStateCommandHandler(
        IIndexingService indexingService
    )
    {
        _indexingService = indexingService;
    }

    public async Task<Unit> Handle(SaveStateCommand request, CancellationToken cancellationToken)
    {
        _indexingService.CheckExistOrCreateStateIndex(request.State.GetType().Name);
        await SaveIndexAsync(request);
        return Unit.Value;
    }

    private async Task SaveIndexAsync(SaveStateCommand request)
    {
        var index = new BaseStateIndex
        {
            Id = request.Id,
            Ctime = DateTime.Now,
            State = JsonConvert.SerializeObject(request.State)
        };
        await _indexingService.SaveOrUpdateStateIndexAsync(request.State.GetType().Name, index);
    }
}