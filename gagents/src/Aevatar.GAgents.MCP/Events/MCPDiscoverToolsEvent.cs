using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.MCP.GEvents;

[GenerateSerializer]
[Description("Discover available tools from MCP server")]
public class MCPDiscoverToolsEvent : EventWithResponseBase<MCPToolsDiscoveredEvent>;