using System;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("WorkflowView")]
[Route("api/workflow-view")]
[Authorize]
public class WorkflowViewController : AevatarController
{
    private readonly IWorkflowViewService _workflowViewService;

    public WorkflowViewController(IWorkflowViewService workflowViewService)
    {
        _workflowViewService = workflowViewService;
    }

    [HttpPost]
    [Route("{guid}/publish-workflow")]
    public virtual Task<AgentDto> PublishWorkflowAsync(Guid guid)
    {
        return _workflowViewService.PublishWorkflowAsync(guid);
    }
}