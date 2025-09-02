using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Aevatar.GAgents.Basic.BasicGAgents;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.Test;

/// <summary>
/// Unit tests for IConfigStorageService and BasicConfigStorageService
/// </summary>
public class ConfigStorageServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IConfigStorageService _storageService;

    public ConfigStorageServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _storageService = new BasicConfigStorageService();
    }

    #region External Storage Tests

    [Fact]
    public async Task BasicConfigStorageService_StoreAndRetrieve_ShouldWorkCorrectly()
    {
        // Arrange
        var testConfig = CreateTestConfig("StoreRetrieveTest", 1024);
        var configType = "TestConfig";

        // Act
        var storageKey = await _storageService.StoreConfigAsync(testConfig, configType);
        var retrievedConfig = await _storageService.RetrieveConfigAsync(storageKey);

        // Assert
        storageKey.ShouldNotBeNullOrEmpty();
        storageKey.ShouldStartWith(configType);
        retrievedConfig.ShouldBe(testConfig);

        _testOutputHelper.WriteLine($"Successfully stored and retrieved config with key: {storageKey}");
    }

    [Fact]
    public async Task BasicConfigStorageService_DeleteConfig_ShouldWorkCorrectly()
    {
        // Arrange
        var testConfig = CreateTestConfig("DeleteTest", 1024);
        var configType = "TestConfig";

        var storageKey = await _storageService.StoreConfigAsync(testConfig, configType);

        // Act
        var deleteResult = await _storageService.DeleteConfigAsync(storageKey);

        // Assert
        deleteResult.ShouldBeTrue();

        // Verify config is actually deleted
        var retrievedConfig = await _storageService.RetrieveConfigAsync(storageKey);
        retrievedConfig.ShouldBeNull();

        _testOutputHelper.WriteLine($"Successfully deleted config with key: {storageKey}");
    }

    [Fact]
    public async Task BasicConfigStorageService_RetrieveNonExistentConfig_ShouldReturnNull()
    {
        // Arrange
        var nonExistentKey = "NonExistent_12345";

        // Act
        var result = await _storageService.RetrieveConfigAsync(nonExistentKey);

        // Assert
        result.ShouldBeNull();

        _testOutputHelper.WriteLine("Non-existent config correctly returned null");
    }

    [Fact]
    public async Task BasicConfigStorageService_DeleteNonExistentConfig_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentKey = "NonExistent_12345";

        // Act
        var result = await _storageService.DeleteConfigAsync(nonExistentKey);

        // Assert
        result.ShouldBeFalse();

        _testOutputHelper.WriteLine("Non-existent config deletion correctly returned false");
    }

    [Fact]
    public async Task BasicConfigStorageService_LargeConfig_ShouldHandleCorrectly()
    {
        // Arrange
        var largeConfig = CreateTestConfig("LargeConfigTest", 500 * 1024); // 500KB
        var configType = "LargeTestConfig";

        // Act
        var storageKey = await _storageService.StoreConfigAsync(largeConfig, configType);
        var retrievedConfig = await _storageService.RetrieveConfigAsync(storageKey);

        // Assert
        storageKey.ShouldNotBeNullOrEmpty();
        retrievedConfig.ShouldBe(largeConfig);

        var configSize = Encoding.UTF8.GetByteCount(largeConfig);
        _testOutputHelper.WriteLine($"Successfully handled large config ({configSize} bytes) with key: {storageKey}");
    }

    #endregion

    #region Compression Utility Tests

    [Fact]
    public void CompressString_ValidInput_ShouldCompress()
    {
        // Arrange
        var testString = CreateRepetitiveString(1024);

        // Act
        var compressed = CompressString(testString);

        // Assert
        compressed.ShouldNotBeNull();
        compressed.Length.ShouldBeLessThan(Encoding.UTF8.GetByteCount(testString));

        var compressionRatio = (double)(Encoding.UTF8.GetByteCount(testString) - compressed.Length) /
                               Encoding.UTF8.GetByteCount(testString);
        _testOutputHelper.WriteLine($"Compression ratio: {compressionRatio:P}");
    }

    [Fact]
    public void DecompressToString_ValidInput_ShouldDecompress()
    {
        // Arrange
        var originalString = CreateRepetitiveString(1024);
        var compressed = CompressString(originalString);

        // Act
        var decompressed = DecompressToString(compressed);

        // Assert
        decompressed.ShouldBe(originalString);

        _testOutputHelper.WriteLine(
            $"Successfully compressed and decompressed {Encoding.UTF8.GetByteCount(originalString)} bytes");
    }

    [Fact]
    public void CompressDecompress_EmptyString_ShouldHandleCorrectly()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var compressed = CompressString(emptyString);
        var decompressed = DecompressToString(compressed);

        // Assert
        decompressed.ShouldBe(emptyString);

        _testOutputHelper.WriteLine("Empty string compression/decompression handled correctly");
    }

    [Fact]
    public void CompressDecompress_JsonConfig_ShouldMaintainStructure()
    {
        // Arrange
        var jsonConfig = CreateTestConfig("JsonStructureTest", 10 * 1024);

        // Act
        var compressed = CompressString(jsonConfig);
        var decompressed = DecompressToString(compressed);

        // Verify JSON structure is maintained
        var originalDoc = JsonDocument.Parse(jsonConfig);
        var decompressedDoc = JsonDocument.Parse(decompressed);

        // Assert
        decompressed.ShouldBe(jsonConfig);

        // Verify JSON structure integrity
        originalDoc.RootElement.GetProperty("type").GetString().ShouldBe(
            decompressedDoc.RootElement.GetProperty("type").GetString());

        _testOutputHelper.WriteLine("JSON structure maintained through compression/decompression");
    }

    [Fact]
    public void CompressString_LargeRepetitiveData_ShouldAchieveHighCompressionRatio()
    {
        // Arrange
        var repetitiveData = CreateRepetitiveString(100 * 1024); // 100KB of repetitive data

        // Act
        var compressed = CompressString(repetitiveData);

        // Assert
        var originalSize = Encoding.UTF8.GetByteCount(repetitiveData);
        var compressionRatio = (double)(originalSize - compressed.Length) / originalSize;

        compressionRatio.ShouldBeGreaterThan(0.8); // Should achieve >80% compression on repetitive data

        _testOutputHelper.WriteLine(
            $"High compression ratio achieved: {originalSize} -> {compressed.Length} bytes ({compressionRatio:P})");
    }

    #endregion

    #region Helper Methods

    private static string CreateTestConfig(string type, int targetSize)
    {
        var config = new Dictionary<string, object>
        {
            ["type"] = type,
            ["timestamp"] = DateTime.UtcNow,
            ["version"] = "1.0.0",
            ["data"] = new List<Dictionary<string, object>>()
        };

        var jsonString = JsonSerializer.Serialize(config);
        var currentSize = Encoding.UTF8.GetByteCount(jsonString);

        while (currentSize < targetSize)
        {
            var dataItem = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["value"] = GenerateRandomString(100)
            };

            ((List<Dictionary<string, object>>)config["data"]).Add(dataItem);
            jsonString = JsonSerializer.Serialize(config);
            currentSize = Encoding.UTF8.GetByteCount(jsonString);
        }

        return jsonString;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringBuilder = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(chars[Random.Shared.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }

    private static string CreateRepetitiveString(int targetSize)
    {
        const string pattern = "This is a repetitive pattern for compression testing. ";
        var stringBuilder = new StringBuilder();

        while (stringBuilder.Length < targetSize)
        {
            stringBuilder.Append(pattern);
        }

        return stringBuilder.ToString().Substring(0, Math.Min(targetSize, stringBuilder.Length));
    }

    /// <summary>
    /// Compress a string using GZip compression
    /// </summary>
    private static byte[] CompressString(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gzipStream.Write(bytes, 0, bytes.Length);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Decompress a byte array back to string using GZip decompression
    /// </summary>
    private static string DecompressToString(byte[] compressedBytes)
    {
        using var memoryStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
        using var streamReader = new StreamReader(gzipStream, Encoding.UTF8);
        return streamReader.ReadToEnd();
    }

    #endregion
}