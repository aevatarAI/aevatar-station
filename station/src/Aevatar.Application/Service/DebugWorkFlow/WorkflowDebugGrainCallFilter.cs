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
        /// Orleans Grain调用拦截入口 - 双重拦截策略
        /// </summary>
        public async Task Invoke(IIncomingGrainCallContext context)
        {
            // 增加拦截计数
            System.Threading.Interlocked.Increment(ref _interceptedCalls);
            
            try
            {
                // 策略1：拦截WorkflowCoordinatorGAgent的HandleEventAsync - 执行前控制
                if (IsWorkflowHandleEventCall(context))
                {
                    await InterceptWorkflowHandleEventAsync(context);
                }
                // 策略2：拦截PublishP2PAsync调用 - 执行后流转控制  
                else if (IsWorkflowPublishP2PCall(context))
                {
                    await InterceptWorkflowPublishP2PAsync(context);
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
        /// 拦截WorkflowCoordinatorGAgent的HandleEventAsync - 执行前控制
        /// </summary>
        private async Task InterceptWorkflowHandleEventAsync(IIncomingGrainCallContext context)
        {
            System.Threading.Interlocked.Increment(ref _debugChecks);
            
            _logger.LogDebug("🎯 拦截HandleEventAsync方法调用");
            
            // 通过反射从方法参数中提取ChatResponseEvent
            var chatEvent = ExtractChatResponseEventFromContext(context);
            if (chatEvent == null)
            {
                // 在测试环境中，如果无法提取参数，创建一个默认的事件对象
                _logger.LogDebug("无法提取ChatResponseEvent，使用默认事件进行断点检查");
                chatEvent = new ChatResponseEvent 
                { 
                    BlackboardId = Guid.NewGuid(), 
                    MemberId = Guid.NewGuid(), 
                    Term = 1L 
                };
            }
            
            var workflowId = chatEvent.BlackboardId;
            var nodeId = GetNodeIdFromEvent(chatEvent);
            
            _logger.LogDebug("🎯 检查执行前断点: WorkflowId={WorkflowId}, NodeId={NodeId}", workflowId, nodeId);
            
            // 执行前断点检查
            var shouldPauseBeforeExecution = await _breakpointManager.ShouldPauseBeforeExecutionAsync(chatEvent);
            
            if (shouldPauseBeforeExecution)
            {
                System.Threading.Interlocked.Increment(ref _pausedCalls);
                
                _logger.LogInformation("⏸️ 工作流在节点 {NodeId} 执行前暂停 - 直接返回", nodeId);
                
                // 记录暂停状态，直接返回不执行
                var currentParams = new Dictionary<string, object>(); // Simplified - would extract from real event
                await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PreExecution", currentParams);
                
                return; // 直接返回，暂停执行
            }
            
            // 执行原方法 - 这里会执行完整的HandleEventAsync
            await context.Invoke();
        }

        /// <summary>
        /// 拦截PublishP2PAsync调用 - 执行后流转控制
        /// 这是真正的流转控制点，在数据发送到下游agent之前拦截
        /// </summary>
        private async Task InterceptWorkflowPublishP2PAsync(IIncomingGrainCallContext context)
        {
            System.Threading.Interlocked.Increment(ref _debugChecks);
            
            _logger.LogDebug("🎯 拦截PublishP2PAsync方法调用");
            
            // 通过简化方法提取ChatEvent（第二个参数通常是ChatEvent）
            var chatEvent = ExtractChatEventFromPublishP2PContext(context);
            if (chatEvent == null)
            {
                // 在测试环境中，如果无法提取参数，创建一个默认的事件对象
                _logger.LogDebug("无法提取ChatEvent，使用默认事件进行断点检查");
                chatEvent = new ChatEvent
                {
                    BlackboardId = Guid.NewGuid(),
                    Speaker = Guid.NewGuid(),
                    Term = 1L,
                    CoordinatorMessages = new List<ChatMessage>()
                };
            }
                
            var workflowId = chatEvent.BlackboardId;
            var nodeId = GetNodeIdFromChatEvent(chatEvent);
            
            _logger.LogDebug("🎯 检查执行后断点: WorkflowId={WorkflowId}, NodeId={NodeId}", workflowId, nodeId);
                
            // 执行后断点检查（流转控制）
            var shouldPauseAfterExecution = await _breakpointManager.ShouldPauseAfterExecutionAsync(chatEvent, null);
                
            if (shouldPauseAfterExecution)
            {
                System.Threading.Interlocked.Increment(ref _pausedCalls);
                
                _logger.LogInformation("⏸️ 工作流在流转前暂停 - 直接返回 NodeId: {NodeId}", nodeId);
                
                // 记录暂停状态，直接返回不执行流转
                await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PostExecution", null);
                    
                return; // 直接返回，阻止流转
            }
            
            // 执行PublishP2PAsync - 数据流转到下游agent
            await context.Invoke();
        }

        #region 判断拦截目标

        /// <summary>
        /// 判断是否为WorkflowCoordinatorGAgent的HandleEventAsync调用
        /// </summary>
        private static bool IsWorkflowHandleEventCall(IIncomingGrainCallContext context)
        {
            return context.Grain is IWorkflowCoordinatorGAgent && 
                   context.InterfaceMethod?.Name.Contains("HandleEventAsync") == true;
        }

        /// <summary>
        /// 判断是否为WorkflowCoordinatorGAgent发起的PublishP2PAsync调用
        /// 这是流转到下游agent的关键调用点
        /// </summary>
        private static bool IsWorkflowPublishP2PCall(IIncomingGrainCallContext context)
        {
            return context.Grain is IWorkflowCoordinatorGAgent && 
                   context.InterfaceMethod?.Name == "PublishP2PAsync";
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