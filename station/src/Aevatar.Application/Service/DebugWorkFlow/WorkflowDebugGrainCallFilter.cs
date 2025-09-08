using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Aevatar.GAgents.GroupChat;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using GroupChat.GAgent.Feature.Common;

namespace Aevatar.Service.DebugWorkFlow
{
    /// <summary>
    /// Workflow Debug Orleans Grain Call Filter - Dual Interception Strategy
    /// Intercepts before data flows to next agent to solve post-execution control
    /// </summary>
    public class WorkflowDebugGrainCallFilter : IIncomingGrainCallFilter, ISingletonDependency
    {
        private readonly IBreakpointManager _breakpointManager;
        private readonly ILogger<WorkflowDebugGrainCallFilter> _logger;
        
        // Performance statistics
        private static long _interceptedCalls = 0;
        private static long _debugChecks = 0;
        private static long _pausedCalls = 0;

        public WorkflowDebugGrainCallFilter(
            IBreakpointManager breakpointManager,
            ILogger<WorkflowDebugGrainCallFilter> logger)
        {
            _breakpointManager = breakpointManager;
            _logger = logger;
            
            _logger.LogInformation("🔧 WorkflowDebugGrainCallFilter initialized - dual interception strategy");
        }

        /// <summary>
        /// Orleans Grain调用拦截入口 - 完整接口拦截策略
        /// </summary>
        public async Task Invoke(IIncomingGrainCallContext context)
        {
            // 增加拦截计数
            System.Threading.Interlocked.Increment(ref _interceptedCalls);
            
            try
            {
                // 检查是否为IWorkflowCoordinatorGAgent接口的方法调用
                if (IsWorkflowCoordinatorGAgentCall(context))
                {
                    await InterceptWorkflowCoordinatorMethod(context);
                }
                else
                {
                    // 非目标调用，直接执行
                    await context.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grain调用拦截器发生错误: {GrainType}.{Method}", 
                    context.Grain?.GetType().Name, context.InterfaceMethod?.Name);
                
                // 确保即使拦截器出错也不影响正常流程
                await context.Invoke();
            }
        }

        /// <summary>
        /// 拦截IWorkflowCoordinatorGAgent接口的所有方法调用
        /// </summary>
        private async Task InterceptWorkflowCoordinatorMethod(IIncomingGrainCallContext context)
        {
            System.Threading.Interlocked.Increment(ref _debugChecks);
            
            var methodName = context.InterfaceMethod?.Name ?? "Unknown";
            _logger.LogDebug("🎯 拦截IWorkflowCoordinatorGAgent方法调用: {MethodName}", methodName);
            
            // 从grain获取workflowId
            var workflowId = GetWorkflowIdFromGrain((IGrain)context.Grain);
            
            // 根据方法类型进行断点检查 - 简化逻辑
            switch (methodName)
            {
                case "ExecuteCurrentNodeAsync":
                    await InterceptExecuteCurrentNodeAsync(context, workflowId);
                    break;
                    
                case "ContinueToDownstreamAsync":
                    await InterceptContinueToDownstreamAsync(context, workflowId);
                    break;
                    
                default:
                    // 其他方法直接执行，不进行断点检查
                    await context.Invoke();
                    break;
            }
        }

        /// <summary>
        /// 拦截ExecuteCurrentNodeAsync方法 - 执行前断点控制
        /// 这是事前拦截点：用户想执行某个节点时检查断点
        /// </summary>
        private async Task InterceptExecuteCurrentNodeAsync(IIncomingGrainCallContext context, Guid workflowId)
        {
            _logger.LogDebug("🎯 拦截ExecuteCurrentNodeAsync方法调用 - 事前断点检查");
            
            // 提取term参数（第一个参数是term）
            var term = ExtractTermFromContext(context);
            var nodeId = $"node-{term}";
            
            _logger.LogDebug("🔍 检查执行前断点: WorkflowId={WorkflowId}, NodeId={NodeId}, Term={Term}", workflowId, nodeId, term);
            
            // 检查是否有执行前断点
            var breakpointKey = $"{workflowId}:{nodeId}";
            if (_breakpointManager.HasBreakpointAsync(breakpointKey, "PreExecution").GetAwaiter().GetResult())
            {
                System.Threading.Interlocked.Increment(ref _pausedCalls);
                
                _logger.LogInformation("⏸️ 命中执行前断点 - 暂停执行: WorkflowId={WorkflowId}, NodeId={NodeId}", workflowId, nodeId);
                
                // 记录暂停状态，直接返回不执行
                await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PreExecution", null);
                
                return; // 直接返回，暂停执行
            }
            
            // 没有断点，执行原方法
            _logger.LogDebug("✅ 无执行前断点，执行节点: WorkflowId={WorkflowId}, NodeId={NodeId}", workflowId, nodeId);
            await context.Invoke();
        }

