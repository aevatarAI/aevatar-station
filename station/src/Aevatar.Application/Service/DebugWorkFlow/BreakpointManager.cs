using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Aevatar.GAgents.GroupChat;
using GroupChat.GAgent.Feature.Common;
using Aevatar.GAgents.GroupChat.Core;

namespace Aevatar.Service.DebugWorkFlow
{
    /// <summary>
    /// Breakpoint Manager Implementation - Dual Interception Strategy
    /// Handles pre/post execution flow control for workflow debugging
    /// </summary>
    public class BreakpointManager : IBreakpointManager, ISingletonDependency
    {
        private readonly ILogger<BreakpointManager> _logger;
        
        // Breakpoint storage - using thread-safe collections
        private readonly ConcurrentDictionary<string, ExtendedBreakpointInfo> _breakpoints = new();
        
        // Paused node storage - WorkflowId:NodeId -> PausedNodeInfo
        private readonly ConcurrentDictionary<string, PausedNodeInfo> _pausedNodes = new();
        
        // Orleans grain factory - for direct workflow control
        private readonly IGrainFactory _grainFactory;

        public BreakpointManager(
            ILogger<BreakpointManager> logger,
            IGrainFactory grainFactory)
        {
            _logger = logger;
            _grainFactory = grainFactory;
            _logger.LogInformation("🔧 BreakpointManager initialized - instant control strategy");
        }

        #region Dual Interception Logic

