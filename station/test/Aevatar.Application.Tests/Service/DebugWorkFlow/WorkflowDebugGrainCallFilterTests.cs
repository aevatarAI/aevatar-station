using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Orleans.Runtime;
using Shouldly;
using Xunit;
using Aevatar.Service.DebugWorkFlow;
using Aevatar.GAgents.GroupChat.Core;

namespace Aevatar.Application.Tests.Service.DebugWorkFlow
{
    public class WorkflowDebugGrainCallFilterTests
    {
        private readonly IBreakpointManager _breakpointManager;
        private readonly ILogger<WorkflowDebugGrainCallFilter> _logger;
        private readonly WorkflowDebugGrainCallFilter _filter;

        public WorkflowDebugGrainCallFilterTests()
        {
            _breakpointManager = Substitute.For<IBreakpointManager>();
            _logger = Substitute.For<ILogger<WorkflowDebugGrainCallFilter>>();
            _filter = new WorkflowDebugGrainCallFilter(_breakpointManager, _logger);
        }

        private IIncomingGrainCallContext CreateMockContext(
            object grain, 
            string methodName)
        {
            var context = Substitute.For<IIncomingGrainCallContext>();
            var method = Substitute.For<System.Reflection.MethodInfo>();
            
            method.Name.Returns(methodName);
            context.Grain.Returns(grain);
            context.InterfaceMethod.Returns(method);
            
            return context;
        }

        [Fact]
        public async Task Invoke_Should_PassThrough_When_NotWorkflowCoordinatorGAgent()
        {
            // Arrange
            var otherGrain = Substitute.For<Orleans.IGrain>();
            var context = CreateMockContext(otherGrain, "SomeOtherMethod");

            // Act
            await _filter.Invoke(context);

            // Assert
            await context.Received(1).Invoke();
        }

        [Fact]
        public async Task Invoke_Should_IncrementStats_When_ProcessingCalls()
        {
            // Arrange
            var otherGrain = Substitute.For<Orleans.IGrain>();
            var context = CreateMockContext(otherGrain, "SomeMethod");

            var statsBefore = WorkflowDebugGrainCallFilter.GetStats();

            // Act
            await _filter.Invoke(context);

            // Assert
            var statsAfter = WorkflowDebugGrainCallFilter.GetStats();
            statsAfter.InterceptedCalls.ShouldBeGreaterThan(statsBefore.InterceptedCalls);
        }

        [Fact]
        public async Task Invoke_Should_PassThrough_OtherWorkflowMethods()
        {
            // Arrange
            var coordinatorGrain = Substitute.For<IWorkflowCoordinatorGAgent>();
            var context = CreateMockContext(coordinatorGrain, "GetStateAsync");

            // Act
            await _filter.Invoke(context);

            // Assert
            await context.Received(1).Invoke(); // Other methods should pass through
        }

        [Fact]
        public void GetStats_Should_ReturnDebugStats()
        {
            // Act
            var stats = WorkflowDebugGrainCallFilter.GetStats();

            // Assert
            stats.ShouldNotBeNull();
            stats.InterceptedCalls.ShouldBeGreaterThanOrEqualTo(0);
            stats.DebugChecks.ShouldBeGreaterThanOrEqualTo(0);
            stats.PausedCalls.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task Invoke_Should_HandleException_Gracefully()
        {
            // Arrange
            var otherGrain = Substitute.For<Orleans.IGrain>();
            var context = CreateMockContext(otherGrain, "SomeMethod");
            
            var callCount = 0;
            // Make the context throw an exception on first call but succeed on second
            context.When(x => x.Invoke()).Do(x => 
            {
                if (callCount++ == 0)
                    throw new InvalidOperationException("Test exception");
            });

            // Act - should handle exception gracefully
            await _filter.Invoke(context);

            // Assert - The filter should have called context.Invoke() twice:
            // 1. First call throws exception  
            // 2. Second call in catch block succeeds
            await context.Received(2).Invoke();
        }
    }
}
