namespace Aevatar.SignalR;

public interface ISignalRMessage<T>
{
    string MessageType { get; }
    T Data { get; }
}