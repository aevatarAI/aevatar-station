using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Dto;
using MediatR;
using Volo.Abp.ObjectMapping;

namespace Aevatar.CQRS.Handler;

public class GetLogQueryHandler : IRequestHandler<GetDataQuery, string>
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
    
    public async Task<string> Handle(GetDataQuery request, CancellationToken cancellationToken)
    {
        var indexResult = await _indexingService.GetSortDataDocumentsAsync<BaseIndex>(request.Query,sortFunc:request.Sort,skip:request.Skip, limit: request.Limit);
        return indexResult;
        /*var aiChatLogIndexDtos = indexResult.ChatLogs.Select(i => _objectMapper.Map<AIChatLogIndex,AIChatLogIndexDto>(i)).ToList();
        return new ChatLogPageResultDto()
        {
            TotalRecordCount = indexResult.TotalCount,
            Data = aiChatLogIndexDtos
        };*/
    }
}