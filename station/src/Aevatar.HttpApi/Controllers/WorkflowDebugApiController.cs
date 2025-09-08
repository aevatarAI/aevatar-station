using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Aevatar.Service.DebugWorkFlow;
using GroupChat.GAgent.Feature.Common;

namespace Aevatar.Controllers
{
    /// <summary>
    /// Workflow Debug API Controller - Dual Interception Strategy
    /// Direct integration with BreakpointManager for simplified architecture
    /// </summary>
    [ApiController]
    [Route("api/WorkflowDebug")]
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
        /// Set debugging breakpoint for workflow node
        /// POST /api/WorkflowDebugApi/breakpoint
        /// </summary>
        [HttpPost("breakpoint")]
        public async Task<IActionResult> SetBreakpoint([FromBody] SetBreakpointRequest request)
        {
            try
            {
                _logger.LogInformation("🔴 API set breakpoint: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                
                await _breakpointManager.SetBreakpointAsync(request.WorkflowId, request.NodeId, request.Type, request.Condition);
                
                return Ok(new { Success = true, Message = "Breakpoint set successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set breakpoint: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                return BadRequest(new { Success = false, Message = "Failed to set breakpoint: " + ex.Message });
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


        #region Status Query API

        /// <summary>
        /// Get list of paused workflow nodes
        /// GET /api/WorkflowDebugApi/paused-nodes
        /// </summary>
        [HttpGet("paused-nodes")]
        public async Task<IActionResult> GetPausedNodes()
        {
            try
            {
                _logger.LogInformation("📋 API get paused nodes");
                
                var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
                
                return Ok(new { Success = true, Data = pausedNodes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get paused nodes failed");
                return BadRequest(new { Success = false, Message = "Get paused nodes failed: " + ex.Message });
            }
        }

        /// <summary>
        /// Get detailed information of paused nodes
        /// GET /api/WorkflowDebugApi/paused-nodes/details
        /// </summary>
        [HttpGet("paused-nodes/details")]
        public async Task<IActionResult> GetPausedNodeInfos()
        {
            try
            {
                _logger.LogInformation("📋 API get paused node details");
                
                var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
                
                return Ok(new { Success = true, Data = pausedNodeInfos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get paused node details failed");
                return BadRequest(new { Success = false, Message = "Get paused node details failed: " + ex.Message });
            }
        }


        #endregion

        #region Advanced Features API

        /// <summary>
        /// Set multiple breakpoints in batch
        /// POST /api/WorkflowDebugApi/breakpoint/batch
        /// </summary>
        [HttpPost("breakpoint/batch")]
        public async Task<IActionResult> SetBatchBreakpoints([FromBody] BatchBreakpointRequest request)
        {
            _logger.LogInformation("🔴 API batch set breakpoints: {Count} items", request.Breakpoints?.Count ?? 0);
            
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
            
            var successCount = results.Where(r => ((dynamic)r).Success == true).Count();
            var response = new
            {
                Success = true,
                Message = $"Batch breakpoint operation completed: {successCount}/{results.Count} successful",
                Results = results
            };
            
            return Ok(response);
        }

        #endregion

        #region Core Debug Control API

        /// <summary>
        /// Retry node execution using workflowId and nodeId
        /// POST /api/WorkflowDebugApi/retry-node
        /// </summary>
        [HttpPost("retry-node")]
        public async Task<IActionResult> RetryNode([FromBody] RetryNodeRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 API retry node: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                
                var result = await _breakpointManager.RetryNodeAsync(request.WorkflowId, request.NodeId, request.CoordinatorMessages);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retry node failed: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                return BadRequest(new { Success = false, Message = "Retry node failed: " + ex.Message });
            }
        }

        /// <summary>
        /// Continue node execution using workflowId and nodeId
        /// POST /api/WorkflowDebugApi/continue-node
        /// </summary>
        [HttpPost("continue-node")]
        public async Task<IActionResult> ContinueNode([FromBody] ContinueNodeRequest request)
        {
            try
            {
                _logger.LogInformation("▶️ API continue to downstream: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                
                var result = await _breakpointManager.ContinueNodeAsync(request.WorkflowId, request.NodeId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Continue to downstream failed: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                return BadRequest(new { Success = false, Message = "Continue to downstream failed: " + ex.Message });
            }
        }

        /// <summary>
        /// Edit node input data
        /// PUT /api/WorkflowDebugApi/edit-input-data
        /// </summary>
        [HttpPut("edit-input-data")]
        public async Task<IActionResult> EditNodeInputData([FromBody] EditInputDataRequest request)
        {
            try
            {
                _logger.LogInformation("📝 API edit input data: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                
                var result = await _breakpointManager.EditNodeInputDataAsync(request.WorkflowId, request.NodeId, request.InputData);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit input data failed: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                return BadRequest(new { Success = false, Message = "Edit input data failed: " + ex.Message });
            }
        }

        /// <summary>
        /// Edit node state data
        /// PUT /api/WorkflowDebugApi/edit-state-data
        /// </summary>
        [HttpPut("edit-state-data")]
        public async Task<IActionResult> EditNodeStateData([FromBody] EditStateDataRequest request)
        {
            try
            {
                _logger.LogInformation("🔧 API edit state data: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                
                var result = await _breakpointManager.EditNodeStateAsync(request.WorkflowId, request.NodeId, request.StateData);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit state data failed: {WorkflowId}:{NodeId}", request.WorkflowId, request.NodeId);
                return BadRequest(new { Success = false, Message = "Edit state data failed: " + ex.Message });
            }
        }


        #endregion
    }

    #region DTO Definitions

    /// <summary>
    /// Request for setting workflow breakpoint
    /// </summary>
    public class SetBreakpointRequest
    {
        public Guid WorkflowId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public BreakpointType Type { get; set; }
        public string? Condition { get; set; }
    }


    /// <summary>
    /// Request for retrying node execution
    /// </summary>
    public class RetryNodeRequest
    {
        public Guid WorkflowId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public List<ChatMessage> CoordinatorMessages { get; set; } = new List<ChatMessage>();
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request for batch breakpoint operations
    /// </summary>
    public class BatchBreakpointRequest
    {
        public List<SetBreakpointRequest>? Breakpoints { get; set; }
    }

    /// <summary>
    /// Request for continuing node execution
    /// </summary>
    public class ContinueNodeRequest
    {
        public Guid WorkflowId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request for editing node input data
    /// </summary>
    public class EditInputDataRequest
    {
        public Guid WorkflowId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public string InputData { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request for editing node state data
    /// </summary>
    public class EditStateDataRequest
    {
        public Guid WorkflowId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public Dictionary<string, object> StateData { get; set; } = new Dictionary<string, object>();
        public string? Note { get; set; }
    }

    #endregion
}
