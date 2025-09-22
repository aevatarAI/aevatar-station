namespace Aevatar.Application.Contracts.Query;

public class WorkflowLogQueryDto
{
    public string WorkflowId { get; set; } = string.Empty;
    public long? RoundId { get; set; }
    public string? GrainId { get; set; }
    public string? Level { get; set; }
    public string? MessagePattern { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}
