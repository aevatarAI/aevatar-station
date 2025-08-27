using System;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Serialization;
using Orleans.Streams.Kafka.Config;
using Aevatar.Core.Abstractions;

namespace VerifySerializerPerformance;

/// <summary>
/// Main serialization test program to compare Orleans and System.Text.Json serializers
/// </summary>
public class Program
{
    // Constants for serialization testing
    private const int TEST_OBJECT_SIZE_MB = 7; // Size in MB for large test objects
    private const int BYTES_PER_MB = 1024 * 1024;
    private const int PRODUCER_COUNT = 50;
    // Default consumer count (will be overridden in tests)
    private const int CONSUMER_COUNT = 2000;
    private const int METADATA_ENTRIES = 500;
    
    // Array of consumer counts to test
    private static readonly int[] CONSUMER_COUNT_VALUES = { 500, 1000, 2000, 4000, 8000, 16000 };
    
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Orleans Serializer Performance Tests (Large Objects)");
        Console.WriteLine("==================================================");
        
        // Test with different object sizes by varying consumer count
        await TestWithDifferentSizes();
        
        Console.WriteLine("\nPress Enter to run benchmarks...");
        Console.ReadLine();
        
        // Run benchmarks with BenchmarkDotNet
        BenchmarkRunner.Run<SerializerBenchmarks>();
    }
    
    /// <summary>
    /// Tests serializer performance with different object sizes
    /// </summary>
    private static async Task TestWithDifferentSizes()
    {
        Console.WriteLine("Setting up Orleans Client...");
        
        // Create host builder with Orleans client
        var builder = HostHelperExtensions.CreateHostBuilder();
        using IHost host = builder.Build();
        await host.StartAsync();
        
        try
        {
            Console.WriteLine("\nComparing serializer performance with different object sizes:");
            Console.WriteLine("--------------------------------------------------------------------------------------------");
            Console.WriteLine("| Consumer Count | Size (MB) | Orleans | System.Text.Json | Binary | BSON | Newtonsoft.Json |");
            Console.WriteLine("|---------------|-----------|---------|------------------|--------|------|-----------------|");
            
            var client = host.Services.GetRequiredService<IClusterClient>();
            var orleansSerializer = host.Services.GetRequiredService<Orleans.Serialization.Serializer>();
            
            // Create MongoDB serializers
            var binarySerializer = new BinaryGrainStateSerializer(orleansSerializer);
            var bsonSerializer = new BsonGrainStateSerializer();
            
            // Create JsonGrainStateSerializer which requires IOptions<JsonGrainStateSerializerOptions>
            var jsonSerializerOptions = Options.Create(new JsonGrainStateSerializerOptions());
            var jsonSerializer = new JsonGrainStateSerializer(jsonSerializerOptions, host.Services.GetRequiredService<IServiceProvider>());
            
            // Test with different consumer counts
            foreach (int consumerCount in CONSUMER_COUNT_VALUES)
            {
                // Create test object with specified consumer count
                var testObject = CreateSampleBasedTestObject(consumerCount);
                
                // Measure serialized size
                var jsonString = JsonSerializer.Serialize(testObject);
                double actualSizeMB = jsonString.Length / (double)BYTES_PER_MB;
                
                // Test Orleans serializer
                var orleansResult = await MeasureOrleansSerializerPerformance(orleansSerializer, testObject);
                
                // Test System.Text.Json
                var jsonResult = MeasureSystemTextJsonPerformance(testObject);
                
                // Test MongoDB serializers
                var binaryResult = MeasureMongoDBSerializerPerformance(binarySerializer, testObject);
                var bsonResult = MeasureMongoDBSerializerPerformance(bsonSerializer, testObject);
                var newtonsoftResult = MeasureMongoDBSerializerPerformance(jsonSerializer, testObject);
                
                // Output results (showing total round-trip time in ms)
                Console.WriteLine($"| {consumerCount,13} | {actualSizeMB,9:F2} | {orleansResult.TotalMs,7:F1} | " +
                                 $"{jsonResult.TotalMs,16:F1} | {binaryResult.TotalMs,6:F1} | " +
                                 $"{bsonResult.TotalMs,4:F1} | {newtonsoftResult.TotalMs,15:F1} |");
                
                // Force GC to clean up large objects
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            
            Console.WriteLine("--------------------------------------------------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during test: {ex}");
        }
        
        await host.StopAsync();
    }
    
    /// <summary>
    /// Creates a test object with structure similar to the sample.json file
    /// </summary>
    private static PubSubGrainStateDocument CreateSampleBasedTestObject(int consumerCount = CONSUMER_COUNT)
    {
        // Create a random number generator with fixed seed for reproducibility
        Random random = new Random(42);
        
        // Build a document structure similar to what's seen in sample.json
        var document = new PubSubGrainStateDocument
        {
            Id = "pubsubrendezvous/Aevatar/AevatarBroadCast/TestDbScheduleGAgent.TestDbEvent",
            Etag = Guid.NewGuid().ToString(),
            Doc = new PubSubGrainState
            {
                Id = "1",
                Type = "Orleans.Streams.PubSubGrainState, Orleans.Streaming",
                Producers = new HashSetContainer<PubSubPublisherState>
                {
                    Type = "System.Collections.Generic.HashSet`1[[Orleans.Streams.PubSubPublisherState, Orleans.Streaming]], System.Private.CoreLib",
                    Values = new List<PubSubPublisherState>()
                },
                Consumers = new HashSetContainer<PubSubSubscriptionState>
                {
                    Type = "System.Collections.Generic.HashSet`1[[Orleans.Streams.PubSubSubscriptionState, Orleans.Streaming]], System.Private.CoreLib",
                    Values = new List<PubSubSubscriptionState>()
                },
                Metadata = new Dictionary<string, MetadataEntry>()
            }
        };
        
        // Add producers
        for (int i = 0; i < PRODUCER_COUNT; i++)
        {
            document.Doc.Producers.Values.Add(new PubSubPublisherState
            {
                Id = $"{i + 100}",
                Type = "Orleans.Streams.PubSubPublisherState, Orleans.Streaming",
                Producer = new StreamProducer
                {
                    Type = $"producer{i}",
                    Key = Guid.NewGuid().ToString("N")
                },
                Stream = CreateStreamReference(random, i),
                LastTimeUsed = DateTime.UtcNow.AddMinutes(-random.Next(0, 1000)).ToString("O")
            });
        }
        
        // Add consumers (most of the data volume) - use the specified count
        for (int i = 0; i < consumerCount; i++)
        {
            document.Doc.Consumers.Values.Add(new PubSubSubscriptionState
            {
                Id = $"{i + 1000}",
                Type = "Orleans.Streams.PubSubSubscriptionState, Orleans.Streaming",
                SubscriptionId = new GuidId
                {
                    Id = $"{i + 2000}",
                    Type = "Orleans.Runtime.GuidId, Orleans.Core.Abstractions",
                    Guid = Guid.NewGuid()
                },
                Stream = CreateStreamReference(random, i),
                Consumer = new StreamConsumer
                {
                    Type = $"testdbgagent{i % 5}",
                    Key = Guid.NewGuid().ToString("N")
                },
                FilterData = i % 3 == 0 ? CreateFilterData(random, i) : null,
                State = random.Next(0, 3)
            });
        }
        
        // Add metadata entries
        for (int i = 0; i < METADATA_ENTRIES; i++)
        {
            // Generate random binary data for some metadata entries
            byte[] binaryData = new byte[random.Next(500, 2000)];
            random.NextBytes(binaryData);
            
            document.Doc.Metadata[$"meta_key_{i}"] = new MetadataEntry
            {
                Name = $"MetadataEntry_{i}",
                Value = Convert.ToBase64String(binaryData),
                Timestamp = DateTime.UtcNow.AddDays(-random.Next(0, 365)).ToString("O"),
                Tags = CreateTags(random, i)
            };
        }
        
        return document;
    }
    
    /// <summary>
    /// Measure Orleans serializer performance
    /// </summary>
    private static async Task<(double SerializeMs, double DeserializeMs, double TotalMs, double SizeMB)> MeasureOrleansSerializerPerformance(
        Orleans.Serialization.Serializer serializer, PubSubGrainStateDocument testObject)
    {
        var sw = Stopwatch.StartNew();
        byte[] bytes = serializer.SerializeToArray(testObject);
        double serializeMs = sw.ElapsedMilliseconds;
        
        sw.Restart();
        var deserialized = serializer.Deserialize<PubSubGrainStateDocument>(bytes);
        double deserializeMs = sw.ElapsedMilliseconds;
        
        double sizeMB = bytes.Length / (double)BYTES_PER_MB;
        
        return (serializeMs, deserializeMs, serializeMs + deserializeMs, sizeMB);
    }
    
    /// <summary>
    /// Measure System.Text.Json performance
    /// </summary>
    private static (double SerializeMs, double DeserializeMs, double TotalMs, double SizeMB) MeasureSystemTextJsonPerformance(
        PubSubGrainStateDocument testObject)
    {
        var sw = Stopwatch.StartNew();
        var jsonString = JsonSerializer.Serialize(testObject);
        double serializeMs = sw.ElapsedMilliseconds;
        
        sw.Restart();
        var jsonDeserialized = JsonSerializer.Deserialize<PubSubGrainStateDocument>(jsonString);
        double deserializeMs = sw.ElapsedMilliseconds;
        
        double sizeMB = jsonString.Length / (double)BYTES_PER_MB;
        
        return (serializeMs, deserializeMs, serializeMs + deserializeMs, sizeMB);
    }
    
    /// <summary>
    /// Measure MongoDB serializer performance
    /// </summary>
    private static (double SerializeMs, double DeserializeMs, double TotalMs, double SizeMB) MeasureMongoDBSerializerPerformance(
        IGrainStateSerializer serializer, PubSubGrainStateDocument testObject)
    {
        var sw = Stopwatch.StartNew();
        var bsonValue = serializer.Serialize(testObject);
        double serializeMs = sw.ElapsedMilliseconds;
        
        sw.Restart();
        var deserialized = serializer.Deserialize<PubSubGrainStateDocument>(bsonValue);
        double deserializeMs = sw.ElapsedMilliseconds;
        
        // Calculate the size of the serialized data
        double sizeMB = 0;
        if (bsonValue.IsBsonDocument)
        {
            sizeMB = bsonValue.AsBsonDocument.ToBson().Length / (double)BYTES_PER_MB;
        }
        else if (bsonValue.IsBsonBinaryData)
        {
            sizeMB = bsonValue.AsBsonBinaryData.Bytes.Length / (double)BYTES_PER_MB;
        }
        
        return (serializeMs, deserializeMs, serializeMs + deserializeMs, sizeMB);
    }
    
    private static QualifiedStreamId CreateStreamReference(Random random, int index)
    {
        // Create a byte array to simulate binary data
        byte[] binaryData = new byte[random.Next(50, 200)];
        random.NextBytes(binaryData);
        
        return new QualifiedStreamId
        {
            Id = $"{index + 3000}",
            Type = "Orleans.Runtime.QualifiedStreamId, Orleans.Streaming",
            ProviderName = "Aevatar",
            StreamId = new StreamId
            {
                Id = $"{index + 4000}",
                Type = "Orleans.Runtime.StreamId, Orleans.Streaming",
                FullKey = new BinaryData
                {
                    Type = "System.Byte[], System.Private.CoreLib",
                    Value = new BinaryValue
                    {
                        Binary = new BinaryInfo
                        {
                            Base64 = Convert.ToBase64String(binaryData),
                            SubType = "00"
                        }
                    }
                },
                KeyIndex = random.Next(10, 30),
                HashCode = random.Next(-2000000000, 2000000000)
            }
        };
    }
    
    private static FilterData CreateFilterData(Random random, int index)
    {
        // Create a filter with some random properties
        var filter = new FilterData
        {
            Id = $"{index + 5000}",
            Type = "CustomFilter",
            Properties = new Dictionary<string, string>()
        };
        
        // Add some properties to the filter
        int propertyCount = random.Next(3, 10);
        for (int i = 0; i < propertyCount; i++)
        {
            filter.Properties[$"prop_{i}"] = $"value_{random.Next(1000, 9999)}";
        }
        
        return filter;
    }
    
    private static List<string> CreateTags(Random random, int index)
    {
        var tags = new List<string>();
        int tagCount = random.Next(1, 5);
        
        for (int i = 0; i < tagCount; i++)
        {
            tags.Add($"tag_{index}_{i}_{random.Next(1000, 9999)}");
        }
        
        return tags;
    }
}

