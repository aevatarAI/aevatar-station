using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Shouldly;
using Xunit;
using Aevatar.Service.DebugWorkFlow;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Aevatar.GAgents.GroupChat;
using Microsoft.AspNetCore.Mvc;
using Aevatar.Application.Tests.Controllers;

namespace Aevatar.Application.Tests.Service.DebugWorkFlow;

/// <summary>
/// 工作流调试系统集成测试 - 验证完整调试流程
/// </summary>
public class WorkflowDebugIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBreakpointManager _breakpointManager;
    private readonly WorkflowDebugGrainCallFilter _grainCallFilter;
    private readonly TestWorkflowDebugApiController _apiController;

    public WorkflowDebugIntegrationTests()
    {
        // 设置依赖注入容器
        var services = new ServiceCollection();
        
        // 注册日志
        services.AddLogging(builder => builder.AddConsole());
        
        // 注册Mock的Orleans
        var grainFactory = Substitute.For<IGrainFactory>();
        services.AddSingleton(grainFactory);
        
        // 注册调试服务
        services.AddSingleton<IBreakpointManager, BreakpointManager>();
        services.AddTransient<WorkflowDebugGrainCallFilter>();
        services.AddTransient<TestWorkflowDebugApiController>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // 获取服务实例
        _breakpointManager = _serviceProvider.GetRequiredService<IBreakpointManager>();
        _grainCallFilter = _serviceProvider.GetRequiredService<WorkflowDebugGrainCallFilter>();
        _apiController = _serviceProvider.GetRequiredService<TestWorkflowDebugApiController>();
    }

    #region 完整调试流程测试

    [Fact]
    public async Task CompleteDebugFlow_PreExecution_Should_Work_EndToEnd()
    {
        // Arrange - 设置执行前断点
        var workflowId = Guid.NewGuid();
        var nodeId = "integration-test-node";
        
        var breakpointRequest = new SetBreakpointRequest
        {
            WorkflowId = workflowId,
            NodeId = nodeId,
            Type = BreakpointType.BeforeExecution,
            Description = "Integration test breakpoint"
        };

        // Step 1: 通过API设置断点
        var setResult = await _apiController.SetBreakpoint(breakpointRequest);
        setResult.ShouldBeOfType<OkObjectResult>();

        // Step 2: 验证断点已设置
        var getResult = await _apiController.GetBreakpoints();
        getResult.ShouldBeOfType<OkObjectResult>();
        
        var breakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
        breakpoints.ShouldNotBeEmpty();
        breakpoints.First().NodeId.ShouldBe(nodeId);

        // Step 3: 模拟工作流执行并命中断点
        var chatEvent = new ChatResponseEvent
        {
            BlackboardId = workflowId,
            Term = 1L // Use simple numeric value
        };

        // Update breakpoint to use Term as nodeId for consistency
        await _breakpointManager.RemoveBreakpointAsync(workflowId, nodeId);
        await _breakpointManager.SetBreakpointAsync(workflowId, "1", BreakpointType.BeforeExecution);

        var shouldPause = await _breakpointManager.ShouldPauseBeforeExecutionAsync(chatEvent);
        shouldPause.ShouldBeTrue();

        // Step 4: 记录暂停状态（模拟拦截器行为）
        var context = new Dictionary<string, object>
        {
            { "TestParam", "TestValue" },
            { "Timestamp", DateTime.UtcNow }
        };
        
        await _breakpointManager.RecordPausedNodeAsync(workflowId, "1", "PreExecution", context);

        // Step 5: 验证暂停状态
        var pausedNodesResult = await _apiController.GetPausedNodeInfos();
        pausedNodesResult.ShouldBeOfType<OkObjectResult>();
        
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldContain("1");

        // Step 6: 通过API继续执行（跳过）
        var skipResult = await _apiController.SkipNode("1");
        skipResult.ShouldBeOfType<OkObjectResult>();

        // Step 7: 验证暂停状态已清理
        var finalPausedNodes = await _breakpointManager.GetPausedNodesAsync();
        finalPausedNodes.ShouldNotContain("1");
    }

    [Fact]
    public async Task CompleteDebugFlow_PostExecution_Should_Work_EndToEnd()
    {
        // Arrange - 设置执行后断点
        var workflowId = Guid.NewGuid();
        var nodeId = "post-execution-test-node";
        
        // Step 1: 设置执行后断点（使用数字节点ID与Term匹配）
        await _breakpointManager.SetBreakpointAsync(workflowId, "1", BreakpointType.AfterExecution);

        // Step 2: 模拟工作流执行完成，准备流转
        var chatEvent = new ChatEvent
        {
            BlackboardId = workflowId,
            Term = 1L, // Use long value instead of string
            Speaker = Guid.NewGuid()
        };

        var shouldPause = await _breakpointManager.ShouldPauseAfterExecutionAsync(chatEvent, null);
        shouldPause.ShouldBeTrue();

        // Step 3: 记录流转前暂停状态
        var context = new Dictionary<string, object>
        {
            { "CoordinatorMessages", new List<object>() },
            { "Speaker", chatEvent.Speaker },
            { "Term", chatEvent.Term }
        };
        
        await _breakpointManager.RecordPausedNodeAsync(workflowId, "1", "PostExecution", context);

        // Step 4: 验证暂停状态
        var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
        var pausedInfo = pausedNodeInfos.FirstOrDefault(p => p.NodeId == "1");
        
        pausedInfo.ShouldNotBeNull();
        pausedInfo.Stage.ShouldBe(PauseStage.PostExecution);

        // Step 5: 通过API继续到下游
        var continueResult = await _apiController.ContinueToNextNode(workflowId, "1");
        continueResult.ShouldBeOfType<OkObjectResult>();

        // Step 6: 验证暂停状态已清理
        var finalPausedNodes = await _breakpointManager.GetPausedNodesAsync();
        finalPausedNodes.ShouldNotContain("1");
    }

    [Fact]
    public async Task CompleteDebugFlow_RetryWithModifiedParams_Should_Work_EndToEnd()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "retry-test-node";
        
        // Step 1: 设置断点并触发暂停
        await _breakpointManager.SetBreakpointAsync(workflowId, nodeId, BreakpointType.AfterExecution);
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PostExecution", new Dictionary<string, object>());

        // Step 2: 通过API重试节点（带修改参数）
        var modifiedParams = new Dictionary<string, object>
        {
            { "temperature", 0.3 },
            { "maxTokens", 1000 }
        };

        var retryRequest = new RetryNodeRequest
        {
            ModifiedParams = modifiedParams,
            Note = "Retry with optimized parameters"
        };

        var retryResult = await _apiController.RetryCurrentNode(workflowId, nodeId, retryRequest);
        retryResult.ShouldBeOfType<OkObjectResult>();

        // Step 3: 验证暂停状态已清理
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldNotContain(nodeId);
    }

    #endregion

    #region 多工作流并行调试测试

    [Fact]
    public async Task MultipleWorkflows_Should_Work_Independently()
    {
        // Arrange - 两个独立的工作流
        var workflow1Id = Guid.NewGuid();
        var workflow2Id = Guid.NewGuid();
        var nodeId = "parallel-test-node";

        // Step 1: 为两个工作流设置相同节点的断点
        await _breakpointManager.SetBreakpointAsync(workflow1Id, nodeId, BreakpointType.BeforeExecution);
        await _breakpointManager.SetBreakpointAsync(workflow2Id, nodeId, BreakpointType.AfterExecution);

        // Step 2: 两个工作流都命中断点并暂停
        await _breakpointManager.RecordPausedNodeAsync(workflow1Id, nodeId, "PreExecution", new Dictionary<string, object> { { "workflow", "1" } });
        await _breakpointManager.RecordPausedNodeAsync(workflow2Id, nodeId, "PostExecution", new Dictionary<string, object> { { "workflow", "2" } });

        // Step 3: 验证两个工作流都在暂停状态
        var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
        pausedNodeInfos.Count().ShouldBe(2);
        
        var workflow1Paused = pausedNodeInfos.FirstOrDefault(p => p.WorkflowId == workflow1Id);
        var workflow2Paused = pausedNodeInfos.FirstOrDefault(p => p.WorkflowId == workflow2Id);
        
        workflow1Paused.ShouldNotBeNull();
        workflow1Paused.Stage.ShouldBe(PauseStage.PreExecution);
        
        workflow2Paused.ShouldNotBeNull();
        workflow2Paused.Stage.ShouldBe(PauseStage.PostExecution);

        // Step 4: 继续第一个工作流
        await _apiController.SkipNode(nodeId + "-workflow1");  // 由于是相同nodeId，这里模拟不同的处理
        await _breakpointManager.SkipNodeAsync(nodeId); // 直接调用会影响第一个找到的
        
        // Step 5: 中止第二个工作流
        var abortResult = await _apiController.AbortWorkflow(new AbortWorkflowRequest { WorkflowId = workflow2Id });
        abortResult.ShouldBeOfType<OkObjectResult>();

        // Step 6: 验证第二个工作流的暂停状态已清理
        var finalPausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
        var remainingPaused = finalPausedNodeInfos.Where(p => p.WorkflowId == workflow2Id);
        remainingPaused.ShouldBeEmpty();
    }

    #endregion

    #region 批量操作测试

    [Fact]
    public async Task BatchOperations_Should_Work_Correctly()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var batchRequest = new BatchBreakpointRequest
        {
            Breakpoints = new List<SetBreakpointRequest>
            {
                new SetBreakpointRequest { WorkflowId = workflowId, NodeId = "node-1", Type = BreakpointType.BeforeExecution },
                new SetBreakpointRequest { WorkflowId = workflowId, NodeId = "node-2", Type = BreakpointType.AfterExecution },
                new SetBreakpointRequest { WorkflowId = workflowId, NodeId = "node-3", Type = BreakpointType.BeforeExecution }
            }
        };

        // Step 1: 批量设置断点
        var batchResult = await _apiController.BatchSetBreakpoints(batchRequest);
        batchResult.ShouldBeOfType<OkObjectResult>();

        // Step 2: 验证所有断点都已设置
        var breakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
        breakpoints.Count().ShouldBe(3);
        breakpoints.Select(b => b.NodeId).ShouldContain("node-1");
        breakpoints.Select(b => b.NodeId).ShouldContain("node-2");
        breakpoints.Select(b => b.NodeId).ShouldContain("node-3");

        // Step 3: 清理所有断点
        await _breakpointManager.RemoveBreakpointAsync(workflowId, "node-1");
        await _breakpointManager.RemoveBreakpointAsync(workflowId, "node-2");
        await _breakpointManager.RemoveBreakpointAsync(workflowId, "node-3");

        // Step 4: 验证断点已清理
        var finalBreakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
        finalBreakpoints.ShouldBeEmpty();
    }

    #endregion

    #region 异常处理集成测试

    [Fact]
    public async Task ErrorHandling_Should_Work_Gracefully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "error-test-node";

        // Step 1: 设置断点
        await _breakpointManager.SetBreakpointAsync(workflowId, nodeId, BreakpointType.BeforeExecution);

        // Step 2: 记录暂停状态
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PreExecution", new Dictionary<string, object>());

        // Step 3: 尝试操作不存在的节点（应该优雅处理）
        var nonExistentResult = await _apiController.ContinueToNextNode(Guid.NewGuid(), "non-existent-node");
        nonExistentResult.ShouldBeOfType<OkObjectResult>(); // 应该返回成功但记录警告日志

        // Step 4: 验证原始暂停状态未受影响
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldContain(nodeId);

        // Step 5: 正常清理
        await _breakpointManager.SkipNodeAsync(nodeId);
        var finalPausedNodes = await _breakpointManager.GetPausedNodesAsync();
        finalPausedNodes.ShouldNotContain(nodeId);
    }

    #endregion

    #region 性能验证测试

    [Fact]
    public async Task PerformanceTest_Should_Handle_Multiple_Concurrent_Operations()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeCount = 10;
        var tasks = new List<Task>();

        // Step 1: 并发设置多个断点
        for (int i = 0; i < nodeCount; i++)
        {
            var nodeId = $"perf-test-node-{i}";
            var breakpointType = i % 2 == 0 ? BreakpointType.BeforeExecution : BreakpointType.AfterExecution;
            
            tasks.Add(_breakpointManager.SetBreakpointAsync(workflowId, nodeId, breakpointType));
        }

        await Task.WhenAll(tasks);
        tasks.Clear();

        // Step 2: 验证所有断点都已设置
        var breakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
        breakpoints.Count().ShouldBe(nodeCount);

        // Step 3: 并发触发断点和暂停
        for (int i = 0; i < nodeCount; i++)
        {
            var nodeId = $"perf-test-node-{i}";
            var stage = i % 2 == 0 ? "PreExecution" : "PostExecution";
            
            tasks.Add(_breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, stage, new Dictionary<string, object> { { "index", i } }));
        }

        await Task.WhenAll(tasks);
        tasks.Clear();

        // Step 4: 验证所有节点都已暂停
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.Count().ShouldBe(nodeCount);

        // Step 5: 并发清理暂停状态
        for (int i = 0; i < nodeCount; i++)
        {
            var nodeId = $"perf-test-node-{i}";
            tasks.Add(_breakpointManager.SkipNodeAsync(nodeId));
        }

        await Task.WhenAll(tasks);

        // Step 6: 验证所有暂停状态已清理
        var finalPausedNodes = await _breakpointManager.GetPausedNodesAsync();
        finalPausedNodes.ShouldBeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.GetService<IServiceScope>()?.Dispose();
    }
}