        /// <summary>
        /// 拦截ContinueToDownstreamAsync方法 - 执行后断点控制
        /// 这是事后拦截点：用户确认结果要继续到下游时检查断点
        /// </summary>
        private async Task InterceptContinueToDownstreamAsync(IIncomingGrainCallContext context, Guid workflowId)
        {
            _logger.LogDebug("🎯 拦截ContinueToDownstreamAsync方法调用 - 事后断点检查");
            
            // 提取term参数（第一个参数是term）
            var term = ExtractTermFromContext(context);
            var nodeId = $"node-{term}";
            
            _logger.LogDebug("🔍 检查执行后断点: WorkflowId={WorkflowId}, NodeId={NodeId}, Term={Term}", workflowId, nodeId, term);
            
            // 检查是否有执行后断点
            var breakpointKey = $"{workflowId}:{nodeId}";
            if (_breakpointManager.HasBreakpointAsync(breakpointKey, "PostExecution").GetAwaiter().GetResult())
            {
                System.Threading.Interlocked.Increment(ref _pausedCalls);
                
                _logger.LogInformation("⏸️ 命中执行后断点 - 暂停流转: WorkflowId={WorkflowId}, NodeId={NodeId}", workflowId, nodeId);
                
                // 记录暂停状态，直接返回不执行流转
                await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PostExecution", null);
                
                return; // 直接返回，暂停流转
            }
            
            // 没有断点，执行原方法
            _logger.LogDebug("✅ 无执行后断点，继续到下游: WorkflowId={WorkflowId}, NodeId={NodeId}", workflowId, nodeId);
            await context.Invoke();
        }


        #region 判断拦截目标

        /// <summary>
        /// 判断是否为IWorkflowCoordinatorGAgent接口的方法调用
        /// </summary>
        private static bool IsWorkflowCoordinatorGAgentCall(IIncomingGrainCallContext context)
        {
            return context.Grain is IWorkflowCoordinatorGAgent;
        }

