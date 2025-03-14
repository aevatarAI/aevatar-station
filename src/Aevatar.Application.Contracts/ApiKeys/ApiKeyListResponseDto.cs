using System;
using System.Collections.Generic;

namespace Aevatar.ApiKeys;

public class ApiKeyListResponseDto
{
    public Guid ProjectId { get; set; }
    public string ApiKeyName { get; set; }
    public string ApiKey { get; set; }
    public string CreatorName { get; set; }
    public DateTime CreateTime { get; set; }
}