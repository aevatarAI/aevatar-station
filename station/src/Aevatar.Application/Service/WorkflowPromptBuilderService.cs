using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;
using ValidationError = Aevatar.Application.Contracts.WorkflowOrchestration.ValidationError;
using ValidationWarning = Aevatar.Application.Contracts.WorkflowOrchestration.ValidationWarning;
using PromptValidationResult = Aevatar.Application.Contracts.WorkflowOrchestration.PromptValidationResult;

namespace Aevatar.Application.Service
{
    /// <summary>
    /// 工作流提示词构建服务
    /// </summary>
    public class WorkflowPromptBuilderService : IWorkflowPromptBuilder
    {
        private readonly ILogger<WorkflowPromptBuilderService> _logger;

        public WorkflowPromptBuilderService(ILogger<WorkflowPromptBuilderService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 构建工作流生成提示词
        /// </summary>
        public async Task<string> BuildWorkflowGenerationPromptAsync(
            string userGoal,
            IEnumerable<AgentIndexInfo> availableAgents,
            WorkflowGenerationOptions options)
        {
            try
            {
                var prompt = new StringBuilder();

                // 系统角色定义
                prompt.AppendLine("你是专业的工作流编排专家，根据用户目标从Agent列表中选择合适的Agent并设计完整的执行流程。");
                prompt.AppendLine();

                // 用户目标
                prompt.AppendLine($"用户目标：{userGoal}");
                prompt.AppendLine();

                // 可用Agent列表
                prompt.AppendLine("可用Agent列表：");
                foreach (var agent in availableAgents)
                {
                    prompt.AppendLine($"- {agent.AgentId}: {agent.L1Description}");
                }
                prompt.AppendLine();

                // 基础约束
                prompt.AppendLine("约束条件：");
                prompt.AppendLine($"- 最大节点数：{options.MaxNodes}");
                prompt.AppendLine($"- 目标复杂度：{options.TargetComplexity}");
                
                if (options.PreferredCategories.Any())
                {
                    prompt.AppendLine($"- 偏好类别：{string.Join(", ", options.PreferredCategories)}");
                }

                if (options.ExcludedAgents.Any())
                {
                    prompt.AppendLine($"- 排除Agent：{string.Join(", ", options.ExcludedAgents)}");
                }
                
                prompt.AppendLine();

                // 输出格式要求
                prompt.AppendLine("请直接输出标准JSON格式的工作流定义，包括：");
                prompt.AppendLine("1. nodes: 节点列表，每个节点包含id、type、agentId、name等字段");
                prompt.AppendLine("2. connections: 连接列表，定义节点间的执行顺序");
                prompt.AppendLine("3. variables: 变量定义");
                prompt.AppendLine();
                prompt.AppendLine("输出格式：");
                prompt.AppendLine("{");
                prompt.AppendLine("  \"nodes\": [...],");
                prompt.AppendLine("  \"connections\": [...],");
                prompt.AppendLine("  \"variables\": [...]");
                prompt.AppendLine("}");

                return prompt.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建工作流生成提示词时出错");
                throw;
            }
        }

        /// <summary>
        /// 验证提示词格式
        /// </summary>
        public async Task<PromptValidationResult> ValidatePromptAsync(string prompt)
        {
            await Task.CompletedTask;
            
            var result = new PromptValidationResult
            {
                IsValid = !string.IsNullOrWhiteSpace(prompt)
            };

            if (!result.IsValid)
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "EMPTY_PROMPT",
                    Message = "提示词不能为空"
                });
            }

            return result;
        }

        /// <summary>
        /// 构建Agent选择提示词
        /// </summary>
        public async Task<string> BuildAgentSelectionPromptAsync(
            string userGoal,
            IEnumerable<AgentIndexInfo> availableAgents,
            WorkflowGenerationOptions options)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine($"根据用户目标：{userGoal}");
            prompt.AppendLine("从以下Agent中选择最合适的：");
            
            foreach (var agent in availableAgents)
            {
                prompt.AppendLine($"- {agent.AgentId}: {agent.L1Description}");
            }

            return prompt.ToString();
        }
    }
} 