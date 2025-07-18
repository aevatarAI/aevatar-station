using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIAgentDemo.Client;

/// <summary>
/// Workflow Generator Demo Client - 演示简化的workflow生成功能
/// </summary>
public class Program
{
    private static readonly HttpClient HttpClient = new();
    private static string BaseUrl = "http://localhost:7002"; // HttpApi.Host 默认端口
    private static string? AuthToken;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Aevatar Workflow Generator Demo");
        Console.WriteLine("==================================");
        Console.WriteLine("基于专门化 GAgent 的智能工作流生成演示");
        Console.WriteLine();

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.AddConsole());

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            // 配置HTTP客户端
            ConfigureHttpClient();

            // 运行演示
            await RunWorkflowGenerationDemoAsync(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo execution failed");
            Console.WriteLine($"❌ 错误: {ex.Message}");
        }

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    private static void ConfigureHttpClient()
    {
        HttpClient.Timeout = TimeSpan.FromMinutes(3);
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "WorkflowGeneratorDemo/1.0");
        
        if (!string.IsNullOrEmpty(AuthToken))
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AuthToken}");
        }
    }

    private static async Task RunWorkflowGenerationDemoAsync(ILogger logger)
    {
        Console.WriteLine("🎯 开始 Workflow Generator 演示...\n");

        // 1. 获取Agent描述和状态
        await GetAgentInfoAsync(logger);

        // 2. 快速生成workflow演示
        await QuickGenerateWorkflowDemoAsync(logger);

        // 3. 高级生成workflow演示
        await AdvancedGenerateWorkflowDemoAsync(logger);

        // 4. 获取生成历史
        await GetGenerationHistoryAsync(logger);

        // 5. 清空历史记录
        await ClearHistoryDemoAsync(logger);

        Console.WriteLine("\n✅ Workflow Generator 演示完成！");
        Console.WriteLine("\n🌟 总结:");
        Console.WriteLine("   • 专门化的GAgent大大简化了workflow生成流程");
        Console.WriteLine("   • 用户只需提供业务目标，即可获得完整的workflow配置");
        Console.WriteLine("   • 集成了Agent发现、提示词工程和LLM调用，优化了性能");
    }

    private static async Task GetAgentInfoAsync(ILogger logger)
    {
        Console.WriteLine("📝 1. 获取 Workflow Generator 信息");
        Console.WriteLine("────────────────────────────────");

        try
        {
            // 获取描述
            var descResponse = await HttpClient.GetAsync($"{BaseUrl}/api/workflow-generator/description");
            if (descResponse.IsSuccessStatusCode)
            {
                var description = await descResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Agent 描述: {description}");
            }

            // 获取状态
            var statusResponse = await HttpClient.GetAsync($"{BaseUrl}/api/workflow-generator/status");
            if (statusResponse.IsSuccessStatusCode)
            {
                var json = await statusResponse.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<JsonElement>(json);
                
                Console.WriteLine("\nAgent 状态:");
                Console.WriteLine($"  - 是否已初始化: {GetJsonProperty(status, "isInitialized", "N/A")}");
                Console.WriteLine($"  - 总生成次数: {GetJsonProperty(status, "totalWorkflowsGenerated", "0")}");
                Console.WriteLine($"  - 上次生成时间: {GetJsonProperty(status, "lastGenerationTime", "N/A")}");
                Console.WriteLine($"  - 上次用户目标: {GetJsonProperty(status, "lastUserGoal", "无")}");
                
                logger.LogInformation("Successfully retrieved agent info");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            logger.LogError(ex, "Error getting agent info");
        }

        Console.WriteLine();
    }

    private static async Task QuickGenerateWorkflowDemoAsync(ILogger logger)
    {
        Console.WriteLine("⚡ 2. 快速生成 Workflow 演示");
        Console.WriteLine("───────────────────────────");

        var testGoals = new[]
        {
            "处理用户上传的文档，提取关键信息并生成摘要",
            "分析销售数据，生成月度报告",
            "监控系统性能，发现异常并发送告警",
            "自动化客户服务流程，回复常见问题"
        };

        foreach (var goal in testGoals)
        {
            Console.WriteLine($"\n🎯 用户目标: {goal}");
            
            try
            {
                var request = new { UserGoal = goal };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine("⏳ 正在生成workflow...");
                var response = await HttpClient.PostAsync($"{BaseUrl}/api/workflow-generator/quick-generate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    
                    var isSuccessful = GetJsonProperty(result, "isSuccessful", "false") == "true";
                    
                    if (isSuccessful)
                    {
                        var workflowConfig = result.GetProperty("workflowConfig");
                        var workflowName = GetJsonProperty(workflowConfig, "name", "未命名");
                        var nodeCount = 0;
                        var connectionCount = 0;
                        
                        if (workflowConfig.TryGetProperty("workflowNodeList", out var nodeList) && 
                            nodeList.ValueKind == JsonValueKind.Array)
                        {
                            nodeCount = nodeList.GetArrayLength();
                        }
                        
                        if (workflowConfig.TryGetProperty("workflowNodeUnitList", out var unitList) && 
                            unitList.ValueKind == JsonValueKind.Array)
                        {
                            connectionCount = unitList.GetArrayLength();
                        }
                        
                        var usedAgentCount = GetJsonProperty(result, "usedAgentCount", "0");
                        
                        Console.WriteLine($"✅ 生成成功!");
                        Console.WriteLine($"   📋 工作流名称: {workflowName}");
                        Console.WriteLine($"   🔗 节点数量: {nodeCount}");
                        Console.WriteLine($"   ➡️ 连接数量: {connectionCount}");
                        Console.WriteLine($"   🤖 使用Agent数: {usedAgentCount}");
                        
                        logger.LogInformation("Quick generate successful for goal: {Goal}", goal);
                    }
                    else
                    {
                        var errorMessage = GetJsonProperty(result, "errorMessage", "Unknown error");
                        Console.WriteLine($"❌ 生成失败: {errorMessage}");
                        logger.LogWarning("Quick generate failed: {Error}", errorMessage);
                    }
                }
                else
                {
                    Console.WriteLine($"❌ HTTP请求失败: {response.StatusCode}");
                    logger.LogWarning("Quick generate HTTP request failed: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                logger.LogError(ex, "Error in quick generate for goal: {Goal}", goal);
            }

            await Task.Delay(1500); // 避免过快的连续请求
        }

        Console.WriteLine();
    }

    private static async Task AdvancedGenerateWorkflowDemoAsync(ILogger logger)
    {
        Console.WriteLine("🔧 3. 高级生成 Workflow 演示");
        Console.WriteLine("────────────────────────────");

        var request = new
        {
            UserGoal = "构建一个完整的数据处理管道：从数据采集、清洗、分析到可视化展示",
            UseAdvancedPrompt = true,
            MaxAgents = 6
        };

        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"📤 高级生成请求:");
            Console.WriteLine($"   目标: {request.UserGoal}");
            Console.WriteLine($"   高级提示: {request.UseAdvancedPrompt}");
            Console.WriteLine($"   最大Agent数: {request.MaxAgents}");

            Console.WriteLine("\n⏳ 正在生成高级workflow...");
            var response = await HttpClient.PostAsync($"{BaseUrl}/api/workflow-generator/generate", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                var isSuccessful = GetJsonProperty(result, "isSuccessful", "false") == "true";
                
                if (isSuccessful)
                {
                    var workflowConfig = result.GetProperty("workflowConfig");
                    var workflowName = GetJsonProperty(workflowConfig, "name", "未命名");
                    
                    Console.WriteLine($"\n✅ 高级生成成功!");
                    Console.WriteLine($"📋 工作流名称: {workflowName}");
                    
                    // 显示节点详情
                    if (workflowConfig.TryGetProperty("workflowNodeList", out var nodeList) && 
                        nodeList.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"\n🔗 生成的节点 ({nodeList.GetArrayLength()}个):");
                        
                        int index = 1;
                        foreach (var node in nodeList.EnumerateArray())
                        {
                            var nodeName = GetJsonProperty(node, "name", "未命名节点");
                            var agentType = GetJsonProperty(node, "agentType", "未知类型");
                            Console.WriteLine($"   {index}. {nodeName} ({agentType})");
                            index++;
                        }
                    }
                    
                    // 显示原始JSON (截断显示)
                    var rawJson = GetJsonProperty(result, "rawJson", "");
                    if (!string.IsNullOrEmpty(rawJson))
                    {
                        var truncatedJson = rawJson.Length > 200 ? rawJson.Substring(0, 200) + "..." : rawJson;
                        Console.WriteLine($"\n📄 生成的JSON (截断): {truncatedJson}");
                    }
                    
                    logger.LogInformation("Advanced generate successful");
                }
                else
                {
                    var errorMessage = GetJsonProperty(result, "errorMessage", "Unknown error");
                    Console.WriteLine($"❌ 高级生成失败: {errorMessage}");
                    logger.LogWarning("Advanced generate failed: {Error}", errorMessage);
                }
            }
            else
            {
                Console.WriteLine($"❌ HTTP请求失败: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   错误详情: {errorContent}");
                logger.LogWarning("Advanced generate HTTP request failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            logger.LogError(ex, "Error in advanced generate");
        }

        Console.WriteLine();
    }

    private static async Task GetGenerationHistoryAsync(ILogger logger)
    {
        Console.WriteLine("📜 4. 获取生成历史记录");
        Console.WriteLine("─────────────────────");

        try
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/api/workflow-generator/history");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var history = JsonSerializer.Deserialize<JsonElement[]>(json);
                
                Console.WriteLine($"📊 历史记录总数: {history?.Length ?? 0}");
                
                if (history != null && history.Length > 0)
                {
                    Console.WriteLine("\n最近的生成记录:");
                    
                    var recentRecords = history.TakeLast(3);
                    foreach (var record in recentRecords)
                    {
                        var userGoal = GetJsonProperty(record, "userGoal", "N/A");
                        var isSuccessful = GetJsonProperty(record, "isSuccessful", "false") == "true";
                        var generationTime = GetJsonProperty(record, "generationTime", "N/A");
                        var agentCount = GetJsonProperty(record, "agentCount", "0");
                        
                        Console.WriteLine($"  • 时间: {generationTime}");
                        Console.WriteLine($"    目标: {TruncateString(userGoal, 60)}");
                        Console.WriteLine($"    结果: {(isSuccessful ? "✅ 成功" : "❌ 失败")}, Agent数: {agentCount}");
                    }
                }
                else
                {
                    Console.WriteLine("📭 暂无历史记录");
                }
                
                logger.LogInformation("Successfully retrieved generation history");
            }
            else
            {
                Console.WriteLine($"❌ 获取历史失败: {response.StatusCode}");
                logger.LogWarning("Failed to get generation history: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            logger.LogError(ex, "Error getting generation history");
        }

        Console.WriteLine();
    }

    private static async Task ClearHistoryDemoAsync(ILogger logger)
    {
        Console.WriteLine("🗑️ 5. 清空历史记录");
        Console.WriteLine("─────────────────");

        try
        {
            var response = await HttpClient.DeleteAsync($"{BaseUrl}/api/workflow-generator/history");
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                var message = GetJsonProperty(result, "message", "Success");
                
                Console.WriteLine($"✅ {message}");
                logger.LogInformation("Successfully cleared history");
            }
            else
            {
                Console.WriteLine($"❌ 清空历史失败: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   错误详情: {errorContent}");
                logger.LogWarning("Failed to clear history: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            logger.LogError(ex, "Error clearing history");
        }

        Console.WriteLine();
    }

    #region Helper Methods

    private static string GetJsonProperty(JsonElement element, string propertyName, string defaultValue)
    {
        try
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind == JsonValueKind.String 
                    ? property.GetString() ?? defaultValue
                    : property.ToString();
            }
        }
        catch
        {
            // 忽略解析错误，返回默认值
        }
        
        return defaultValue;
    }

    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;
        
        return input.Substring(0, maxLength) + "...";
    }

    #endregion
} 