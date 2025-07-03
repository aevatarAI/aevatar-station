using System.Diagnostics;

namespace Aevatar.SignalR;

public sealed class ActivityScope(string operationName) : IDisposable
{
    private readonly Activity? _activity = new Activity(operationName).Start();

    public void Dispose() => _activity?.Dispose();
}