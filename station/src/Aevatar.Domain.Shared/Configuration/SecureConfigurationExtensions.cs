// ABOUTME: This file provides extension methods for secure configuration setup
// ABOUTME: Simplifies the process of setting up protected configuration with business config validation

using System;
using Microsoft.Extensions.Configuration;

namespace Aevatar.Domain.Shared.Configuration;

/// <summary>
/// Extension methods for secure configuration building with protected key validation
/// </summary>
public static class SecureConfigurationExtensions
{
    /// <summary>
    /// Adds secure configuration with separate system and business configuration files
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="systemConfigPath">Path to system configuration file</param>
    /// <param name="businessConfigPath">Path to business configuration file</param>
    /// <param name="protectedKeyPrefixes">Key prefixes that cannot be overridden by business config</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddSecureConfiguration(
        this IConfigurationBuilder builder,
        string systemConfigPath,
        string businessConfigPath,
        params string[] protectedKeyPrefixes)
    {
        return builder.AddSecureConfiguration(systemConfigPath, businessConfigPath, optional: true, protectedKeyPrefixes);
    }

    /// <summary>
    /// Adds secure configuration with separate system and business configuration files
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="systemConfigPath">Path to system configuration file</param>
    /// <param name="businessConfigPath">Path to business configuration file</param>
    /// <param name="optional">Whether business configuration file is optional</param>
    /// <param name="protectedKeyPrefixes">Key prefixes that cannot be overridden by business config</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddSecureConfiguration(
        this IConfigurationBuilder builder,
        string systemConfigPath,
        string businessConfigPath,
        bool optional,
        params string[] protectedKeyPrefixes)
    {
        // Add system configuration first (lowest priority)
        builder.AddJsonFile(systemConfigPath, optional: false);
        
        // Build system configuration for validation
        var systemConfig = new ConfigurationBuilder()
            .AddJsonFile(systemConfigPath, optional: false)
            .Build();
        
        // Add business configuration with validation
        builder.AddJsonFile(businessConfigPath, optional: optional);
        
        // If business config file exists, validate it
        if (System.IO.File.Exists(businessConfigPath))
        {
            var businessConfig = new ConfigurationBuilder()
                .AddJsonFile(businessConfigPath, optional: false)
                .Build();
            
            var validator = new ProtectedKeyConfigurationProvider(protectedKeyPrefixes);
            validator.ValidateBusinessConfiguration(systemConfig, businessConfig);
        }
        
        return builder;
    }

    /// <summary>
    /// Adds secure configuration for Aevatar platform with multiple system configuration files
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="systemConfigPaths">Paths to system configuration files (in priority order)</param>
    /// <param name="businessConfigPath">Path to business configuration file (optional)</param>
    /// <param name="ephemeralConfigPath">Path to ephemeral environment configuration file (optional)</param>
    /// <param name="optional">Whether business configuration file is optional</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddAevatarSecureConfiguration(
        this IConfigurationBuilder builder,
        string[] systemConfigPaths,
        string businessConfigPath = "appsettings.business.json",
        string ephemeralConfigPath = "appsettings.ephemeral.json",
        bool optional = true)
    {
        // 1. Add default appsettings.json (optional)
        builder.AddJsonFile("appsettings.json", optional: true);
        
        // 2. Add all system configurations (always required)
        foreach (var systemConfigPath in systemConfigPaths)
        {
            builder.AddJsonFile(systemConfigPath, optional: false);
        }
        
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
        // Only preserve critical environment variable prefixes as explicit protected keys
        var explicitProtectedKeys = new[] { "ASPNETCORE_", "DOTNET_" };
        var validator = new ProtectedKeyConfigurationProvider(explicitProtectedKeys);
        validator.ValidateBusinessConfiguration(systemConfig, businessConfig);
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