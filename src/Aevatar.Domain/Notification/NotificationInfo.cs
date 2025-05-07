using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Notification;

public class NotificationInfo : FullAuditedAggregateRoot<Guid>
{
    public NotificationTypeEnum Type { get; set; }
    
    public Dictionary<string,object> Input { get; set; }
    public string Content { get; set; }
    public Guid Receiver { get; set; }
    public NotificationStatusEnum Status { get; set; }
}