using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.ApiKey;

public class ApiKeyInfo : FullAuditedAggregateRoot<Guid>
{
    public Guid ProjectId { get; set; }
    public string ApiKeyName { get; set; }
    public string ApiKey { get; set; }

    public ApiKeyInfo(Guid apiKeyId, Guid projectId, string apiKeyName, string apiKey) : base(apiKeyId)
    {
        ProjectId = projectId;
        ApiKeyName = apiKeyName;
        ApiKey = apiKey;
    }
}