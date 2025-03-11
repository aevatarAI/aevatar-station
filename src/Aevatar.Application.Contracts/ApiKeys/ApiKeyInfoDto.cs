using System;

namespace Aevatar.ApiKeys;

public class ApiKeyInfoDto
{
    public Guid ProjectId { get; set; }
    public string ApiKeyName { get; set; }
    public string ApiKey { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime CreationTime { get; set; }
}