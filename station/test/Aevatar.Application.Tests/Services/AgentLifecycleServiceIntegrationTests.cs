// ABOUTME: This file contains integration tests for the AgentLifecycleService implementation
// ABOUTME: Tests integration with TypeMetadataService and validates service dependencies

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Aevatar.Application.Services;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Services
{
    public class AgentLifecycleServiceIntegrationTests
    {
        private readonly Mock<ITypeMetadataService> _mockTypeMetadataService;
        private readonly Mock<IGrainFactory> _mockGrainFactory;
        private readonly Mock<ILogger<AgentLifecycleService>> _mockLogger;
        private readonly AgentLifecycleService _service;

        public AgentLifecycleServiceIntegrationTests()
        {
            _mockTypeMetadataService = new Mock<ITypeMetadataService>();
            _mockGrainFactory = new Mock<IGrainFactory>();
            _mockLogger = new Mock<ILogger<AgentLifecycleService>>();
            _service = new AgentLifecycleService(_mockTypeMetadataService.Object, _mockGrainFactory.Object, _mockLogger.Object);
        }

        #region Integration with TypeMetadataService

        [Fact]
        public async Task Should_CreateAgentWithCapabilities_When_TypeMetadataServiceReturnsValidData()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "BusinessAgent",
                Name = "Test Business Agent",
                Properties = new Dictionary<string, object> { { "category", "business" } }
            };

            var typeMetadata = new AgentTypeMetadata
            {
                AgentType = "BusinessAgent",
                Capabilities = new List<string> { "TaskCompleted", "MessageSent", "DataProcessed" },
                Description = "Agent for business process management",
                AssemblyVersion = "1.0.0",
                InterfaceVersions = new List<string> { "IBusinessAgent:1.0" }
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("BusinessAgent"))
                .ReturnsAsync(typeMetadata);

            // Act
            var result = await _service.CreateAgentAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.UserId.ShouldBe(request.UserId);
            result.AgentType.ShouldBe(request.AgentType);
            result.Name.ShouldBe(request.Name);
            result.Properties.ShouldBe(request.Properties);
            result.Capabilities.ShouldBe(typeMetadata.Capabilities);
            result.Description.ShouldBe(typeMetadata.Description);
            result.Version.ShouldBe(typeMetadata.AssemblyVersion);
            result.Status.ShouldBe(AgentStatus.Initializing);
            result.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
            result.LastActivity.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));

            // Verify TypeMetadataService was called
            _mockTypeMetadataService.Verify(x => x.GetTypeMetadataAsync("BusinessAgent"), Times.Once);
        }

        [Fact]
        public async Task Should_CreateAgentWithEmptyCapabilities_When_TypeMetadataHasNullCapabilities()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "SimpleAgent",
                Name = "Simple Agent"
            };

            var typeMetadata = new AgentTypeMetadata
            {
                AgentType = "SimpleAgent",
                Capabilities = null, // Null capabilities
                Description = "Simple agent without capabilities"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("SimpleAgent"))
                .ReturnsAsync(typeMetadata);

            // Act
            var result = await _service.CreateAgentAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Capabilities.ShouldNotBeNull();
            result.Capabilities.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_HandlePropertiesCorrectly_When_RequestHasNullProperties()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Test Agent",
                Properties = null // Null properties
            };

            var typeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "Test" }
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(typeMetadata);

            // Act
            var result = await _service.CreateAgentAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Properties.ShouldNotBeNull();
            result.Properties.ShouldBeEmpty();
        }

        #endregion

        #region Error Handling Integration

        [Fact]
        public async Task Should_ThrowInvalidOperationException_When_TypeMetadataServiceReturnsNull()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "UnknownAgent",
                Name = "Unknown Agent"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("UnknownAgent"))
                .ReturnsAsync((AgentTypeMetadata)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(() => _service.CreateAgentAsync(request));
            exception.Message.ShouldContain("Unknown agent type: UnknownAgent");

            // Verify TypeMetadataService was called
            _mockTypeMetadataService.Verify(x => x.GetTypeMetadataAsync("UnknownAgent"), Times.Once);
        }

        [Fact]
        public async Task Should_PropagateException_When_TypeMetadataServiceThrows()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "ProblematicAgent",
                Name = "Problematic Agent"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("ProblematicAgent"))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            var exception = await Should.ThrowAsync<Exception>(() => _service.CreateAgentAsync(request));
            exception.Message.ShouldBe("Database connection failed");

            // Verify TypeMetadataService was called
            _mockTypeMetadataService.Verify(x => x.GetTypeMetadataAsync("ProblematicAgent"), Times.Once);
        }

        #endregion

        #region Logging Integration

        [Fact]
        public async Task Should_LogInformation_When_AgentCreatedSuccessfully()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "LogTestAgent",
                Name = "Log Test Agent"
            };

            var typeMetadata = new AgentTypeMetadata
            {
                AgentType = "LogTestAgent",
                Capabilities = new List<string> { "Logging", "Testing" }
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("LogTestAgent"))
                .ReturnsAsync(typeMetadata);

            // Act
            var result = await _service.CreateAgentAsync(request);

            // Assert
            result.ShouldNotBeNull();

            // Verify logging calls - using generic verification for extension methods
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating agent of type LogTestAgent")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Agent") && v.ToString().Contains("created successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_LogError_When_AgentTypeNotFound()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "NonExistentAgent",
                Name = "Non-existent Agent"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("NonExistentAgent"))
                .ReturnsAsync((AgentTypeMetadata)null);

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _service.CreateAgentAsync(request));

            // Verify error logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Agent type NonExistentAgent not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Performance and Consistency Tests

        [Fact]
        public async Task Should_CreateAgentsWithUniqueIds_When_CalledMultipleTimes()
        {
            // Arrange
            var typeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "Test" }
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(typeMetadata);

            var requests = new List<CreateAgentRequest>();
            for (int i = 0; i < 5; i++)
            {
                requests.Add(new CreateAgentRequest
                {
                    UserId = Guid.NewGuid(),
                    AgentType = "TestAgent",
                    Name = $"Test Agent {i}"
                });
            }

            // Act
            var results = new List<AgentInfo>();
            foreach (var request in requests)
            {
                results.Add(await _service.CreateAgentAsync(request));
            }

            // Assert
            results.Count.ShouldBe(5);
            var uniqueIds = results.Select(r => r.Id).Distinct().ToList();
            uniqueIds.Count.ShouldBe(5); // All IDs should be unique

            // Verify each result is valid
            foreach (var result in results)
            {
                result.Id.ShouldNotBe(Guid.Empty);
                result.AgentType.ShouldBe("TestAgent");
                result.Status.ShouldBe(AgentStatus.Initializing);
            }
        }

        [Fact]
        public async Task Should_PreserveTimestampConsistency_When_AgentCreated()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TimestampAgent",
                Name = "Timestamp Agent"
            };

            var typeMetadata = new AgentTypeMetadata
            {
                AgentType = "TimestampAgent",
                Capabilities = new List<string> { "Time" }
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TimestampAgent"))
                .ReturnsAsync(typeMetadata);

            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await _service.CreateAgentAsync(request);

            var afterCreation = DateTime.UtcNow;

            // Assert
            result.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
            result.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
            result.LastActivity.ShouldBeGreaterThanOrEqualTo(beforeCreation);
            result.LastActivity.ShouldBeLessThanOrEqualTo(afterCreation);
            
            // CreatedAt and LastActivity should be very close for new agents
            var timeDifference = Math.Abs((result.LastActivity - result.CreatedAt).TotalMilliseconds);
            timeDifference.ShouldBeLessThan(1000); // Less than 1 second difference
        }

        #endregion

        #region Dependency Injection Tests

        [Fact]
        public void Should_ThrowArgumentNullException_When_TypeMetadataServiceIsNull()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => new AgentLifecycleService(null, _mockGrainFactory.Object, _mockLogger.Object));
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_GrainFactoryIsNull()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => new AgentLifecycleService(_mockTypeMetadataService.Object, null, _mockLogger.Object));
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => new AgentLifecycleService(_mockTypeMetadataService.Object, _mockGrainFactory.Object, null));
        }

        #endregion
    }
}