using System;

namespace Aevatar.ApiRequests;

public class GetApiRequestDto
{
    public Guid? OrganizationId { get; set; }
    public Guid? ProjectId { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
}