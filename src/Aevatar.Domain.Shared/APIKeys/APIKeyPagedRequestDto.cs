using System;
using Volo.Abp.Application.Dtos;

namespace Aevatar.APIKeys;

public class APIKeyPagedRequestDto : PagedResultRequestDto
{
    public Guid ProjectId { get; set; }
}