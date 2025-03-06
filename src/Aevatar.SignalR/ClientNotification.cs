namespace Aevatar.SignalR;

[GenerateSerializer]
public class ClientNotification(string target, object[] args)
{
    [Id(0)] public string Target { get; set; } = target;
    [Id(1)] public object[] Args { get; set; } = args;
}