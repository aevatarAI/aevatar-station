using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Shouldly;
using Xunit;
using Aevatar.Service.DebugWorkFlow;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Aevatar.GAgents.GroupChat;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using GroupChat.GAgent.Feature.Common;

namespace Aevatar.Application.Tests.Service.DebugWorkFlow;

/// <summary>
/// BreakpointManager 单元测试 - 验证即时控制策略
/// </summary>
public class BreakpointManagerTests
{
    private readonly ILogger<BreakpointManager> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly BreakpointManager _breakpointManager;

    public BreakpointManagerTests()
    {
        _logger = Substitute.For<ILogger<BreakpointManager>>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _breakpointManager = new BreakpointManager(_logger, _grainFactory);
    }

    #region 断点管理测试

    [Fact]
    public async Task SetBreakpoint_Should_AddBreakpoint_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        var breakpointType = BreakpointType.AfterExecution;

        // Act
        await _breakpointManager.SetBreakpointAsync(workflowId, nodeId, breakpointType);

        // Assert
        var activeBreakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
        activeBreakpoints.ShouldNotBeEmpty();
        
        var breakpoint = activeBreakpoints.FirstOrDefault();
        breakpoint.ShouldNotBeNull();
        breakpoint.NodeId.ShouldBe(nodeId);
        breakpoint.Type.ShouldBe(breakpointType);
    }

    [Fact]
    public async Task RemoveBreakpoint_Should_RemoveBreakpoint_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        
        await _breakpointManager.SetBreakpointAsync(workflowId, nodeId, BreakpointType.BeforeExecution);

        // Act
        await _breakpointManager.RemoveBreakpointAsync(workflowId, nodeId);

        // Assert
        var activeBreakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
        activeBreakpoints.ShouldBeEmpty();
    }

    #endregion

    #region 断点检查测试

    [Fact]
    public async Task ShouldPauseBeforeExecution_Should_ReturnTrue_When_BreakpointExists()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "1"; // Match the Term value that will be converted to string
        
        await _breakpointManager.SetBreakpointAsync(workflowId, nodeId, BreakpointType.BeforeExecution);
        
        var chatEvent = new ChatResponseEvent
        {
            BlackboardId = workflowId,
            Term = 1L // Use long value instead of string
        };

        // Act
        var shouldPause = await _breakpointManager.ShouldPauseBeforeExecutionAsync(chatEvent);

        // Assert
        shouldPause.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldPauseAfterExecution_Should_ReturnTrue_When_BreakpointExists()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "1"; // Match the Term value that will be converted to string
        
        await _breakpointManager.SetBreakpointAsync(workflowId, nodeId, BreakpointType.AfterExecution);
        
        var chatEvent = new ChatEvent
        {
            BlackboardId = workflowId,
            Term = 1L // Use long value instead of string
        };

        // Act
        var shouldPause = await _breakpointManager.ShouldPauseAfterExecutionAsync(chatEvent, null);

        // Assert
        shouldPause.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldPauseBeforeExecution_Should_ReturnFalse_When_NoBreakpoint()
    {
        // Arrange
        var chatEvent = new ChatResponseEvent
        {
            BlackboardId = Guid.NewGuid(),
            Term = 2L // Use long value instead of string
        };

        // Act
        var shouldPause = await _breakpointManager.ShouldPauseBeforeExecutionAsync(chatEvent);

        // Assert
        shouldPause.ShouldBeFalse();
    }

    #endregion

    #region 即时控制测试

    [Fact]
    public async Task RecordPausedNode_Should_StorePausedInfo_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        var stage = "PreExecution";
        var context = new Dictionary<string, object> { { "TestParam", "TestValue" } };

        // Act
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, stage, context);

        // Assert
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldContain(nodeId);
        
        var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
        var pausedInfo = pausedNodeInfos.FirstOrDefault(p => p.NodeId == nodeId);
        
        pausedInfo.ShouldNotBeNull();
        pausedInfo.WorkflowId.ShouldBe(workflowId);
        pausedInfo.Stage.ShouldBe(PauseStage.PreExecution);
        pausedInfo.Context.ShouldNotBeNull();
        pausedInfo.Context["TestParam"].ShouldBe("TestValue");
    }

    [Fact]
    public async Task ContinueToNextNode_Should_ClearPausedState_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        
        // 先记录暂停状态
        var context = new Dictionary<string, object>
        {
            { "CoordinatorMessages", new List<ChatMessage>() },
            { "Speaker", Guid.NewGuid() },
            { "Term", nodeId }
        };
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PostExecution", context);

        // Mock WorkflowCoordinatorGAgent with proper state
        var coordinatorGAgent = Substitute.For<IWorkflowCoordinatorGAgent>();
        var coordinatorState = new WorkflowCoordinatorState
        {
            TermToWorkUnitGrainId = new Dictionary<long, string> { { 1L, nodeId } }
        };
        coordinatorGAgent.GetStateAsync().Returns(coordinatorState);
        _grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId).Returns(coordinatorGAgent);

        // Act
        await _breakpointManager.ContinueToNextNodeAsync(workflowId, nodeId);

        // Assert
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldNotContain(nodeId);
    }

    [Fact]
    public async Task RetryCurrentNode_Should_ClearPausedState_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        var modifiedParams = new Dictionary<string, object> { { "temperature", 0.3 } };
        
        // 先记录暂停状态
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PreExecution", new Dictionary<string, object>());

        // Mock WorkflowCoordinatorGAgent with proper state
        var coordinatorGAgent = Substitute.For<IWorkflowCoordinatorGAgent>();
        var coordinatorState = new WorkflowCoordinatorState
        {
            TermToWorkUnitGrainId = new Dictionary<long, string> { { 1L, nodeId } }
        };
        coordinatorGAgent.GetStateAsync().Returns(coordinatorState);
        _grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId).Returns(coordinatorGAgent);

        // Act
        var result = await _breakpointManager.RetryNodeAsync(workflowId, nodeId, new List<ChatMessage>());

        // Assert
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldNotContain(nodeId);
    }

    [Fact]
    public async Task SkipNode_Should_ClearPausedState_Successfully()
    {
        // Arrange
        var nodeId = "test-node";
        var workflowId = Guid.NewGuid();
        
        // 先记录暂停状态
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PreExecution", new Dictionary<string, object>());

        // Act
        await _breakpointManager.SkipNodeAsync(nodeId);

        // Assert
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldNotContain(nodeId);
    }

    [Fact]
    public async Task AbortWorkflow_Should_ClearAllRelatedPausedNodes_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var node1 = "node-1";
        var node2 = "node-2";
        var otherWorkflowId = Guid.NewGuid();
        var node3 = "node-3";
        
        // 记录多个暂停状态
        await _breakpointManager.RecordPausedNodeAsync(workflowId, node1, "PreExecution", new Dictionary<string, object>());
        await _breakpointManager.RecordPausedNodeAsync(workflowId, node2, "PostExecution", new Dictionary<string, object>());
        await _breakpointManager.RecordPausedNodeAsync(otherWorkflowId, node3, "PreExecution", new Dictionary<string, object>());

        // Act
        await _breakpointManager.AbortWorkflowAsync(workflowId);

        // Assert
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
        pausedNodes.ShouldNotContain(node1);
        pausedNodes.ShouldNotContain(node2);
        pausedNodes.ShouldContain(node3); // 其他工作流的节点应该保留
    }

    #endregion

    #region 状态查询测试

    [Fact]
    public async Task GetPausedNodeInfos_Should_ReturnCorrectInfo_Successfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        var context = new Dictionary<string, object> { { "param1", "value1" } };
        
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, "PostExecution", context);

        // Act
        var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();

        // Assert
        pausedNodeInfos.ShouldNotBeEmpty();
        
        var info = pausedNodeInfos.FirstOrDefault();
        info.ShouldNotBeNull();
        info.WorkflowId.ShouldBe(workflowId);
        info.NodeId.ShouldBe(nodeId);
        info.Stage.ShouldBe(PauseStage.PostExecution);
        info.Context.ShouldNotBeNull();
        info.Context["param1"].ShouldBe("value1");
        info.PausedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetPausedNodes_Should_ReturnDistinctNodeIds_Successfully()
    {
        // Arrange
        var workflowId1 = Guid.NewGuid();
        var workflowId2 = Guid.NewGuid();
        var nodeId = "same-node";
        
        // 同一个节点在不同工作流中暂停
        await _breakpointManager.RecordPausedNodeAsync(workflowId1, nodeId, "PreExecution", new Dictionary<string, object>());
        await _breakpointManager.RecordPausedNodeAsync(workflowId2, nodeId, "PostExecution", new Dictionary<string, object>());

        // Act
        var pausedNodes = await _breakpointManager.GetPausedNodesAsync();

        // Assert
        pausedNodes.Count().ShouldBe(1); // 应该返回去重后的节点ID
        pausedNodes.ShouldContain(nodeId);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task ContinueToNextNode_Should_LogWarning_When_NoPausedNodeFound()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "non-existent-node";

        // Act
        await _breakpointManager.ContinueToNextNodeAsync(workflowId, nodeId);

        // Assert
        // 验证日志记录了警告（通过Mock验证）
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("No paused node found")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [Fact]
    public async Task RetryCurrentNode_Should_LogWarning_When_NoPausedNodeFound()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "non-existent-node";

        // Act
        var result = await _breakpointManager.RetryNodeAsync(workflowId, nodeId, new List<ChatMessage>());

        // Assert
        // 验证日志记录了警告
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("No state or TermToWorkUnitGrainId found")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    [Theory]
    [InlineData("PreExecution", PauseStage.PreExecution)]
    [InlineData("PostExecution", PauseStage.PostExecution)]
    [InlineData("Unknown", PauseStage.PostExecution)] // 默认值
    public async Task RecordPausedNode_Should_SetCorrectStage_For_DifferentStageStrings(string stageString, PauseStage expectedStage)
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";

        // Act
        await _breakpointManager.RecordPausedNodeAsync(workflowId, nodeId, stageString, new Dictionary<string, object>());

        // Assert
        var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
        var info = pausedNodeInfos.FirstOrDefault();
        
        info.ShouldNotBeNull();
        info.Stage.ShouldBe(expectedStage);
    }

    #endregion
}