public static class HostHelperExtensions
{
        /// <summary>
    /// Creates a host builder configured with Orleans
    /// </summary>
    public static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .UseOrleansClient(client =>
            {
                //client.UseLocalhostClustering();
                var hostId = "Aevatar";
                client.UseMongoDBClient("mongodb://localhost:27017/?maxPoolSize=15000")
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = "AevatarDb";
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                        options.CollectionPrefix = hostId.IsNullOrEmpty() ? "OrleansAevatar" : $"Orleans{hostId}";
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "AevatarSiloCluster";
                        options.ServiceId = "AevatarBasicService";
                    })
                    .AddActivityPropagation()

                // client.UseLocalhostClustering(gatewayPort: 20001)
                    // .AddMemoryStreams(AevatarCoreConstants.StreamProvider);
                    .AddKafka("Aevatar")
                    .WithOptions(options =>
                    {
                        options.BrokerList = new List<string> { "localhost:9092" };  // BrokerList expects List<string>
                        options.ConsumerGroupId = "Aevatar";
                        options.ConsumeMode = ConsumeMode.LastCommittedMessage;

                        var partitions = 1;
                        var replicationFactor = (short)1;  // ReplicationFactor should be short
                        var topics = "Aevatar,AevatarStateProjection,AevatarBroadCast";
                        foreach (var topic in topics.Split(','))
                        {
                            options.AddTopic(topic.Trim(), new TopicCreationConfig
                            {
                                AutoCreate = true,
                                Partitions = partitions,
                                ReplicationFactor = replicationFactor
                            });
                        }
                    })
                    .AddJson()  // Add logging tracker for better observability
                    .Build();
            })
            .ConfigureLogging(logging => logging.AddConsole())
            .UseConsoleLifetime();
    }
    
}
/// <summary>
/// Classes that model the structure observed in sample.json
/// </summary>
[GenerateSerializer]
public class PubSubGrainStateDocument
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Etag { get; set; } = string.Empty;
    [Id(2)] public PubSubGrainState Doc { get; set; } = new();
}

