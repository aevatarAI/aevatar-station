using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using MediatR;

namespace Aevatar.CQRS.Handler;

public class TokenUsageCommandHandler : IRequestHandler<TokenUsageCommand>
{
    private readonly IIndexingService _indexingService;

    public TokenUsageCommandHandler(IIndexingService indexingService)
    {
        _indexingService = indexingService;
    }

    public async Task Handle(TokenUsageCommand request, CancellationToken cancellationToken)
    {
        // var indexName = _indexingService.GetIndexName("TokenUsage");
        // await _indexingService.TryCreateTokenUsageIndexAsync(indexName);
        //
        // await _indexingService.SaveTokenUsageAsync(indexName, request.TokenUsages);
    }
}