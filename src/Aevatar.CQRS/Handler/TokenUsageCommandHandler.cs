using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using MediatR;
using Nest;

namespace Aevatar.CQRS.Handler;

public class TokenUsageCommandHandler : IRequestHandler<TokenUsageCommand>
{
    private readonly ICQRSProvider _cqrsProvider;
    private readonly IIndexingService _indexingService;

    public TokenUsageCommandHandler(ICQRSProvider cqrsProvider, IIndexingService indexingService)
    {
        _cqrsProvider = cqrsProvider;
        _indexingService = indexingService;
    }

    public async Task Handle(TokenUsageCommand request, CancellationToken cancellationToken)
    {
        var indexName = _cqrsProvider.GetIndexName("TokenUsage");
        await _indexingService.TryCreateTokenUsageIndexAsync(indexName);

        await _indexingService.SaveTokenUsageAsync(indexName, request.TokenUsages);
    }
}