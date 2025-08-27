namespace Aevatar.Options;

public class ProjectorBatchOptions
{
    public int BatchSize { get; set; } = 5;
    public int BatchTimeoutSeconds { get; set; } = 1;
}