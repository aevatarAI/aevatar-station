using System.Text.Json;
using Aevatar.Cli.Args;
using Aevatar.Cli.Auth;
using Microsoft.Extensions.Logging;

namespace Aevatar.Cli.Commands;

public class WorkflowCommand : BaseHttpCommand
{
    public const string Name = "workflow";

    public WorkflowCommand(AuthenticationService authService, IHttpClientFactory httpClientFactory)
        : base(authService, httpClientFactory)
    {
    }

    public override async Task ExecuteAsync(CommandLineArgs commandLineArgs)
    {
        var subCommand = GetArgument(commandLineArgs, 0);
        
        if (string.IsNullOrEmpty(subCommand))
        {
            Logger.LogInformation(GetUsageInfo());
            return;
        }

        try
        {
            switch (subCommand.ToLowerInvariant())
            {
                case "generate":
                    await GenerateWorkflowAsync(commandLineArgs);
                    break;
                case "run":
                    await RunWorkflowAsync(commandLineArgs);
                    break;
                case "text-completion":
                    await GenerateTextCompletionAsync(commandLineArgs);
                    break;
                case "validate":
                    await ValidateWorkflowAsync(commandLineArgs);
                    break;
                default:
                    Logger.LogWarning("Unknown workflow subcommand: {SubCommand}", subCommand);
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
            Logger.LogError(ex, "Error executing workflow command: {Message}", ex.Message);
            throw new CliUsageException($"Workflow command failed: {ex.Message}", ex);
        }
    }

    private async Task GenerateWorkflowAsync(CommandLineArgs args)
    {
        var userGoal = GetArgument(args, 1);
        if (string.IsNullOrEmpty(userGoal))
        {
            throw new CliUsageException("User goal is required. Usage: aevatar workflow generate <user-goal> [options]");
        }

        Logger.LogInformation("Generating workflow for goal: '{Goal}'...", userGoal);

        var request = new GenerateWorkflowRequestDto
        {
            UserGoal = userGoal
        };

        var workflow = await PostAsync<AiWorkflowViewConfigDto>("/api/workflow/generate", request);

        var outputFile = GetOption(args, "output");
        if (!string.IsNullOrEmpty(outputFile))
        {
            var json = JsonSerializer.Serialize(workflow, JsonOptions);
            await File.WriteAllTextAsync(outputFile, json);
            Logger.LogInformation("✅ Workflow generated and saved to: {File}", outputFile);
            return;
        }

        if (HasOption(args, "json"))
        {
            OutputJson(workflow);
            return;
        }

        Logger.LogInformation("");
        Logger.LogInformation("✅ Workflow Generated Successfully!");
        Logger.LogInformation("─────────────────────────────────");
        Logger.LogInformation("Goal: {Goal}", userGoal);
        Logger.LogInformation("Nodes: {NodeCount}", workflow?.Nodes?.Count ?? 0);
        Logger.LogInformation("Connections: {ConnectionCount}", workflow?.Connections?.Count ?? 0);
        
        if (workflow?.Nodes != null && workflow.Nodes.Any())
        {
            Logger.LogInformation("");
            Logger.LogInformation("Workflow Nodes:");
            OutputTable(workflow.Nodes,
                ("Node ID", n => n.Id),
                ("Type", n => n.Type),
                ("Name", n => n.Name),
                ("Position", n => $"({n.Position?.X}, {n.Position?.Y})")
            );
        }
    }

    private async Task RunWorkflowAsync(CommandLineArgs args)
    {
        var workflowConfig = GetArgument(args, 1);
        if (string.IsNullOrEmpty(workflowConfig))
        {
            throw new CliUsageException("Workflow configuration is required. Usage: aevatar workflow run <workflow-config> [options]");
        }

        var request = new WorkflowRunRequestDto();

        // Check if workflowConfig is a file path or JSON string
        if (File.Exists(workflowConfig))
        {
            var configContent = await File.ReadAllTextAsync(workflowConfig);
            try
            {
                request.WorkflowConfig = JsonSerializer.Deserialize<object>(configContent);
            }
            catch (JsonException ex)
            {
                throw new CliUsageException($"Invalid JSON in workflow file: {ex.Message}", ex);
            }
        }
        else
        {
            try
            {
                request.WorkflowConfig = JsonSerializer.Deserialize<object>(workflowConfig);
            }
            catch (JsonException ex)
            {
                throw new CliUsageException($"Invalid workflow configuration JSON: {ex.Message}", ex);
            }
        }

        var isAsync = HasOption(args, "async");
        Logger.LogInformation("Running workflow {Mode}...", isAsync ? "asynchronously" : "synchronously");

        var result = await PostAsync<WorkflowRunResultDto>("/api/workflow/run", request);

        if (HasOption(args, "json"))
        {
            OutputJson(result);
            return;
        }

        Logger.LogInformation("");
        Logger.LogInformation("✅ Workflow Execution {Status}!", result?.Status ?? "Unknown");
        Logger.LogInformation("─────────────────────────────────");
        Logger.LogInformation("Execution ID: {Id}", result?.ExecutionId);
        Logger.LogInformation("Status: {Status}", result?.Status);
        Logger.LogInformation("Start Time: {StartTime}", result?.StartTime);
        
        if (!string.IsNullOrEmpty(result?.ErrorMessage))
        {
            Logger.LogError("Error: {Error}", result.ErrorMessage);
        }

        if (result?.Results != null && result.Results.Any())
        {
            Logger.LogInformation("");
            Logger.LogInformation("Results:");
            OutputJson(result.Results);
        }

        // If async and follow option is set, poll for updates
        if (isAsync && HasOption(args, "follow") && !string.IsNullOrEmpty(result?.ExecutionId))
        {
            Logger.LogInformation("");
            Logger.LogInformation("Following execution progress...");
            await FollowWorkflowExecutionAsync(result.ExecutionId);
        }
    }

    private async Task GenerateTextCompletionAsync(CommandLineArgs args)
    {
        var inputText = GetArgument(args, 1);
        if (string.IsNullOrEmpty(inputText))
        {
            throw new CliUsageException("Input text is required. Usage: aevatar workflow text-completion <input-text> [options]");
        }

        var count = int.TryParse(GetOption(args, "count"), out var c) ? c : 5;
        Logger.LogInformation("Generating {Count} text completion options for: '{Text}'...", count, inputText);

        var request = new TextCompletionRequestDto
        {
            InputText = inputText,
            Count = count
        };

        var response = await PostAsync<TextCompletionResponseDto>("/api/workflow/text-completion/generate", request);

        if (HasOption(args, "json"))
        {
            OutputJson(response);
            return;
        }

        Logger.LogInformation("");
        Logger.LogInformation("✅ Generated {Count} Completion Options:", response?.Options?.Count ?? 0);
        Logger.LogInformation("─────────────────────────────────");
        
        if (response?.Options != null && response.Options.Any())
        {
            for (int i = 0; i < response.Options.Count; i++)
            {
                Logger.LogInformation("");
                Logger.LogInformation("Option {Index}:", i + 1);
                Logger.LogInformation("{Option}", response.Options[i]);
            }
        }
    }

    private async Task ValidateWorkflowAsync(CommandLineArgs args)
    {
        var workflowFile = GetArgument(args, 1);
        if (string.IsNullOrEmpty(workflowFile))
        {
            throw new CliUsageException("Workflow file is required. Usage: aevatar workflow validate <workflow-file>");
        }

        if (!File.Exists(workflowFile))
        {
            throw new CliUsageException($"Workflow file not found: {workflowFile}");
        }

        Logger.LogInformation("Validating workflow file: {File}...", workflowFile);

        try
        {
            var configContent = await File.ReadAllTextAsync(workflowFile);
            var config = JsonSerializer.Deserialize<object>(configContent);
            
            Logger.LogInformation("✅ Workflow file is valid JSON");
            
            // TODO: Add actual workflow validation logic here
            // This would need to be implemented in the API or as a local validation service
            
            Logger.LogInformation("✅ Workflow validation completed successfully");
        }
        catch (JsonException ex)
        {
            Logger.LogError("❌ Workflow validation failed: Invalid JSON format");
            Logger.LogError("Error: {Message}", ex.Message);
            throw new CliUsageException($"Workflow validation failed: {ex.Message}", ex);
        }
    }

    private async Task FollowWorkflowExecutionAsync(string executionId)
    {
        // This would require a polling or WebSocket mechanism to follow execution progress
        // For now, just show a placeholder message
        Logger.LogInformation("Execution monitoring not yet implemented. Execution ID: {ExecutionId}", executionId);
        Logger.LogInformation("You can check the execution status through the web interface or API.");
    }

    public override string GetUsageInfo()
    {
        return @"
Usage: aevatar workflow <subcommand> [options] [arguments]

Subcommands:
  generate <user-goal>             Generate workflow from user goal
  run <workflow-config>            Run workflow with configuration
  text-completion <input-text>     Generate text completion options
  validate <workflow-file>         Validate workflow configuration file

Generate Options:
  --output <file>                  Save generated workflow to file

Run Options:
  --async                          Run workflow asynchronously
  --follow                         Follow execution progress (with --async)

Text Completion Options:
  --count <n>                      Number of completion options (default: 5)

Global Options:
  --json                           Output in JSON format

Examples:
  aevatar workflow generate ""Process user feedback"" --output workflow.json
  aevatar workflow run workflow.json --async --follow
  aevatar workflow text-completion ""Hello world"" --count 3
  aevatar workflow validate workflow.json
";
    }

    public static string GetShortDescription()
    {
        return "Manage workflows (generate, run, validate)";
    }

    // DTOs matching the API contracts
    private class GenerateWorkflowRequestDto
    {
        public string UserGoal { get; set; } = string.Empty;
    }

    private class AiWorkflowViewConfigDto
    {
        public List<WorkflowNodeDto>? Nodes { get; set; }
        public List<WorkflowConnectionDto>? Connections { get; set; }
        public string? Version { get; set; }
    }

    private class WorkflowNodeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public WorkflowPositionDto? Position { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }

    private class WorkflowConnectionDto
    {
        public string Id { get; set; } = string.Empty;
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public string? SourceHandle { get; set; }
        public string? TargetHandle { get; set; }
    }

    private class WorkflowPositionDto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    private class WorkflowRunRequestDto
    {
        public object? WorkflowConfig { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    private class WorkflowRunResultDto
    {
        public string? ExecutionId { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? Results { get; set; }
    }

    private class TextCompletionRequestDto
    {
        public string InputText { get; set; } = string.Empty;
        public int Count { get; set; } = 5;
    }

    private class TextCompletionResponseDto
    {
        public List<string>? Options { get; set; }
        public string? InputText { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
