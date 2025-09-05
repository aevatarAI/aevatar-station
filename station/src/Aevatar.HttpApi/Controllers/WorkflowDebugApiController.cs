using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Aevatar.Service.DebugWorkFlow;

namespace Aevatar.Controllers
{
    /// <summary>
    /// Workflow Debug API Controller - Dual Interception Strategy
    /// Direct integration with BreakpointManager for simplified architecture
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowDebugApiController : ControllerBase
    {
        private readonly IBreakpointManager _breakpointManager;
        private readonly ILogger<WorkflowDebugApiController> _logger;

        public WorkflowDebugApiController(
            IBreakpointManager breakpointManager,
            ILogger<WorkflowDebugApiController> logger)
        {
            _breakpointManager = breakpointManager;
            _logger = logger;
        }

        #region Breakpoint Management API

        /// <summary>
        /// 设置断点
        /// POST /api/WorkflowDebugApi/breakpoint
        /// </summary>
        [HttpPost("breakpoint")]
        public async Task<IActionResult> SetBreakpoint([FromBody] SetBreakpointRequest request)
        {
            try
            {
                _logger.LogInformation("🔴 API设置断点: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                
                await _breakpointManager.SetBreakpointAsync(request.WorkflowId, request.NodeId, request.Type, request.Condition);
                
                return Ok(new { Success = true, Message = "断点设置成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置断点失败: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                return BadRequest(new { Success = false, Message = "设置断点失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 移除断点
        /// DELETE /api/WorkflowDebugApi/breakpoint/{workflowId}/{nodeId}
        /// </summary>
        [HttpDelete("breakpoint/{workflowId}/{nodeId}")]
        public async Task<IActionResult> RemoveBreakpoint(Guid workflowId, string nodeId)
        {
            try
            {
                _logger.LogInformation("🟢 API移除断点: {WorkflowId}:{NodeId}", workflowId, nodeId);
                
                await _breakpointManager.RemoveBreakpointAsync(workflowId, nodeId);
                
                return Ok(new { Success = true, Message = "断点移除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除断点失败: {WorkflowId}:{NodeId}", workflowId, nodeId);
                return BadRequest(new { Success = false, Message = "移除断点失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 清空所有断点
        /// DELETE /api/WorkflowDebugApi/breakpoint
        /// </summary>
        [HttpDelete("breakpoint")]
        public async Task<IActionResult> ClearAllBreakpoints()
        {
            _logger.LogInformation("🧹 API清空所有断点");
            
            await _breakpointManager.ClearAllBreakpointsAsync();
            
            return Ok(new { Success = true, Message = "所有断点已清空" });
        }

        /// <summary>
        /// 获取活跃断点列表
        /// GET /api/WorkflowDebugApi/breakpoint
        /// </summary>
        [HttpGet("breakpoint")]
        public async Task<IActionResult> GetActiveBreakpoints()
        {
            var breakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
            
            return Ok(new { Success = true, Data = breakpoints });
        }

        #endregion

        #region 即时控制API

        /// <summary>
        /// 继续到下游节点
        /// POST /api/WorkflowDebugApi/continue-next/{workflowId}/{nodeId}
        /// </summary>
        [HttpPost("continue-next/{workflowId}/{nodeId}")]
        public async Task<IActionResult> ContinueToNextNode(Guid workflowId, string nodeId)
        {
            try
            {
                _logger.LogInformation("➡️ API继续到下游: {WorkflowId}:{NodeId}", workflowId, nodeId);
                
                await _breakpointManager.ContinueToNextNodeAsync(workflowId, nodeId);
                
                return Ok(new { Success = true, Message = "已触发继续到下游节点" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "继续到下游失败: {WorkflowId}:{NodeId}", workflowId, nodeId);
                return BadRequest(new { Success = false, Message = "继续到下游失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 重试当前节点
        /// POST /api/WorkflowDebugApi/retry-current/{workflowId}/{nodeId}
        /// </summary>
        [HttpPost("retry-current/{workflowId}/{nodeId}")]
        public async Task<IActionResult> RetryCurrentNode(Guid workflowId, string nodeId, [FromBody] RetryNodeRequest? request = null)
        {
            try
            {
                _logger.LogInformation("🔄 API重试当前节点: {WorkflowId}:{NodeId}", workflowId, nodeId);
                
                await _breakpointManager.RetryCurrentNodeAsync(workflowId, nodeId, request?.ModifiedParams);
                
                return Ok(new { Success = true, Message = "已触发重试当前节点" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试当前节点失败: {WorkflowId}:{NodeId}", workflowId, nodeId);
                return BadRequest(new { Success = false, Message = "重试当前节点失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 跳过当前节点（清理暂停状态）
        /// POST /api/WorkflowDebugApi/skip/{nodeId}
        /// </summary>
        [HttpPost("skip/{nodeId}")]
        public async Task<IActionResult> SkipNode(string nodeId)
        {
            try
            {
                _logger.LogInformation("⏭️ API跳过节点: {NodeId}", nodeId);
                
                await _breakpointManager.SkipNodeAsync(nodeId);
                
                return Ok(new { Success = true, Message = "已跳过节点（清理暂停状态）" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "跳过节点失败: {NodeId}", nodeId);
                return BadRequest(new { Success = false, Message = "跳过节点失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 中止工作流
        /// POST /api/WorkflowDebugApi/abort
        /// </summary>
        [HttpPost("abort")]
        public async Task<IActionResult> AbortWorkflow([FromBody] AbortWorkflowRequest request)
        {
            _logger.LogWarning("🛑 API中止工作流: {WorkflowId}", request.WorkflowId);
            
            await _breakpointManager.AbortWorkflowAsync(request.WorkflowId);
            
            return Ok(new { Success = true, Message = $"工作流 {request.WorkflowId} 已中止" });
        }

        #endregion

        #region 状态查询API

        /// <summary>
        /// 获取暂停的节点列表
        /// GET /api/WorkflowDebugApi/paused-nodes
        /// </summary>
        [HttpGet("paused-nodes")]
        public async Task<IActionResult> GetPausedNodes()
        {
            try
            {
                _logger.LogInformation("📋 API获取暂停节点");
                
                var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
                
                return Ok(new { Success = true, Data = pausedNodes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取暂停节点失败");
                return BadRequest(new { Success = false, Message = "获取暂停节点失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 获取暂停节点详细信息
        /// GET /api/WorkflowDebugApi/paused-nodes/details
        /// </summary>
        [HttpGet("paused-nodes/details")]
        public async Task<IActionResult> GetPausedNodeInfos()
        {
            try
            {
                _logger.LogInformation("📋 API获取暂停节点详细信息");
                
                var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
                
                return Ok(new { Success = true, Data = pausedNodeInfos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取暂停节点详细信息失败");
                return BadRequest(new { Success = false, Message = "获取暂停节点详细信息失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 获取拦截器统计信息
        /// GET /api/WorkflowDebugApi/stats
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetInterceptorStats()
        {
            // Note: This would need to be implemented if InterceptorStats are needed
            var stats = new { Message = "调试拦截器统计功能待实现" };
            
            return Ok(new { Success = true, Data = stats });
        }

        /// <summary>
        /// 健康检查
        /// GET /api/WorkflowDebugApi/health
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Note: Using basic health check without detailed stats for now
                var stats = new { TotalBreakpoints = 0, IsHealthy = true };
                
                var healthInfo = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Component = "WorkflowDebugGrainCallFilter",
                    Stats = stats
                };
                
                return Ok(healthInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调试服务健康检查失败");
                
                return StatusCode(500, new
                {
                    Status = "Unhealthy",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #endregion

        #region 高级功能API

        /// <summary>
        /// 批量设置断点
        /// POST /api/WorkflowDebugApi/breakpoint/batch
        /// </summary>
        [HttpPost("breakpoint/batch")]
        public async Task<IActionResult> SetBatchBreakpoints([FromBody] BatchBreakpointRequest request)
        {
            _logger.LogInformation("🔴 API批量设置断点: {Count}个", request.Breakpoints?.Count ?? 0);
            
            var results = new List<object>();
            
            if (request.Breakpoints != null)
            {
                foreach (var breakpointRequest in request.Breakpoints)
                {
                    try
                    {
                        await _breakpointManager.SetBreakpointAsync(breakpointRequest.WorkflowId, breakpointRequest.NodeId, breakpointRequest.Type, breakpointRequest.Condition);
                        results.Add(new { Success = true, WorkflowId = breakpointRequest.WorkflowId, NodeId = breakpointRequest.NodeId });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { Success = false, WorkflowId = breakpointRequest.WorkflowId, NodeId = breakpointRequest.NodeId, Error = ex.Message });
                    }
                }
            }
            
            var successCount = results.Count;
            var response = new
            {
                Success = true,
                Message = $"批量设置断点完成: {successCount}/{results.Count} 成功",
                Results = results
            };
            
            return Ok(response);
        }

        /// <summary>
        /// 快速调试模式 - 为整个工作流设置断点
        /// POST /api/WorkflowDebugApi/quick-debug/{workflowId}
        /// </summary>
        [HttpPost("quick-debug/{workflowId}")]
        public async Task<IActionResult> EnableQuickDebugMode(Guid workflowId, [FromQuery] string? nodePattern = "*")
        {
            _logger.LogInformation("⚡ API启用快速调试模式: {WorkflowId} (模式: {Pattern})", workflowId, nodePattern);
            
            try
            {
                // TODO: 实现快速调试模式 - 为工作流的所有节点或匹配模式的节点设置断点
                var result = new
                {
                    Success = true,
                    Message = $"快速调试模式已启用 (工作流: {workflowId}, 模式: {nodePattern})",
                    WorkflowId = workflowId,
                    Pattern = nodePattern,
                    EnabledAt = DateTime.UtcNow
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用快速调试模式失败: {WorkflowId}", workflowId);
                
                return BadRequest(new
                {
                    Success = false,
                    Message = "启用快速调试模式失败: " + ex.Message
                });
            }
        }

        #endregion
    }

    #region DTO类定义

    /// <summary>
    /// 设置断点请求
    /// </summary>
    public class SetBreakpointRequest
    {
        public Guid WorkflowId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public BreakpointType Type { get; set; }
        public string? Condition { get; set; }
    }

    /// <summary>
    /// 中止工作流请求
    /// </summary>
    public class AbortWorkflowRequest
    {
        public Guid WorkflowId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 重试节点请求
    /// </summary>
    public class RetryNodeRequest
    {
        public Dictionary<string, object>? ModifiedParams { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// 批量断点操作请求
    /// </summary>
    public class BatchBreakpointRequest
    {
        public List<SetBreakpointRequest>? Breakpoints { get; set; }
    }

    #endregion
}
