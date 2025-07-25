// ABOUTME: This file implements a configuration provider that prevents override of protected keys
// ABOUTME: Validates configuration to ensure business configs cannot tamper with system settings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Aevatar.Domain.Shared.Configuration;

/// <summary>
/// Configuration provider that prevents business configurations from overriding protected system keys
/// </summary>
public class ProtectedKeyConfigurationProvider : IConfigurationProvider
{
    private readonly string[] _protectedKeyPrefixes;
    private readonly Dictionary<string, string> _data = new();

    public ProtectedKeyConfigurationProvider(params string[] protectedKeyPrefixes)
    {
        _protectedKeyPrefixes = protectedKeyPrefixes ?? Array.Empty<string>();
    }

    public bool TryGet(string key, out string? value)
    {
        return _data.TryGetValue(key, out value);
    }

    public void Set(string key, string? value)
    {
        _data[key] = value ?? string.Empty;
    }

    public IChangeToken GetReloadToken()
    {
        return new CancellationChangeToken(CancellationToken.None);
    }

    public void Load()
    {
        // This provider validates configuration after all other providers are loaded
        // It doesn't load any data itself, but validates against protected keys
    }

    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        var prefix = parentPath == null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;

        return _data
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(kv => Segment(kv.Key, prefix.Length))
            .Concat(earlierKeys)
            .OrderBy(k => k, ConfigurationKeyComparer.Instance);
    }

    private static string Segment(string key, int prefixLength)
    {
        var indexOf = key.IndexOf(ConfigurationPath.KeyDelimiter, prefixLength, StringComparison.OrdinalIgnoreCase);
        return indexOf < 0 ? key.Substring(prefixLength) : key.Substring(prefixLength, indexOf - prefixLength);
    }

    /// <summary>
    /// Validates that business configuration keys do not conflict with protected system keys
    /// </summary>
    /// <param name="systemConfig">System configuration with protected keys</param>
    /// <param name="businessConfig">Business configuration to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when protected keys are found in business configuration</exception>
    public void ValidateBusinessConfiguration(IConfiguration systemConfig, IConfiguration businessConfig)
    {
        var conflictingKeys = new List<string>();

        foreach (var kvp in businessConfig.AsEnumerable())
        {
            if (IsProtectedKey(kvp.Key))
            {
                conflictingKeys.Add(kvp.Key);
            }
        }

        if (conflictingKeys.Any())
        {
            throw new InvalidOperationException(
                $"Business configuration cannot override protected keys: {string.Join(", ", conflictingKeys)}. " +
                $"Protected key prefixes: {string.Join(", ", _protectedKeyPrefixes)}");
        }
    }

    private bool IsProtectedKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return _protectedKeyPrefixes.Any(prefix => 
            key.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase) ||
            key.Equals(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the default set of protected configuration key prefixes that business configurations cannot override
    /// </summary>
    /// <returns>Array of protected key prefixes</returns>
    public static string[] GetProtectedKeys()
    {
        return new[]
        {
            "Orleans",
            "AevatarOrleans", 
            "Serilog",
            "Logging",
            "OpenTelemetry",
            "ConnectionStrings",
            "Redis",
            "MongoDB",
            "Elasticsearch",
            "Qdrant",
            "Kafka",
            "AuthServer",
            "Kestrel",
            "Hosting",
            "ASPNETCORE_",
            "DOTNET_",
            "FileUpload",
            "Storage",
            "Security",
            "Authentication",
            "Authorization",
            "Cors",
            "Https",
            "Certificates"
        };
    }

    /// <summary>
    /// Validates that business configuration dictionary doesn't contain protected keys
    /// </summary>
    /// <param name="businessConfiguration">Business configuration dictionary to validate</param>
    /// <param name="protectedKeys">Array of protected key prefixes (optional, uses default if null)</param>
    /// <returns>List of conflicting keys found</returns>
    public static List<string> ValidateBusinessConfigurationKeys(Dictionary<string, object> businessConfiguration, string[] protectedKeys = null)
    {
        if (businessConfiguration == null)
            return new List<string>();

        protectedKeys ??= GetProtectedKeys();
        var conflictingKeys = new List<string>();

        foreach (var key in businessConfiguration.Keys)
        {
            if (protectedKeys.Any(protectedKey => 
                key.StartsWith(protectedKey + ":", StringComparison.OrdinalIgnoreCase) ||
                key.Equals(protectedKey, StringComparison.OrdinalIgnoreCase)))
            {
                conflictingKeys.Add(key);
            }
        }

        return conflictingKeys;
    }
}

/// <summary>
/// Configuration source for ProtectedKeyConfigurationProvider
/// </summary>
public class ProtectedKeyConfigurationSource : IConfigurationSource
{
    public string[] ProtectedKeyPrefixes { get; set; } = Array.Empty<string>();

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ProtectedKeyConfigurationProvider(ProtectedKeyPrefixes);
    }
}