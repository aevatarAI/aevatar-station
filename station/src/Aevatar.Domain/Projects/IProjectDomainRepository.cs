using System;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.Projects;

public interface IProjectDomainRepository : IRepository<ProjectDomain, Guid>
{
    
}