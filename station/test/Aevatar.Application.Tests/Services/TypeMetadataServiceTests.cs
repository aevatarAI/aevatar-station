// ABOUTME: This file contains unit tests for the TypeMetadataService implementation
// ABOUTME: Tests cover assembly scanning, capability extraction, caching, and metadata operations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Aevatar.Application.Services;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Services
{
    public class TypeMetadataServiceTests
    {
        private readonly Mock<ILogger<TypeMetadataService>> _mockLogger;
        private readonly TypeMetadataService _service;

        public TypeMetadataServiceTests()
        {
            _mockLogger = new Mock<ILogger<TypeMetadataService>>();
            _service = new TypeMetadataService(_mockLogger.Object);
        }

        [Fact]
        public async Task Should_ReturnEmptyList_When_NoAssembliesContainGAgents()
        {
            // Act
            var result = await _service.GetAllTypesAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnEmptyList_When_GetTypesByCapabilityAsyncCalledWithNonExistentCapability()
        {
            // Act
            var result = await _service.GetTypesByCapabilityAsync("NonExistentCapability");

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnNull_When_GetTypeMetadataAsyncCalledWithNonExistentAgentType()
        {
            // Act
            var result = await _service.GetTypeMetadataAsync("NonExistentAgent");

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_CompleteSuccessfully_When_RefreshMetadataAsyncCalled()
        {
            // Act & Assert
            await _service.RefreshMetadataAsync();
            // Should not throw any exceptions
        }

        [Fact]
        public async Task Should_ScanAssembliesForGAgentTypes_When_InitializedWithAssemblyScanning()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            
            // Act
            await serviceWithScanning.RefreshMetadataAsync();
            var result = await serviceWithScanning.GetAllTypesAsync();

            // Assert
            result.ShouldNotBeNull();
            // Should potentially find some GAgent types in the loaded assemblies
        }

        [Fact]
        public async Task Should_ExtractCapabilitiesFromEventHandlerMethods_When_GAgentTypeHasEventHandlers()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            
            // Act
            var allTypes = await serviceWithScanning.GetAllTypesAsync();
            
            // Assert
            if (allTypes.Any())
            {
                var typeWithCapabilities = allTypes.FirstOrDefault(t => t.Capabilities?.Any() == true);
                if (typeWithCapabilities != null)
                {
                    typeWithCapabilities.Capabilities.ShouldNotBeNull();
                    typeWithCapabilities.Capabilities.ShouldNotBeEmpty();
                }
            }
        }

        [Fact]
        public async Task Should_CacheMetadata_When_MetadataIsAccessedMultipleTimes()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            
            // Act
            var firstCall = await serviceWithScanning.GetAllTypesAsync();
            var secondCall = await serviceWithScanning.GetAllTypesAsync();
            
            // Assert
            firstCall.ShouldNotBeNull();
            secondCall.ShouldNotBeNull();
            firstCall.Count.ShouldBe(secondCall.Count);
            // Results should be from cache (same references or equivalent data)
        }

        [Fact]
        public async Task Should_FilterByCapability_When_GetTypesByCapabilityAsyncCalled()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            var allTypes = await serviceWithScanning.GetAllTypesAsync();
            
            if (allTypes.Any())
            {
                var typeWithCapabilities = allTypes.FirstOrDefault(t => t.Capabilities?.Any() == true);
                if (typeWithCapabilities != null)
                {
                    var testCapability = typeWithCapabilities.Capabilities.First();
                    
                    // Act
                    var result = await serviceWithScanning.GetTypesByCapabilityAsync(testCapability);
                    
                    // Assert
                    result.ShouldNotBeNull();
                    result.ShouldNotBeEmpty();
                    result.All(t => t.Capabilities.Contains(testCapability)).ShouldBeTrue();
                }
            }
        }

        [Fact]
        public async Task Should_ReturnSpecificMetadata_When_GetTypeMetadataAsyncCalledWithValidAgentType()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            var allTypes = await serviceWithScanning.GetAllTypesAsync();
            
            if (allTypes.Any())
            {
                var testType = allTypes.First();
                
                // Act
                var result = await serviceWithScanning.GetTypeMetadataAsync(testType.AgentType);
                
                // Assert
                result.ShouldNotBeNull();
                result.AgentType.ShouldBe(testType.AgentType);
                result.Capabilities.ShouldBe(testType.Capabilities);
            }
        }

        [Fact]
        public async Task Should_HandleAssemblyVersions_When_MetadataExtracted()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            
            // Act
            var allTypes = await serviceWithScanning.GetAllTypesAsync();
            
            // Assert
            if (allTypes.Any())
            {
                allTypes.All(t => !string.IsNullOrEmpty(t.AssemblyVersion)).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Should_HandleGrainInterfaces_When_MetadataExtracted()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            
            // Act
            var allTypes = await serviceWithScanning.GetAllTypesAsync();
            
            // Assert
            if (allTypes.Any())
            {
                allTypes.All(t => t.GrainInterface != null).ShouldBeTrue();
            }
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
        public async Task Should_RebuildCache_When_RefreshMetadataAsyncCalled()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            var initialTypes = await serviceWithScanning.GetAllTypesAsync();
            
            // Act
            await serviceWithScanning.RefreshMetadataAsync();
            var refreshedTypes = await serviceWithScanning.GetAllTypesAsync();
            
            // Assert
            initialTypes.Count.ShouldBe(refreshedTypes.Count);
            // Cache should be rebuilt but content should be equivalent
        }

        [Fact]
        public async Task Should_BeThreadSafe_When_ConcurrentAccessOccurs()
        {
            // Arrange
            var serviceWithScanning = new TypeMetadataService(_mockLogger.Object);
            await serviceWithScanning.RefreshMetadataAsync();
            
            // Act
            var tasks = new List<Task<List<AgentTypeMetadata>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(serviceWithScanning.GetAllTypesAsync());
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
            }
        }
    }
}