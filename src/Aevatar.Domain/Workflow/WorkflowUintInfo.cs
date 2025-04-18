namespace Aevatar.Workflow;

public class WorkflowUintInfo
{
    public string GrainId { get; set; }
    public string? NextGrainId { get; set; }
    public int XPosition { get; set; }
    public int YPosition { get; set; }
}