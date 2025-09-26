using System.Text.Json;
using Aevatar.Cli.Args;
using Aevatar.Cli.Auth;
using Microsoft.Extensions.Logging;

namespace Aevatar.Cli.Commands;

public class AgentCommand : BaseHttpCommand
{
    public const string Name = "agent";

    public AgentCommand(AuthenticationService authService, IHttpClientFactory httpClientFactory)
        : base(authService, httpClientFactory)
    {
    }

    public override async Task ExecuteAsync(CommandLineArgs commandLineArgs)
    {
        var subCommand = GetArgument(commandLineArgs, 0);
        
        Logger.LogInformation("🔧 Debug: AgentCommand.ExecuteAsync called with subCommand: '{SubCommand}'", subCommand ?? "null");
        Logger.LogInformation("🔧 Debug: CommandLineArgs - Command: '{Command}', Target: '{Target}'", commandLineArgs.Command, commandLineArgs.Target);
        
        if (string.IsNullOrEmpty(subCommand))
        {
            Logger.LogInformation(GetUsageInfo());
            return;
        }

        try
        {
            Logger.LogDebug("Executing agent subcommand: {SubCommand}", subCommand);
            switch (subCommand.ToLowerInvariant())
            {
                case "types":
                    await ListAgentTypesAsync(commandLineArgs);
                    break;
                case "list":
                    await ListAgentInstancesAsync(commandLineArgs);
                    break;
                case "create":
                    await CreateAgentAsync(commandLineArgs);
                    break;
                case "get":
                    await GetAgentAsync(commandLineArgs);
                    break;
                case "delete":
                    await DeleteAgentAsync(commandLineArgs);
                    break;
                default:
                    Logger.LogWarning("Unknown agent subcommand: {SubCommand}", subCommand);
                    Logger.LogInformation(GetUsageInfo());
                    break;
            }
        }
        catch (CliUsageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing agent command: {Message}", ex.Message);
            throw new CliUsageException($"Agent command failed: {ex.Message}", ex);
        }
    }

    private async Task ListAgentTypesAsync(CommandLineArgs args)
    {
        Logger.LogInformation("Retrieving available agent types...");
        Logger.LogDebug("Calling API: /api/agent/agent-type-info-list");
        
        var response = await GetAsync<AbpApiResponse<List<AgentTypeDto>>>("/api/agent/agent-type-info-list");
        Logger.LogDebug("API response received. Data count: {Count}", response?.Data?.Count ?? 0);
        
        var agentTypes = response.Data ?? new List<AgentTypeDto>();
        
        // Generate timestamped filename
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"aevatar_agent_types_{timestamp}.json";
        var currentDirectory = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectory, fileName);
        
