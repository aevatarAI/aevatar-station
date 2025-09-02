using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Aevatar.GAgents.Basic.BasicGAgents;
using BenchmarkDotNet.Attributes;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.Test;

/// <summary>
/// Benchmark tests for ConfigManagerGAgent performance using BenchmarkDotNet
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class ConfigManagerBenchmarks
{
    private string _smallConfig = string.Empty;
    private string _mediumConfig = string.Empty;
    private string _largeConfig = string.Empty;
    private BasicConfigStorageService _storageService = new();
    private byte[] _compressedMedium = Array.Empty<byte>();
    private byte[] _compressedLarge = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _smallConfig = GenerateTestConfig(512);      // 512 bytes
        _mediumConfig = GenerateTestConfig(10240);   // 10KB
        _largeConfig = GenerateTestConfig(102400);   // 100KB
        
        // Pre-compress for decompression benchmarks
        _compressedMedium = _storageService.CompressConfigAsync(_mediumConfig).Result;
        _compressedLarge = _storageService.CompressConfigAsync(_largeConfig).Result;
    }

    private static string GenerateTestConfig(int targetSize)
    {
        var config = new Dictionary<string, object>
        {
            ["metadata"] = new { version = "1.0.0", timestamp = DateTime.UtcNow },
            ["settings"] = new Dictionary<string, string>(),
            ["data"] = new List<object>()
        };

        var settings = (Dictionary<string, string>)config["settings"];
        var counter = 0;

        while (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(config)) < targetSize && counter < 1000)
        {
            settings[$"key_{counter:D4}"] = $"value_{counter:D4}_" + new string('x', 20);
            counter++;
        }

        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = false });
    }

    #region Storage Strategy Benchmarks

    [Benchmark]
    public ConfigStorageStrategy DetermineStorageStrategy_Small()
    {
        var size = Encoding.UTF8.GetByteCount(_smallConfig);
        return size < 1024 ? ConfigStorageStrategy.Inline : 
               size < 102400 ? ConfigStorageStrategy.Compressed : ConfigStorageStrategy.External;
    }

    [Benchmark]
    public ConfigStorageStrategy DetermineStorageStrategy_Medium()
    {
        var size = Encoding.UTF8.GetByteCount(_mediumConfig);
        return size < 1024 ? ConfigStorageStrategy.Inline : 
               size < 102400 ? ConfigStorageStrategy.Compressed : ConfigStorageStrategy.External;
    }

    [Benchmark]
    public ConfigStorageStrategy DetermineStorageStrategy_Large()
    {
        var size = Encoding.UTF8.GetByteCount(_largeConfig);
        return size < 1024 ? ConfigStorageStrategy.Inline : 
               size < 102400 ? ConfigStorageStrategy.Compressed : ConfigStorageStrategy.External;
    }

    #endregion

    #region Compression Benchmarks

    [Benchmark]
    public byte[] CompressConfig_Medium() => _storageService.CompressConfigAsync(_mediumConfig).Result;

    [Benchmark]
    public byte[] CompressConfig_Large() => _storageService.CompressConfigAsync(_largeConfig).Result;

    [Benchmark]
    public string DecompressConfig_Medium() => _storageService.DecompressConfigAsync(_compressedMedium).Result;

    [Benchmark]
    public string DecompressConfig_Large() => _storageService.DecompressConfigAsync(_compressedLarge).Result;

    #endregion

    #region Hash Computation Benchmarks

    [Benchmark]
    public string ComputeHash_Small() => ComputeConfigHash(_smallConfig);

    [Benchmark]
    public string ComputeHash_Medium() => ComputeConfigHash(_mediumConfig);

    [Benchmark]
    public string ComputeHash_Large() => ComputeConfigHash(_largeConfig);

    private static string ComputeConfigHash(string configJson)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(configJson);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }

    #endregion

    #region External Storage Benchmarks

    [Benchmark]
    public string StoreAndRetrieve_Small()
    {
        var key = _storageService.StoreConfigAsync(_smallConfig, "SmallConfig").Result;
        return _storageService.RetrieveConfigAsync(key).Result;
    }

    [Benchmark]
    public string StoreAndRetrieve_Medium()
    {
        var key = _storageService.StoreConfigAsync(_mediumConfig, "MediumConfig").Result;
        return _storageService.RetrieveConfigAsync(key).Result;
    }

    [Benchmark]
    public string StoreAndRetrieve_Large()
    {
        var key = _storageService.StoreConfigAsync(_largeConfig, "LargeConfig").Result;
        return _storageService.RetrieveConfigAsync(key).Result;
    }

    #endregion
}

