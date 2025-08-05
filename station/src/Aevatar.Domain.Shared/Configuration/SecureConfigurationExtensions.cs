// ABOUTME: This file provides extension methods for secure configuration setup
// ABOUTME: Simplifies the process of setting up protected configuration with business config validation

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Aevatar.Domain.Shared.Configuration;

/// <summary>
/// Extension methods for secure configuration building with protected key validation
/// </summary>
public static class SecureConfigurationExtensions
{
    /// <summary>
    /// Default business configuration file path
    /// </summary>
    public static string DefaultBusinessConfigPath { get; private set; } = "appsettings.business.json";

 
    /// <summary>
    /// Adds secure configuration for Aevatar platform with multiple system configuration files
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="systemConfigPaths">Paths to system configuration files (in priority order)</param>
    /// <param name="businessConfigPath">Path to business configuration file (optional, defaults to DefaultBusinessConfigPath)</param>
    /// <param name="ephemeralConfigPath">Path to ephemeral environment configuration file (optional)</param>
    /// <param name="optional">Whether business configuration file is optional</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddAevatarSecureConfiguration(
        this IConfigurationBuilder builder,
        string[]? systemConfigPaths = null,
        string? businessConfigPath = null,
        string ephemeralConfigPath = "appsettings.ephemeral.json",
        bool optional = true)
    {
        // Set default empty array for system config paths if not specified
        systemConfigPaths ??= new string[0];
        
        // Get business config path from static configuration if not specified
        businessConfigPath ??= DefaultBusinessConfigPath;
       
        
        // 1. Add all system configurations (always required)
        foreach (var systemConfigPath in systemConfigPaths)
        {
            builder.AddJsonFile(systemConfigPath, optional: true);
        }
        // 2. Add default appsettings.json (optional)
        builder.AddJsonFile("appsettings.json", optional: true);
        builder.AddJsonFile("appsettings.mcp.json", optional: true);
        // 3. Add business configuration with validation if it exists
        builder.AddJsonFile(businessConfigPath, optional: optional);
        
        // 4. Validate business configuration against system configuration
        if (System.IO.File.Exists(businessConfigPath))
        {
            ValidateBusinessConfigurationAgainstSystem(systemConfigPaths, businessConfigPath);
        }
        
        // 5. Add ephemeral configuration (only if explicitly enabled)
        if (IsEphemeralConfigEnabled())
        {
            builder.AddJsonFile(ephemeralConfigPath, optional: true);
        }
        
        return builder;
    }

    private static void ValidateBusinessConfigurationAgainstSystem(string[] systemConfigPaths, string businessConfigPath)
    {
        // Build system configuration for validation
        var systemConfig = BuildConfigurationFromPaths(systemConfigPaths, includeDefaultAppSettings: true);
        
        // Build business configuration
        var businessConfig = new ConfigurationBuilder()
            .AddJsonFile(businessConfigPath, optional: false)
            .Build();
        
        // Validate business configuration against system configuration (dynamic validation)
        ValidateBusinessConfiguration(systemConfig, businessConfig);
    }

    /// <summary>
    /// Validates that business configuration keys do not conflict with protected system keys
    /// </summary>
    /// <param name="systemConfig">System configuration with protected keys</param>
    /// <param name="businessConfig">Business configuration to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when protected keys are found in business configuration</exception>
    private static void ValidateBusinessConfiguration(IConfiguration systemConfig, IConfiguration businessConfig)
    {
        // Critical environment variable prefixes that are always protected
        var explicitProtectedKeys = new[] { "DOTNET_" };
        
        // Get dynamic protected keys from system configuration + explicit protected keys
        var protectedKeys = GetDynamicProtectedKeys(systemConfig, explicitProtectedKeys);
        var conflictingKeys = new List<string>();

        foreach (var kvp in businessConfig.AsEnumerable())
        {
            if (IsProtectedKey(kvp.Key, protectedKeys))
            {
                conflictingKeys.Add(kvp.Key);
            }
        }

        if (conflictingKeys.Any())
        {
            throw new InvalidOperationException(
                $"Business configuration cannot override protected keys: {string.Join(", ", conflictingKeys)}. " +
                $"Protected keys (from system + explicit): {string.Join(", ", protectedKeys)}");
        }
    }

    /// <summary>
    /// Gets dynamic protected keys by combining system configuration keys with explicit protected keys
    /// </summary>
    /// <param name="systemConfig">System configuration</param>
    /// <param name="explicitProtectedKeys">Explicit protected key prefixes</param>
    /// <returns>Set of protected key prefixes</returns>
    private static HashSet<string> GetDynamicProtectedKeys(IConfiguration systemConfig, string[] explicitProtectedKeys)
    {
        var protectedKeys = new HashSet<string>(explicitProtectedKeys, StringComparer.OrdinalIgnoreCase);
        
        // Add all top-level keys from system configuration as protected
        foreach (var section in systemConfig.GetChildren())
        {
            protectedKeys.Add(section.Key);
        }
        
        return protectedKeys;
    }

    /// <summary>
    /// Checks if a key is protected by any of the protected prefixes
    /// </summary>
    /// <param name="key">Key to check</param>
    /// <param name="protectedKeys">Set of protected key prefixes</param>
    /// <returns>True if the key is protected</returns>
    private static bool IsProtectedKey(string key, HashSet<string> protectedKeys)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return protectedKeys.Any(prefix => 
            key.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase) ||
            key.Equals(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static IConfiguration BuildConfigurationFromPaths(string[] configPaths, bool includeDefaultAppSettings = false)
    {
        var configBuilder = new ConfigurationBuilder();
        
        if (includeDefaultAppSettings)
        {
            configBuilder.AddJsonFile("appsettings.json", optional: true);
        }
        
        foreach (var configPath in configPaths)
        {
            configBuilder.AddJsonFile(configPath, optional: false);
        }
        
        return configBuilder.Build();
    }

    private static bool IsEphemeralConfigEnabled()
    {
        var enableEphemeralConfig = Environment.GetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG");
        return string.Equals(enableEphemeralConfig, "true", StringComparison.OrdinalIgnoreCase);
    }
    
}