        // Always save to file with complete information
        var fileData = new
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Count = agentTypes.Count,
            AgentTypes = agentTypes
        };
        
        try
        {
            var json = JsonSerializer.Serialize(fileData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            await File.WriteAllTextAsync(filePath, json);
            
            Logger.LogInformation("💾 Agent types 信息已保存到文件:");
            Logger.LogInformation("📁 文件路径: {FilePath}", filePath);
            Logger.LogInformation("📊 包含 {Count} 个 Agent 类型", agentTypes.Count);
        }
        catch (Exception ex)
        {
            Logger.LogWarning("保存文件失败: {Error}", ex.Message);
        }
        
        // Display output based on format option
        if (HasOption(args, "json"))
        {
            OutputJson(agentTypes);
            return;
        }
        
        Logger.LogInformation("");
        Logger.LogInformation("💡 完整的 Agent 类型信息(包括参数和配置模式)已保存到: {FileName}", fileName);
    }
    

    private async Task ListAgentInstancesAsync(CommandLineArgs args)
    {
        Logger.LogInformation("Retrieving agent instances...");
        
        var parameters = new Dictionary<string, string>();
        
        var projectId = GetOption(args, "project-id");
        if (!string.IsNullOrEmpty(projectId))
        {
            parameters["projectId"] = projectId;
        }
        
        var organizationId = GetOption(args, "organization-id");
        if (!string.IsNullOrEmpty(organizationId))
        {
            parameters["organizationId"] = organizationId;
        }
        
        var status = GetOption(args, "status");
        if (!string.IsNullOrEmpty(status))
        {
            parameters["status"] = status;
        }

        var response = await GetAsync<AbpApiResponse<List<AgentInstanceDto>>>("/api/agent/agent-list", parameters);
        var agents = response.Data ?? new List<AgentInstanceDto>();
        
        if (HasOption(args, "json"))
        {
            OutputJson(agents);
            return;
        }

        OutputTable(agents,
            ("Agent ID", a => a.Id),
            ("Name", a => a.Name),
            ("Type", a => a.AgentType),
            ("Business Grain ID", a => a.BusinessAgentGrainId ?? ""),
            ("Created", a => a.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "")
        );
    }

    private async Task CreateAgentAsync(CommandLineArgs args)
    {
        var agentType = GetArgument(args, 1);
        if (string.IsNullOrEmpty(agentType))
        {
            throw new CliUsageException("Agent type is required. Usage: aevatar agent create <agent-type> [options]");
        }

        var request = new CreateAgentDto
        {
            AgentType = agentType,
            Name = GetOption(args, "name") ?? $"Agent-{DateTime.Now:yyyyMMdd-HHmmss}",
            Description = GetOption(args, "description"),
            ProjectId = GetOption(args, "project-id")
        };

        var configJson = GetOption(args, "config");
        if (!string.IsNullOrEmpty(configJson))
        {
            try
            {
                request.Configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
            }
            catch (JsonException ex)
            {
                throw new CliUsageException($"Invalid JSON configuration: {ex.Message}", ex);
            }
        }

        Logger.LogInformation("Creating agent '{Name}' of type '{Type}'...", request.Name, agentType);
        
        var agent = await PostAsync<AgentDto>("/api/agent", request);
        
        if (HasOption(args, "json"))
        {
            OutputJson(agent);
            return;
        }

        Logger.LogInformation("✅ Agent created successfully!");
        Logger.LogInformation("   ID: {Id}", agent.Id);
        Logger.LogInformation("   Name: {Name}", agent.Name);
        Logger.LogInformation("   Type: {Type}", agent.Type);
        Logger.LogInformation("   Status: {Status}", agent.Status);
    }

    private async Task GetAgentAsync(CommandLineArgs args)
    {
        var agentId = GetArgument(args, 1);
        if (string.IsNullOrEmpty(agentId))
        {
            throw new CliUsageException("Agent ID is required. Usage: aevatar agent get <agent-id>");
        }

        Logger.LogInformation("Retrieving agent {AgentId}...", agentId);
        
        var agent = await GetAsync<AgentDto>($"/api/agent/{agentId}");
        
        if (HasOption(args, "json"))
        {
            OutputJson(agent);
            return;
        }

        Logger.LogInformation("");
        Logger.LogInformation("Agent Details:");
        Logger.LogInformation("─────────────────");
        Logger.LogInformation("ID:          {Id}", agent.Id);
        Logger.LogInformation("Name:        {Name}", agent.Name);
        Logger.LogInformation("Type:        {Type}", agent.Type);
        Logger.LogInformation("Status:      {Status}", agent.Status);
        Logger.LogInformation("Description: {Description}", agent.Description ?? "N/A");
        Logger.LogInformation("Created:     {Created}", agent.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");
        Logger.LogInformation("Updated:     {Updated}", agent.LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");
        
        if (agent.Configuration != null && agent.Configuration.Any())
        {
            Logger.LogInformation("");
            Logger.LogInformation("Configuration:");
            var configJson = JsonSerializer.Serialize(agent.Configuration, JsonOptions);
            Logger.LogInformation(configJson);
        }
    }

    private async Task DeleteAgentAsync(CommandLineArgs args)
    {
        var agentId = GetArgument(args, 1);
        if (string.IsNullOrEmpty(agentId))
        {
            throw new CliUsageException("Agent ID is required. Usage: aevatar agent delete <agent-id>");
        }

        if (!HasOption(args, "confirm"))
        {
            Logger.LogWarning("This will permanently delete the agent. Use --confirm to proceed.");
            return;
        }

        Logger.LogInformation("Deleting agent {AgentId}...", agentId);
        
        await DeleteAsync($"/api/agent/{agentId}");
        
        Logger.LogInformation("✅ Agent {AgentId} deleted successfully.", agentId);
    }

    public override string GetUsageInfo()
    {
        return @"
Usage: aevatar agent <subcommand> [options] [arguments]

Subcommands:
  types                             List all available agent types
  list                             List agent instances
  create <agent-type>              Create a new agent
  get <agent-id>                   Get agent details
  delete <agent-id>                Delete an agent

List Options:
  --project-id <id>                Filter by project ID
  --organization-id <id>           Filter by organization ID  
  --status <status>                Filter by status

Create Options:
  --name <name>                    Agent name (default: auto-generated)
  --description <desc>             Agent description
  --project-id <id>                Project ID
  --config <json>                  Configuration JSON

Delete Options:
  --confirm                        Confirm deletion

Global Options:
  --json                           Output in JSON format

Examples:
  aevatar agent types
  aevatar agent list --project-id abc123
  aevatar agent create ChatAgent --name MyBot --description ""Chat assistant""
  aevatar agent get agent-id-123
  aevatar agent delete agent-id-123 --confirm
";
    }

    public static string GetShortDescription()
    {
        return "Manage agents (list types, create, get, delete)";
    }

    // DTOs matching the API contracts
    private class AbpApiResponse<T>
    {
        public string? Code { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }

    private class AgentTypeDto
    {
        public string AgentType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<AgentParamDto>? AgentParams { get; set; }
        public string? PropertyJsonSchema { get; set; }
        public object? DefaultValues { get; set; }
    }
    
    private class AgentParamDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    private class AgentInstanceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        public object? Properties { get; set; }
        public string? BusinessAgentGrainId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ProjectId { get; set; }
    }

    private class CreateAgentDto
    {
        public string AgentType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ProjectId { get; set; }
        public Dictionary<string, object>? Configuration { get; set; }
    }

    private class AgentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModified { get; set; }
        public Dictionary<string, object>? Configuration { get; set; }
    }
}