[GenerateSerializer]
public class PubSubGrainState
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public HashSetContainer<PubSubPublisherState> Producers { get; set; } = new();
    [Id(3)] public HashSetContainer<PubSubSubscriptionState> Consumers { get; set; } = new();
    [Id(4)] public Dictionary<string, MetadataEntry> Metadata { get; set; } = new();
}

[GenerateSerializer]
public class HashSetContainer<T>
{
    [Id(0)] public string Type { get; set; } = string.Empty;
    [Id(1)] public List<T> Values { get; set; } = new();
}

[GenerateSerializer]
public class PubSubPublisherState
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public StreamProducer Producer { get; set; } = new();
    [Id(3)] public QualifiedStreamId Stream { get; set; } = new();
    [Id(4)] public string LastTimeUsed { get; set; } = string.Empty;
}

[GenerateSerializer]
public class PubSubSubscriptionState
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public GuidId SubscriptionId { get; set; } = new();
    [Id(3)] public QualifiedStreamId Stream { get; set; } = new();
    [Id(4)] public StreamConsumer Consumer { get; set; } = new();
    [Id(5)] public FilterData? FilterData { get; set; }
    [Id(6)] public int State { get; set; }
}

[GenerateSerializer]
public class GuidId
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public Guid Guid { get; set; }
}

