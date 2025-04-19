using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace Aevatar.ApiRequests;

public interface IApiRequestService
{
    Task<ListResultDto<ApiRequestDto>> GetListAsync(GetApiRequestDto input);
}