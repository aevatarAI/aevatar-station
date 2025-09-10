namespace Aevatar.SignalR;

[GenerateSerializer]
public class AevatarSignalRResponse<TResponse> : ResponseToPublisherEventBase
    where TResponse : ResponseToPublisherEventBase
{
    [Id(0)] public bool IsSuccess { get; set; }
    [Id(1)] public TResponse? Response { get; set; }
    [Id(2)] public ErrorType ErrorType { get; set; }
    [Id(3)] public string? ErrorMessage { get; set; }
}

public enum ErrorType
{
    None,
    Framework,
    EventHandler
}