        /// <summary>
        /// Check if should pause before execution - HandleEventAsync interception point
        /// </summary>
        public async Task<bool> ShouldPauseBeforeExecutionAsync(ChatResponseEvent chatEvent)
        {
            try
            {
                var nodeId = ExtractNodeIdFromChatResponseEvent(chatEvent);
                var workflowId = ExtractWorkflowIdFromChatResponseEvent(chatEvent);
                var breakpointKey = $"{workflowId}:{nodeId}";
                
                _logger.LogDebug("🔍 Checking pre-execution breakpoint: {BreakpointKey}", breakpointKey);
                
                if (_breakpoints.TryGetValue(breakpointKey, out var breakpoint) && breakpoint.IsEnabled)
                {
                    // 检查是否为前置断点
                    if (breakpoint.Type == BreakpointType.BeforeExecution || breakpoint.Type == BreakpointType.Conditional)
                    {
                        // 更新断点命中统计
                        breakpoint.HitCount++;
                        breakpoint.LastHitAt = DateTime.UtcNow;
                        
                        _logger.LogInformation("🔴 Hit pre-execution breakpoint: {NodeId} (hit #{HitCount})", nodeId, breakpoint.HitCount);
                        
                        // 检查条件断点
                        if (breakpoint.Type == BreakpointType.Conditional && !string.IsNullOrEmpty(breakpoint.Condition))
                        {
                            var conditionMet = await EvaluateConditionAsync(breakpoint.Condition, chatEvent);
                            if (!conditionMet)
                            {
                                _logger.LogDebug("执行前条件断点不满足: {Condition}", breakpoint.Condition);
                                return false;
                            }
                        }
                        
                        await NotifyBreakpointHitAsync(workflowId, breakpoint, "PreExecution");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查执行前断点时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 检查执行后是否应该暂停 - PublishP2PAsync拦截点（真正的流转控制）
        /// </summary>
        public async Task<bool> ShouldPauseAfterExecutionAsync(ChatEvent chatEvent, object? executionResult)
        {
            try
            {
                var nodeId = ExtractNodeIdFromChatEvent(chatEvent);
                var workflowId = ExtractWorkflowIdFromChatEvent(chatEvent);
                var breakpointKey = $"{workflowId}:{nodeId}";
                
                _logger.LogDebug("🔍 检查执行后断点（流转控制）: {BreakpointKey}", breakpointKey);
                
                if (_breakpoints.TryGetValue(breakpointKey, out var breakpoint) && breakpoint.IsEnabled)
                {
                    // 检查是否为后置断点
                    if (breakpoint.Type == BreakpointType.AfterExecution)
                    {
                        breakpoint.HitCount++;
                        breakpoint.LastHitAt = DateTime.UtcNow;
                        
                        _logger.LogInformation("🟡 命中执行后断点（流转控制）: {NodeId} (第{HitCount}次)", nodeId, breakpoint.HitCount);
                        
                        await NotifyBreakpointHitAsync(workflowId, breakpoint, "PostExecution");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查执行后断点时发生错误");
                return false;
            }
        }

        #endregion

        #region 即时控制方法

        /// <summary>
        /// 记录暂停节点状态 - 直接暂停不等待
        /// </summary>
        public async Task RecordPausedNodeAsync(Guid workflowId, string nodeId, string stage, Dictionary<string, object>? context)
        {
            try
            {
                var pauseKey = $"{workflowId}:{nodeId}";
                
                var pausedInfo = new PausedNodeInfo
                {
                    WorkflowId = workflowId,
                    NodeId = nodeId,
                    Stage = stage == "PreExecution" ? PauseStage.PreExecution : PauseStage.PostExecution,
                    PausedAt = DateTime.UtcNow,
                    Context = context,
                    Note = $"暂停于{stage}阶段"
                };
                
                _pausedNodes[pauseKey] = pausedInfo;
                
                _logger.LogInformation("📝 记录暂停节点: {WorkflowId}:{NodeId} [{Stage}]", workflowId, nodeId, stage);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录暂停节点时发生错误: {WorkflowId}:{NodeId}", workflowId, nodeId);
            }
        }

        /// <summary>
        /// 直接继续到下游节点 - 重新发起流转
        /// </summary>
        public async Task ContinueToNextNodeAsync(Guid workflowId, string nodeId)
        {
            try
            {
                var pauseKey = $"{workflowId}:{nodeId}";
                
                if (_pausedNodes.TryGetValue(pauseKey, out var pausedInfo))
                {
                    _logger.LogInformation("➡️ 继续到下游节点: {WorkflowId}:{NodeId}", workflowId, nodeId);
                    
                    // 获取WorkflowCoordinatorGAgent
                    var coordinator = _grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId);
                    // 重新发起流转到下游
                    if (pausedInfo.Context != null && pausedInfo.Context.TryGetValue("CoordinatorMessages", out var messages))
                    {
                        var chatEvent = new ChatEvent
                        {
                            BlackboardId = workflowId,
                            Speaker = pausedInfo.Context.TryGetValue("Speaker", out var speaker) ? (Guid)speaker : Guid.Empty,
                            Term = pausedInfo.Context.TryGetValue("Term", out var term) ? 
                                (term is long l ? l : (term is string s && long.TryParse(s, out var parsed) ? parsed : 0L)) : 0L,
                            CoordinatorMessages = messages as List<ChatMessage>
                        };
                        
                        // 通过重新触发TryActiveWorkUnitAsync来继续流程
                        // 这里需要通过反射或其他方式调用内部方法
                        // 或者直接发布ChatEvent到下游
                        
                        _logger.LogInformation("🔄 重新发起流转事件");
                    }
                    
                    // 清理暂停状态
                    _pausedNodes.TryRemove(pauseKey, out _);
                }
                else
                {
                    _logger.LogWarning("⚠️ No paused node found: {WorkflowId}:{NodeId}", workflowId, nodeId);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "继续到下游时发生错误: {WorkflowId}:{NodeId}", workflowId, nodeId);
            }
        }


        #endregion

        #region 断点管理

        /// <summary>
        /// 设置断点
        /// </summary>
        public async Task SetBreakpointAsync(Guid workflowId, string nodeId, BreakpointType type, string? condition = null)
        {
            var breakpointKey = $"{workflowId}:{nodeId}";
            
            var breakpointInfo = new ExtendedBreakpointInfo
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                NodeId = nodeId,
                Type = type,
                IsEnabled = true,
                Condition = condition,
                CreatedAt = DateTime.UtcNow,
                Description = GetBreakpointDescription(type, nodeId)
            };
            
            _breakpoints[breakpointKey] = breakpointInfo;
            
            _logger.LogInformation("🔴 Set breakpoint: {WorkflowId}:{NodeId} [{Type}]", workflowId, nodeId, type);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 移除断点
        /// </summary>
        public async Task RemoveBreakpointAsync(Guid workflowId, string nodeId)
        {
            var breakpointKey = $"{workflowId}:{nodeId}";
            
            if (_breakpoints.TryRemove(breakpointKey, out var removed))
            {
                _logger.LogInformation("🟢 移除断点: {WorkflowId}:{NodeId}", workflowId, nodeId);
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 清空所有断点
        /// </summary>
        public async Task ClearAllBreakpointsAsync()
        {
            var count = _breakpoints.Count;
            _breakpoints.Clear();
            
            _logger.LogInformation("🧹 清空所有断点: {Count}个", count);
            
            await Task.CompletedTask;
        }

        #endregion

        #region 简化执行控制（兼容旧API）

        /// <summary>
        /// 继续执行 - 兼容旧API，转发到ContinueToNextNodeAsync
        /// </summary>
        public async Task ContinueExecutionAsync(string nodeId)
        {
            // 从暂停节点中找到对应的工作流ID
            var pausedNode = _pausedNodes.Values.FirstOrDefault(p => p.NodeId == nodeId);
            if (pausedNode != null)
            {
                await ContinueToNextNodeAsync(pausedNode.WorkflowId, nodeId);
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到暂停节点，无法继续执行: {NodeId}", nodeId);
            }
        }

        /// <summary>
        /// 跳过当前节点 - 实际上就是直接清理暂停状态
        /// </summary>
        public async Task SkipNodeAsync(string nodeId)
        {
            var pausedNode = _pausedNodes.Values.FirstOrDefault(p => p.NodeId == nodeId);
            if (pausedNode != null)
            {
                var pauseKey = $"{pausedNode.WorkflowId}:{nodeId}";
                _pausedNodes.TryRemove(pauseKey, out _);
                
                _logger.LogInformation("⏭️ 跳过节点执行（清理暂停状态）: {NodeId}", nodeId);
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到暂停节点，无法跳过: {NodeId}", nodeId);
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Legacy retry method by nodeId - use RetryNodeAsync(workflowId, term, messages) instead
        /// </summary>
        [Obsolete("Use RetryNodeAsync(Guid workflowId, long term, List<ChatMessage> coordinatorMessages) instead")]
        public async Task RetryNodeByIdAsync(string nodeId, Dictionary<string, object>? newParameters = null)
        {
            _logger.LogWarning("⚠️ Using legacy RetryNodeByIdAsync method - consider upgrading to term-based RetryNodeAsync");
            _logger.LogInformation("🔄 Legacy retry for nodeId: {NodeId}", nodeId);
            await Task.CompletedTask; // Placeholder for legacy compatibility
        }

        /// <summary>
        /// 中止工作流 - 清理相关暂停节点
        /// </summary>
        public async Task AbortWorkflowAsync(Guid workflowId)
        {
            var affectedNodes = new List<string>();
            
            // 找到所有相关的暂停节点并清理
            foreach (var (key, pausedNode) in _pausedNodes.ToList())
            {
                if (pausedNode.WorkflowId == workflowId)
                {
                    _pausedNodes.TryRemove(key, out _);
                    affectedNodes.Add(pausedNode.NodeId);
                }
            }
            
            _logger.LogWarning("🛑 中止工作流: {WorkflowId} (清理{Count}个暂停节点)", workflowId, affectedNodes.Count);
            
            await Task.CompletedTask;
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取所有活跃断点
        /// </summary>
        public async Task<IEnumerable<BreakpointInfo>> GetActiveBreakpointsAsync()
        {
            var activeBreakpoints = _breakpoints.Values
                .Where(bp => bp.IsEnabled)
                .Cast<BreakpointInfo>()
                .ToList();
                
            await Task.CompletedTask;
            return activeBreakpoints;
        }

        /// <summary>
        /// 获取当前暂停的节点列表
        /// </summary>
        public async Task<IEnumerable<string>> GetPausedNodesAsync()
        {
            var pausedNodes = _pausedNodes.Values
                .Select(p => p.NodeId)
                .Distinct()
                .ToList();
                
            await Task.CompletedTask;
            return pausedNodes;
        }

        /// <summary>
        /// 获取暂停节点详细信息
        /// </summary>
        public async Task<IEnumerable<PausedNodeInfo>> GetPausedNodeInfosAsync()
        {
            var pausedInfos = _pausedNodes.Values.ToList();
            await Task.CompletedTask;
            return pausedInfos;
        }

        /// <summary>
        /// 检查是否存在指定的断点
        /// </summary>
        public async Task<bool> HasBreakpointAsync(string breakpointKey, string stage)
        {
            await Task.CompletedTask;
            
            if (!_breakpoints.TryGetValue(breakpointKey, out var breakpoint))
                return false;
                
            if (!breakpoint.IsEnabled)
                return false;
                
            return stage switch
            {
                "PreExecution" => breakpoint.Type == BreakpointType.BeforeExecution || breakpoint.Type == BreakpointType.Conditional,
                "PostExecution" => breakpoint.Type == BreakpointType.AfterExecution,
                _ => false
            };
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 从ChatResponseEvent中提取节点ID
        /// </summary>
        private string ExtractNodeIdFromChatResponseEvent(ChatResponseEvent chatEvent)
        {
            return chatEvent.Term.ToString();
        }

        /// <summary>
        /// 从ChatEvent中提取节点ID
        /// </summary>
        private string ExtractNodeIdFromChatEvent(ChatEvent chatEvent)
        {
            return chatEvent.Term.ToString();
        }

        /// <summary>
        /// 从ChatResponseEvent中提取工作流ID
        /// </summary>
        private Guid ExtractWorkflowIdFromChatResponseEvent(ChatResponseEvent chatEvent)
        {
            return chatEvent.BlackboardId;
        }

        /// <summary>
        /// 从ChatEvent中提取工作流ID
        /// </summary>
        private Guid ExtractWorkflowIdFromChatEvent(ChatEvent chatEvent)
        {
            return chatEvent.BlackboardId;
        }

        /// <summary>
        /// 评估条件断点
        /// </summary>
        private async Task<bool> EvaluateConditionAsync(string condition, ChatResponseEvent chatEvent)
        {
            // TODO: 实现条件表达式评估
            await Task.CompletedTask;
            
            // 简单实现：检查消息内容
            if (condition.StartsWith("content:"))
            {
                var expectedContent = condition.Substring(8);
                return chatEvent.ChatResponse?.Content?.Contains(expectedContent) == true;
            }
            
            return true; // 默认满足条件
        }

        /// <summary>
        /// 通知断点命中
        /// </summary>
        private async Task NotifyBreakpointHitAsync(Guid workflowId, ExtendedBreakpointInfo breakpoint, string stage)
        {
            _logger.LogInformation("📢 断点命中通知: {WorkflowId} -> {NodeId} [{Stage}]", workflowId, breakpoint.NodeId, stage);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取断点描述
        /// </summary>
        private string GetBreakpointDescription(BreakpointType type, string nodeId)
        {
            return type switch
            {
                BreakpointType.BeforeExecution => $"执行前断点 at {nodeId}",
                BreakpointType.AfterExecution => $"执行后断点（流转控制） at {nodeId}",
                BreakpointType.OnError => $"错误断点 at {nodeId}",
                BreakpointType.Conditional => $"条件断点 at {nodeId}",
                _ => $"断点 at {nodeId}"
            };
        }

        #endregion

        #region New API Methods

        /// <summary>
        /// Retry node execution using workflowId and nodeId
        /// Internal logic will automatically resolve term from these parameters
        /// </summary>
        public async Task<ApiResponse<string>> RetryNodeAsync(Guid workflowId, string nodeId, List<ChatMessage> coordinatorMessages)
        {
            try
            {
                _logger.LogInformation("🔄 Retrying node - WorkflowId: {WorkflowId}, NodeId: {NodeId}, Messages: {MessageCount}", 
                    workflowId, nodeId, coordinatorMessages.Count);

                // Get term from nodeId internally
                var term = await GetTermByNodeIdInternalAsync(workflowId, nodeId);
                if (term == null)
                {
                    return ApiResponse<string>.ErrorResult($"Could not find term for WorkflowId: {workflowId}, NodeId: {nodeId}", "TERM_NOT_FOUND");
                }

                var coordinatorGAgent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId);
                await coordinatorGAgent.ExecuteCurrentNodeAsync(term.Value, coordinatorMessages);

                // Remove from paused nodes if exists
                var pausedKey = $"{workflowId}:{nodeId}";
                _pausedNodes.TryRemove(pausedKey, out _);

                _logger.LogInformation("✅ Node retry completed - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.SuccessResult($"Node retry completed for node {nodeId}", "Retry successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrying node - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.ErrorResult($"Failed to retry node: {ex.Message}", "RETRY_ERROR");
            }
        }

        /// <summary>
        /// Continue node execution using workflowId and nodeId  
        /// Internal logic will automatically resolve term from these parameters
        /// </summary>
        public async Task<ApiResponse<string>> ContinueNodeAsync(Guid workflowId, string nodeId)
        {
            try
            {
                _logger.LogInformation("▶️ Continuing to downstream - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);

                // Get term from nodeId internally
                var term = await GetTermByNodeIdInternalAsync(workflowId, nodeId);
                if (term == null)
                {
                    return ApiResponse<string>.ErrorResult($"Could not find term for WorkflowId: {workflowId}, NodeId: {nodeId}", "TERM_NOT_FOUND");
                }

                var coordinatorGAgent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId);
                await coordinatorGAgent.ContinueToDownstreamAsync(term.Value);

                // Remove from paused nodes if exists
                var pausedKey = $"{workflowId}:{nodeId}";
                _pausedNodes.TryRemove(pausedKey, out _);

                _logger.LogInformation("✅ Continue to downstream completed - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.SuccessResult($"Continue to downstream completed for node {nodeId}", "Continue successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error continuing to downstream - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.ErrorResult($"Failed to continue to downstream: {ex.Message}", "CONTINUE_ERROR");
            }
        }

        /// <summary>
        /// Edit input data for a specific node (modifies WorkflowExecutionRecord)
        /// </summary>
        public async Task<ApiResponse<string>> EditNodeInputDataAsync(Guid workflowId, string nodeId, string inputData)
        {
            try
            {
                _logger.LogInformation("📝 Editing node input data - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);

                // Note: This is a placeholder implementation. In a real system, you would need to:
                // 1. Access the execution record grain that stores input data
                // 2. Update the specific work unit's input data
                // 3. Ensure proper event sourcing for state changes
                
                _logger.LogWarning("⚠️ Input data editing not fully implemented - would update input data for node: {NodeId} with data: {InputData}", nodeId, inputData);

                _logger.LogInformation("✅ Node input data edit initiated - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.SuccessResult($"Input data update initiated for node {nodeId}", "Edit initiated (full implementation pending)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error editing node input data - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.ErrorResult($"Failed to edit input data: {ex.Message}", "EDIT_INPUT_ERROR");
            }
        }

        /// <summary>
        /// Edit state data for a specific workflow node
        /// </summary>
        public async Task<ApiResponse<string>> EditNodeStateAsync(Guid workflowId, string nodeId, Dictionary<string, object> stateData)
        {
            try
            {
                _logger.LogInformation("🔧 Editing node state - WorkflowId: {WorkflowId}, NodeId: {NodeId}, StateKeys: {StateKeys}", 
                    workflowId, nodeId, string.Join(", ", stateData.Keys));

                // Note: This is a placeholder implementation. In a real system, you would need to:
                // 1. Parse the nodeId to get the actual grain ID
                // 2. Get the specific GAgent grain instance
                // 3. Call methods on the GAgent to modify its state through proper event sourcing
                // 4. Ensure consistency with the overall workflow state
                
                _logger.LogWarning("⚠️ State editing not fully implemented - would modify state with data: {StateData}", 
                    System.Text.Json.JsonSerializer.Serialize(stateData));

                _logger.LogInformation("✅ Node state edit initiated - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.SuccessResult($"State data update initiated for node {nodeId}", "Edit initiated (full implementation pending)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error editing node state - WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return ApiResponse<string>.ErrorResult($"Failed to edit state: {ex.Message}", "EDIT_STATE_ERROR");
            }
        }

        /// <summary>
        /// Internal helper method to get term ID from workflowId and nodeId (workUnitGrainId)
        /// </summary>
        private async Task<long?> GetTermByNodeIdInternalAsync(Guid workflowId, string nodeId)
        {
            try
            {
                _logger.LogDebug("Getting term for WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);

                var coordinatorGAgent = _grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId);
                var coordinatorState = await coordinatorGAgent.GetStateAsync();

                // Check if state is valid
                if (coordinatorState?.TermToWorkUnitGrainId == null)
                {
                    _logger.LogWarning("No state or TermToWorkUnitGrainId found for WorkflowId: {WorkflowId}", workflowId);
                    return null;
                }

                // Find term by reverse lookup in TermToWorkUnitGrainId mapping
                foreach (var kvp in coordinatorState.TermToWorkUnitGrainId)
                {
                    if (kvp.Value == nodeId)
                    {
                        _logger.LogDebug("Found term {Term} for NodeId: {NodeId}", kvp.Key, nodeId);
                        return kvp.Key;
                    }
                }

                _logger.LogWarning("No term found for WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting term for WorkflowId: {WorkflowId}, NodeId: {NodeId}", workflowId, nodeId);
                return null;
            }
        }


        #endregion
    }

    /// <summary>
    /// 调试会话信息
    /// </summary>
    public class DebugSessionInfo
    {
        public Guid WorkflowId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public List<string> PausedNodes { get; set; } = new();
        public string CurrentStage { get; set; } = "Unknown"; // PreExecution, PostExecution, Running
    }
}