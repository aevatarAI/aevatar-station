using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Aevatar.Core.Streaming.Extensions;
using Aevatar.Core.Streaming.Monitors;
using Aevatar.Core.Streaming.Kafka;
using Orleans.Streams;
using Orleans.Streams.Core;
using Orleans.Configuration;
using Orleans.Streams.Kafka.Config;
using Orleans.Providers;
using Orleans.Streams.Utils.Serialization;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aevatar.Silo.Tests.Streaming
{
    /// <summary>
    /// Integration tests for Aevatar streaming infrastructure.
    /// Tests factory creation, real service interaction, and multi-provider scenarios.
    /// </summary>
    public class StreamingIntegrationTests
    {

        #region Factory Tests

        [Fact]
        public void AevatarKafkaAdapterFactory_Create_WithMissingOrleansServices_ThrowsInvalidOperationException()
        {
            // Arrange - Missing Orleans serialization services
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAevatarStreamingMonitoring();
            // NOT adding Orleans services to test dependency validation

            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert - Should throw due to missing Orleans services
            var exception = Assert.Throws<InvalidOperationException>(() =>
                AevatarKafkaAdapterFactory.Create(serviceProvider, "TestProvider"));

            Assert.Contains("Orleans.Serialization", exception.Message);
        }

        [Fact]
        public void AevatarKafkaAdapterFactory_Create_WithMissingMonitoringServices_ThrowsInvalidOperationException()
        {
            // Arrange - Missing monitoring services but with proper Kafka configuration
            var services = new ServiceCollection();
            services.AddLogging();
            // Add Orleans services but NOT monitoring services
            services.AddSingleton<Orleans.Serialization.Serializer>();
            services.AddSingleton<Orleans.Serialization.OrleansJsonSerializer>();
            services.AddSingleton<IGrainFactory, MockGrainFactory>();

            // Configure Kafka stream options properly to avoid ArgumentNullException
            var kafkaOptions = new KafkaStreamOptions
            {
                BrokerList = new[] { "localhost:9092" },
                ConsumerGroupId = "test-group"
            };
            kafkaOptions.AddTopic("test-topic");

            services.Configure<KafkaStreamOptions>("TestProvider", options =>
            {
                options.BrokerList = kafkaOptions.BrokerList;
                options.ConsumerGroupId = kafkaOptions.ConsumerGroupId;
                options.Topics = kafkaOptions.Topics;
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert - Should throw due to missing monitoring services
            var exception = Assert.Throws<InvalidOperationException>(() =>
                AevatarKafkaAdapterFactory.Create(serviceProvider, "TestProvider"));

            Assert.True(
                exception.Message.Contains("IMonitoredQueueCacheFactory") ||
                exception.Message.Contains("IStreamPressureMonitorFactory"),
                $"Expected error about missing monitoring services, but got: {exception.Message}");
        }

        [Fact]
        public void AevatarKafkaAdapterFactory_Create_WithAllRequiredServices_CreatesFactorySuccessfully()
        {
            // Arrange - All required services provided
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAevatarStreamingMonitoring();

            // Add required Orleans services
            services.AddSingleton<Orleans.Serialization.Serializer>();
            services.AddSingleton<Orleans.Serialization.OrleansJsonSerializer>();
            services.AddSingleton<IGrainFactory, MockGrainFactory>();

            // Configure Kafka stream options
            var kafkaOptions = new KafkaStreamOptions
            {
                BrokerList = new[] { "localhost:9092" },
                ConsumerGroupId = "test-group"
            };
            kafkaOptions.AddTopic("test-topic");

            services.Configure<KafkaStreamOptions>("TestProvider", options =>
            {
                options.BrokerList = kafkaOptions.BrokerList;
                options.ConsumerGroupId = kafkaOptions.ConsumerGroupId;
                options.Topics = kafkaOptions.Topics;
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act - Create factory (this tests our service registration logic)
            var factory = AevatarKafkaAdapterFactory.Create(serviceProvider, "TestProvider");

            // Assert - Factory should be created successfully
            Assert.NotNull(factory);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void StreamingIntegration_MultipleProviders_ConfiguresIndependently()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAevatarStreamingMonitoring();

            // Configure services for Kafka provider
            services.Configure<KafkaStreamOptions>("KafkaProvider", options =>
            {
                options.BrokerList = new[] { "localhost:9092" };
                options.ConsumerGroupId = "kafka-group";
            });

            // Configure services for Memory provider
            services.Configure<StreamCacheEvictionOptions>("MemoryProvider", options =>
            {
                options.DataMaxAgeInCache = TimeSpan.FromMinutes(5);
            });

            // Act
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            // Verify monitoring services are registered only once
            var monitorFactories = serviceProvider.GetServices<IAevatarStreamCacheMonitorFactory>().ToList();
            Assert.Single(monitorFactories);

            // Verify both providers can coexist
            Assert.NotNull(serviceProvider.GetService<IAevatarStreamCacheMonitorFactory>());
            Assert.NotNull(serviceProvider.GetService<IStreamPressureMonitorFactory>());
            Assert.NotNull(serviceProvider.GetService<IMonitoredQueueCacheFactory>());
        }

        #endregion

        #region Mock Implementations

        /// <summary>
        /// Mock implementation of IGrainFactory for testing purposes.
        /// </summary>
        private class MockGrainFactory : IGrainFactory
        {
            public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidKey
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerKey
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithStringKey
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidCompoundKey
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerCompoundKey
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey)
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey)
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey)
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension)
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension)
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId) where TGrainInterface : IAddressable
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public IAddressable GetGrain(GrainId grainId)
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public IAddressable GetGrain(GrainId grainId, GrainInterfaceType grainInterfaceType)
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public TGrainObserverInterface CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }

            public void DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
            {
                throw new NotImplementedException("Mock implementation for testing only");
            }


        }

        #endregion
    }
}