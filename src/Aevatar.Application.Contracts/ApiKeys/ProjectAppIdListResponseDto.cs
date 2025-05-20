using System;

namespace Aevatar.ApiKeys;

public class ProjectAppIdListResponseDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string AppName { get; set; }
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string CreatorName { get; set; }
    public DateTime CreateTime { get; set; }
}