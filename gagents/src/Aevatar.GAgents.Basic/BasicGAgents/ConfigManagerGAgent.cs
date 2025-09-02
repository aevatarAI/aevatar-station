using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGEvent;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Orleans.Concurrency;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Aevatar.GAgents.Basic.BasicGAgents;

/// <summary>
/// Configuration storage strategy based on config size
/// </summary>
[GenerateSerializer]
public enum ConfigStorageStrategy
{
    [Id(0)] Inline,      // < 1KB: Store directly in state
    [Id(1)] Compressed,  // 1KB-100KB: Store compressed in state
    [Id(2)] External     // > 100KB: Store in external storage
}

/// <summary>
/// Configuration storage information
/// </summary>
[GenerateSerializer]
public class ConfigStorageInfo
{
    [Id(0)] public ConfigStorageStrategy Strategy { get; set; }
    [Id(1)] public string ConfigJson { get; set; } = string.Empty;
    [Id(2)] public byte[] CompressedData { get; set; } = Array.Empty<byte>();
    [Id(3)] public string ExternalKey { get; set; } = string.Empty;
    [Id(4)] public string Hash { get; set; } = string.Empty;
    [Id(5)] public long Size { get; set; }
}

/// <summary>
/// Interface for configuration storage service
/// </summary>
public interface IConfigStorageService
{
    Task<string> StoreConfigAsync(string configJson, string configType);
    Task<string?> RetrieveConfigAsync(string storageKey);
    Task<bool> DeleteConfigAsync(string storageKey);
    Task<byte[]> CompressConfigAsync(string configJson);
    Task<string> DecompressConfigAsync(byte[] compressedData);
}

/// <summary>
/// State for ConfigManagerGAgent - Optimized version for better performance
/// </summary>
[GenerateSerializer]
public class ConfigManagerGAgentState : StateBase
{
    [Id(0)] public DateTime LastUpdated { get; set; }
    [Id(1)] public Dictionary<string, DateTime> ConfigUpdateTimes { get; set; } = new();
    [Id(2)] public int TotalUpdates { get; set; }
    
    // Only used for small configs
    [Id(3)] public string ConfigJson { get; set; } = string.Empty;
    [Id(4)] public string ConfigType { get; set; } = string.Empty;

    // Optimized storage fields
    [Id(5)] public string ConfigHash { get; set; } = string.Empty;
    [Id(6)] public long ConfigSize { get; set; }
    [Id(7)] public ConfigStorageStrategy StorageStrategy { get; set; } = ConfigStorageStrategy.Inline;
    [Id(8)] public byte[] CompressedConfig { get; set; } = Array.Empty<byte>();
    [Id(9)] public string ExternalStorageKey { get; set; } = string.Empty;
    [Id(10)] public int SchemaVersion { get; set; } = 2; // For migration support
}

/// <summary>
/// State log events for ConfigManagerGAgent
/// </summary>
[GenerateSerializer]
public class ConfigManagerStateLogEvent : StateLogEventBase<ConfigManagerStateLogEvent>;

[GenerateSerializer]
public class ConfigUpdatedLogEvent : ConfigManagerStateLogEvent
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty;
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public string? ErrorMessage { get; set; }
    [Id(3)] public DateTime Timestamp { get; set; }
    [Id(4)] public string ConfigHash { get; set; } = string.Empty; // Only store hash, not full JSON
    [Id(5)] public long ConfigSize { get; set; }
}

[GenerateSerializer]
public class OptimizedConfigSetLogEvent : ConfigManagerStateLogEvent
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty;
    [Id(1)] public ConfigStorageStrategy StorageStrategy { get; set; }
    [Id(2)] public string ConfigHash { get; set; } = string.Empty;
    [Id(3)] public long ConfigSize { get; set; }
    [Id(4)] public DateTime Timestamp { get; set; }
    
    // Storage-specific data
    [Id(5)] public string ConfigJson { get; set; } = string.Empty; // For inline storage only
    [Id(6)] public byte[] CompressedConfig { get; set; } = Array.Empty<byte>(); // For compressed storage
    [Id(7)] public string ExternalStorageKey { get; set; } = string.Empty; // For external storage
}

