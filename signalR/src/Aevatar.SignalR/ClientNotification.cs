namespace Aevatar.SignalR;

[GenerateSerializer]
public class ClientNotification(string target, string[] args)
{
    [Id(0)] public string Target { get; set; } = target;
    // TODO: Remove this.
    [Id(1)] public object[] Args { get; set; }
    [Id(2)] public string[] Arguments { get; set; } = args;
}