        /// <summary>
        /// 从Grain实例中获取WorkflowId
        /// </summary>
        private Guid GetWorkflowIdFromGrain(IGrain grain)
        {
            try
            {
                // WorkflowCoordinatorGAgent的GrainId就是WorkflowId
                if (grain is IWorkflowCoordinatorGAgent coordinatorGAgent)
                {
                    // 获取grain的ID，这通常就是workflowId
                    var grainId = coordinatorGAgent.GetGrainId();
                    if (grainId.TryGetGuidKey(out var workflowId, out var _))
                    {
                        return workflowId;
                    }
                }
                
                _logger.LogWarning("无法从Grain中提取WorkflowId");
                return Guid.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取WorkflowId时发生错误");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// 从Orleans调用上下文中提取Term参数
        /// </summary>
        private long ExtractTermFromContext(IIncomingGrainCallContext context)
        {
            try
            {
                // TODO: 在生产环境中需要实现真正的参数提取逻辑
                // Orleans的IIncomingGrainCallContext接口可能不直接暴露Arguments属性
                // 这里暂时返回默认值，由调用方处理
                _logger.LogDebug("Orleans参数提取功能待实现 - 方法: {MethodName}", context.InterfaceMethod?.Name);
                return 1L; // 默认返回1
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从Orleans上下文提取Term失败");
                return 1L;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 从ChatResponseEvent中提取参数
        /// </summary>
        private Dictionary<string, object>? ExtractParametersFromEvent(ChatResponseEvent chatEvent)
        {
            try
            {
                var parameters = new Dictionary<string, object>();
                
                if (chatEvent.ChatResponse != null)
                    parameters["ChatResponse"] = chatEvent.ChatResponse;
                
                if (chatEvent.BlackboardId != null)
                    parameters["BlackboardId"] = chatEvent.BlackboardId;
                
                if (chatEvent.Term != null)
                    parameters["Term"] = chatEvent.Term;
                
                if (chatEvent.MemberName != null)
                    parameters["MemberName"] = chatEvent.MemberName;
                
                return parameters.Count > 0 ? parameters : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取事件参数时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 从Orleans上下文中提取状态信息
        /// </summary>
        private object? ExtractStateFromContext(IIncomingGrainCallContext context)
        {
            try
            {
                if (context.Grain is IWorkflowCoordinatorGAgent coordinator)
                {
                    // 通过反射获取状态属性
                    var grainType = coordinator.GetType();
                    var stateProperty = grainType.GetProperty("State");
                    
                    if (stateProperty != null)
                    {
                        return stateProperty.GetValue(coordinator);
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取状态信息时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 从Orleans上下文中提取执行结果
        /// </summary>
        private object? ExtractExecutionResultFromContext(IIncomingGrainCallContext context)
        {
            try
            {
                return new
                {
                    Success = true,
                    ExecutedAt = DateTime.UtcNow,
                    MethodName = context.InterfaceMethod?.Name,
                    Note = "节点执行完成，等待流转决策"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取执行结果时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 将修改后的参数应用到Orleans上下文
        /// </summary>
        private void ApplyModifiedParametersToContext(IIncomingGrainCallContext context, Dictionary<string, object>? modifiedParams)
        {
            try
            {
                if (modifiedParams == null || modifiedParams.Count == 0)
                    return;
                
                _logger.LogInformation("应用修改后的参数: {Count}个", modifiedParams.Count);
                
                foreach (var param in modifiedParams)
                {
                    _logger.LogDebug("参数修改: {Key} = {Value}", param.Key, param.Value);
                }
                
                // TODO: 实现实际的参数修改逻辑
                // Orleans的Arguments是只读的，需要通过其他方式实现参数修改
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用修改参数时发生错误");
            }
        }

        /// <summary>
        /// 从ChatEvent中提取参数
        /// </summary>
        private Dictionary<string, object>? ExtractParametersFromChatEvent(ChatEvent chatEvent)
        {
            try
            {
                var parameters = new Dictionary<string, object>();
                
                if (chatEvent.CoordinatorMessages != null)
                    parameters["CoordinatorMessages"] = chatEvent.CoordinatorMessages;
                
                if (chatEvent.BlackboardId != null)
                    parameters["BlackboardId"] = chatEvent.BlackboardId;
                
                if (chatEvent.Term != null)
                    parameters["Term"] = chatEvent.Term;
                
                if (chatEvent.Speaker != null)
                    parameters["Speaker"] = chatEvent.Speaker;
                
                return parameters.Count > 0 ? parameters : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取ChatEvent参数时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 获取拦截器统计信息 - 用于监控和调试
        /// </summary>
        public static InterceptorStats GetStats()
        {
            return new InterceptorStats
            {
                InterceptedCalls = _interceptedCalls,
                DebugChecks = _debugChecks,
                PausedCalls = _pausedCalls,
                SuccessRate = _debugChecks > 0 ? (double)(_debugChecks - _pausedCalls) / _debugChecks : 1.0
            };
        }
        
        /// <summary>
        /// 从Orleans调用上下文中提取ChatResponseEvent
        /// </summary>
        private ChatResponseEvent? ExtractChatResponseEventFromContext(IIncomingGrainCallContext context)
        {
            try
            {
                // TODO: 在生产环境中需要实现真正的参数提取逻辑
                // Orleans的IIncomingGrainCallContext接口可能不直接暴露Arguments属性
                // 这里暂时返回null，由调用方处理null情况
                _logger.LogDebug("Orleans参数提取功能待实现 - 方法: {MethodName}", context.InterfaceMethod?.Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从Orleans上下文提取ChatResponseEvent失败");
            }
            
            return null;
        }
        
        /// <summary>
        /// 从事件中提取节点ID
        /// </summary>
        private string GetNodeIdFromEvent(ChatResponseEvent chatEvent)
        {
            // 在实际实现中，这里应该从事件中提取真实的节点ID
            // 现在返回一个基于Term的简化ID
            return $"node-{chatEvent.Term}";
        }
        
        /// <summary>
        /// 从ChatEvent中提取节点ID
        /// </summary>
        private string GetNodeIdFromChatEvent(ChatEvent chatEvent)
        {
            // 在实际实现中，这里应该从事件中提取真实的节点ID
            // 现在返回一个基于Term的简化ID
            return $"node-{chatEvent.Term}";
        }
        
        /// <summary>
        /// 从PublishP2PAsync调用上下文中提取ChatEvent
        /// </summary>
        private ChatEvent? ExtractChatEventFromPublishP2PContext(IIncomingGrainCallContext context)
        {
            try
            {
                // TODO: 在生产环境中需要实现真正的参数提取逻辑
                // Orleans的IIncomingGrainCallContext接口可能不直接暴露Arguments属性
                // 这里暂时返回null，由调用方处理null情况
                _logger.LogDebug("Orleans参数提取功能待实现 - 方法: {MethodName}", context.InterfaceMethod?.Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从PublishP2PAsync上下文提取ChatEvent失败");
            }
            
            return null;
        }

        #endregion
    }

    /// <summary>
    /// 工作流中止异常
    /// </summary>
    public class WorkflowAbortedException : Exception
    {
        public WorkflowAbortedException(string message) : base(message) { }
        public WorkflowAbortedException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 拦截器统计信息
    /// </summary>
    public class InterceptorStats
    {
        public long InterceptedCalls { get; set; }
        public long DebugChecks { get; set; }
        public long PausedCalls { get; set; }
        public double SuccessRate { get; set; }
    }
}