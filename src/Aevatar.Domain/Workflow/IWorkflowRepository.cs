using System;
using System.Threading.Tasks;
using Aevatar.ApiKey;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.Workflow;

public interface IWorkflowRepository: IRepository<WorkflowInfo, Guid>
{
    Task<WorkflowInfo?> GetByWorkflowGrainId(string grainId);
}