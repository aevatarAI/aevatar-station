using System.Collections.Generic;
using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.MCP.Options;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentConfigDtoPlus : ConfigurationBase
{
    [Id(0)] 
    [Description("System instructions that define the AI assistant's behavior and capabilities")]
    public string Instructions { get; set; } = "You are a helpful AI assistant";

    [Id(1)] 
    [Description("The system LLM configuration to use for AI chat functionality")]
    [DynamicDropDown("SystemLLMConfigs")]
    public string SystemLLM { get; set; } = "OpenAI";

    [Id(2)] 
    [Description("List of MCP (Model Context Protocol) servers to enable additional tools and capabilities")]
    public List<MCPServerConfig> MCPServers { get; set; } = [];
    
    [Id(3)] 
    [Description("List of GAgent types that can be used as tools by the AI chat agent")]
    public List<GrainType> ToolGAgentTypes { get; set; } = [];
    
    [Id(4)] 
    [Description("List of specific GAgent instances that can be invoked as tools by the AI chat agent")]
    public List<GrainId> ToolGAgents { get; set; } = [];
    
    [Id(5)]
    [Description("The member name for this ChatAI agent in the workflow")]
    public string MemberName { get; set; } = "ChatAI Agent";
}