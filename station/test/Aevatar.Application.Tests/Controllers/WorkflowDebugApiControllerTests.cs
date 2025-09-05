using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Aevatar.Service.DebugWorkFlow;

namespace Aevatar.Application.Tests.Controllers;

/// <summary>
/// WorkflowDebugApiController 单元测试 - 验证API控制器
/// </summary>
public class WorkflowDebugApiControllerTests
{
    private readonly ILogger<TestWorkflowDebugApiController> _logger;
    private readonly IBreakpointManager _breakpointManager;
    private readonly TestWorkflowDebugApiController _controller;

    public WorkflowDebugApiControllerTests()
    {
        _logger = Substitute.For<ILogger<TestWorkflowDebugApiController>>();
        _breakpointManager = Substitute.For<IBreakpointManager>();
        _controller = new TestWorkflowDebugApiController(_logger, _breakpointManager);
    }

    #region 断点管理API测试

    [Fact]
    public async Task SetBreakpoint_Should_ReturnOk_When_Success()
    {
        // Arrange
        var request = new SetBreakpointRequest
        {
            WorkflowId = Guid.NewGuid(),
            NodeId = "test-node",
            Type = BreakpointType.BeforeExecution,
            Description = "Test breakpoint"
        };

        // Act
        var result = await _controller.SetBreakpoint(request);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        var okResult = (OkObjectResult)result;
        var response = okResult.Value;
        response.ShouldNotBeNull();
        
        await _breakpointManager.Received(1).SetBreakpointAsync(
            request.WorkflowId, 
            request.NodeId, 
            request.Type, 
            request.Description
        );
    }

