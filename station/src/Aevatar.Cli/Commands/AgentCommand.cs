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
        // Check if non-interactive mode (with explicit agent type)
        var agentType = GetArgument(args, 1);
        if (!string.IsNullOrEmpty(agentType) && !HasOption(args, "interactive"))
        {
            await CreateAgentNonInteractiveAsync(args, agentType);
            return;
        }

        // Interactive mode
        await CreateAgentInteractiveAsync(args);
    }

    private async Task CreateAgentNonInteractiveAsync(CommandLineArgs args, string agentType)
    {
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
        Logger.LogInformation("   Type: {Type}", agent.AgentType);
        Logger.LogInformation("   Business Grain ID: {BusinessGrainId}", agent.BusinessGrainId);
    }

    private async Task CreateAgentInteractiveAsync(CommandLineArgs args)
    {
        Logger.LogInformation("🎯 交互式 Agent 创建向导");
        Logger.LogInformation("─────────────────────────");
        Logger.LogInformation("");

        // Step 1: Get available agent types
        Logger.LogInformation("📥 获取可用的 Agent 类型...");
        var response = await GetAsync<AbpApiResponse<List<AgentTypeDto>>>("/api/agent/agent-type-info-list");
        var agentTypes = response.Data ?? new List<AgentTypeDto>();

        if (!agentTypes.Any())
        {
            Logger.LogError("❌ 没有找到可用的 Agent 类型");
            return;
        }

        // Step 2: Let user select agent type
        var selectedAgentType = await SelectAgentTypeInteractiveAsync(agentTypes);
        if (selectedAgentType == null)
        {
            Logger.LogInformation("❌ Agent 创建已取消");
            return;
        }

        // Step 3: Collect basic information
        var agentName = await PromptForInputAsync("Agent 名称", $"{selectedAgentType.AgentType}-{DateTime.Now:MMdd-HHmm}");
        var agentDescription = await PromptForInputAsync("Agent 描述 (可选)", selectedAgentType.Description ?? "");
        var projectId = await PromptForInputAsync("项目 ID (可选)", "");

        // Step 4: Configure agent properties
        var configuration = await ConfigureAgentPropertiesInteractiveAsync(selectedAgentType);

        // Step 5: Show summary and confirm
        Logger.LogInformation("");
        Logger.LogInformation("📋 Agent 创建摘要:");
        Logger.LogInformation("─────────────────");
        Logger.LogInformation("类型: {Type}", selectedAgentType.AgentType);
        Logger.LogInformation("名称: {Name}", agentName);
        Logger.LogInformation("描述: {Description}", string.IsNullOrEmpty(agentDescription) ? "无" : agentDescription);
        Logger.LogInformation("项目: {ProjectId}", string.IsNullOrEmpty(projectId) ? "无" : projectId);
        Logger.LogInformation("配置参数: {Count} 个", configuration?.Count ?? 0);
        Logger.LogInformation("");

        var confirmed = await PromptForConfirmationAsync("确认创建这个 Agent?");
        if (!confirmed)
        {
            Logger.LogInformation("❌ Agent 创建已取消");
            return;
        }

        // Step 6: Create the agent
        var request = new CreateAgentDto
        {
            AgentType = selectedAgentType.AgentType,
            Name = agentName,
            Description = string.IsNullOrEmpty(agentDescription) ? null : agentDescription,
            ProjectId = string.IsNullOrEmpty(projectId) ? null : projectId,
            Configuration = configuration
        };

        try
        {
            Logger.LogInformation("🚀 正在创建 Agent...");
            var agent = await PostAsync<AgentDto>("/api/agent", request);

            Logger.LogInformation("");
            Logger.LogInformation("✅ Agent 创建成功!");
            Logger.LogInformation("─────────────────");
            Logger.LogInformation("🆔 Agent ID: {Id}", agent.Id);
            Logger.LogInformation("📛 名称: {Name}", agent.Name);
            Logger.LogInformation("🏷️  类型: {Type}", agent.AgentType);
            Logger.LogInformation("🔗 Business Grain ID: {BusinessGrainId}", agent.BusinessGrainId);
            Logger.LogInformation("");
            Logger.LogInformation("💡 你现在可以使用以下命令管理这个 Agent:");
            Logger.LogInformation("   aevatar agent get {Id}", agent.Id);
            Logger.LogInformation("   aevatar agent delete {Id} --confirm", agent.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError("❌ Agent 创建失败: {Message}", ex.Message);
            throw;
        }
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
        Logger.LogInformation("Type:        {Type}", agent.AgentType);
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
  --name <name>                    Agent name (non-interactive mode only)
  --description <desc>             Agent description (non-interactive mode only)
  --project-id <id>                Project ID (non-interactive mode only)
  --config <json>                  Configuration JSON (non-interactive mode only)
  --interactive                    Force interactive mode even with agent type specified

Delete Options:
  --confirm                        Confirm deletion

Global Options:
  --json                           Output in JSON format

Examples:
  aevatar agent types
  aevatar agent list --project-id abc123
  
  # Interactive mode (recommended)
  aevatar agent create
  aevatar agent create --interactive
  
  # Non-interactive mode
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

    // Interactive helper methods
    private async Task<AgentTypeDto?> SelectAgentTypeInteractiveAsync(List<AgentTypeDto> agentTypes)
    {
        Logger.LogInformation("📋 可用的 Agent 类型:");
        Logger.LogInformation("");

        // Show simplified list for selection
        for (int i = 0; i < agentTypes.Count; i++)
        {
            var type = agentTypes[i];
            var paramCount = type.AgentParams?.Count ?? 0;
            var shortDescription = TruncateString(type.Description ?? "", 80);
            
            Logger.LogInformation("{Index}. {Type}", i + 1, type.AgentType);
            Logger.LogInformation("   📄 {Description}", shortDescription);
            Logger.LogInformation("   ⚙️  参数数量: {Count}", paramCount);
            Logger.LogInformation("");
        }

        while (true)
        {
            var input = await PromptForInputAsync($"请选择 Agent 类型 (1-{agentTypes.Count}, 或 'q' 退出)", "");
            
            if (input.ToLowerInvariant() == "q")
            {
                return null;
            }

            if (int.TryParse(input, out var index) && index >= 1 && index <= agentTypes.Count)
            {
                var selected = agentTypes[index - 1];
                Logger.LogInformation("✅ 已选择: {Type}", selected.AgentType);
                return selected;
            }

            Logger.LogWarning("❌ 无效选择，请输入 1-{Count} 之间的数字", agentTypes.Count);
        }
    }

    private async Task<Dictionary<string, object>?> ConfigureAgentPropertiesInteractiveAsync(AgentTypeDto agentType)
    {
        if (agentType.AgentParams == null || !agentType.AgentParams.Any())
        {
            Logger.LogInformation("ℹ️  此 Agent 类型无需配置参数");
            return null;
        }

        Logger.LogInformation("");
        Logger.LogInformation("⚙️  配置 Agent 参数:");
        Logger.LogInformation("─────────────────");

        var configuration = new Dictionary<string, object>();

        // Parse default values if available
        var defaultValues = new Dictionary<string, object>();
        if (agentType.DefaultValues != null)
        {
            try
            {
                var defaultJson = JsonSerializer.Serialize(agentType.DefaultValues);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(defaultJson);
                if (parsed != null)
                {
                    defaultValues = parsed;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Failed to parse default values: {Error}", ex.Message);
            }
        }

        foreach (var param in agentType.AgentParams)
        {
            var defaultValue = GetDefaultValueForParameter(param, defaultValues);
            var userInput = await PromptForParameterAsync(param, defaultValue);
            
            if (!string.IsNullOrEmpty(userInput))
            {
                configuration[param.Name] = ConvertValueToType(userInput, param.Type);
            }
        }

        return configuration.Any() ? configuration : null;
    }

    private string? GetDefaultValueForParameter(AgentParamDto param, Dictionary<string, object> defaultValues)
    {
        if (defaultValues.TryGetValue(param.Name, out var value))
        {
            return value?.ToString();
        }
        
        // Common default values based on type
        return param.Type switch
        {
            var t when t.Contains("String") => "",
            var t when t.Contains("Int") => "0",
            var t when t.Contains("Boolean") => "false",
            var t when t.Contains("List") => "[]",
            _ => ""
        };
    }

    private async Task<string> PromptForParameterAsync(AgentParamDto param, string? defaultValue)
    {
        var typeDescription = GetTypeDescription(param.Type);
        var prompt = $"{param.Name} ({typeDescription})";
        
        if (!string.IsNullOrEmpty(defaultValue))
        {
            prompt += $" [默认: {defaultValue}]";
        }

        return await PromptForInputAsync(prompt, defaultValue ?? "");
    }

    private string GetTypeDescription(string type)
    {
        return type switch
        {
            var t when t.Contains("String") => "文本",
            var t when t.Contains("Int32") => "整数",
            var t when t.Contains("Boolean") => "true/false",
            var t when t.Contains("List") => "列表",
            var t when t.Contains("Guid") => "GUID",
            var t when t.Contains("TimeSpan") => "时间跨度 (如: 00:30:00)",
            _ => "对象"
        };
    }

    private object ConvertValueToType(string value, string type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            return type switch
            {
                var t when t.Contains("Int32") => int.Parse(value),
                var t when t.Contains("Boolean") => bool.Parse(value),
                var t when t.Contains("Guid") => Guid.Parse(value),
                var t when t.Contains("TimeSpan") => TimeSpan.Parse(value),
                var t when t.Contains("List") && value.StartsWith('[') => JsonSerializer.Deserialize<object>(value) ?? value,
                _ => value
            };
        }
        catch
        {
            return value; // Fallback to string
        }
    }

    private Task<string> PromptForInputAsync(string prompt, string defaultValue)
    {
        Logger.LogInformation($"💬 {prompt}:");
        Console.Write("   > ");
        
        var input = Console.ReadLine();
        return Task.FromResult(string.IsNullOrEmpty(input) ? defaultValue : input);
    }

    private Task<bool> PromptForConfirmationAsync(string message)
    {
        Logger.LogInformation($"❓ {message} (y/N):");
        Console.Write("   > ");
        
        var input = Console.ReadLine();
        return Task.FromResult(!string.IsNullOrEmpty(input) && input.ToLowerInvariant().StartsWith('y'));
    }

    private string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }
        
        return text.Substring(0, maxLength - 3) + "...";
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
        public string AgentType { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? BusinessGrainId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModified { get; set; }
        public Dictionary<string, object>? Configuration { get; set; }
    }
}
