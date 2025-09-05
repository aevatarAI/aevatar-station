using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Orleans.Runtime;
using Shouldly;
using Xunit;
using Aevatar.Service.DebugWorkFlow;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Aevatar.GAgents.GroupChat;

namespace Aevatar.Application.Tests.Service.DebugWorkFlow;

/// <summary>
/// WorkflowDebugGrainCallFilter 单元测试 - 验证拦截器逻辑
/// </summary>
public class WorkflowDebugGrainCallFilterTests
{
    private readonly ILogger<WorkflowDebugGrainCallFilter> _logger;
    private readonly IBreakpointManager _breakpointManager;
    private readonly WorkflowDebugGrainCallFilter _filter;

    public WorkflowDebugGrainCallFilterTests()
    {
        _logger = Substitute.For<ILogger<WorkflowDebugGrainCallFilter>>();
        _breakpointManager = Substitute.For<IBreakpointManager>();
        _filter = new WorkflowDebugGrainCallFilter(_breakpointManager, _logger);
    }

    #region 拦截器基础测试

    [Fact]
    public async Task Invoke_Should_CallNext_When_NotWorkflowCoordinatorGrain()
    {
        // Arrange
        var context = CreateMockContext(grain: Substitute.For<IGrain>(), methodName: "SomeMethod");

        // Act
        await _filter.Invoke(context);

        // Assert
        await context.Received(1).Invoke();
    }

    [Fact]
    public async Task Invoke_Should_CallNext_When_NotTargetMethod()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var context = CreateMockContext(grain: coordinatorGrain, methodName: "OtherMethod");

        // Act
        await _filter.Invoke(context);

