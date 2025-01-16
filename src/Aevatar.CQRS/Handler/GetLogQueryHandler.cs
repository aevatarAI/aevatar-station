using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;
using Volo.Abp.ObjectMapping;

namespace Aevatar.CQRS.Handler;

public class GetLogQueryHandler : IRequestHandler<GetLogQuery, ChatLogPageResultDto>
{
    private readonly IIndexingService  _indexingService ;
    private readonly IObjectMapper _objectMapper;

    public GetLogQueryHandler(
        IIndexingService indexingService,
        IObjectMapper objectMapper
    )
    {
        _indexingService = indexingService;
        _objectMapper = objectMapper;

    }
    
    public async Task<ChatLogPageResultDto> Handle(GetLogQuery query, CancellationToken cancellationToken)
    {
        var queryInput = _objectMapper.Map<GetLogQuery, ChatLogQueryInputDto>(query);
        var indexResult = await _indexingService.QueryChatLogListAsync(queryInput);
        var aiChatLogIndexDtos = indexResult.ChatLogs.Select(i => _objectMapper.Map<AIChatLogIndex,AIChatLogIndexDto>(i)).ToList();
        return new ChatLogPageResultDto()
        {
            TotalRecordCount = indexResult.TotalCount,
            Data = aiChatLogIndexDtos
        };
    }
}