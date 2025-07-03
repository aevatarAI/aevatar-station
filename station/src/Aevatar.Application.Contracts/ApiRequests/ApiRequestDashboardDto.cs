using System.Collections.Generic;

namespace Aevatar.ApiRequests;

public class ApiRequestDashboardDto
{
    public long TotalRequests { get; set; }
    public List<ApiRequestDto> Requests { get; set; }
}