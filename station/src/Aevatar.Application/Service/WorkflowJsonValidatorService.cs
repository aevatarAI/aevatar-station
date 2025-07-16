using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Aevatar.Domain.WorkflowOrchestration;
using ValidationError = Aevatar.Application.Contracts.WorkflowOrchestration.ValidationError;
using ValidationWarning = Aevatar.Application.Contracts.WorkflowOrchestration.ValidationWarning;

namespace Aevatar.Application.Service
{
    /// <summary>
    /// 工作流JSON验证服务实现
    /// </summary>
    public class WorkflowJsonValidatorService : IWorkflowJsonValidator
    {
        private readonly ILogger<WorkflowJsonValidatorService> _logger;

        public WorkflowJsonValidatorService(ILogger<WorkflowJsonValidatorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 验证和解析工作流JSON
        /// </summary>
        public async Task<WorkflowJsonValidationResult> ValidateWorkflowJsonAsync(string jsonContent)
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
                var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (workflow != null)
                {
                    result.IsValid = true;
                    result.ParsedWorkflow = workflow;
                    
                    // 基本验证
                    ValidateBasicStructure(workflow, result);
                }
                else
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError 
                    { 
                        Code = "PARSE_FAILED", 
                        Message = "JSON解析失败" 
                    });
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError 
                { 
                    Code = "INVALID_JSON", 
                    Message = $"JSON格式错误：{ex.Message}" 
                });
                _logger.LogWarning(ex, "JSON解析失败");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError 
                { 
                    Code = "VALIDATION_ERROR", 
                    Message = $"验证过程出错：{ex.Message}" 
                });
                _logger.LogError(ex, "JSON验证出错");
            }

            return result;
        }

        /// <summary>
        /// 清理JSON内容
        /// </summary>
        public string CleanJsonContent(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return "{}";

            var cleaned = jsonContent.Trim();

            // 移除markdown代码块标记
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            if (cleaned.StartsWith("```"))
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
        /// 验证基本结构
        /// </summary>
        private void ValidateBasicStructure(WorkflowDefinition workflow, WorkflowJsonValidationResult result)
        {
            if (string.IsNullOrEmpty(workflow.Name))
            {
                result.Warnings.Add(new ValidationWarning 
                { 
                    Code = "MISSING_NAME", 
                    Message = "工作流名称为空" 
                });
            }

            if (workflow.Nodes == null || workflow.Nodes.Count == 0)
            {
                result.Errors.Add(new ValidationError 
                { 
                    Code = "NO_NODES", 
                    Message = "工作流必须包含节点" 
                });
                result.IsValid = false;
            }
        }
    }
} 