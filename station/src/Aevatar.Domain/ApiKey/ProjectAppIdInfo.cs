using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.ApiKey;

public class ProjectAppIdInfo : FullAuditedAggregateRoot<Guid>
{
    public Guid ProjectId { get; set; }
    public string AppName { get; set; }
    public string AppId { get; set; }
    public string AppSecret { get; set; }

    public ProjectAppIdInfo(Guid apiKeyId, Guid projectId, string appName, string appId, string appSecret) : base(apiKeyId)
    {
        ProjectId = projectId;
        AppName = appName;
        AppId = appId;
        AppSecret = appSecret;
    }
}