/// <summary>
/// Test class to run benchmarks and verify performance characteristics
/// </summary>
public class ConfigManagerBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public ConfigManagerBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RunPerformanceBenchmarks()
    {
        // This test demonstrates how to run benchmarks
        // In actual usage, benchmarks would be run separately via dotnet run -c Release
        
        _output.WriteLine("ConfigManagerGAgent Performance Benchmarks");
        _output.WriteLine("==========================================");
        _output.WriteLine("");
        _output.WriteLine("To run full benchmarks, use:");
        _output.WriteLine("dotnet run -c Release --project test/Aevatar.GAgents.Basic.ConfigManagerGAgent.Test");
        _output.WriteLine("");
        _output.WriteLine("Benchmark categories:");
        _output.WriteLine("- Storage Strategy Selection");
        _output.WriteLine("- Compression/Decompression");
        _output.WriteLine("- Hash Computation");
        _output.WriteLine("- External Storage Operations");
        
        // Run a simple performance test here
        var benchmark = new ConfigManagerBenchmarks();
        benchmark.Setup();
        
        var sw = Stopwatch.StartNew();
        var strategy = benchmark.DetermineStorageStrategy_Medium();
        sw.Stop();
        
        _output.WriteLine($"\nSample result: Medium config strategy = {strategy} (computed in {sw.ElapsedTicks} ticks)");
    }

    [Fact]
    public void CompressionRatioAnalysis()
    {
        var benchmark = new ConfigManagerBenchmarks();
        benchmark.Setup();
        
        // Test compression ratios for different config sizes
        var testCases = new[]
        {
            ("1KB", GenerateTestConfig(1024)),
            ("5KB", GenerateTestConfig(5120)),
            ("10KB", GenerateTestConfig(10240)),
            ("50KB", GenerateTestConfig(51200)),
            ("100KB", GenerateTestConfig(102400))
        };

        var storageService = new BasicConfigStorageService();
        
        _output.WriteLine("Compression Analysis:");
        _output.WriteLine("Config Size\tOriginal\tCompressed\tRatio\tTime (ms)");
        
        foreach (var (name, config) in testCases)
        {
            var originalSize = Encoding.UTF8.GetByteCount(config);
            
            var sw = Stopwatch.StartNew();
            var compressed = storageService.CompressConfigAsync(config).Result;
            sw.Stop();
            
            var ratio = (double)compressed.Length / originalSize;
            
            _output.WriteLine($"{name}\t\t{originalSize:N0}\t\t{compressed.Length:N0}\t\t{ratio:P1}\t{sw.ElapsedMilliseconds}");
        }
    }

    [Fact]
    public void MemoryUsageComparison()
    {
        var testSizes = new[] { 1024, 5120, 10240, 51200, 102400, 512000 }; // 1KB to 500KB
        
        _output.WriteLine("Memory Usage Comparison (Old vs New Approach):");
        _output.WriteLine("Config Size\tOld Memory\tNew Memory\tReduction\tStrategy");
        
        foreach (var size in testSizes)
        {
            var config = GenerateTestConfig(size);
            var actualSize = Encoding.UTF8.GetByteCount(config);
            
            // Old approach: Full JSON in state + 2 events
            var oldMemory = actualSize * 3;
            
            // New approach: Depends on strategy
            var strategy = actualSize < 1024 
                ? ConfigStorageStrategy.Inline
                : actualSize < 102400 
                    ? ConfigStorageStrategy.Compressed 
                    : ConfigStorageStrategy.External;
            
            var newMemory = strategy switch
            {
                ConfigStorageStrategy.Inline => actualSize + 200,
                ConfigStorageStrategy.Compressed => (actualSize / 3) + 200, // Assume 3:1 compression
                ConfigStorageStrategy.External => 200,
                _ => actualSize
            };
            
            var reduction = (double)(oldMemory - newMemory) / oldMemory;
            
            _output.WriteLine($"{actualSize:N0}\t\t{oldMemory:N0}\t\t{newMemory:N0}\t\t{reduction:P1}\t\t{strategy}");
        }
    }

    private static string GenerateTestConfig(int targetSize)
    {
        var config = new Dictionary<string, object>
        {
            ["metadata"] = new { version = "1.0.0", timestamp = DateTime.UtcNow },
            ["settings"] = new Dictionary<string, string>(),
            ["data"] = new List<object>()
        };

        var settings = (Dictionary<string, string>)config["settings"];
        var counter = 0;

        while (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(config)) < targetSize && counter < 10000)
        {
            settings[$"key_{counter:D4}"] = $"value_{counter:D4}_" + new string('x', 20);
            counter++;
        }

        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = false });
    }
}