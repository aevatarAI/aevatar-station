using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;
using ValidationError = Aevatar.Application.Contracts.WorkflowOrchestration.ValidationError;
using ValidationWarning = Aevatar.Application.Contracts.WorkflowOrchestration.ValidationWarning;

namespace Aevatar.Application.Service
{
    /// <summary>
    /// 工作流编排服务实现
    /// </summary>
    public class WorkflowOrchestrationService : IWorkflowOrchestrationService
    {
        private readonly IWorkflowPromptBuilder _promptBuilder;
        private readonly IAgentIndexPool _agentIndexPool;
        private readonly IWorkflowJsonValidator _jsonValidator;
        private readonly ILogger<WorkflowOrchestrationService> _logger;

        public WorkflowOrchestrationService(
            IWorkflowPromptBuilder promptBuilder,
            IAgentIndexPool agentIndexPool,
            IWorkflowJsonValidator jsonValidator,
            ILogger<WorkflowOrchestrationService> logger)
        {
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _agentIndexPool = agentIndexPool ?? throw new ArgumentNullException(nameof(agentIndexPool));
            _jsonValidator = jsonValidator ?? throw new ArgumentNullException(nameof(jsonValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 生成工作流定义
        /// </summary>
        public async Task<WorkflowGenerationResult> GenerateWorkflowAsync(
            string userGoal, 
            WorkflowGenerationOptions? options = null)
        {
            try
            {
                _logger.LogInformation("开始生成工作流，用户目标：{UserGoal}", userGoal);

                options ??= new WorkflowGenerationOptions();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // 1. 获取所有可用Agent
                var availableAgents = await _agentIndexPool.GetAllAgentsAsync();

                // 2. 构建提示词
                var prompt = await _promptBuilder.BuildWorkflowGenerationPromptAsync(userGoal, availableAgents, options);

                // 3. 模拟LLM调用生成工作流（这里使用简单的模拟实现）
                var workflowJson = await SimulateLLMCallAsync(prompt);

                // 4. 验证和解析JSON
                var validationResult = await _jsonValidator.ValidateWorkflowJsonAsync(workflowJson);
                
                stopwatch.Stop();

                if (validationResult.IsValid && validationResult.ParsedWorkflow != null)
                {
                    return new WorkflowGenerationResult
                    {
                        Success = true,
                        Workflow = validationResult.ParsedWorkflow,
                        Statistics = new WorkflowGenerationStatistics
                        {
                            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                            AgentCount = availableAgents.Count(),
                            GeneratedNodeCount = validationResult.ParsedWorkflow.Nodes.Count,
                            ValidationScore = 95.0
                        },
                        Metadata = new Dictionary<string, object>
                        {
                            ["UserGoal"] = userGoal,
                            ["ProcessingTime"] = stopwatch.ElapsedMilliseconds,
                            ["PromptLength"] = prompt.Length
                        }
                    };
                }
                else
                {
                    return new WorkflowGenerationResult
                    {
                        Success = false,
                        Errors = validationResult.Errors.Select(e => e.Message).ToList(),
                        Statistics = new WorkflowGenerationStatistics
                        {
                            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                            AgentCount = availableAgents.Count()
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成工作流时出错");
                return new WorkflowGenerationResult
                {
                    Success = false,
                    Errors = new List<string> { $"生成失败：{ex.Message}" }
                };
            }
        }

        /// <summary>
        /// 验证工作流定义
        /// </summary>
        public async Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow)
        {
            try
            {
                if (workflow == null)
                {
                    return new WorkflowValidationResult
                    {
                        IsValid = false,
                        Errors = new List<ValidationError>
                        {
                            new ValidationError { Code = "NULL_WORKFLOW", Message = "工作流定义不能为空" }
                        }
                    };
                }

                var result = new WorkflowValidationResult { IsValid = true };

                // 基本验证
                if (string.IsNullOrEmpty(workflow.Name))
                {
                    result.Errors.Add(new ValidationError { Code = "EMPTY_NAME", Message = "工作流名称不能为空" });
                    result.IsValid = false;
                }

                if (workflow.Nodes == null || !workflow.Nodes.Any())
                {
                    result.Errors.Add(new ValidationError { Code = "NO_NODES", Message = "工作流必须包含节点" });
                    result.IsValid = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证工作流时出错");
                return new WorkflowValidationResult
                {
                    IsValid = false,
                    Errors = new List<ValidationError>
                    {
                        new ValidationError { Code = "VALIDATION_ERROR", Message = $"验证出错：{ex.Message}" }
                    }
                };
            }
        }

        /// <summary>
        /// 获取工作流复杂度分析
        /// </summary>
        public async Task<WorkflowComplexityAnalysis> AnalyzeComplexityAsync(WorkflowDefinition workflow)
        {
            await Task.CompletedTask;

            if (workflow?.Nodes == null)
            {
                return new WorkflowComplexityAnalysis
                {
                    Score = 0,
                    Level = "Unknown",
                    Details = new ComplexityDetails
                    {
                        NodeCount = 0,
                        ConnectionCount = 0
                    }
                };
            }

            var nodeCount = workflow.Nodes.Count;
            var connectionCount = workflow.Connections?.Count ?? 0;

            var score = nodeCount * 2 + connectionCount;
            var level = score switch
            {
                <= 10 => "Simple",
                <= 20 => "Medium",
                _ => "Complex"
            };

            return new WorkflowComplexityAnalysis
            {
                Score = score,
                Level = level,
                Details = new ComplexityDetails
                {
                    NodeCount = nodeCount,
                    ConnectionCount = connectionCount
                }
            };
        }

        /// <summary>
        /// 模拟LLM调用
        /// </summary>
        private async Task<string> SimulateLLMCallAsync(string prompt)
        {
            await Task.Delay(100); // 模拟网络延迟

            // 返回一个简单的工作流JSON模板
            return @"{
  ""workflowId"": ""sample-workflow"",
  ""name"": ""示例工作流"",
  ""description"": ""这是一个示例工作流"",
  ""nodes"": [
    {
      ""id"": ""start"",
      ""type"": ""Start"",
      ""name"": ""开始"",
      ""position"": { ""x"": 100, ""y"": 100 }
    },
    {
      ""id"": ""end"",
      ""type"": ""End"",
      ""name"": ""结束"",
      ""position"": { ""x"": 300, ""y"": 100 }
    }
  ],
  ""connections"": [
    {
      ""id"": ""conn1"",
      ""sourceId"": ""start"",
      ""targetId"": ""end"",
      ""type"": ""Sequential""
    }
  ],
  ""variables"": []
}";
        }
    }
} 