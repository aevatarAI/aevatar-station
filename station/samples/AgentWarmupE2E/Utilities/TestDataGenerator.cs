using System.Security.Cryptography;
using System.Text;

namespace AgentWarmupE2E.Utilities;

/// <summary>
/// Utility class for generating consistent test data
/// </summary>
public class TestDataGenerator
{
    private static readonly Random _random = new(42); // Fixed seed for reproducible tests

    /// <summary>
    /// Generates a deterministic GUID based on an index
    /// </summary>
    public static Guid GenerateTestGuid(int index)
    {
        // Create deterministic GUID based on index
        var bytes = new byte[16];
        var indexBytes = BitConverter.GetBytes(index);
        
        // Fill first 4 bytes with index
        Array.Copy(indexBytes, 0, bytes, 0, 4);
        
        // Fill remaining bytes with deterministic pattern
        for (int i = 4; i < 16; i++)
        {
            bytes[i] = (byte)((index + i) % 256);
        }
        
        return new Guid(bytes);
    }

    /// <summary>
    /// Generates a GUID with a specific prefix for categorization
    /// </summary>
    public static Guid GenerateGuidWithPrefix(string prefix, int index)
    {
        using var sha256 = SHA256.Create();
        var input = $"{prefix}-{index}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        
        // Take first 16 bytes of hash to create GUID
        var guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);
        
        return new Guid(guidBytes);
    }

    /// <summary>
    /// Generates a range of test GUIDs
    /// </summary>
    public static List<Guid> GenerateTestGuidRange(int startIndex, int count)
    {
        var guids = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            guids.Add(GenerateTestGuid(startIndex + i));
        }
        return guids;
    }

    /// <summary>
    /// Generates GUIDs for different test categories
    /// </summary>
    public static class TestCategories
    {
        public static List<Guid> GenerateBasicTestAgents(int count) =>
            Enumerable.Range(0, count)
                .Select(i => GenerateGuidWithPrefix("basic", i))
                .ToList();

        public static List<Guid> GeneratePerformanceTestAgents(int count) =>
            Enumerable.Range(0, count)
                .Select(i => GenerateGuidWithPrefix("perf", i))
                .ToList();

        public static List<Guid> GenerateWarmupTestAgents(int count) =>
            Enumerable.Range(0, count)
                .Select(i => GenerateGuidWithPrefix("warmup", i))
                .ToList();

        public static List<Guid> GenerateColdTestAgents(int count) =>
            Enumerable.Range(0, count)
                .Select(i => GenerateGuidWithPrefix("cold", i))
                .ToList();

        public static List<Guid> GenerateErrorTestAgents(int count) =>
            Enumerable.Range(0, count)
                .Select(i => GenerateGuidWithPrefix("error", i))
                .ToList();

        public static List<Guid> GenerateLargeScaleTestAgents(int count) =>
            Enumerable.Range(0, count)
                .Select(i => GenerateGuidWithPrefix("large", i))
                .ToList();
    }

    /// <summary>
    /// Generates test data sets for different test scenarios
    /// </summary>
    public static class TestDataSets
    {
        public static (List<Guid> warmed, List<Guid> cold) GenerateLatencyComparisonSet(int count)
        {
            var warmed = TestCategories.GenerateWarmupTestAgents(count);
            var cold = TestCategories.GenerateColdTestAgents(count);
            return (warmed, cold);
        }

        public static List<Guid> GenerateConcurrentAccessSet(int threadCount, int agentsPerThread)
        {
            var agents = new List<Guid>();
            for (int thread = 0; thread < threadCount; thread++)
            {
                for (int agent = 0; agent < agentsPerThread; agent++)
                {
                    agents.Add(GenerateGuidWithPrefix($"concurrent-t{thread}", agent));
                }
            }
            return agents;
        }

        public static List<Guid> GenerateProgressiveWarmupSet(int totalCount, int batchCount)
        {
            var agents = new List<Guid>();
            var agentsPerBatch = totalCount / batchCount;
            
            for (int batch = 0; batch < batchCount; batch++)
            {
                for (int agent = 0; agent < agentsPerBatch; agent++)
                {
                    agents.Add(GenerateGuidWithPrefix($"progressive-b{batch}", agent));
                }
            }
            
            return agents;
        }
    }

    /// <summary>
    /// Shuffles a list using Fisher-Yates algorithm with fixed seed
    /// </summary>
    public static List<T> Shuffle<T>(IEnumerable<T> source)
    {
        var list = source.ToList();
        var random = new Random(42); // Fixed seed for reproducible results
        
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        
        return list;
    }

    /// <summary>
    /// Splits a list into chunks of specified size
    /// </summary>
    public static List<List<T>> ChunkList<T>(IEnumerable<T> source, int chunkSize)
    {
        var list = source.ToList();
        var chunks = new List<List<T>>();
        
        for (int i = 0; i < list.Count; i += chunkSize)
        {
            chunks.Add(list.Skip(i).Take(chunkSize).ToList());
        }
        
        return chunks;
    }

    /// <summary>
    /// Generates random delays for testing timing scenarios
    /// </summary>
    public static List<int> GenerateRandomDelays(int count, int minMs, int maxMs)
    {
        var delays = new List<int>();
        for (int i = 0; i < count; i++)
        {
            delays.Add(_random.Next(minMs, maxMs + 1));
        }
        return delays;
    }
} 