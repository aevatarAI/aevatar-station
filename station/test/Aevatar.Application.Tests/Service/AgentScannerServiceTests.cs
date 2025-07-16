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

namespace Aevatar.Application.Tests.Service
{
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
        public async Task ScanAgentsAsync_WithValidAssembly_ShouldReturnAgents()
        {
            // Arrange
            var testAssembly = Assembly.GetExecutingAssembly();

            // Act
            var result = await _service.ScanAgentsAsync(new[] { testAssembly });

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<AgentIndexInfo>>(result);
        }

        [Fact]
        public async Task ScanAgentsAsync_WithNullAssemblies_ShouldUseDefaultAssemblies()
        {
            // Act
            var result = await _service.ScanAgentsAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<AgentIndexInfo>>(result);
        }

        [Fact]
        public void ExtractAgentInfo_WithValidAgentType_ShouldReturnAgentIndexInfo()
        {
            // Arrange
            var agentType = typeof(TestValidAgent);

            // Act
            var result = _service.ExtractAgentInfo(agentType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestValidAgent", result.Name);
            Assert.Equal("Test L1 Description for valid agent", result.L1Description);
            Assert.Equal("Test L2 Description providing detailed information about this test agent for workflow orchestration validation purposes", result.L2Description);
            Assert.Equal("Testing", result.Category);
            Assert.Equal(WorkflowComplexity.Simple, result.ComplexityLevel);
            Assert.True(result.IsActive);
        }

        [Fact]
        public void ExtractAgentInfo_WithNullType_ShouldReturnNull()
        {
            // Act
            var result = _service.ExtractAgentInfo(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExtractAgentInfo_WithTypeWithoutAttribute_ShouldReturnNull()
        {
            // Arrange
            var typeWithoutAttribute = typeof(TestClassWithoutAttribute);

            // Act
            var result = _service.ExtractAgentInfo(typeWithoutAttribute);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExtractAgentInfo_WithInvalidAttribute_ShouldReturnNull()
        {
            // Arrange
            var typeWithInvalidAttribute = typeof(TestInvalidAgent);

            // Act
            var result = _service.ExtractAgentInfo(typeWithInvalidAttribute);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsValidAgent_WithValidAgentType_ShouldReturnTrue()
        {
            // Arrange
            var validAgentType = typeof(TestValidAgent);

            // Act
            var result = _service.IsValidAgent(validAgentType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidAgent_WithNullType_ShouldReturnFalse()
        {
            // Act
            var result = _service.IsValidAgent(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAgent_WithAbstractType_ShouldReturnFalse()
        {
            // Arrange
            var abstractType = typeof(TestAbstractAgent);

            // Act
            var result = _service.IsValidAgent(abstractType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAgent_WithInterfaceType_ShouldReturnFalse()
        {
            // Arrange
            var interfaceType = typeof(ITestInterface);

            // Act
            var result = _service.IsValidAgent(interfaceType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAgent_WithTypeWithoutAttribute_ShouldReturnFalse()
        {
            // Arrange
            var typeWithoutAttribute = typeof(TestClassWithoutAttribute);

            // Act
            var result = _service.IsValidAgent(typeWithoutAttribute);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAgent_WithInvalidAttribute_ShouldReturnFalse()
        {
            // Arrange
            var typeWithInvalidAttribute = typeof(TestInvalidAgent);

            // Act
            var result = _service.IsValidAgent(typeWithInvalidAttribute);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetScanStatistics_InitialState_ShouldReturnDefaultStatistics()
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
        "test-valid-agent",
        "TestValidAgent",
        "Test L1 Description for valid agent with sufficient length to meet requirements",
        "Test L2 Description providing detailed information about this test agent for workflow orchestration validation purposes. This description contains enough detail to meet the minimum length requirements for L2 descriptions in the agent description system.")]
    public class TestValidAgent
    {
        public void Execute()
        {
            // Test implementation
        }
    }

    [AgentDescription(
        "test-invalid-agent",
        "TestInvalidAgent",
        "Short", // Too short (< 50 characters)
        "Also short")] // Too short (< 300 characters)
    public class TestInvalidAgent
    {
        public void Execute()
        {
            // Test implementation
        }
    }

    public abstract class TestAbstractAgent
    {
        public abstract void Execute();
    }

    public interface ITestInterface
    {
        void Execute();
    }

    public class TestClassWithoutAttribute
    {
        public void Execute()
        {
            // Test implementation
        }
    }
} 