// Keep legacy event for backward compatibility
[GenerateSerializer]
public class ConfigSetLogEvent : ConfigManagerStateLogEvent
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty;
    [Id(1)] public string ConfigJson { get; set; } = string.Empty;
    [Id(2)] public DateTime Timestamp { get; set; }
}

public interface IConfigManagerGAgent : IStateGAgent<ConfigManagerGAgentState>
{
    Task<ConfigResponseEvent> UpdateConfigAsync(ConfigUpdateEvent updateEvent);
    [ReadOnly]
    Task<ConfigResponseEvent> RequestConfigAsync(ConfigRequestEvent requestEvent);
}

/// <summary>
/// Basic implementation of IConfigStorageService for Phase 1
/// </summary>
public class BasicConfigStorageService : IConfigStorageService
{
    private readonly Dictionary<string, string> _externalStorage = new();
    
    public Task<string> StoreConfigAsync(string configJson, string configType)
    {
        var key = $"{configType}_{Guid.NewGuid():N}";
        _externalStorage[key] = configJson;
        return Task.FromResult(key);
    }
    
    public Task<string?> RetrieveConfigAsync(string storageKey)
    {
        return Task.FromResult(_externalStorage.TryGetValue(storageKey, out var config) ? config : null);
    }
    
    public Task<bool> DeleteConfigAsync(string storageKey)
    {
        return Task.FromResult(_externalStorage.Remove(storageKey));
    }
    
    public Task<byte[]> CompressConfigAsync(string configJson)
    {
        var bytes = Encoding.UTF8.GetBytes(configJson);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return Task.FromResult(output.ToArray());
    }
    
    public Task<string> DecompressConfigAsync(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Task.FromResult(Encoding.UTF8.GetString(output.ToArray()));
    }
}

