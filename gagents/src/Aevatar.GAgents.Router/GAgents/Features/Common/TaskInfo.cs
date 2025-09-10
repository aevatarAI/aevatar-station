namespace Aevatar.GAgents.Router.GAgents.Features.Common;

[GenerateSerializer]
public class TaskInfo
{
    [Id(0)] public string TaskDescription { get; set; }
    [Id(1)] public List<RouterRecord> History { get; set; } = new();
}

[GenerateSerializer]
public class RouterRecord : EventSchema
{
    [Id(4)] public string ProcessResult { get; set; }
}


