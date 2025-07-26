// ABOUTME: This file implements a configuration validator that prevents override of protected keys
// ABOUTME: Validates configuration to ensure business configs cannot tamper with system settings

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Aevatar.Domain.Shared.Configuration;

/// <summary>
/// Configuration validator that prevents business configurations from overriding protected system keys
/// </summary>
public class ProtectedKeyConfigurationProvider
{
    private readonly string[] _protectedKeyPrefixes;

    public ProtectedKeyConfigurationProvider(params string[] protectedKeyPrefixes)
    {
        _protectedKeyPrefixes = protectedKeyPrefixes ?? Array.Empty<string>();
    }

    /// <summary>
    /// Validates that business configuration keys do not conflict with protected system keys
    /// </summary>
    /// <param name="systemConfig">System configuration with protected keys</param>
    /// <param name="businessConfig">Business configuration to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when protected keys are found in business configuration</exception>
    public void ValidateBusinessConfiguration(IConfiguration systemConfig, IConfiguration businessConfig)
    {
        // Get dynamic protected keys from system configuration + explicit protected keys
        var dynamicProtectedKeys = GetDynamicProtectedKeys(systemConfig);
        var conflictingKeys = new List<string>();

        foreach (var kvp in businessConfig.AsEnumerable())
        {
            if (IsProtectedKeyDynamic(kvp.Key, dynamicProtectedKeys))
            {
                conflictingKeys.Add(kvp.Key);
            }
        }

        if (conflictingKeys.Any())
        {
            throw new InvalidOperationException(
                $"Business configuration cannot override protected keys: {string.Join(", ", conflictingKeys)}. " +
                $"Protected keys (from system + explicit): {string.Join(", ", dynamicProtectedKeys)}");
        }
    }

    /// <summary>
    /// Gets dynamic protected keys by combining system configuration keys with explicit protected keys
    /// </summary>
    /// <param name="systemConfig">System configuration</param>
    /// <returns>Set of protected key prefixes</returns>
    private HashSet<string> GetDynamicProtectedKeys(IConfiguration systemConfig)
    {
        var protectedKeys = new HashSet<string>(_protectedKeyPrefixes, StringComparer.OrdinalIgnoreCase);
        
        // Add all top-level keys from system configuration as protected
        foreach (var section in systemConfig.GetChildren())
        {
            protectedKeys.Add(section.Key);
        }
        
        return protectedKeys;
    }

    /// <summary>
    /// Checks if a key is protected by any of the dynamic protected prefixes
    /// </summary>
    /// <param name="key">Key to check</param>
    /// <param name="protectedKeys">Set of protected key prefixes</param>
    /// <returns>True if the key is protected</returns>
    private static bool IsProtectedKeyDynamic(string key, HashSet<string> protectedKeys)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return protectedKeys.Any(prefix => 
            key.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase) ||
            key.Equals(prefix, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// Gets the default set of protected configuration key prefixes that business configurations cannot override
    /// </summary>
    /// <returns>Array of protected key prefixes</returns>
    [Obsolete("Use ValidateBusinessConfiguration with dynamic system config checking instead. This method maintains a hardcoded list which is difficult to maintain.")]
    public static string[] GetProtectedKeys()
    {
        // Simplified list - most protection now comes from dynamic system config checking
        return new[]
        {
            "Orleans",
            "Serilog",
            "Logging",
            "OpenTelemetry",
            "ConnectionStrings",
            "ASPNETCORE_",
            "DOTNET_"
        };
    }

    /// <summary>
    /// Validates that business configuration dictionary doesn't contain protected keys
    /// </summary>
    /// <param name="businessConfiguration">Business configuration dictionary to validate</param>
    /// <param name="protectedKeys">Array of protected key prefixes (optional, uses default if null)</param>
    /// <returns>List of conflicting keys found</returns>
    [Obsolete("Use ValidateBusinessConfiguration with IConfiguration objects for dynamic validation instead.")]
    public static List<string> ValidateBusinessConfigurationKeys(Dictionary<string, object> businessConfiguration, string[]? protectedKeys = null)
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