/// <summary>
/// GAgent responsible for managing configuration updates
/// Each instance stores one type of Options configuration
/// Primary key is generated from Options type's FullName
/// Optimized version with intelligent storage strategies
/// </summary>
[GAgent("config", "aevatar")]
public class ConfigManagerGAgent : GAgentBase<ConfigManagerGAgentState, ConfigManagerStateLogEvent>,
    IConfigManagerGAgent
{
    private readonly IConfigStorageService _storageService = new BasicConfigStorageService();

    // Configuration size thresholds for storage strategy selection
    private const int INLINE_THRESHOLD = 1024; // 1KB
    private const int COMPRESSED_THRESHOLD = 102400; // 100KB

    public override Task<string> GetDescriptionAsync()
    {
        var configType = string.IsNullOrEmpty(State.ConfigType) ? "Not configured" : State.ConfigType;
        var storageInfo = State.StorageStrategy != ConfigStorageStrategy.Inline
            ? $"Storage: {State.StorageStrategy}, Size: {State.ConfigSize} bytes, "
            : "";

        return Task.FromResult($"Configuration manager GAgent for type: {configType}. " +
                               storageInfo +
                               $"Last updated: {State.LastUpdated:yyyy-MM-dd HH:mm:ss}, " +
                               $"Total updates: {State.TotalUpdates}");
    }

    /// <summary>
    /// Determine the optimal storage strategy based on configuration size
    /// </summary>
    private ConfigStorageStrategy DetermineStorageStrategy(string configJson)
    {
        var sizeBytes = Encoding.UTF8.GetByteCount(configJson);

        if (sizeBytes < INLINE_THRESHOLD)
            return ConfigStorageStrategy.Inline;
        if (sizeBytes < COMPRESSED_THRESHOLD)
            return ConfigStorageStrategy.Compressed;
        return ConfigStorageStrategy.External;
    }

    /// <summary>
    /// Compute SHA256 hash for configuration content
    /// </summary>
    private string ComputeConfigHash(string configJson)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(configJson);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Process configuration for storage according to the determined strategy
    /// </summary>
    private async Task<ConfigStorageInfo> ProcessConfigForStorageAsync(string configJson, string configType)
    {
        var strategy = DetermineStorageStrategy(configJson);
        var configHash = ComputeConfigHash(configJson);
        var configSize = Encoding.UTF8.GetByteCount(configJson);

        var storageInfo = new ConfigStorageInfo
        {
            Strategy = strategy,
            Hash = configHash,
            Size = configSize
        };

        switch (strategy)
        {
            case ConfigStorageStrategy.Inline:
                storageInfo.ConfigJson = configJson;
                Logger.LogDebug("Storing config inline, size: {Size} bytes", configSize);
                break;

            case ConfigStorageStrategy.Compressed:
                storageInfo.CompressedData = await _storageService.CompressConfigAsync(configJson);
                Logger.LogInformation("Compressing config, original: {Original} bytes, compressed: {Compressed} bytes",
                    configSize, storageInfo.CompressedData.Length);
                break;

            case ConfigStorageStrategy.External:
                storageInfo.ExternalKey = await _storageService.StoreConfigAsync(configJson, configType);
                Logger.LogInformation("Storing config externally, size: {Size} bytes, key: {Key}",
                    configSize, storageInfo.ExternalKey);
                break;

            default:
                throw new ArgumentException($"Unsupported storage strategy: {strategy}");
        }

        return storageInfo;
    }

    /// <summary>
    /// Retrieve configuration content from storage
    /// </summary>
    private async Task<string> RetrieveConfigFromStorageAsync()
    {
        // Handle legacy state (schema version 1 or unset)
        if (State.SchemaVersion < 2)
        {
            return State.ConfigJson;
        }

        switch (State.StorageStrategy)
        {
            case ConfigStorageStrategy.Inline:
                return State.ConfigJson;

            case ConfigStorageStrategy.Compressed:
                if (State.CompressedConfig.Length == 0)
                    return string.Empty;
                return await _storageService.DecompressConfigAsync(State.CompressedConfig);

            case ConfigStorageStrategy.External:
                if (string.IsNullOrEmpty(State.ExternalStorageKey))
                    return string.Empty;
                return await _storageService.RetrieveConfigAsync(State.ExternalStorageKey);

            default:
                Logger.LogWarning("Unknown storage strategy: {Strategy}, falling back to legacy",
                    State.StorageStrategy);
                return State.ConfigJson;
        }
    }

    /// <summary>
    /// Update configuration
    /// </summary>
    public async Task<ConfigResponseEvent> UpdateConfigAsync(ConfigUpdateEvent updateEvent)
    {
        return await HandleEventAsync(updateEvent);
    }

    /// <summary>
    /// Request configuration
    /// </summary>
    public async Task<ConfigResponseEvent> RequestConfigAsync(ConfigRequestEvent requestEvent)
    {
        return await HandleEventAsync(requestEvent);
    }

    /// <summary>
    /// Handle configuration update events - Optimized version
    /// </summary>
    [EventHandler]
    public async Task<ConfigResponseEvent> HandleEventAsync(ConfigUpdateEvent updateEvent)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(updateEvent.ConfigType))
            {
                throw new ArgumentException("ConfigType cannot be empty");
            }

            if (string.IsNullOrEmpty(updateEvent.ConfigJson))
            {
                throw new ArgumentException("ConfigJson cannot be empty");
            }

            // Validate JSON format
            try
            {
                JsonDocument.Parse(updateEvent.ConfigJson);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
            }

            // Process configuration for optimal storage
            var storageInfo = await ProcessConfigForStorageAsync(updateEvent.ConfigJson, updateEvent.ConfigType);

            // Clean up old external storage if switching strategies
            if (State.StorageStrategy == ConfigStorageStrategy.External &&
                !string.IsNullOrEmpty(State.ExternalStorageKey) &&
                storageInfo.Strategy != ConfigStorageStrategy.External)
            {
                await _storageService.DeleteConfigAsync(State.ExternalStorageKey);
            }

            // Raise optimized state update event
            RaiseEvent(new OptimizedConfigSetLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                StorageStrategy = storageInfo.Strategy,
                ConfigHash = storageInfo.Hash,
                ConfigSize = storageInfo.Size,
                Timestamp = DateTime.UtcNow,
                ConfigJson = storageInfo.ConfigJson,
                CompressedConfig = storageInfo.CompressedData,
                ExternalStorageKey = storageInfo.ExternalKey
            });

            // Log success with optimized event (no full JSON)
            RaiseEvent(new ConfigUpdatedLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                Success = true,
                Timestamp = DateTime.UtcNow,
                ConfigHash = storageInfo.Hash,
                ConfigSize = storageInfo.Size
            });

            await ConfirmEvents();

            Logger.LogInformation("Successfully updated configuration for type: {ConfigType}, " +
                                  "Strategy: {Strategy}, Size: {Size} bytes, Hash: {Hash}",
                updateEvent.ConfigType, storageInfo.Strategy, storageInfo.Size, storageInfo.Hash[..8]);

            return new ConfigResponseEvent
            {
                ConfigType = updateEvent.ConfigType,
                ConfigJson = updateEvent.ConfigJson, // Return full JSON to caller
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update configuration for type: {ConfigType}", updateEvent.ConfigType);

            // Log failure with minimal data
            RaiseEvent(new ConfigUpdatedLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow,
                ConfigHash = string.Empty,
                ConfigSize = 0
            });

            await ConfirmEvents();

            return new ConfigResponseEvent
            {
                ConfigType = updateEvent.ConfigType,
                ConfigJson = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Handle configuration get events - Optimized version
    /// </summary>
    [EventHandler]
    public async Task<ConfigResponseEvent> HandleEventAsync(ConfigRequestEvent requestEvent)
    {
        try
        {
            // Check if we have configuration stored (considering all storage strategies)
            var hasConfig = State.SchemaVersion < 2
                ? !string.IsNullOrEmpty(State.ConfigJson)
                : State.StorageStrategy switch
                {
                    ConfigStorageStrategy.Inline => !string.IsNullOrEmpty(State.ConfigJson),
                    ConfigStorageStrategy.Compressed => State.CompressedConfig.Length > 0,
                    ConfigStorageStrategy.External => !string.IsNullOrEmpty(State.ExternalStorageKey),
                    _ => !string.IsNullOrEmpty(State.ConfigJson)
                };

            if (!hasConfig)
            {
                return new ConfigResponseEvent
                {
                    ConfigType = requestEvent.ConfigType,
                    ConfigJson = string.Empty,
                    Success = false,
                    ErrorMessage = "No configuration found"
                };
            }

            // Check if config type matches
            if (!string.IsNullOrEmpty(State.ConfigType) &&
                State.ConfigType != requestEvent.ConfigType)
            {
                return new ConfigResponseEvent
                {
                    ConfigType = requestEvent.ConfigType,
                    ConfigJson = string.Empty,
                    Success = false,
                    ErrorMessage =
                        $"Configuration type mismatch. Expected: {State.ConfigType}, Requested: {requestEvent.ConfigType}"
                };
            }

            // Retrieve configuration from storage
            var configJson = await RetrieveConfigFromStorageAsync();

            if (string.IsNullOrEmpty(configJson))
            {
                return new ConfigResponseEvent
                {
                    ConfigType = requestEvent.ConfigType,
                    ConfigJson = string.Empty,
                    Success = false,
                    ErrorMessage = "Failed to retrieve configuration from storage"
                };
            }

            // If a specific key is requested, extract it from the JSON
            if (requestEvent.ConfigKey != null)
            {
                try
                {
                    var configObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);
                    if (configObject != null && configObject.TryGetValue(requestEvent.ConfigKey, out var value))
                    {
                        var valueJson = JsonSerializer.Serialize(value);
                        Logger.LogInformation(
                            "Successfully extracted configuration key: {ConfigKey} for type: {ConfigType}",
                            requestEvent.ConfigKey, requestEvent.ConfigType);
                        return new ConfigResponseEvent
                        {
                            ConfigType = requestEvent.ConfigType,
                            ConfigJson = valueJson,
                            Success = true
                        };
                    }

                    return new ConfigResponseEvent
                    {
                        ConfigType = requestEvent.ConfigType,
                        ConfigJson = string.Empty,
                        Success = false,
                        ErrorMessage = $"Configuration key '{requestEvent.ConfigKey}' not found"
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to extract configuration key: {ConfigKey}", requestEvent.ConfigKey);
                    // If extraction fails, return the whole config
                }
            }

            if (requestEvent.ConfigKey == string.Empty)
            {
                return new ConfigResponseEvent
                {
                    ConfigType = requestEvent.ConfigType,
                    ConfigJson = string.Empty,
                    Success = false,
                    ErrorMessage = "Request config key is empty"
                };
            }

            Logger.LogInformation("Successfully retrieved configuration for type: {ConfigType}, " +
                                  "Strategy: {Strategy}, Size: {Size} bytes",
                State.ConfigType, State.StorageStrategy, State.ConfigSize);

            return new ConfigResponseEvent
            {
                ConfigType = State.ConfigType,
                ConfigJson = configJson,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve configuration for type: {ConfigType}", requestEvent.ConfigType);

            return new ConfigResponseEvent
            {
                ConfigType = requestEvent.ConfigType,
                ConfigJson = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    protected override void GAgentTransitionState(ConfigManagerGAgentState state,
        StateLogEventBase<ConfigManagerStateLogEvent> @event)
    {
        switch (@event)
        {
            case OptimizedConfigSetLogEvent optimizedSetEvent:
                // Update to schema version 2 (optimized)
                state.SchemaVersion = 2;
                state.ConfigType = optimizedSetEvent.ConfigType;
                state.ConfigHash = optimizedSetEvent.ConfigHash;
                state.ConfigSize = optimizedSetEvent.ConfigSize;
                state.StorageStrategy = optimizedSetEvent.StorageStrategy;
                state.LastUpdated = optimizedSetEvent.Timestamp;

                // Clear all storage fields first
                state.ConfigJson = string.Empty;
                state.CompressedConfig = Array.Empty<byte>();
                state.ExternalStorageKey = string.Empty;

                // Set appropriate storage field based on strategy
                switch (optimizedSetEvent.StorageStrategy)
                {
                    case ConfigStorageStrategy.Inline:
                        state.ConfigJson = optimizedSetEvent.ConfigJson;
                        break;
                    case ConfigStorageStrategy.Compressed:
                        state.CompressedConfig = optimizedSetEvent.CompressedConfig;
                        break;
                    case ConfigStorageStrategy.External:
                        state.ExternalStorageKey = optimizedSetEvent.ExternalStorageKey;
                        break;
                }

                break;

            case ConfigSetLogEvent legacySetEvent:
                // Handle legacy events (maintain backward compatibility)
                if (state.SchemaVersion < 2)
                {
                    state.SchemaVersion = 1; // Mark as legacy
                }

                state.ConfigType = legacySetEvent.ConfigType;
                state.ConfigJson = legacySetEvent.ConfigJson;
                state.LastUpdated = legacySetEvent.Timestamp;

                // For legacy events, calculate size and set inline strategy
                if (state.SchemaVersion < 2)
                {
                    state.ConfigSize = string.IsNullOrEmpty(legacySetEvent.ConfigJson)
                        ? 0
                        : Encoding.UTF8.GetByteCount(legacySetEvent.ConfigJson);
                    state.StorageStrategy = ConfigStorageStrategy.Inline;
                }

                break;

            case ConfigUpdatedLogEvent updateEvent:
                state.LastUpdated = updateEvent.Timestamp;
                state.ConfigUpdateTimes[updateEvent.ConfigType] = updateEvent.Timestamp;
                if (updateEvent.Success)
                {
                    state.TotalUpdates++;

                    // Update hash and size if provided (optimized events)
                    if (!string.IsNullOrEmpty(updateEvent.ConfigHash))
                    {
                        state.ConfigHash = updateEvent.ConfigHash;
                        state.ConfigSize = updateEvent.ConfigSize;
                    }
                }

                break;
        }
    }
}