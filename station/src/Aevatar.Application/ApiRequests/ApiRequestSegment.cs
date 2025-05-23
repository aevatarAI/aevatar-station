using System;

namespace Aevatar.ApiRequests;

public class ApiRequestSegment
{
    public string AppId { get; set; }
    public DateTime SegmentTime { get; set; }
    public long Count { get; set; }
    
}