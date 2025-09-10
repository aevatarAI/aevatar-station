using Microsoft.AspNetCore.SignalR;
using Aevatar.SignalR.Core;
using Aevatar.SignalR.Clients;
using Aevatar.SignalR.ConnectionGroups;

// ReSharper disable once CheckNamespace
namespace Orleans;

public static class GrainFactoryExtensions
{
    internal static IClientGrain GetClientGrain(this IGrainFactory factory, string hubName, string connectionId)
    {
        var key = new ClientKey { HubType = hubName, ConnectionId = connectionId }.ToGrainPrimaryKey();
        return factory.GetGrain<IClientGrain>(key);
    }

    internal static IConnectionGroupGrain GetGroupGrain(this IGrainFactory factory, string hubName, string groupName)
    {
        var key = new ConnectionGroupKey { GroupId = groupName, HubType = hubName, GroupType = ConnectionGroupType.NamedGroup }.ToPrimaryGrainKey();
        return factory.GetGrain<IConnectionGroupGrain>(key);
    }

    internal static IConnectionGroupGrain GetUserGrain(this IGrainFactory factory, string hubName, string userId)
    {
        var key = new ConnectionGroupKey { GroupId = userId, HubType = hubName, GroupType = ConnectionGroupType.AuthenticatedUser }.ToPrimaryGrainKey();
        return factory.GetGrain<IConnectionGroupGrain>(key);
    }

    internal static IServerDirectoryGrain GetServerDirectoryGrain(this IGrainFactory factory)
        => factory.GetGrain<IServerDirectoryGrain>(0);
}