[GenerateSerializer]
public class QualifiedStreamId
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public string ProviderName { get; set; } = string.Empty;
    [Id(3)] public StreamId StreamId { get; set; } = new();
}

[GenerateSerializer]
public class StreamId
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public BinaryData FullKey { get; set; } = new();
    [Id(3)] public int KeyIndex { get; set; }
    [Id(4)] public int HashCode { get; set; }
}

[GenerateSerializer]
public class BinaryData
{
    [Id(0)] public string Type { get; set; } = string.Empty;
    [Id(1)] public BinaryValue Value { get; set; } = new();
}

[GenerateSerializer]
public class BinaryValue
{
    [Id(0)] public BinaryInfo Binary { get; set; } = new();
}

[GenerateSerializer]
public class BinaryInfo
{
    [Id(0)] public string Base64 { get; set; } = string.Empty;
    [Id(1)] public string SubType { get; set; } = string.Empty;
}

[GenerateSerializer]
public class StreamProducer
{
    [Id(0)] public string Type { get; set; } = string.Empty;
    [Id(1)] public string Key { get; set; } = string.Empty;
}

[GenerateSerializer]
public class StreamConsumer
{
    [Id(0)] public string Type { get; set; } = string.Empty;
    [Id(1)] public string Key { get; set; } = string.Empty;
}

