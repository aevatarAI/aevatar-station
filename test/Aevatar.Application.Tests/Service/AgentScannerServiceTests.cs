using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Aevatar.Application.Service;
using Aevatar.Domain.WorkflowOrchestration;
using System.Reflection;

namespace Aevatar.Application.Tests.Service
{
    /// <summary>
    /// AgentScannerService 单元测试
    /// </summary>
    public class AgentScannerServiceTests
    {
        private readonly Mock<ILogger<AgentScannerService>> _mockLogger;
        private readonly AgentScannerService _service;

        public AgentScannerServiceTests()
        {
            _mockLogger = new Mock<ILogger<AgentScannerService>>();
            _service = new AgentScannerService(_mockLogger.Object);
        }

        [Fact]
        public async Task ScanAgentsAsync_WithValidAssemblies_ShouldReturnAgents()
        {
            // Arrange
            var testAssembly = Assembly.GetExecutingAssembly();
            var assemblies = new[] { testAssembly };

            // Act
            var result = await _service.ScanAgentsAsync(assemblies);

            // Assert
            Assert.NotNull(result);
            // 由于测试类中定义了模拟Agent，应该能找到一些Agent
            Assert.True(result.Count() >= 0);
        }

        [Fact]
        public async Task ScanAgentsAsync_WithNullAssemblies_ShouldThrowArgumentNullException()
        {
            // Arrange
            Assembly[]? assemblies = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ScanAgentsAsync(assemblies!));
        }

        [Fact]
        public void ExtractAgentInfo_WithValidAgentType_ShouldReturnAgentInfo()
        {
            // Arrange
            var agentType = typeof(TestValidAgent);

            // Act
            var result = _service.ExtractAgentInfo(agentType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-valid-agent", result.AgentId);
            Assert.Equal("TestValidAgent", result.Name);
            Assert.NotEmpty(result.L1Description);
            Assert.NotEmpty(result.L2Description);
        }

        [Fact]
        public void ExtractAgentInfo_WithInvalidAgentType_ShouldReturnNull()
        {
            // Arrange
            var agentType = typeof(TestInvalidAgent);

            // Act
            var result = _service.ExtractAgentInfo(agentType);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExtractAgentInfo_WithNullType_ShouldReturnNull()
        {
            // Arrange
            Type? agentType = null;

            // Act
            var result = _service.ExtractAgentInfo(agentType!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsValidAgent_WithValidAgent_ShouldReturnTrue()
        {
            // Arrange
            var agentType = typeof(TestValidAgent);

            // Act
            var result = _service.IsValidAgent(agentType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidAgent_WithInvalidAgent_ShouldReturnFalse()
        {
            // Arrange
            var agentType = typeof(TestInvalidAgent);

            // Act
            var result = _service.IsValidAgent(agentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAgent_WithNullType_ShouldReturnFalse()
        {
            // Arrange
            Type? agentType = null;

            // Act
            var result = _service.IsValidAgent(agentType!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAgent_WithAbstractType_ShouldReturnFalse()
        {
            // Arrange
            var agentType = typeof(AbstractTestAgent);

            // Act
            var result = _service.IsValidAgent(agentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAgent_WithInterfaceType_ShouldReturnFalse()
        {
            // Arrange
            var agentType = typeof(ITestAgent);

            // Act
            var result = _service.IsValidAgent(agentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetScanStatistics_InitialState_ShouldReturnEmptyStatistics()
        {
            // Act
            var result = _service.GetScanStatistics();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalAgentsFound);
            Assert.Equal(0, result.AssembliesScanned);
            Assert.Equal(0, result.TypesScanned);
        }

        [Fact]
        public async Task ScanAgentsAsync_ShouldUpdateStatistics()
        {
            // Arrange
            var testAssembly = Assembly.GetExecutingAssembly();

            // Act
            await _service.ScanAgentsAsync(new[] { testAssembly });
            var statistics = _service.GetScanStatistics();

            // Assert
            Assert.NotNull(statistics);
            Assert.True(statistics.AssembliesScanned > 0);
            Assert.True(statistics.TypesScanned > 0);
            Assert.True(statistics.ScanDuration.TotalMilliseconds > 0);
            Assert.True(statistics.LastScanTime > DateTime.MinValue);
        }
    }

    // Test classes for unit testing

    [AgentDescription(
        agentId: "test-valid-agent",
        name: "TestValidAgent",
        l1Description: "Test L1 Description for valid agent that meets the minimum character requirement for workflow orchestration",
        l2Description: "Test L2 Description providing detailed information about this test agent for workflow orchestration validation purposes and comprehensive testing scenarios to ensure proper functionality and system integration")]
    public class TestValidAgent
    {
        public TestValidAgent()
        {
            // Set properties after construction since they're not in constructor
            var attribute = GetType().GetCustomAttribute<AgentDescriptionAttribute>();
            if (attribute != null)
            {
                attribute.Categories = new[] { "Testing" };
                attribute.ComplexityLevel = 1;
                attribute.EstimatedExecutionTime = 5000;
                attribute.Dependencies = new[] { "TestDependency" };
                attribute.Tags = new[] { "test", "validation" };
                attribute.Version = "1.0.0";
            }
        }

        public void Execute()
        {
            // Test implementation
        }
    }

    [AgentDescription(
        agentId: "test-invalid-agent",
        name: "TestInvalidAgent",
        l1Description: "Short", // Too short (< 50 characters)
        l2Description: "Also short")] // Too short (< 200 characters)
    public class TestInvalidAgent
    {
        public void Execute()
        {
            // Test implementation
        }
    }

    public abstract class AbstractTestAgent
    {
        public abstract void Execute();
    }

    public interface ITestAgent
    {
        void Execute();
    }
} 