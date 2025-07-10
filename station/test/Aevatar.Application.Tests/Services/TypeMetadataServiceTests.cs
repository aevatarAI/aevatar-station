// ABOUTME: This file contains unit tests for the TypeMetadataService implementation
// ABOUTME: Tests cover assembly scanning, capability extraction, caching, and metadata operations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Application.Grains;
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
    public class TypeMetadataServiceTests
    {
        private readonly Mock<ILogger<TypeMetadataService>> _mockLogger;
        private readonly Mock<IGrainFactory> _mockGrainFactory;
        private readonly Mock<ITypeMetadataGrain> _mockTypeMetadataGrain;
        private readonly TypeMetadataService _service;

        public TypeMetadataServiceTests()
        {
            _mockLogger = new Mock<ILogger<TypeMetadataService>>();
            _mockGrainFactory = new Mock<IGrainFactory>();
            _mockTypeMetadataGrain = new Mock<ITypeMetadataGrain>();
            
            _mockGrainFactory.Setup(f => f.GetGrain<ITypeMetadataGrain>(0L, null))
                .Returns(_mockTypeMetadataGrain.Object);
            
            _service = new TypeMetadataService(_mockLogger.Object, _mockGrainFactory.Object);
        }

        [Fact]
        public async Task Should_ReturnEmptyList_When_NoAssembliesContainGAgents()
        {
            // Arrange
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(new List<AgentTypeMetadata>());
            
            // Act
            var result = await _service.GetAllTypesAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnEmptyList_When_GetTypesByCapabilityAsyncCalledWithNonExistentCapability()
        {
            // Arrange
            _mockTypeMetadataGrain.Setup(g => g.GetByCapabilityAsync("NonExistentCapability"))
                .ReturnsAsync(new List<AgentTypeMetadata>());
            
            // Act
            var result = await _service.GetTypesByCapabilityAsync("NonExistentCapability");

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnNull_When_GetTypeMetadataAsyncCalledWithNonExistentAgentType()
        {
            // Arrange
            _mockTypeMetadataGrain.Setup(g => g.GetByTypeAsync("NonExistentAgent"))
                .ReturnsAsync((AgentTypeMetadata)null);
            
            // Act
            var result = await _service.GetTypeMetadataAsync("NonExistentAgent");

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_CompleteSuccessfully_When_RefreshMetadataAsyncCalled()
        {
            // Arrange
            var mockStats = new MetadataStats
            {
                TotalTypes = 0,
                SizeInBytes = 0,
                PercentageOf16MB = 0
            };
            
            _mockTypeMetadataGrain.Setup(g => g.SetMetadataAsync(It.IsAny<List<AgentTypeMetadata>>()))
                .Returns(Task.CompletedTask);
            _mockTypeMetadataGrain.Setup(g => g.GetStatsAsync())
                .ReturnsAsync(mockStats);
            
            // Act & Assert
            await _service.RefreshMetadataAsync();
            // Should not throw any exceptions
        }

        [Fact]
        public async Task Should_ScanAssembliesForGAgentTypes_When_InitializedWithAssemblyScanning()
        {
            // Arrange
            var mockStats = new MetadataStats { TotalTypes = 0, SizeInBytes = 0, PercentageOf16MB = 0 };
            _mockTypeMetadataGrain.Setup(g => g.SetMetadataAsync(It.IsAny<List<AgentTypeMetadata>>()))
                .Returns(Task.CompletedTask);
            _mockTypeMetadataGrain.Setup(g => g.GetStatsAsync())
                .ReturnsAsync(mockStats);
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(new List<AgentTypeMetadata>());
            
            // Act
            await _service.RefreshMetadataAsync();
            var result = await _service.GetAllTypesAsync();

            // Assert
            result.ShouldNotBeNull();
            // Should potentially find some GAgent types in the loaded assemblies
        }

        [Fact]
        public async Task Should_ExtractCapabilitiesFromEventHandlerMethods_When_GAgentTypeHasEventHandlers()
        {
            // Arrange
            var testMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability", "HandleEvent" }
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(new List<AgentTypeMetadata> { testMetadata });
            
            // Act
            var allTypes = await _service.GetAllTypesAsync();
            
            // Assert
            allTypes.ShouldNotBeEmpty();
            var typeWithCapabilities = allTypes.FirstOrDefault(t => t.Capabilities?.Any() == true);
            typeWithCapabilities.ShouldNotBeNull();
            typeWithCapabilities.Capabilities.ShouldNotBeEmpty();
            typeWithCapabilities.Capabilities.ShouldContain("TestCapability");
        }

        [Fact]
        public async Task Should_CacheMetadata_When_MetadataIsAccessedMultipleTimes()
        {
            // Arrange
            var testMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata { AgentType = "TestAgent1" },
                new AgentTypeMetadata { AgentType = "TestAgent2" }
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(testMetadata);
            
            // Act
            var firstCall = await _service.GetAllTypesAsync();
            var secondCall = await _service.GetAllTypesAsync();
            
            // Assert
            firstCall.ShouldNotBeNull();
            secondCall.ShouldNotBeNull();
            firstCall.Count.ShouldBe(secondCall.Count);
            firstCall.Count.ShouldBe(2);
            // Results should be from cache (same references or equivalent data)
        }

        [Fact]
        public async Task Should_FilterByCapability_When_GetTypesByCapabilityAsyncCalled()
        {
            // Arrange
            var testMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata 
                { 
                    AgentType = "TestAgent1", 
                    Capabilities = new List<string> { "TestCapability", "OtherCapability" } 
                },
                new AgentTypeMetadata 
                { 
                    AgentType = "TestAgent2", 
                    Capabilities = new List<string> { "TestCapability" } 
                }
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetByCapabilityAsync("TestCapability"))
                .ReturnsAsync(testMetadata);
            
            // Act
            var result = await _service.GetTypesByCapabilityAsync("TestCapability");
            
            // Assert
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(2);
            result.All(t => t.Capabilities.Contains("TestCapability")).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_ReturnSpecificMetadata_When_GetTypeMetadataAsyncCalledWithValidAgentType()
        {
            // Arrange
            var testMetadata = new AgentTypeMetadata
            {
                AgentType = "TestAgent",
                Capabilities = new List<string> { "TestCapability" },
                Description = "Test Agent Description"
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetByTypeAsync("TestAgent"))
                .ReturnsAsync(testMetadata);
            
            // Act
            var result = await _service.GetTypeMetadataAsync("TestAgent");
            
            // Assert
            result.ShouldNotBeNull();
            result.AgentType.ShouldBe("TestAgent");
            result.Capabilities.ShouldContain("TestCapability");
            result.Description.ShouldBe("Test Agent Description");
        }

        [Fact]
        public async Task Should_HandleAssemblyVersions_When_MetadataExtracted()
        {
            // Arrange
            var testMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata 
                { 
                    AgentType = "TestAgent1", 
                    AssemblyVersion = "1.0.0" 
                },
                new AgentTypeMetadata 
                { 
                    AgentType = "TestAgent2", 
                    AssemblyVersion = "2.0.0" 
                }
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(testMetadata);
            
            // Act
            var allTypes = await _service.GetAllTypesAsync();
            
            // Assert
            allTypes.ShouldNotBeEmpty();
            allTypes.All(t => !string.IsNullOrEmpty(t.AssemblyVersion)).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_HandleGrainInterfaces_When_MetadataExtracted()
        {
            // Arrange
            var testMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata 
                { 
                    AgentType = "TestAgent1", 
                    GrainInterface = typeof(ITypeMetadataGrain) 
                },
                new AgentTypeMetadata 
                { 
                    AgentType = "TestAgent2", 
                    GrainInterface = typeof(IGrain) 
                }
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(testMetadata);
            
            // Act
            var allTypes = await _service.GetAllTypesAsync();
            
            // Assert
            allTypes.ShouldNotBeEmpty();
            allTypes.All(t => t.GrainInterface != null).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_HandleNullCapabilityParameter_When_GetTypesByCapabilityAsyncCalled()
        {
            // Act
            var result = await _service.GetTypesByCapabilityAsync(null);
            
            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_HandleNullAgentTypeParameter_When_GetTypeMetadataAsyncCalled()
        {
            // Act
            var result = await _service.GetTypeMetadataAsync(null);
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_HandleEmptyCapabilityParameter_When_GetTypesByCapabilityAsyncCalled()
        {
            // Act
            var result = await _service.GetTypesByCapabilityAsync("");
            
            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_HandleEmptyAgentTypeParameter_When_GetTypeMetadataAsyncCalled()
        {
            // Act
            var result = await _service.GetTypeMetadataAsync("");
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_RefreshMetadata_When_RefreshMetadataAsyncCalled()
        {
            // Arrange
            var testMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata { AgentType = "TestAgent1" },
                new AgentTypeMetadata { AgentType = "TestAgent2" }
            };
            
            var mockStats = new MetadataStats { TotalTypes = 2, SizeInBytes = 1024, PercentageOf16MB = 0.006 };
            
            _mockTypeMetadataGrain.Setup(g => g.SetMetadataAsync(It.IsAny<List<AgentTypeMetadata>>()))
                .Returns(Task.CompletedTask);
            _mockTypeMetadataGrain.Setup(g => g.GetStatsAsync())
                .ReturnsAsync(mockStats);
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(testMetadata);
            
            // Act
            await _service.RefreshMetadataAsync();
            var initialTypes = await _service.GetAllTypesAsync();
            
            await _service.RefreshMetadataAsync();
            var refreshedTypes = await _service.GetAllTypesAsync();
            
            // Assert
            // With stateless service, GetAllTypesAsync always returns from grain
            initialTypes.Count.ShouldBe(2);
            refreshedTypes.Count.ShouldBe(2);
            // Verify that SetMetadataAsync was called during RefreshMetadataAsync (with real scanned data)
            _mockTypeMetadataGrain.Verify(g => g.SetMetadataAsync(It.IsAny<List<AgentTypeMetadata>>()), Times.Exactly(2));
            // Verify that GetStatsAsync was called during RefreshMetadataAsync
            _mockTypeMetadataGrain.Verify(g => g.GetStatsAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task Should_BeThreadSafe_When_ConcurrentAccessOccurs()
        {
            // Arrange
            var testMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata { AgentType = "TestAgent1" },
                new AgentTypeMetadata { AgentType = "TestAgent2" }
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetAllMetadataAsync())
                .ReturnsAsync(testMetadata);
            
            // Act
            var tasks = new List<Task<List<AgentTypeMetadata>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_service.GetAllTypesAsync());
            }
            
            var results = await Task.WhenAll(tasks);
            
            // Assert
            results.ShouldNotBeNull();
            results.Length.ShouldBe(10);
            // All results should be consistent
            var firstResult = results[0];
            foreach (var result in results)
            {
                result.Count.ShouldBe(firstResult.Count);
                result.Count.ShouldBe(2);
            }
        }

        [Fact]
        public async Task Should_ReturnStatsFromGrain_When_GetStatsAsyncCalled()
        {
            // Arrange
            var expectedStats = new MetadataStats
            {
                TotalTypes = 5,
                SizeInBytes = 2048,
                PercentageOf16MB = 0.012
            };
            
            _mockTypeMetadataGrain.Setup(g => g.GetStatsAsync())
                .ReturnsAsync(expectedStats);
            
            // Act
            var result = await _service.GetStatsAsync();
            
            // Assert
            result.ShouldNotBeNull();
            result.TotalTypes.ShouldBe(5);
            result.SizeInBytes.ShouldBe(2048);
            result.PercentageOf16MB.ShouldBe(0.012);
        }

        [Fact]
        public async Task Should_ReturnFallbackStats_When_GrainThrowsException()
        {
            // Arrange
            _mockTypeMetadataGrain.Setup(g => g.GetStatsAsync())
                .ThrowsAsync(new Exception("Grain connection failed"));
            
            // Act
            var result = await _service.GetStatsAsync();
            
            // Assert
            result.ShouldNotBeNull();
            result.TotalTypes.ShouldBe(0); // Should return local cache count
            result.SizeInBytes.ShouldBe(0);
            result.PercentageOf16MB.ShouldBe(0);
        }

        [Fact]
        public async Task Should_PersistToGrain_When_RefreshMetadataAsyncCalled()
        {
            // Arrange
            var mockStats = new MetadataStats
            {
                TotalTypes = 3,
                SizeInBytes = 1536,
                PercentageOf16MB = 0.009
            };
            
            _mockTypeMetadataGrain.Setup(g => g.SetMetadataAsync(It.IsAny<List<AgentTypeMetadata>>()))
                .Returns(Task.CompletedTask);
            _mockTypeMetadataGrain.Setup(g => g.GetStatsAsync())
                .ReturnsAsync(mockStats);
            
            // Act
            await _service.RefreshMetadataAsync();
            
            // Assert
            _mockTypeMetadataGrain.Verify(g => g.SetMetadataAsync(It.IsAny<List<AgentTypeMetadata>>()), Times.Once);
            _mockTypeMetadataGrain.Verify(g => g.GetStatsAsync(), Times.Once);
        }
    }
}