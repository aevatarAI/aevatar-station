// using Aevatar.Core.Abstractions;
// using Aevatar.SignalR.Clients;
// using Aevatar.SignalR.ConnectionGroups;
// using Microsoft.AspNetCore.SignalR;
//
// namespace Aevatar.SignalR.Core;
//
// public static class GAgentFactoryExtensions
// {
//     public static HubContext<THub> GetHub<THub>(this IGAgentFactory gAgentFactory) where THub : Hub
//     {
//         return new HubContext<THub>(gAgentFactory);
//     }
//
//     internal static ISignalRClientGAgent GetClientGrain(this IGAgentFactory factory, string hubName, string connectionId)
//     {
//         var key = new SignalRClientKey { HubType = hubName, ConnectionId = connectionId }.ToGrainPrimaryKey();
//         return factory.GetGAgentAsync<ISignalRClientGAgent>(key, configuration: new SignalRClientGAgentConfiguration
//         {
//             ConnectionId = connectionId,
//         }).Result;
//     }
//
//     internal static IConnectionGroupGrain GetGroupGrain(this IGAgentFactory factory, string hubName, string groupName)
//     {
//         var key = new ConnectionGroupKey { GroupId = groupName, HubType = hubName, GroupType = ConnectionGroupType.NamedGroup }.ToPrimaryGrainKey();
//         return factory.GetGrain<IConnectionGroupGrain>(key);
//     }
//
//     internal static IConnectionGroupGrain GetUserGrain(this IGAgentFactory factory, string hubName, string userId)
//     {
//         var key = new ConnectionGroupKey { GroupId = userId, HubType = hubName, GroupType = ConnectionGroupType.AuthenticatedUser }.ToPrimaryGrainKey();
//         return factory.GetGrain<IConnectionGroupGrain>(key);
//     }
//
//     internal static IServerDirectoryGrain GetServerDirectoryGrain(this IGAgentFactory factory)
//         => factory.GetGrain<IServerDirectoryGrain>(0);
// }