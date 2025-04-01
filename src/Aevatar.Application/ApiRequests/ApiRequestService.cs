using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Aevatar.ApiRequests;

[RemoteService(IsEnabled = false)]
public class ApiRequestService : AevatarAppService, IApiRequestService
{
    private readonly IApiRequestSnapshotRepository _apiRequestSnapshotRepository;

    public ApiRequestService(IApiRequestSnapshotRepository apiRequestSnapshotRepository)
    {
        _apiRequestSnapshotRepository = apiRequestSnapshotRepository;
    }

    public async Task<ListResultDto<ApiRequestDto>> GetListAsync(GetApiRequestDto input)
    {
        var organizationId = input.ProjectId.HasValue ? input.ProjectId.Value : input.OrganizationId.Value;
        
        var query = await _apiRequestSnapshotRepository.GetQueryableAsync();
        query = query.Where(o =>
            o.OrganizationId == organizationId && o.Time >= DateTimeHelper.FromUnixTimeMilliseconds(input.StartTime) &&
            o.Time <= DateTimeHelper.FromUnixTimeMilliseconds(input.EndTime))
            .OrderBy(o => o.Time);
        var list = query.ToList();

        return new ListResultDto<ApiRequestDto>
        {
            Items = ObjectMapper.Map<List<ApiRequestSnapshot>, List<ApiRequestDto>>(list)
        };
    }
}