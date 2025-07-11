// ABOUTME: This file contains comprehensive unit tests for the AgentLifecycleService implementation
// ABOUTME: Tests cover all CRUD operations, validation, error handling, and integration scenarios following TDD principles

using System;
using System.Collections.Generic;
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
    public class AgentLifecycleServiceTests
    {
        private readonly Mock<ITypeMetadataService> _mockTypeMetadataService;
        private readonly Mock<IGrainFactory> _mockGrainFactory;
        private readonly Mock<IGAgentFactory> _mockGAgentFactory;
        private readonly Mock<ILogger<AgentLifecycleService>> _mockLogger;
        private readonly AgentLifecycleService _service;

        public AgentLifecycleServiceTests()
        {
            _mockTypeMetadataService = new Mock<ITypeMetadataService>();
            _mockGrainFactory = new Mock<IGrainFactory>();
            _mockGAgentFactory = new Mock<IGAgentFactory>();
            _mockLogger = new Mock<ILogger<AgentLifecycleService>>();
            
            // Updated to include IGAgentFactory for Orleans grain support
            _service = new AgentLifecycleService(_mockTypeMetadataService.Object, _mockGrainFactory.Object, _mockGAgentFactory.Object, _mockLogger.Object);
        }

        #region Agent Creation Tests

        [Fact]
        public async Task Should_CreateAgent_When_ValidRequestProvided()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Test Agent",
                Properties = new Dictionary<string, object> { { "key", "value" } }
            };

            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            // Act
            var result = await _service.CreateAgentAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.UserId.ShouldBe(request.UserId);
            result.AgentType.ShouldBe(request.AgentType);
            result.Name.ShouldBe(request.Name);
            result.Properties.ShouldBe(request.Properties);
            result.Capabilities.ShouldBe(expectedTypeMetadata.Capabilities);
            result.Status.ShouldBe(AgentStatus.Initializing);
            result.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_CreateAgentRequestIsNull()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _service.CreateAgentAsync(null));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_CreateAgentRequestHasEmptyUserId()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.Empty,
                AgentType = "TestAgent",
                Name = "Test Agent"
            };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.CreateAgentAsync(request));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_CreateAgentRequestHasEmptyAgentType()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "",
                Name = "Test Agent"
            };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.CreateAgentAsync(request));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_CreateAgentRequestHasEmptyName()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = ""
            };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.CreateAgentAsync(request));
        }

        [Fact]
        public async Task Should_ThrowInvalidOperationException_When_AgentTypeNotFound()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "NonExistentAgent",
                Name = "Test Agent"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("NonExistentAgent"))
                .ReturnsAsync((AgentTypeMetadata)null);

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _service.CreateAgentAsync(request));
        }

        #endregion

        #region Agent Update Tests

        [Fact]
        public async Task Should_UpdateAgent_When_ValidRequestProvided()
        {
            // Arrange - First create an agent
            var createRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Original Agent Name",
                Properties = new Dictionary<string, object> { { "original", "value" } }
            };

            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            var createdAgent = await _service.CreateAgentAsync(createRequest);

            var updateRequest = new UpdateAgentRequest
            {
                Name = "Updated Agent Name",
                Properties = new Dictionary<string, object> { { "updated", "value" } }
            };

            // Act
            var result = await _service.UpdateAgentAsync(createdAgent.Id, updateRequest);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(createdAgent.Id);
            result.Name.ShouldBe(updateRequest.Name);
            result.Properties.ShouldBe(updateRequest.Properties);
            result.LastActivity.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_UpdateAgentIdIsEmpty()
        {
            // Arrange
            var request = new UpdateAgentRequest { Name = "Updated Name" };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.UpdateAgentAsync(Guid.Empty, request));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_UpdateAgentRequestIsNull()
        {
            // Arrange
            var agentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _service.UpdateAgentAsync(agentId, null));
        }

        [Fact]
        public async Task Should_ThrowInvalidOperationException_When_UpdateAgentNotFound()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var request = new UpdateAgentRequest { Name = "Updated Name" };

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _service.UpdateAgentAsync(agentId, request));
        }

        #endregion

        #region Agent Deletion Tests

        [Fact]
        public async Task Should_DeleteAgent_When_ValidAgentIdProvided()
        {
            // Arrange - First create an agent
            var createRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Test Agent to Delete",
                Properties = new Dictionary<string, object> { { "test", "value" } }
            };

            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            var createdAgent = await _service.CreateAgentAsync(createRequest);

            // Act
            await _service.DeleteAgentAsync(createdAgent.Id);

            // Assert - No exception should be thrown
            // Verify that the agent status is set to Deleted
            var deletedAgent = await _service.GetAgentAsync(createdAgent.Id);
            deletedAgent.Status.ShouldBe(AgentStatus.Deleted);
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_DeleteAgentIdIsEmpty()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.DeleteAgentAsync(Guid.Empty));
        }

        [Fact]
        public async Task Should_ThrowInvalidOperationException_When_DeleteAgentNotFound()
        {
            // Arrange
            var agentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _service.DeleteAgentAsync(agentId));
        }

        #endregion

        #region Agent Retrieval Tests

        [Fact]
        public async Task Should_GetAgent_When_ValidAgentIdProvided()
        {
            // Arrange - First create an agent
            var createRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Test Agent to Get",
                Properties = new Dictionary<string, object> { { "test", "value" } }
            };

            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            var createdAgent = await _service.CreateAgentAsync(createRequest);

            // Act
            var result = await _service.GetAgentAsync(createdAgent.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(createdAgent.Id);
            result.Name.ShouldBe(createRequest.Name);
            result.AgentType.ShouldBe(createRequest.AgentType);
            result.UserId.ShouldBe(createRequest.UserId);
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_GetAgentIdIsEmpty()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.GetAgentAsync(Guid.Empty));
        }

        [Fact]
        public async Task Should_ThrowInvalidOperationException_When_GetAgentNotFound()
        {
            // Arrange
            var agentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _service.GetAgentAsync(agentId));
        }

        [Fact]
        public async Task Should_GetUserAgents_When_ValidUserIdProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _service.GetUserAgentsAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeOfType<List<AgentInfo>>();
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_GetUserAgentsUserIdIsEmpty()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.GetUserAgentsAsync(Guid.Empty));
        }

        [Fact]
        public async Task Should_ReturnEmptyList_When_UserHasNoAgents()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _service.GetUserAgentsAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        #endregion

        #region Event Management Tests

        [Fact]
        public async Task Should_SendEventToAgent_When_ValidParametersProvided()
        {
            // Arrange - First create an agent
            var createRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Test Agent for Event",
                Properties = new Dictionary<string, object> { { "test", "value" } }
            };

            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            var createdAgent = await _service.CreateAgentAsync(createRequest);
            var testEvent = new TestEvent { Data = "test data" };

            // Act
            await _service.SendEventToAgentAsync(createdAgent.Id, testEvent);

            // Assert - No exception should be thrown
            // Verify that the event was published to the correct stream
            // Also verify the agent's LastActivity was updated
            var updatedAgent = await _service.GetAgentAsync(createdAgent.Id);
            updatedAgent.LastActivity.ShouldBeGreaterThan(createdAgent.LastActivity);
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_SendEventAgentIdIsEmpty()
        {
            // Arrange
            var testEvent = new TestEvent { Data = "test data" };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.SendEventToAgentAsync(Guid.Empty, testEvent));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_SendEventEventIsNull()
        {
            // Arrange
            var agentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _service.SendEventToAgentAsync(agentId, null));
        }

        [Fact]
        public async Task Should_ThrowInvalidOperationException_When_SendEventAgentNotFound()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var testEvent = new TestEvent { Data = "test data" };

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _service.SendEventToAgentAsync(agentId, testEvent));
        }

        #endregion

        #region Sub-Agent Management Tests

        [Fact]
        public async Task Should_AddSubAgent_When_ValidParametersProvided()
        {
            // Arrange - Create parent and child agents first
            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            var parentRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Parent Agent",
                Properties = new Dictionary<string, object> { { "role", "parent" } }
            };

            var childRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Child Agent",
                Properties = new Dictionary<string, object> { { "role", "child" } }
            };

            var parentAgent = await _service.CreateAgentAsync(parentRequest);
            var childAgent = await _service.CreateAgentAsync(childRequest);

            // Act
            var result = await _service.AddSubAgentAsync(parentAgent.Id, childAgent.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(parentAgent.Id);
            result.SubAgents.ShouldContain(childAgent.Id);
            
            // Verify child agent now has parent reference
            var updatedChild = await _service.GetAgentAsync(childAgent.Id);
            updatedChild.ParentAgentId.ShouldBe(parentAgent.Id);
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_AddSubAgentParentIdIsEmpty()
        {
            // Arrange
            var childId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.AddSubAgentAsync(Guid.Empty, childId));
        }

        [Fact]
        public async Task Should_ThrowArgumentException_When_AddSubAgentChildIdIsEmpty()
        {
            // Arrange
            var parentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.AddSubAgentAsync(parentId, Guid.Empty));
        }

        [Fact]
        public async Task Should_RemoveSubAgent_When_ValidParametersProvided()
        {
            // Arrange - Create parent and child agents first, then add relationship
            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            var parentRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Parent Agent",
                Properties = new Dictionary<string, object> { { "role", "parent" } }
            };

            var childRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Child Agent",
                Properties = new Dictionary<string, object> { { "role", "child" } }
            };

            var parentAgent = await _service.CreateAgentAsync(parentRequest);
            var childAgent = await _service.CreateAgentAsync(childRequest);

            // Add the relationship first
            await _service.AddSubAgentAsync(parentAgent.Id, childAgent.Id);

            // Act
            var result = await _service.RemoveSubAgentAsync(parentAgent.Id, childAgent.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(parentAgent.Id);
            result.SubAgents.ShouldNotContain(childAgent.Id);
            
            // Verify child agent no longer has parent reference
            var updatedChild = await _service.GetAgentAsync(childAgent.Id);
            updatedChild.ParentAgentId.ShouldBeNull();
        }

        [Fact]
        public async Task Should_RemoveAllSubAgents_When_ValidParentIdProvided()
        {
            // Arrange - Create parent and multiple child agents, then add relationships
            var expectedTypeMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Type"
            };

            _mockTypeMetadataService.Setup(x => x.GetTypeMetadataAsync("TestAgent"))
                .ReturnsAsync(expectedTypeMetadata);

            var parentRequest = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Parent Agent",
                Properties = new Dictionary<string, object> { { "role", "parent" } }
            };

            var parentAgent = await _service.CreateAgentAsync(parentRequest);

            // Create multiple child agents
            var childAgents = new List<AgentInfo>();
            for (int i = 0; i < 3; i++)
            {
                var childRequest = new CreateAgentRequest
                {
                    UserId = Guid.NewGuid(),
                    AgentType = "TestAgent",
                    Name = $"Child Agent {i}",
                    Properties = new Dictionary<string, object> { { "role", "child" } }
                };

                var childAgent = await _service.CreateAgentAsync(childRequest);
                childAgents.Add(childAgent);
                
                // Add the relationship
                await _service.AddSubAgentAsync(parentAgent.Id, childAgent.Id);
            }

            // Act
            var result = await _service.RemoveAllSubAgentsAsync(parentAgent.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(parentAgent.Id);
            result.SubAgents.ShouldBeEmpty();
            
            // Verify all child agents no longer have parent reference
            foreach (var childAgent in childAgents)
            {
                var updatedChild = await _service.GetAgentAsync(childAgent.Id);
                updatedChild.ParentAgentId.ShouldBeNull();
            }
        }

        #endregion

        #region Request Model Validation Tests

        [Fact]
        public void Should_ValidateCreateAgentRequest_When_AllFieldsValid()
        {
            // Arrange
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "TestAgent",
                Name = "Test Agent"
            };

            // Act
            var isValid = request.IsValid();

            // Assert
            isValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_InvalidateCreateAgentRequest_When_RequiredFieldsMissing()
        {
            // Arrange
            var request = new CreateAgentRequest();

            // Act
            var isValid = request.IsValid();
            var errors = request.GetValidationErrors();

            // Assert
            isValid.ShouldBeFalse();
            errors.ShouldNotBeEmpty();
            errors.Count.ShouldBe(3); // UserId, AgentType, Name
        }

        [Fact]
        public void Should_ValidateUpdateAgentRequest_When_FieldsProvided()
        {
            // Arrange
            var request = new UpdateAgentRequest
            {
                Name = "Updated Name"
            };

            // Act
            var isValid = request.IsValid();

            // Assert
            isValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_InvalidateUpdateAgentRequest_When_NoFieldsProvided()
        {
            // Arrange
            var request = new UpdateAgentRequest();

            // Act
            var isValid = request.IsValid();
            var errors = request.GetValidationErrors();

            // Assert
            isValid.ShouldBeFalse();
            errors.ShouldNotBeEmpty();
        }

        #endregion

        #region Helper Classes

        private class TestEvent : EventBase
        {
            public string Data { get; set; }
        }

        #endregion
    }
}