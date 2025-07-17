using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Service;

/// <summary>
/// 统一的工作流编排服务实现（集成JSON验证和提示词构建功能）
/// </summary>
public class WorkflowOrchestrationService : IWorkflowOrchestrationService
{
    private readonly IAgentIndexService _agentIndexService;
    private readonly ILogger<WorkflowOrchestrationService> _logger;

    public WorkflowOrchestrationService(
        IAgentIndexService agentIndexService,
        ILogger<WorkflowOrchestrationService> logger)
    {
        _agentIndexService = agentIndexService;
        _logger = logger;
    }

    /// <summary>
    /// 根据用户目标生成完整工作流
    /// </summary>
    /// <param name="userGoal">用户目标描述</param>
    /// <returns>生成的工作流定义</returns>
    public async Task<WorkflowDefinition> GenerateWorkflowAsync(string userGoal)
    {
        if (string.IsNullOrWhiteSpace(userGoal))
        {
            throw new ArgumentException("用户目标不能为空", nameof(userGoal));
        }

        _logger.LogInformation("开始生成工作流，用户目标：{UserGoal}", userGoal);

        try
        {
            // 1. 获取所有可用的Agent信息
            var allAgents = await _agentIndexService.GetAllAgentsAsync();
            _logger.LogDebug("获取到 {Count} 个可用Agent", allAgents.Count());

            // 2. 构建一体化提示词
            var prompt = await BuildGenerationPromptAsync(userGoal, allAgents);
            _logger.LogDebug("构建提示词完成，长度：{Length}", prompt.Length);

            // 3. 调用LLM进行一次性工作流生成（包含Agent筛选和编排）
            var workflowJson = await CallLLMForWorkflowGenerationAsync(prompt);
            _logger.LogDebug("LLM生成完成，响应长度：{Length}", workflowJson.Length);

            // 4. 验证和修复JSON格式
            var validationResult = await ValidateWorkflowJsonAsync(workflowJson);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("工作流JSON验证失败，尝试自动修复");
                workflowJson = await TryFixWorkflowJsonAsync(workflowJson);
            }

            // 5. 解析为WorkflowDefinition
            var workflow = await ParseWorkflowJsonAsync(workflowJson);
            
            _logger.LogInformation("工作流生成成功，包含 {NodeCount} 个节点，{ConnectionCount} 个连接", 
                workflow.Nodes?.Count ?? 0, 
                workflow.Connections?.Count ?? 0);

            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成工作流失败：{UserGoal}", userGoal);
            throw;
        }
    }

    #region Private Methods - Prompt Building

    /// <summary>
    /// 构建工作流生成提示词
    /// </summary>
    private async Task<string> BuildGenerationPromptAsync(string userGoal, IEnumerable<AgentIndexInfo> availableAgents)
    {
        await Task.CompletedTask;

        var agentList = availableAgents?.ToList() ?? new List<AgentIndexInfo>();
        _logger.LogDebug("构建工作流生成提示词，用户目标：{UserGoal}，可用Agent数量：{AgentCount}", userGoal, agentList.Count);

        var prompt = new StringBuilder();

        // 系统角色定义
        prompt.AppendLine("# 工作流编排专家");
        prompt.AppendLine("你是一位专业的AI工作流编排专家。根据用户目标，从提供的Agent列表中选择合适的Agent，并设计完整的工作流执行方案。");
        prompt.AppendLine();

        // 用户目标
        prompt.AppendLine("## 用户目标");
        prompt.AppendLine($"{userGoal}");
        prompt.AppendLine();

        // 可用Agent列表
        prompt.AppendLine("## 可用Agent列表");
        if (agentList.Any())
        {
            foreach (var agent in agentList)
            {
                prompt.AppendLine($"### {agent.Name} (TypeName: {agent.TypeName})");
                prompt.AppendLine($"**简介**: {agent.L1Description}");
                prompt.AppendLine($"**详细**: {agent.L2Description}");
                prompt.AppendLine($"**类别**: {string.Join(", ", agent.Categories)}");
                prompt.AppendLine($"**执行时间**: {agent.EstimatedExecutionTime}ms");
                prompt.AppendLine();
            }
        }
        else
        {
            prompt.AppendLine("暂无可用Agent");
            prompt.AppendLine();
        }

        // 输出要求
        prompt.AppendLine("## 输出要求");
        prompt.AppendLine("请输出完整的工作流JSON，包括：1) 从上述列表中选择合适的Agent，2) 设计节点（开始/Agent/结束节点），3) 定义连接关系和执行顺序，4) 配置节点间的数据流。");
        prompt.AppendLine();

        // JSON格式规范
        prompt.AppendLine("## JSON格式规范");
        prompt.AppendLine("```json");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"workflowId\": \"workflow-{guid}\",");
        prompt.AppendLine("  \"name\": \"工作流名称\",");
        prompt.AppendLine("  \"description\": \"工作流描述\",");
        prompt.AppendLine("  \"nodes\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"nodeId\": \"节点ID\",");
        prompt.AppendLine("      \"type\": \"Start|Agent|End|Condition|Loop|Parallel|Merge\",");
        prompt.AppendLine("      \"name\": \"节点名称\",");
        prompt.AppendLine("      \"agentId\": \"Agent节点必需\",");
        prompt.AppendLine("      \"position\": { \"x\": 100, \"y\": 100 }");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ],");
        prompt.AppendLine("  \"connections\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"connectionId\": \"连接ID\",");
        prompt.AppendLine("      \"sourceNodeId\": \"源节点ID\",");
        prompt.AppendLine("      \"targetNodeId\": \"目标节点ID\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ],");
        prompt.AppendLine("  \"globalVariables\": {},");
        prompt.AppendLine("  \"metadata\": {");
        prompt.AppendLine("    \"createdAt\": \"ISO时间戳\",");
        prompt.AppendLine("    \"userGoal\": \"用户目标\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine("```");

        return prompt.ToString();
    }

    #endregion

    #region Private Methods - JSON Validation

    /// <summary>
    /// 验证和解析工作流JSON
    /// </summary>
    private async Task<WorkflowJsonValidationResult> ValidateWorkflowJsonAsync(string jsonContent)
    {
        await Task.CompletedTask;

        var result = new WorkflowJsonValidationResult();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError 
            { 
                Code = "EMPTY_JSON", 
                Message = "JSON内容不能为空" 
            });
            return result;
        }

        try
        {
            // 清理JSON内容
            var cleanJson = CleanJsonContent(jsonContent);

            // 尝试解析JSON
            using var document = JsonDocument.Parse(cleanJson);
            var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (workflow == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "PARSE_ERROR",
                    Message = "无法解析工作流JSON"
                });
                return result;
            }

            // 验证工作流结构
            ValidateWorkflowStructure(workflow, result);

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
        catch (JsonException ex)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Code = "JSON_PARSE_ERROR",
                Message = $"JSON解析错误：{ex.Message}"
            });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON验证过程中发生意外错误");
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Code = "INTERNAL_ERROR",
                Message = "内部验证错误"
            });
            return result;
        }
    }

    /// <summary>
    /// 清理JSON内容（移除markdown标记等）
    /// </summary>
    private string CleanJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return string.Empty;

        var cleaned = jsonContent.Trim();

        // 移除markdown代码块标记
        if (cleaned.StartsWith("```json"))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }

        return cleaned.Trim();
    }

    /// <summary>
    /// 尝试修复工作流JSON格式
    /// </summary>
    private async Task<string> TryFixWorkflowJsonAsync(string workflowJson)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(workflowJson))
        {
            return string.Empty;
        }

        try
        {
            // 清理JSON内容
            var cleanJson = CleanJsonContent(workflowJson);

            // 验证JSON格式是否正确
            using var document = JsonDocument.Parse(cleanJson);

            // 如果解析成功，返回格式化后的JSON
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("修复工作流JSON失败：{Error}", ex.Message);
            
            // 尝试基本修复策略
            return AttemptBasicJsonFix(workflowJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修复工作流JSON时发生意外错误");
            return workflowJson; // 返回原始内容
        }
    }

    /// <summary>
    /// 解析工作流JSON为WorkflowDefinition对象
    /// </summary>
    private async Task<WorkflowDefinition> ParseWorkflowJsonAsync(string workflowJson)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(workflowJson))
        {
            throw new ArgumentException("工作流JSON不能为空", nameof(workflowJson));
        }

        try
        {
            // 清理JSON内容
            var cleanJson = CleanJsonContent(workflowJson);

            // 尝试修复JSON（如果需要）
            var fixedJson = await TryFixWorkflowJsonAsync(cleanJson);

            // 解析为WorkflowDefinition对象
            var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(fixedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (workflow == null)
            {
                throw new InvalidOperationException("无法反序列化工作流JSON");
            }

            return workflow;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析工作流JSON失败");
            throw new ArgumentException($"无效的工作流JSON格式：{ex.Message}", nameof(workflowJson));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析工作流JSON时发生意外错误");
            throw;
        }
    }

    /// <summary>
    /// 验证工作流结构
    /// </summary>
    private void ValidateWorkflowStructure(WorkflowDefinition workflow, WorkflowJsonValidationResult result)
    {
        try
        {
            // 验证必需字段
            if (string.IsNullOrWhiteSpace(workflow.WorkflowId))
            {
                result.Errors.Add(new ValidationError { Code = "MISSING_WORKFLOW_ID", Message = "工作流ID是必需的" });
            }

            if (string.IsNullOrWhiteSpace(workflow.Name))
            {
                result.Errors.Add(new ValidationError { Code = "MISSING_WORKFLOW_NAME", Message = "工作流名称是必需的" });
            }

            // 验证节点结构  
            if (workflow.Nodes == null || workflow.Nodes.Count == 0)
            {
                result.Errors.Add(new ValidationError { Code = "NO_NODES", Message = "工作流必须包含至少一个节点" });
                return;
            }

            // 检查是否存在开始和结束节点
            var hasStartNode = workflow.Nodes.Any(n => n.Type == WorkflowNodeType.Start);
            var hasEndNode = workflow.Nodes.Any(n => n.Type == WorkflowNodeType.End);

            // 验证每个节点
            for (int i = 0; i < workflow.Nodes.Count; i++)
            {
                var node = workflow.Nodes[i];
                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "MISSING_NODE_ID",
                        Message = $"节点{i + 1}缺少NodeId"
                    });
                }

                if (string.IsNullOrWhiteSpace(node.Name))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "MISSING_NODE_NAME",
                        Message = $"节点{i + 1}没有名称"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作流结构验证过程中发生意外错误");
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Code = "INTERNAL_ERROR",
                Message = "内部验证错误"
            });
        }
    }

    /// <summary>
    /// 尝试基本JSON修复
    /// </summary>
    private string AttemptBasicJsonFix(string json)
    {
        try
        {
            // 简单修复策略：
            // 1. 移除可能的BOM
            if (json.StartsWith("\uFEFF"))
            {
                json = json.Substring(1);
            }

            // 2. 修复常见的引号问题
            json = json.Replace("'", "\"");

            // 3. 移除尾随逗号
            json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*}", "}");
            json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*\]", "]");

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("基本JSON修复尝试失败：{Error}", ex.Message);
            return json; // 返回原始内容
        }
    }

    #endregion

    #region Private Methods - LLM Integration

    /// <summary>
    /// 调用LLM生成工作流JSON
    /// </summary>
    private async Task<string> CallLLMForWorkflowGenerationAsync(string prompt)
    {
        // TODO: 实现LLM调用逻辑
        // 这里应该调用实际的LLM服务（如OpenAI、Azure OpenAI等）
        
        await Task.CompletedTask;
        
        // 临时返回示例JSON，实际实现时应该调用LLM服务
        return @"{
            ""workflowId"": ""workflow-generated"",
            ""name"": ""AI生成的工作流"",
            ""description"": ""根据用户目标AI生成的工作流"",
            ""userGoal"": ""示例目标"",
            ""nodes"": [
                {
                    ""nodeId"": ""start-1"",
                    ""name"": ""开始"",
                    ""type"": 1,
                    ""description"": ""开始节点"",
                    ""configuration"": {},
                    ""position"": { ""x"": 100, ""y"": 100 }
                },
                {
                    ""nodeId"": ""end-1"",
                    ""name"": ""结束"",
                    ""type"": 2,
                    ""description"": ""结束节点"",
                    ""configuration"": {},
                    ""position"": { ""x"": 300, ""y"": 100 }
                }
            ],
            ""connections"": [
                {
                    ""connectionId"": ""conn-1"",
                    ""sourceNodeId"": ""start-1"",
                    ""targetNodeId"": ""end-1"",
                    ""type"": 1,
                    ""label"": ""下一步""
                }
            ],
            ""globalVariables"": {},
            ""selectedAgents"": [],
            ""complexity"": 1,
            ""estimatedExecutionTime"": 1000,
            ""version"": ""1.0.0""
        }";
    }

    #endregion
} 