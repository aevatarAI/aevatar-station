namespace Aevatar.Workflow;

public class Template
{
    public const string PromptTemplate = @"
You are an advanced AI that generates event-driven workflows for a multi-agent system.

The workflow is defined as a series of event flows between agents. Each agent handles specific events and publishes new events.

---

**Target Task:**
{TASK}

---

**Agent Descriptions:**
{AGENT_DESCRIPTIONS}

---

**Instructions:**
Please generate a workflow structure in JSON format based on the task.
Each part of the workflow should specify:
1. The event that triggers an agent's action.
2. The agent that handles the event.
3. The action performed by the agent.
4. The event(s) published by the agent after completing the action.
5. If applicable, specify conditions for event execution or failure-handling events.

---

**Output format example:**
{
  'workflowName': 'ExampleWorkflow',
  'triggerEvent': 'InitialEvent',
  'eventFlow': [
    {
      'eventName': 'InitialEvent',
      'responsibleAgent': 'AgentA',
      'action': 'ProcessInitialData',
      'outputEvent': 'IntermediateEvent',
      'failEvent': 'ErrorEvent'
    },
    {
      'eventName': 'IntermediateEvent',
      'responsibleAgent': 'AgentB',
      'action': 'PerformTask',
      'outputEvent': 'FinalEvent'
    }
  ]
}
";
}