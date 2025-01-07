using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.User;

public class IdentityUserExtension:  FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    /// <summary>
    /// EOA Address or CA Address
    /// </summary>
    public string WalletAddress { get; set; }
    
    public IdentityUserExtension(Guid id)
    {
        Id = id;
    }
}