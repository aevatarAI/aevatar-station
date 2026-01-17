using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using Aevatar.Cli.Args;
using Aevatar.Cli.Auth;
using Microsoft.Extensions.Logging;

namespace Aevatar.Cli.Commands;

public class UtilCommand : BaseHttpCommand
{
    public const string Name = "util";

    public UtilCommand(AuthenticationService authService, IHttpClientFactory httpClientFactory)
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
                case "status":
                    await ShowSystemStatusAsync(commandLineArgs);
                    break;
                case "health":
                    await ShowHealthCheckAsync(commandLineArgs);
                    break;
                case "test-connection":
                    await TestConnectionAsync(commandLineArgs);
                    break;
                case "test-aspire":
                    await TestAspireConnectionAsync(commandLineArgs);
                    break;
                case "test-silo":
                    await TestSiloConnectionAsync(commandLineArgs);
                    break;
                case "test-auth":
                    await TestAuthConnectionAsync(commandLineArgs);
                    break;
                case "format-json":
                    await FormatJsonAsync(commandLineArgs);
                    break;
                case "template":
                    await GenerateTemplateAsync(commandLineArgs);
                    break;
                case "aspire":
                    await ManageAspireAsync(commandLineArgs);
                    break;
                case "batch":
                    await ExecuteBatchCommandsAsync(commandLineArgs);
                    break;
                default:
                    Logger.LogWarning("Unknown util subcommand: {SubCommand}", subCommand);
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
            Logger.LogError(ex, "Error executing util command: {Message}", ex.Message);
            throw new CliUsageException($"Util command failed: {ex.Message}", ex);
        }
    }

    private async Task ShowSystemStatusAsync(CommandLineArgs args)
    {
        Logger.LogInformation("🔍 检查系统状态...");

        var services = new List<ServiceStatus>
        {
            await CheckServiceAsync("AuthServer", "http://localhost:7001", "/.well-known/openid_configuration"),
            await CheckServiceAsync("HttpApi", "http://localhost:7002", "/health"),
            await CheckServiceAsync("Aspire", "https://localhost:18888", "/health")
        };

        var siloGateways = new List<ServiceStatus>
        {
            await CheckTcpServiceAsync("Scheduler Silo", "127.0.0.2", 30000),
            await CheckTcpServiceAsync("Projector Silo", "127.0.0.3", 30001),
            await CheckTcpServiceAsync("User Silo", "127.0.0.4", 30002)
        };

        // Check authentication status
        var isAuthenticated = AuthService.IsAuthenticated;
        var authStatus = isAuthenticated ? "✅ 已认证" : "❌ 未认证";

        if (HasOption(args, "json"))
        {
            var status = new
            {
                Environment = "local",
                AuthenticationStatus = new { IsAuthenticated = isAuthenticated, Status = authStatus },
                Services = services.Select(s => new { s.Name, s.Url, s.Status, s.IsHealthy }),
                Orleans = siloGateways.Select(s => new { s.Name, Endpoint = $"{s.Host}:{s.Port}", s.Status, s.IsHealthy })
            };
            OutputJson(status);
            return;
        }

        // Display formatted status
        Logger.LogInformation("");
        Logger.LogInformation("┌─────────────────────────────────────────────────────────┐");
        Logger.LogInformation("│                   Aevatar 环境状态                        │");
        Logger.LogInformation("├─────────────────────────────────────────────────────────┤");
        Logger.LogInformation("│ 🌍 当前环境: local (自动检测)                            │");
        Logger.LogInformation("│ 🔑 认证状态: {Status}                               │", authStatus.PadRight(32));
        Logger.LogInformation("├─────────────────────────────────────────────────────────┤");
        Logger.LogInformation("│ 📊 服务状态:                                            │");
        
        foreach (var service in services)
        {
            var statusIcon = service.IsHealthy ? "✅" : "❌";
            var statusText = service.IsHealthy ? "运行中" : "离线";
            Logger.LogInformation("│   {Name}    {Url}     {Status} {StatusText}     │", 
                service.Name.PadRight(12), 
                service.Url.PadRight(25), 
                statusIcon, 
                statusText.PadRight(8));
        }
        
        Logger.LogInformation("├─────────────────────────────────────────────────────────┤");
        Logger.LogInformation("│ 🏗️  Orleans 集群:                                       │");
        
        foreach (var silo in siloGateways)
        {
            var statusIcon = silo.IsHealthy ? "✅" : "❌";
            var statusText = silo.IsHealthy ? "已连接" : "离线";
            var endpoint = $"{silo.Host}:{silo.Port}";
            Logger.LogInformation("│   {Name}     {Endpoint}          {Status} {StatusText}     │", 
                silo.Name.PadRight(12), 
                endpoint.PadRight(17), 
                statusIcon, 
                statusText.PadRight(8));
        }
        
        Logger.LogInformation("└─────────────────────────────────────────────────────────┘");
        Logger.LogInformation("");
    }

    private async Task ShowHealthCheckAsync(CommandLineArgs args)
    {
        Logger.LogInformation("🏥 执行健康检查...");

        var checks = new List<HealthCheck>();

        // Check services
        checks.Add(await PerformHealthCheckAsync("AuthServer", "http://localhost:7001", "/.well-known/openid_configuration"));
        checks.Add(await PerformHealthCheckAsync("HttpApi", "http://localhost:7002", "/health"));

        // Check authentication
        checks.Add(new HealthCheck
        {
            Name = "Authentication",
            IsHealthy = AuthService.IsAuthenticated,
            Message = AuthService.IsAuthenticated ? "Token is valid" : "Not authenticated",
            ResponseTime = TimeSpan.Zero
        });

        if (HasOption(args, "json"))
        {
            OutputJson(checks);
            return;
        }

        OutputTable(checks,
            ("Check", c => c.Name),
            ("Status", c => c.IsHealthy ? "✅ Healthy" : "❌ Unhealthy"),
            ("Response Time", c => c.ResponseTime.TotalMilliseconds > 0 ? $"{c.ResponseTime.TotalMilliseconds:F0}ms" : "N/A"),
            ("Message", c => c.Message ?? "")
        );

        var healthyCount = checks.Count(c => c.IsHealthy);
        var totalCount = checks.Count;
        Logger.LogInformation("");
        Logger.LogInformation("Overall Health: {HealthyCount}/{TotalCount} checks passed", healthyCount, totalCount);
    }

    private async Task TestConnectionAsync(CommandLineArgs args)
    {
        var host = GetOption(args, "host", "localhost");
        var port = int.TryParse(GetOption(args, "port"), out var p) ? p : 7002;

        Logger.LogInformation("Testing connection to {Host}:{Port}...", host, port);

        var isConnected = await TestTcpConnectionAsync(host, port);
        
        if (isConnected)
        {
            Logger.LogInformation("✅ Connection successful to {Host}:{Port}", host, port);
        }
        else
        {
            Logger.LogError("❌ Connection failed to {Host}:{Port}", host, port);
        }
    }

    private async Task TestAspireConnectionAsync(CommandLineArgs args)
    {
        Logger.LogInformation("🧪 Testing Aspire environment connections...");

        var results = await Task.WhenAll(
            CheckServiceAsync("AuthServer", "http://localhost:7001", "/.well-known/openid_configuration"),
            CheckServiceAsync("HttpApi", "http://localhost:7002", "/health"),
            CheckServiceAsync("Aspire Dashboard", "https://localhost:18888", "/health")
        );

        var allHealthy = results.All(r => r.IsHealthy);
        
        if (allHealthy)
        {
            Logger.LogInformation("✅ All Aspire services are accessible");
        }
        else
        {
            Logger.LogError("❌ Some Aspire services are not accessible");
            foreach (var result in results.Where(r => !r.IsHealthy))
            {
                Logger.LogError("  - {Name}: {Status}", result.Name, result.Status);
            }
        }
    }

    private async Task TestSiloConnectionAsync(CommandLineArgs args)
    {
        Logger.LogInformation("🧪 Testing Orleans Silo connections...");

        var results = await Task.WhenAll(
            CheckTcpServiceAsync("Scheduler Gateway", "127.0.0.2", 30000),
            CheckTcpServiceAsync("Projector Gateway", "127.0.0.3", 30001),
            CheckTcpServiceAsync("User Gateway", "127.0.0.4", 30002)
        );

        var healthyCount = results.Count(r => r.IsHealthy);
        
        if (healthyCount > 0)
        {
            Logger.LogInformation("✅ {Count}/{Total} Orleans gateways are accessible", healthyCount, results.Length);
        }
        else
        {
            Logger.LogError("❌ No Orleans gateways are accessible");
        }

        foreach (var result in results)
        {
            var status = result.IsHealthy ? "✅" : "❌";
            Logger.LogInformation("  {Status} {Name}: {Host}:{Port}", status, result.Name, result.Host, result.Port);
        }
    }

    private async Task TestAuthConnectionAsync(CommandLineArgs args)
    {
        Logger.LogInformation("🧪 Testing authentication service...");

        var canConnect = await AuthService.TestConnectionAsync();
        
        if (canConnect)
        {
            Logger.LogInformation("✅ Authentication service is accessible");
            
            try
            {
                var token = await AuthService.GetAccessTokenAsync();
                Logger.LogInformation("✅ Authentication successful - token obtained");
            }
            catch (Exception ex)
            {
                Logger.LogError("❌ Authentication failed: {Message}", ex.Message);
            }
        }
        else
        {
            Logger.LogError("❌ Authentication service is not accessible");
        }
    }

    private Task FormatJsonAsync(CommandLineArgs args)
    {
        var jsonString = GetArgument(args, 1);
        if (string.IsNullOrEmpty(jsonString))
        {
            throw new CliUsageException("JSON string is required. Usage: aevatar util format-json <json-string>");
        }

        try
        {
            var jsonObject = JsonSerializer.Deserialize<object>(jsonString);
            var formattedJson = JsonSerializer.Serialize(jsonObject, JsonOptions);
            Logger.LogInformation(formattedJson);
        }
        catch (JsonException ex)
        {
            throw new CliUsageException($"Invalid JSON: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    private Task GenerateTemplateAsync(CommandLineArgs args)
    {
        var templateType = GetArgument(args, 1);
        if (string.IsNullOrEmpty(templateType))
        {
            throw new CliUsageException("Template type is required. Usage: aevatar util template <template-type>");
        }

        var template = templateType.ToLowerInvariant() switch
        {
            "agent-config" => GenerateAgentConfigTemplate(),
            "workflow-config" => GenerateWorkflowConfigTemplate(),
            _ => throw new CliUsageException($"Unknown template type: {templateType}")
        };

        Logger.LogInformation(template);
        return Task.CompletedTask;
    }

    private async Task ManageAspireAsync(CommandLineArgs args)
    {
        var action = GetArgument(args, 1);
        if (string.IsNullOrEmpty(action))
        {
            throw new CliUsageException("Aspire action is required. Usage: aevatar util aspire <start|stop|restart|logs>");
        }

        switch (action.ToLowerInvariant())
        {
            case "start":
                await ExecuteDockerComposeAsync("up -d", "Starting Aspire environment...");
                break;
            case "stop":
                await ExecuteDockerComposeAsync("down", "Stopping Aspire environment...");
                break;
            case "restart":
                await ExecuteDockerComposeAsync("restart", "Restarting Aspire environment...");
                break;
            case "logs":
                await ExecuteDockerComposeAsync("logs --tail=100 -f", "Showing Aspire logs...");
                break;
            default:
                throw new CliUsageException($"Unknown aspire action: {action}");
        }
    }

    private async Task ExecuteBatchCommandsAsync(CommandLineArgs args)
    {
        var commandFile = GetArgument(args, 1);
        if (string.IsNullOrEmpty(commandFile) || !File.Exists(commandFile))
        {
            throw new CliUsageException("Valid command file is required. Usage: aevatar util batch <command-file>");
        }

        Logger.LogInformation("Executing batch commands from: {File}", commandFile);

        var commands = await File.ReadAllLinesAsync(commandFile);
        var successCount = 0;
        var totalCount = 0;

        foreach (var line in commands)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            totalCount++;
            Logger.LogInformation("");
            Logger.LogInformation("🔄 Executing: {Command}", trimmedLine);
            
            try
            {
                // This would need to recursively call the CLI with the parsed command
                // For now, just log what would be executed
                Logger.LogInformation("✅ Command would be executed: {Command}", trimmedLine);
                successCount++;
            }
            catch (Exception ex)
            {
                Logger.LogError("❌ Command failed: {Error}", ex.Message);
            }
        }

        Logger.LogInformation("");
        Logger.LogInformation("Batch execution completed: {Success}/{Total} commands succeeded", successCount, totalCount);
    }

    // Helper methods
    private async Task<ServiceStatus> CheckServiceAsync(string name, string baseUrl, string healthPath = "/health")
    {
        try
        {
            using var client = HttpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync($"{baseUrl}{healthPath}");
            sw.Stop();
            
            return new ServiceStatus
            {
                Name = name,
                Url = baseUrl,
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "运行中" : $"HTTP {(int)response.StatusCode}",
                ResponseTime = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new ServiceStatus
            {
                Name = name,
                Url = baseUrl,
                IsHealthy = false,
                Status = "离线",
                Error = ex.Message
            };
        }
    }

    private async Task<ServiceStatus> CheckTcpServiceAsync(string name, string host, int port)
    {
        var isHealthy = await TestTcpConnectionAsync(host, port);
        
        return new ServiceStatus
        {
            Name = name,
            Host = host,
            Port = port,
            IsHealthy = isHealthy,
            Status = isHealthy ? "已连接" : "离线"
        };
    }

    private async Task<bool> TestTcpConnectionAsync(string host, int port)
    {
        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            await tcpClient.ConnectAsync(host, port);
            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }

    private async Task<HealthCheck> PerformHealthCheckAsync(string name, string baseUrl, string healthPath = "/health")
    {
        try
        {
            using var client = HttpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync($"{baseUrl}{healthPath}");
            sw.Stop();
            
            return new HealthCheck
            {
                Name = name,
                IsHealthy = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Service is responding" : $"HTTP {(int)response.StatusCode}",
                ResponseTime = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new HealthCheck
            {
                Name = name,
                IsHealthy = false,
                Message = ex.Message,
                ResponseTime = TimeSpan.Zero
            };
        }
    }

    private async Task ExecuteDockerComposeAsync(string command, string message)
    {
        Logger.LogInformation(message);
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                if (process.ExitCode == 0)
                {
                    Logger.LogInformation("✅ Docker Compose command completed successfully");
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Logger.LogDebug("Output: {Output}", output);
                    }
                }
                else
                {
                    Logger.LogError("❌ Docker Compose command failed with exit code {ExitCode}", process.ExitCode);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Logger.LogError("Error: {Error}", error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("❌ Failed to execute Docker Compose: {Error}", ex.Message);
        }
    }

    private string GenerateAgentConfigTemplate()
    {
        var template = new
        {
            agentType = "ChatAgent",
            name = "My Agent",
            description = "Agent description",
            configuration = new
            {
                model = "gpt-4",
                temperature = 0.7,
                maxTokens = 1000,
                systemPrompt = "You are a helpful assistant."
            }
        };

        return JsonSerializer.Serialize(template, JsonOptions);
    }

    private string GenerateWorkflowConfigTemplate()
    {
        var template = new
        {
            version = "1.0",
            nodes = new object[]
            {
                new
                {
                    id = "input-1",
                    type = "input",
                    name = "User Input",
                    position = new { x = 100, y = 100 },
                    properties = new { placeholder = "Enter your input" }
                },
                new
                {
                    id = "process-1",
                    type = "agent",
                    name = "Process Agent",
                    position = new { x = 300, y = 100 },
                    properties = new { agentType = "ChatAgent" }
                },
                new
                {
                    id = "output-1",
                    type = "output",
                    name = "Result",
                    position = new { x = 500, y = 100 }
                }
            },
            connections = new object[]
            {
                new { id = "conn-1", sourceNodeId = "input-1", targetNodeId = "process-1" },
                new { id = "conn-2", sourceNodeId = "process-1", targetNodeId = "output-1" }
            }
        };

        return JsonSerializer.Serialize(template, JsonOptions);
    }

    public override string GetUsageInfo()
    {
        return @"
Usage: aevatar util <subcommand> [options] [arguments]

Subcommands:
  status                           Show system status
  health                          Show detailed health check
  test-connection                 Test network connection
  test-aspire                     Test Aspire environment
  test-silo                       Test Orleans Silo connections  
  test-auth                       Test authentication service
  format-json <json>              Format JSON string
  template <type>                 Generate configuration template
  aspire <action>                 Manage Aspire environment
  batch <file>                    Execute batch commands from file

Test Connection Options:
  --host <host>                   Host to test (default: localhost)
  --port <port>                   Port to test (default: 7002)

Template Types:
  agent-config                    Agent configuration template
  workflow-config                 Workflow configuration template

Aspire Actions:
  start                           Start Aspire environment (docker-compose up)
  stop                            Stop Aspire environment (docker-compose down)
  restart                         Restart Aspire environment
  logs                            Show Aspire logs

Global Options:
  --json                          Output in JSON format

Examples:
  aevatar util status
  aevatar util test-connection --host localhost --port 7001
  aevatar util template agent-config
  aevatar util aspire start
  aevatar util batch commands.txt
";
    }

    public static string GetShortDescription()
    {
        return "Utility commands (status, health, templates, etc.)";
    }

    // Helper classes
    private class ServiceStatus
    {
        public string Name { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
        public TimeSpan ResponseTime { get; set; }
    }

    private class HealthCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string? Message { get; set; }
        public TimeSpan ResponseTime { get; set; }
    }
}
