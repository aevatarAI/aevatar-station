using System;
using Volo.Abp.Domain.Entities;

namespace Aevatar.ApiRequests;

public class ApiRequestSnapshot : Entity<Guid>
{
    public Guid OrganizationId { get; set; }
    public DateTime Time { get; set; }
    public long Count { get; set; }

    public ApiRequestSnapshot(Guid id)
        : base(id)
    {

    }
}