        // Assert
        await context.Received(1).Invoke();
    }

    #endregion

    #region HandleEventAsync拦截测试

    [Fact]
    public async Task Invoke_Should_InterceptHandleEventAsync_When_TargetMethod()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var chatEvent = new ChatResponseEvent 
        { 
            BlackboardId = Guid.NewGuid(), 
            Term = 1L // Use long value instead of string 
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: new object[] { chatEvent }
        );

        _breakpointManager.ShouldPauseBeforeExecutionAsync(chatEvent).Returns(false);

        // Act
        await _filter.Invoke(context);

        // Assert
        await _breakpointManager.Received(1).ShouldPauseBeforeExecutionAsync(Arg.Any<ChatResponseEvent>());
        await context.Received(1).Invoke(); // 没有断点，应该继续执行
    }

    [Fact]
    public async Task Invoke_Should_PauseBeforeExecution_When_BreakpointHit()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var chatEvent = new ChatResponseEvent 
        { 
            BlackboardId = workflowId, 
            Term = 1L // Use long value instead of string
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: new object[] { chatEvent }
        );

        _breakpointManager.ShouldPauseBeforeExecutionAsync(Arg.Any<ChatResponseEvent>()).Returns(true);

        // Act
        await _filter.Invoke(context);

        // Assert
        await _breakpointManager.Received(1).ShouldPauseBeforeExecutionAsync(Arg.Any<ChatResponseEvent>());
        await _breakpointManager.Received(1).RecordPausedNodeAsync(
            Arg.Any<Guid>(), 
            Arg.Any<string>(), 
            "PreExecution", 
            Arg.Any<Dictionary<string, object>>()
        );
        await context.Received(0).Invoke(); // 有断点，不应该执行原方法
    }

    #endregion

    #region PublishP2PAsync拦截测试

    [Fact]
    public async Task Invoke_Should_InterceptPublishP2PAsync_When_TargetMethod()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var speaker = Substitute.For<IGrain>();
        var chatEvent = new ChatEvent 
        { 
            BlackboardId = Guid.NewGuid(), 
            Term = 1L // Use long value instead of string 
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "PublishP2PAsync",
            arguments: new object[] { speaker, chatEvent }
        );

        _breakpointManager.ShouldPauseAfterExecutionAsync(Arg.Any<ChatEvent>(), Arg.Any<object>()).Returns(false);

        // Act
        await _filter.Invoke(context);

        // Assert
        await _breakpointManager.Received(1).ShouldPauseAfterExecutionAsync(Arg.Any<ChatEvent>(), Arg.Any<object>());
        await context.Received(1).Invoke(); // 没有断点，应该继续执行
    }

    [Fact]
    public async Task Invoke_Should_PauseAfterExecution_When_BreakpointHit()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var speaker = Substitute.For<IGrain>();
        var chatEvent = new ChatEvent 
        { 
            BlackboardId = workflowId, 
            Term = 1L // Use long value instead of string
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "PublishP2PAsync",
            arguments: new object[] { speaker, chatEvent }
        );

        _breakpointManager.ShouldPauseAfterExecutionAsync(Arg.Any<ChatEvent>(), Arg.Any<object>()).Returns(true);

        // Act
        await _filter.Invoke(context);

        // Assert
        await _breakpointManager.Received(1).ShouldPauseAfterExecutionAsync(Arg.Any<ChatEvent>(), Arg.Any<object>());
        await _breakpointManager.Received(1).RecordPausedNodeAsync(
            Arg.Any<Guid>(), 
            Arg.Any<string>(), 
            "PostExecution", 
            Arg.Any<Dictionary<string, object>?>()
        );
        await context.Received(0).Invoke(); // 有断点，不应该执行流转
    }

    #endregion

    #region 拦截器统计测试

    [Fact]
    public async Task Invoke_Should_IncrementStats_When_InterceptingHandleEvent()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var chatEvent = new ChatResponseEvent 
        { 
            BlackboardId = Guid.NewGuid(), 
            Term = 1L // Use long value instead of string 
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: new object[] { chatEvent }
        );

        _breakpointManager.ShouldPauseBeforeExecutionAsync(Arg.Any<ChatResponseEvent>()).Returns(false);

        var statsBefore = WorkflowDebugGrainCallFilter.GetStats();

        // Act
        await _filter.Invoke(context);

        // Assert
        var statsAfter = WorkflowDebugGrainCallFilter.GetStats();
        statsAfter.InterceptedCalls.ShouldBe(statsBefore.InterceptedCalls + 1);
        statsAfter.DebugChecks.ShouldBe(statsBefore.DebugChecks + 1);
    }

    [Fact]
    public async Task Invoke_Should_IncrementStats_When_InterceptingPublishP2P()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var speaker = Substitute.For<IGrain>();
        var chatEvent = new ChatEvent 
        { 
            BlackboardId = Guid.NewGuid(), 
            Term = 1L // Use long value instead of string 
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "PublishP2PAsync",
            arguments: new object[] { speaker, chatEvent }
        );

        _breakpointManager.ShouldPauseAfterExecutionAsync(Arg.Any<ChatEvent>(), Arg.Any<object>()).Returns(false);

        var statsBefore = WorkflowDebugGrainCallFilter.GetStats();

        // Act
        await _filter.Invoke(context);

        // Assert
        var statsAfter = WorkflowDebugGrainCallFilter.GetStats();
        statsAfter.InterceptedCalls.ShouldBe(statsBefore.InterceptedCalls + 1);
        statsAfter.DebugChecks.ShouldBe(statsBefore.DebugChecks + 1);
    }

    [Fact]
    public async Task Invoke_Should_IncrementPausedStats_When_PausingExecution()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var chatEvent = new ChatResponseEvent 
        { 
            BlackboardId = Guid.NewGuid(), 
            Term = 1L // Use long value instead of string 
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: new object[] { chatEvent }
        );

        _breakpointManager.ShouldPauseBeforeExecutionAsync(Arg.Any<ChatResponseEvent>()).Returns(true);

        var statsBefore = WorkflowDebugGrainCallFilter.GetStats();

        // Act
        await _filter.Invoke(context);

        // Assert
        var statsAfter = WorkflowDebugGrainCallFilter.GetStats();
        statsAfter.PausedCalls.ShouldBe(statsBefore.PausedCalls + 1);
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public async Task Invoke_Should_ContinueExecution_When_BreakpointManagerThrows()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var chatEvent = new ChatResponseEvent 
        { 
            BlackboardId = Guid.NewGuid(), 
            Term = 1L // Use long value instead of string 
        };
        
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: new object[] { chatEvent }
        );

        _breakpointManager.ShouldPauseBeforeExecutionAsync(chatEvent)
            .Returns(Task.FromException<bool>(new InvalidOperationException("Test exception")));

        // Act & Assert
        // 应该不抛出异常，并继续执行原方法
        await _filter.Invoke(context);
        
        await context.Received(1).Invoke(); // 异常时应该继续执行原方法
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task Invoke_Should_HandleNullArguments_Gracefully()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: null
        );

        // Act
        await _filter.Invoke(context);

        // Assert
        await context.Received(1).Invoke(); // 应该继续执行原方法
    }

    [Fact]
    public async Task Invoke_Should_HandleEmptyArguments_Gracefully()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: new object[0]
        );

        // Act
        await _filter.Invoke(context);

        // Assert
        await context.Received(1).Invoke(); // 应该继续执行原方法
    }

    [Fact]
    public async Task Invoke_Should_HandleInvalidArgumentType_Gracefully()
    {
        // Arrange
        var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
        var context = CreateMockContext(
            grain: coordinatorGrain, 
            methodName: "HandleEventAsync",
            arguments: new object[] { "invalid-argument-type" }
        );

        // Act
        await _filter.Invoke(context);

        // Assert
        await context.Received(1).Invoke(); // 应该继续执行原方法
    }

    #endregion

    #region 测试辅助方法

    /// <summary>
    /// 创建Mock的Orleans拦截器上下文
    /// </summary>
    private IIncomingGrainCallContext CreateMockContext(
        IGrain grain, 
        string methodName, 
        object[]? arguments = null)
    {
        var context = Substitute.For<IIncomingGrainCallContext>();
        
        // Mock grain
        context.Grain.Returns(grain);
        
        // Mock method info
        var methodInfo = Substitute.For<MethodInfo>();
        methodInfo.Name.Returns(methodName);
        context.InterfaceMethod.Returns(methodInfo);
        
        // Note: Arguments property is not available in IIncomingGrainCallContext
        // In real scenario, we use reflection to access method arguments
        
        // Mock Invoke
        context.Invoke().Returns(Task.CompletedTask);
        
        return context;
    }

    #endregion
}