[GenerateSerializer]
public class FilterData
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, string> Properties { get; set; } = new();
}

[GenerateSerializer]
public class MetadataEntry
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string Value { get; set; } = string.Empty;
    [Id(2)] public string Timestamp { get; set; } = string.Empty;
    [Id(3)] public List<string> Tags { get; set; } = new();
}

/// <summary>
/// BenchmarkDotNet benchmark class for comparing serializer performance
/// </summary>
public class SerializerBenchmarks : IDisposable
{
    private readonly Orleans.Serialization.Serializer _orleansSerializer;
    private readonly PubSubGrainStateDocument _testObject;
    private readonly BinaryGrainStateSerializer _binarySerializer;
    private readonly BsonGrainStateSerializer _bsonSerializer;
    private readonly JsonGrainStateSerializer _jsonSerializer;
    private const int BYTES_PER_MB = 1024 * 1024;
    
    // Constants for creating test objects
    private const int PRODUCER_COUNT = 50;
    private const int CONSUMER_COUNT = 2000;
    private const int METADATA_ENTRIES = 500;

    private IHost _host;

    // For larger objects, use fewer operations
    [Params(5)]
    public int OperationCount { get; set; }

    public void Dispose()
    {
        _host?.StopAsync().Wait();
        _host?.Dispose();
    }

    public SerializerBenchmarks()
    {
        Console.WriteLine("Setting up Orleans Client...");
        
        // Create host builder with Orleans client
        var builder = HostHelperExtensions.CreateHostBuilder();
        _host = builder.Build();
        _host.StartAsync().Wait();
        var serviceProvider = _host.Services.GetRequiredService<IServiceProvider>();
        
        _orleansSerializer = _host.Services.GetRequiredService<Orleans.Serialization.Serializer>();
        Console.WriteLine("Created {0} ", _orleansSerializer);
        
        
        // Initialize MongoDB serializers
        _binarySerializer = new BinaryGrainStateSerializer(_orleansSerializer);
        _bsonSerializer = new BsonGrainStateSerializer();
        
        // Create JsonGrainStateSerializer which requires IOptions<JsonGrainStateSerializerOptions>
        var jsonSerializerOptions = Options.Create(new JsonGrainStateSerializerOptions());
        _jsonSerializer = new JsonGrainStateSerializer(jsonSerializerOptions, serviceProvider);
        
        // Create large test object similar to sample.json
        _testObject = CreateSampleBasedTestObject();
        
        // Check actual size
        var jsonString = JsonSerializer.Serialize(_testObject);
        double actualSizeMB = jsonString.Length / (double)BYTES_PER_MB;
        
        Console.WriteLine($"Created test object with structure similar to sample.json");
        Console.WriteLine($"Actual serialized size: {actualSizeMB:F2} MB");
    }

