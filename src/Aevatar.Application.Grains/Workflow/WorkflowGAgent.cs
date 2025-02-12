using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workflow;
using Aevatar.Workflow.GEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Workflow;

public class WorkflowGAgent :  GAgentBase<WorkflowGAgentState, WorkflowAgentGEvent>, IWorkflowGAgent
{
    private readonly ILogger<WorkflowGAgent> _logger;
    
    public WorkflowGAgent(ILogger<WorkflowGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This agent is responsible for generating and managing workflow.");
    }
    
    public Task<WorkflowGAgentState> GetWorkflowAsync()
    {
        return Task.FromResult(State);
    }
    
    public async Task SetWorkflowAsync(WorkflowDto workflowDto)
    {
        _logger.LogInformation("SetWorkflowAsync");
        RaiseEvent(new SetWorkflowGEvent()
        {
            WorkflowId = this.GetPrimaryKey(),
            WorkflowName = workflowDto.WorkflowName,
            TriggerEvent = workflowDto.TriggerEvent,
            EventFlow = workflowDto.EventFlow
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(WorkflowGAgentState state, StateLogEventBase<WorkflowAgentGEvent> @event)
    {
        switch (@event)
        {
            case SetWorkflowGEvent setWorkflowGEvent:
                State.WorkflowId = setWorkflowGEvent.Id;
                State.WorkflowName = setWorkflowGEvent.WorkflowName;
                State.TriggerEvent = setWorkflowGEvent.TriggerEvent;
                State.EventFlow = setWorkflowGEvent.EventFlow;
                State.CreateTime = DateTime.Now;
                break;
        }
    }
}

public interface IWorkflowGAgent : IStateGAgent<WorkflowGAgentState>
{
    Task SetWorkflowAsync(WorkflowDto workflowDto);
    Task<WorkflowGAgentState> GetWorkflowAsync();
}