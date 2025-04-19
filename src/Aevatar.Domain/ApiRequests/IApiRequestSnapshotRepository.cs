using System;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.ApiRequests;

public interface IApiRequestSnapshotRepository : IRepository<ApiRequestSnapshot, Guid>
{
    
}