    /// <summary>
    /// Creates a test object similar to sample.json for benchmark testing
    /// </summary>
    private PubSubGrainStateDocument CreateSampleBasedTestObject()
    {
        // Create a random number generator with fixed seed for reproducibility
        Random random = new Random(42);
        
        // Build a document structure similar to what's seen in sample.json
        var document = new PubSubGrainStateDocument
        {
            Id = "pubsubrendezvous/Aevatar/AevatarBroadCast/TestDbScheduleGAgent.TestDbEvent",
            Etag = Guid.NewGuid().ToString(),
            Doc = new PubSubGrainState
            {
                Id = "1",
                Type = "Orleans.Streams.PubSubGrainState, Orleans.Streaming",
                Producers = new HashSetContainer<PubSubPublisherState>
                {
                    Type = "System.Collections.Generic.HashSet`1[[Orleans.Streams.PubSubPublisherState, Orleans.Streaming]], System.Private.CoreLib",
                    Values = new List<PubSubPublisherState>()
                },
                Consumers = new HashSetContainer<PubSubSubscriptionState>
                {
                    Type = "System.Collections.Generic.HashSet`1[[Orleans.Streams.PubSubSubscriptionState, Orleans.Streaming]], System.Private.CoreLib",
                    Values = new List<PubSubSubscriptionState>()
                },
                Metadata = new Dictionary<string, MetadataEntry>()
            }
        };
        
        // Add producers
        for (int i = 0; i < PRODUCER_COUNT; i++)
        {
            document.Doc.Producers.Values.Add(new PubSubPublisherState
            {
                Id = $"{i + 100}",
                Type = "Orleans.Streams.PubSubPublisherState, Orleans.Streaming",
                Producer = new StreamProducer
                {
                    Type = $"producer{i}",
                    Key = Guid.NewGuid().ToString("N")
                },
                Stream = CreateStreamReference(random, i),
                LastTimeUsed = DateTime.UtcNow.AddMinutes(-random.Next(0, 1000)).ToString("O")
            });
        }
        
        // Add consumers (most of the data volume)
        for (int i = 0; i < CONSUMER_COUNT; i++)
        {
            document.Doc.Consumers.Values.Add(new PubSubSubscriptionState
            {
                Id = $"{i + 1000}",
                Type = "Orleans.Streams.PubSubSubscriptionState, Orleans.Streaming",
                SubscriptionId = new GuidId
                {
                    Id = $"{i + 2000}",
                    Type = "Orleans.Runtime.GuidId, Orleans.Core.Abstractions",
                    Guid = Guid.NewGuid()
                },
                Stream = CreateStreamReference(random, i),
                Consumer = new StreamConsumer
                {
                    Type = $"testdbgagent{i % 5}",
                    Key = Guid.NewGuid().ToString("N")
                },
                FilterData = i % 3 == 0 ? CreateFilterData(random, i) : null,
                State = random.Next(0, 3)
            });
        }
        
        // Add metadata entries
        for (int i = 0; i < METADATA_ENTRIES; i++)
        {
            // Generate random binary data for some metadata entries
            byte[] binaryData = new byte[random.Next(500, 2000)];
            random.NextBytes(binaryData);
            
            document.Doc.Metadata[$"meta_key_{i}"] = new MetadataEntry
            {
                Name = $"MetadataEntry_{i}",
                Value = Convert.ToBase64String(binaryData),
                Timestamp = DateTime.UtcNow.AddDays(-random.Next(0, 365)).ToString("O"),
                Tags = CreateTags(random, i)
            };
        }
        
        return document;
    }
    
