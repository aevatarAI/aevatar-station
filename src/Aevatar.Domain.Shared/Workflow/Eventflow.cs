using Orleans;

namespace Aevatar.Workflow;


[GenerateSerializer]
public class EventFlow
{
    [Id(0)] public string EventName { get; set; }
    [Id(1)] public string ResponsibleAgent { get; set; }
    [Id(2)] public string Action { get; set; }
    [Id(3)] public string OutputEvent { get; set; }
}