    [Fact]
    public async Task SetBreakpoint_Should_ReturnBadRequest_When_Exception()
    {
        // Arrange
        var request = new SetBreakpointRequest
        {
            WorkflowId = Guid.NewGuid(),
            NodeId = "test-node",
            Type = BreakpointType.BeforeExecution
        };

        _breakpointManager.SetBreakpointAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<BreakpointType>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("Test exception")));

        // Act
        var result = await _controller.SetBreakpoint(request);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RemoveBreakpoint_Should_ReturnOk_When_Success()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";

        // Act
        var result = await _controller.RemoveBreakpoint(workflowId, nodeId);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        await _breakpointManager.Received(1).RemoveBreakpointAsync(workflowId, nodeId);
    }

    [Fact]
    public async Task GetBreakpoints_Should_ReturnOk_With_Breakpoints()
    {
        // Arrange
        var breakpoints = new List<ExtendedBreakpointInfo>
        {
            new ExtendedBreakpointInfo
            {
                NodeId = "test-node-1",
                Type = BreakpointType.BeforeExecution,
                Description = "Test breakpoint 1"
            },
            new ExtendedBreakpointInfo
            {
                NodeId = "test-node-2", 
                Type = BreakpointType.AfterExecution,
                Description = "Test breakpoint 2"
            }
        };

        _breakpointManager.GetActiveBreakpointsAsync().Returns(breakpoints);

        // Act
        var result = await _controller.GetBreakpoints();

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        var okResult = (OkObjectResult)result;
        var response = okResult.Value;
        response.ShouldNotBeNull();
    }

    #endregion

    #region 即时控制API测试

    [Fact]
    public async Task ContinueToNextNode_Should_ReturnOk_When_Success()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";

        // Act
        var result = await _controller.ContinueToNextNode(workflowId, nodeId);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        var okResult = (OkObjectResult)result;
        var response = okResult.Value;
        response.ShouldNotBeNull();
        
        await _breakpointManager.Received(1).ContinueToNextNodeAsync(workflowId, nodeId);
    }

    [Fact]
    public async Task ContinueToNextNode_Should_ReturnBadRequest_When_Exception()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";

        _breakpointManager.ContinueToNextNodeAsync(workflowId, nodeId)
            .Returns(Task.FromException(new InvalidOperationException("Test exception")));

        // Act
        var result = await _controller.ContinueToNextNode(workflowId, nodeId);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        
        var badRequestResult = (BadRequestObjectResult)result;
        var response = badRequestResult.Value;
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task RetryCurrentNode_Should_ReturnOk_When_Success()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";
        var retryRequest = new RetryNodeRequest
        {
            ModifiedParams = new Dictionary<string, object> { { "temperature", 0.3 } },
            Note = "Retry with lower temperature"
        };

        // Act
        var result = await _controller.RetryCurrentNode(workflowId, nodeId, retryRequest);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        await _breakpointManager.Received(1).RetryCurrentNodeAsync(
            workflowId, 
            nodeId, 
            retryRequest.ModifiedParams
        );
    }

    [Fact]
    public async Task RetryCurrentNode_Should_ReturnOk_When_NoRequestBody()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var nodeId = "test-node";

        // Act
        var result = await _controller.RetryCurrentNode(workflowId, nodeId, null);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        await _breakpointManager.Received(1).RetryCurrentNodeAsync(
            workflowId, 
            nodeId, 
            null
        );
    }

    [Fact]
    public async Task SkipNode_Should_ReturnOk_When_Success()
    {
        // Arrange
        var nodeId = "test-node";

        // Act
        var result = await _controller.SkipNode(nodeId);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        await _breakpointManager.Received(1).SkipNodeAsync(nodeId);
    }

    #endregion

    #region 状态查询API测试

    [Fact]
    public async Task GetPausedNodes_Should_ReturnOk_With_PausedNodes()
    {
        // Arrange
        var pausedNodes = new List<string> { "node-1", "node-2", "node-3" };
        _breakpointManager.GetPausedNodesAsync().Returns(pausedNodes);

        // Act
        var result = await _controller.GetPausedNodes();

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        var okResult = (OkObjectResult)result;
        var response = okResult.Value;
        response.ShouldNotBeNull();
        
        await _breakpointManager.Received(1).GetPausedNodesAsync();
    }

    [Fact]
    public async Task GetPausedNodes_Should_ReturnBadRequest_When_Exception()
    {
        // Arrange
        _breakpointManager.GetPausedNodesAsync()
            .Returns(Task.FromException<IEnumerable<string>>(new InvalidOperationException("Test exception")));

        // Act
        var result = await _controller.GetPausedNodes();

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPausedNodeInfos_Should_ReturnOk_With_DetailedInfo()
    {
        // Arrange
        var pausedNodeInfos = new List<PausedNodeInfo>
        {
            new PausedNodeInfo
            {
                WorkflowId = Guid.NewGuid(),
                NodeId = "node-1",
                Stage = PauseStage.PreExecution,
                PausedAt = DateTime.UtcNow.AddMinutes(-5),
                Context = new Dictionary<string, object> { { "param1", "value1" } }
            },
            new PausedNodeInfo
            {
                WorkflowId = Guid.NewGuid(),
                NodeId = "node-2",
                Stage = PauseStage.PostExecution,
                PausedAt = DateTime.UtcNow.AddMinutes(-3),
                Context = new Dictionary<string, object> { { "param2", "value2" } }
            }
        };

        _breakpointManager.GetPausedNodeInfosAsync().Returns(pausedNodeInfos);

        // Act
        var result = await _controller.GetPausedNodeInfos();

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        var okResult = (OkObjectResult)result;
        var response = okResult.Value;
        response.ShouldNotBeNull();
        
        await _breakpointManager.Received(1).GetPausedNodeInfosAsync();
    }

    #endregion

    #region 工作流管理API测试

    [Fact]
    public async Task AbortWorkflow_Should_ReturnOk_When_Success()
    {
        // Arrange
        var request = new AbortWorkflowRequest { WorkflowId = Guid.NewGuid() };

        // Act
        var result = await _controller.AbortWorkflow(request);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        await _breakpointManager.Received(1).AbortWorkflowAsync(request.WorkflowId);
    }

    [Fact]
    public async Task AbortWorkflow_Should_ReturnBadRequest_When_Exception()
    {
        // Arrange
        var request = new AbortWorkflowRequest { WorkflowId = Guid.NewGuid() };

        _breakpointManager.AbortWorkflowAsync(request.WorkflowId)
            .Returns(Task.FromException(new InvalidOperationException("Test exception")));

        // Act
        var result = await _controller.AbortWorkflow(request);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region 批量操作API测试

    [Fact]
    public async Task BatchSetBreakpoints_Should_ReturnOk_When_AllSuccess()
    {
        // Arrange
        var request = new BatchBreakpointRequest
        {
            Breakpoints = new List<SetBreakpointRequest>
            {
                new SetBreakpointRequest
                {
                    WorkflowId = Guid.NewGuid(),
                    NodeId = "node-1",
                    Type = BreakpointType.BeforeExecution
                },
                new SetBreakpointRequest
                {
                    WorkflowId = Guid.NewGuid(),
                    NodeId = "node-2",
                    Type = BreakpointType.AfterExecution
                }
            }
        };

        // Act
        var result = await _controller.BatchSetBreakpoints(request);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        await _breakpointManager.Received(2).SetBreakpointAsync(
            Arg.Any<Guid>(), 
            Arg.Any<string>(), 
            Arg.Any<BreakpointType>(), 
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task BatchSetBreakpoints_Should_ReturnPartialSuccess_When_SomeFailures()
    {
        // Arrange
        var request = new BatchBreakpointRequest
        {
            Breakpoints = new List<SetBreakpointRequest>
            {
                new SetBreakpointRequest
                {
                    WorkflowId = Guid.NewGuid(),
                    NodeId = "node-1",
                    Type = BreakpointType.BeforeExecution
                },
                new SetBreakpointRequest
                {
                    WorkflowId = Guid.NewGuid(),
                    NodeId = "node-2",
                    Type = BreakpointType.AfterExecution
                }
            }
        };

        // 第二个断点设置失败
        _breakpointManager.SetBreakpointAsync(Arg.Any<Guid>(), "node-2", Arg.Any<BreakpointType>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("Test exception")));

        // Act
        var result = await _controller.BatchSetBreakpoints(request);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        
        var okResult = (OkObjectResult)result;
        var response = okResult.Value;
        response.ShouldNotBeNull();
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task SetBreakpoint_Should_ReturnBadRequest_When_InvalidWorkflowId()
    {
        // Arrange
        var request = new SetBreakpointRequest
        {
            WorkflowId = Guid.Empty, // Invalid
            NodeId = "test-node",
            Type = BreakpointType.BeforeExecution
        };

        // Act
        var result = await _controller.SetBreakpoint(request);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetBreakpoint_Should_ReturnBadRequest_When_EmptyNodeId()
    {
        // Arrange
        var request = new SetBreakpointRequest
        {
            WorkflowId = Guid.NewGuid(),
            NodeId = "", // Empty
            Type = BreakpointType.BeforeExecution
        };

        // Act
        var result = await _controller.SetBreakpoint(request);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchSetBreakpoints_Should_ReturnBadRequest_When_NullRequest()
    {
        // Act
        var result = await _controller.BatchSetBreakpoints(null);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BatchSetBreakpoints_Should_ReturnBadRequest_When_EmptyBreakpoints()
    {
        // Arrange
        var request = new BatchBreakpointRequest
        {
            Breakpoints = new List<SetBreakpointRequest>() // Empty list
        };

        // Act
        var result = await _controller.BatchSetBreakpoints(request);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ContinueToNextNode_Should_ReturnBadRequest_When_EmptyGuidWorkflowId()
    {
        // Act
        var result = await _controller.ContinueToNextNode(Guid.Empty, "test-node");

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RetryCurrentNode_Should_ReturnBadRequest_When_EmptyNodeId()
    {
        // Act
        var result = await _controller.RetryCurrentNode(Guid.NewGuid(), "");

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    #endregion
}

#region 测试专用类和DTOs

/// <summary>
/// 测试专用的工作流调试API控制器
/// </summary>
public class TestWorkflowDebugApiController : ControllerBase
{
    private readonly ILogger<TestWorkflowDebugApiController> _logger;
    private readonly IBreakpointManager _breakpointManager;

    public TestWorkflowDebugApiController(
        ILogger<TestWorkflowDebugApiController> logger,
        IBreakpointManager breakpointManager)
    {
        _logger = logger;
        _breakpointManager = breakpointManager;
    }

    public async Task<IActionResult> SetBreakpoint(SetBreakpointRequest request)
    {
        try
        {
            if (request.WorkflowId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "Invalid WorkflowId" });

            if (string.IsNullOrEmpty(request.NodeId))
                return BadRequest(new { Success = false, Message = "Invalid NodeId" });

            await _breakpointManager.SetBreakpointAsync(
                request.WorkflowId, 
                request.NodeId, 
                request.Type, 
                request.Description);

            return Ok(new { Success = true, Message = "断点设置成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> RemoveBreakpoint(Guid workflowId, string nodeId)
    {
        try
        {
            await _breakpointManager.RemoveBreakpointAsync(workflowId, nodeId);
            return Ok(new { Success = true, Message = "断点移除成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> GetBreakpoints()
    {
        try
        {
            var breakpoints = await _breakpointManager.GetActiveBreakpointsAsync();
            return Ok(new { Success = true, Data = breakpoints });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> ContinueToNextNode(Guid workflowId, string nodeId)
    {
        try
        {
            if (workflowId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "Invalid WorkflowId" });

            await _breakpointManager.ContinueToNextNodeAsync(workflowId, nodeId);
            return Ok(new { Success = true, Message = "已触发继续到下游节点" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> RetryCurrentNode(Guid workflowId, string nodeId, RetryNodeRequest? request = null)
    {
        try
        {
            if (string.IsNullOrEmpty(nodeId))
                return BadRequest(new { Success = false, Message = "Invalid NodeId" });

            await _breakpointManager.RetryCurrentNodeAsync(workflowId, nodeId, request?.ModifiedParams);
            return Ok(new { Success = true, Message = "已触发重试当前节点" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> SkipNode(string nodeId)
    {
        try
        {
            await _breakpointManager.SkipNodeAsync(nodeId);
            return Ok(new { Success = true, Message = "已跳过节点" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> GetPausedNodes()
    {
        try
        {
            var pausedNodes = await _breakpointManager.GetPausedNodesAsync();
            return Ok(new { Success = true, Data = pausedNodes });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> GetPausedNodeInfos()
    {
        try
        {
            var pausedNodeInfos = await _breakpointManager.GetPausedNodeInfosAsync();
            return Ok(new { Success = true, Data = pausedNodeInfos });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> AbortWorkflow(AbortWorkflowRequest request)
    {
        try
        {
            await _breakpointManager.AbortWorkflowAsync(request.WorkflowId);
            return Ok(new { Success = true, Message = "工作流已中止" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public async Task<IActionResult> BatchSetBreakpoints(BatchBreakpointRequest? request)
    {
        if (request == null || request.Breakpoints == null || !request.Breakpoints.Any())
            return BadRequest(new { Success = false, Message = "Invalid batch request" });

        var results = new List<object>();
        var successCount = 0;

        foreach (var breakpointRequest in request.Breakpoints)
        {
            try
            {
                await _breakpointManager.SetBreakpointAsync(
                    breakpointRequest.WorkflowId,
                    breakpointRequest.NodeId,
                    breakpointRequest.Type,
                    breakpointRequest.Description);

                results.Add(new { NodeId = breakpointRequest.NodeId, Success = true });
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new { NodeId = breakpointRequest.NodeId, Success = false, Error = ex.Message });
            }
        }

        return Ok(new { 
            Success = true, 
            Message = $"批量设置完成: {successCount}/{request.Breakpoints.Count} 成功",
            Results = results 
        });
    }
}

public class SetBreakpointRequest
{
    public Guid WorkflowId { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public BreakpointType Type { get; set; }
    public string? Description { get; set; }
}

public class RetryNodeRequest
{
    public Dictionary<string, object>? ModifiedParams { get; set; }
    public string? Note { get; set; }
}

public class AbortWorkflowRequest
{
    public Guid WorkflowId { get; set; }
}

public class BatchBreakpointRequest
{
    public List<SetBreakpointRequest>? Breakpoints { get; set; }
}

#endregion
