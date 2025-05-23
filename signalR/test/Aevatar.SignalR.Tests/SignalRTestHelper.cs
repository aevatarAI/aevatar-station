using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aevatar.SignalR.Tests;

public static class SignalRTestHelper
{
    public static HubConnectionContext CreateHubConnectionContext(ConnectionContext connection)
    {
        var ctx = new HubConnectionContext(connection,
            new HubConnectionContextOptions
            {
                ClientTimeoutInterval = TimeSpan.FromSeconds(15)
            },
            NullLoggerFactory.Instance);
        var protocolProp = ctx
            .GetType()
            .GetProperty("Protocol", BindingFlags.Instance |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Public)!;
        protocolProp.SetValue(ctx, new JsonHubProtocol());
        return ctx;
    }
}