    private QualifiedStreamId CreateStreamReference(Random random, int index)
    {
        // Create a byte array to simulate binary data
        byte[] binaryData = new byte[random.Next(50, 200)];
        random.NextBytes(binaryData);
        
        return new QualifiedStreamId
        {
            Id = $"{index + 3000}",
            Type = "Orleans.Runtime.QualifiedStreamId, Orleans.Streaming",
            ProviderName = "Aevatar",
            StreamId = new StreamId
            {
                Id = $"{index + 4000}",
                Type = "Orleans.Runtime.StreamId, Orleans.Streaming",
                FullKey = new BinaryData
                {
                    Type = "System.Byte[], System.Private.CoreLib",
                    Value = new BinaryValue
                    {
                        Binary = new BinaryInfo
                        {
                            Base64 = Convert.ToBase64String(binaryData),
                            SubType = "00"
                        }
                    }
                },
                KeyIndex = random.Next(10, 30),
                HashCode = random.Next(-2000000000, 2000000000)
            }
        };
    }
    
    private FilterData CreateFilterData(Random random, int index)
    {
        // Create a filter with some random properties
        var filter = new FilterData
        {
            Id = $"{index + 5000}",
            Type = "CustomFilter",
            Properties = new Dictionary<string, string>()
        };
        
        // Add some properties to the filter
        int propertyCount = random.Next(3, 10);
        for (int i = 0; i < propertyCount; i++)
        {
            filter.Properties[$"prop_{i}"] = $"value_{random.Next(1000, 9999)}";
        }
        
        return filter;
    }
    
    private List<string> CreateTags(Random random, int index)
    {
        var tags = new List<string>();
        int tagCount = random.Next(1, 5);
        
        for (int i = 0; i < tagCount; i++)
        {
            tags.Add($"tag_{index}_{i}_{random.Next(1000, 9999)}");
        }
        
        return tags;
    }

    /// <summary>
    /// Benchmarks Orleans serializer round-trip performance
    /// </summary>
    [Benchmark(Baseline = true)]
    public void OrleansSerializerRoundtrip()
    {
        for (int i = 0; i < OperationCount; i++)
        {
            // Serialize
            var bytes = _orleansSerializer.SerializeToArray(_testObject);
            
            // Deserialize
            var deserialized = _orleansSerializer.Deserialize<PubSubGrainStateDocument>(bytes);
        }
    }

    /// <summary>
    /// Benchmarks System.Text.Json round-trip performance
    /// </summary>
    [Benchmark]
    public void SystemTextJsonRoundtrip()
    {
        for (int i = 0; i < OperationCount; i++)
        {
            // Serialize
            var jsonString = JsonSerializer.Serialize(_testObject);
            
            // Deserialize
            var deserialized = JsonSerializer.Deserialize<PubSubGrainStateDocument>(jsonString);
        }
    }
    
    /// <summary>
    /// Benchmarks BinaryGrainStateSerializer round-trip performance
    /// </summary>
    [Benchmark]
    public void BinaryGrainStateSerializerRoundtrip()
    {
        for (int i = 0; i < OperationCount; i++)
        {
            // Serialize
            var bsonValue = _binarySerializer.Serialize(_testObject);
            
            // Deserialize
            var deserialized = _binarySerializer.Deserialize<PubSubGrainStateDocument>(bsonValue);
        }
    }
    
    /// <summary>
    /// Benchmarks BsonGrainStateSerializer round-trip performance
    /// </summary>
    [Benchmark]
    public void BsonGrainStateSerializerRoundtrip()
    {
        for (int i = 0; i < OperationCount; i++)
        {
            // Serialize
            var bsonValue = _bsonSerializer.Serialize(_testObject);
            
            // Deserialize
            var deserialized = _bsonSerializer.Deserialize<PubSubGrainStateDocument>(bsonValue);
        }
    }
    
    /// <summary>
    /// Benchmarks JsonGrainStateSerializer round-trip performance
    /// </summary>
    [Benchmark]
    public void JsonGrainStateSerializerRoundtrip()
    {
        for (int i = 0; i < OperationCount; i++)
        {
            // Serialize
            var bsonValue = _jsonSerializer.Serialize(_testObject);
            
            // Deserialize
            var deserialized = _jsonSerializer.Deserialize<PubSubGrainStateDocument>(bsonValue);
        }
    }
} 