// ABOUTME: This file contains unit tests for the TypeMetadataGrain Orleans integration
// ABOUTME: Tests cover grain lifecycle, persistence, and cluster-wide metadata sharing

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains;
using Aevatar.Application.Models;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Services
{
    public class TypeMetadataGrainTests : IDisposable
    {
        private readonly TestCluster _cluster;
        private readonly IGrainFactory _grainFactory;

        public TypeMetadataGrainTests()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
            _cluster = builder.Build();
            _cluster.Deploy();
            _grainFactory = _cluster.GrainFactory;
        }

        [Fact]
        public async Task Should_StoreMetadata_When_SetMetadataAsyncCalled()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(0);
            var metadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "TestAgent",
                    Capabilities = new List<string> { "TestCapability" },
                    AssemblyVersion = "1.0.0"
                }
            };

            // Act
            await grain.SetMetadataAsync(metadata);
            var result = await grain.GetAllMetadataAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].AgentType.ShouldBe("TestAgent");
            result[0].Capabilities.ShouldContain("TestCapability");
        }

        [Fact]
        public async Task Should_ReturnEmptyList_When_GetAllMetadataAsyncCalledWithoutData()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(1);

            // Act
            var result = await grain.GetAllMetadataAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnFilteredMetadata_When_GetByCapabilityAsyncCalled()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(2);
            var metadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "Agent1",
                    Capabilities = new List<string> { "Capability1", "Capability2" }
                },
                new AgentTypeMetadata
                {
                    AgentType = "Agent2",
                    Capabilities = new List<string> { "Capability2", "Capability3" }
                }
            };

            // Act
            await grain.SetMetadataAsync(metadata);
            var result = await grain.GetByCapabilityAsync("Capability1");

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].AgentType.ShouldBe("Agent1");
        }

        [Fact]
        public async Task Should_ReturnEmptyList_When_GetByCapabilityAsyncCalledWithNonExistentCapability()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(3);
            var metadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "Agent1",
                    Capabilities = new List<string> { "Capability1" }
                }
            };

            // Act
            await grain.SetMetadataAsync(metadata);
            var result = await grain.GetByCapabilityAsync("NonExistentCapability");

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnSpecificMetadata_When_GetByTypeAsyncCalled()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(4);
            var metadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "Agent1",
                    Capabilities = new List<string> { "Capability1" }
                },
                new AgentTypeMetadata
                {
                    AgentType = "Agent2",
                    Capabilities = new List<string> { "Capability2" }
                }
            };

            // Act
            await grain.SetMetadataAsync(metadata);
            var result = await grain.GetByTypeAsync("Agent1");

            // Assert
            result.ShouldNotBeNull();
            result.AgentType.ShouldBe("Agent1");
            result.Capabilities.ShouldContain("Capability1");
        }

        [Fact]
        public async Task Should_ReturnNull_When_GetByTypeAsyncCalledWithNonExistentType()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(5);
            var metadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "Agent1",
                    Capabilities = new List<string> { "Capability1" }
                }
            };

            // Act
            await grain.SetMetadataAsync(metadata);
            var result = await grain.GetByTypeAsync("NonExistentAgent");

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_ClearMetadata_When_ClearMetadataAsyncCalled()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(6);
            var metadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "Agent1",
                    Capabilities = new List<string> { "Capability1" }
                }
            };

            // Act
            await grain.SetMetadataAsync(metadata);
            await grain.ClearMetadataAsync();
            var result = await grain.GetAllMetadataAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_HandleNullMetadata_When_SetMetadataAsyncCalledWithNull()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(7);

            // Act
            await grain.SetMetadataAsync(null);
            var result = await grain.GetAllMetadataAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_HandleNullCapability_When_GetByCapabilityAsyncCalledWithNull()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(8);

            // Act
            var result = await grain.GetByCapabilityAsync(null);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_HandleNullType_When_GetByTypeAsyncCalledWithNull()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(9);

            // Act
            var result = await grain.GetByTypeAsync(null);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_PersistMetadata_When_GrainIsReactivated()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(10);
            var metadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "PersistentAgent",
                    Capabilities = new List<string> { "PersistentCapability" }
                }
            };

            // Act
            await grain.SetMetadataAsync(metadata);
            
            // Force grain deactivation by getting a new reference
            var newGrain = _grainFactory.GetGrain<ITypeMetadataGrain>(10);
            var result = await newGrain.GetAllMetadataAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].AgentType.ShouldBe("PersistentAgent");
        }

        [Fact]
        public async Task Should_UpdateExistingMetadata_When_SetMetadataAsyncCalledMultipleTimes()
        {
            // Arrange
            var grain = _grainFactory.GetGrain<ITypeMetadataGrain>(11);
            var initialMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "Agent1",
                    Capabilities = new List<string> { "Capability1" }
                }
            };
            var updatedMetadata = new List<AgentTypeMetadata>
            {
                new AgentTypeMetadata
                {
                    AgentType = "Agent1",
                    Capabilities = new List<string> { "Capability1", "Capability2" }
                },
                new AgentTypeMetadata
                {
                    AgentType = "Agent2",
                    Capabilities = new List<string> { "Capability3" }
                }
            };

            // Act
            await grain.SetMetadataAsync(initialMetadata);
            await grain.SetMetadataAsync(updatedMetadata);
            var result = await grain.GetAllMetadataAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            var agent1 = result.Find(m => m.AgentType == "Agent1");
            agent1.ShouldNotBeNull();
            agent1.Capabilities.Count.ShouldBe(2);
            agent1.Capabilities.ShouldContain("Capability1");
            agent1.Capabilities.ShouldContain("Capability2");
        }

        public void Dispose()
        {
            _cluster?.StopAllSilos();
        }

        private class TestSiloConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder hostBuilder)
            {
                hostBuilder.AddMemoryGrainStorage("PubSubStore");
            }
        }
    }
}