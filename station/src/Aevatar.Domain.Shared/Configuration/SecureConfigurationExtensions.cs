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
    /// Adds secure configuration for Aevatar platform with standard configuration hierarchy
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="systemConfigPath">Path to system configuration file</param>
    /// <param name="businessConfigPath">Path to business configuration file (optional)</param>
    /// <param name="ephemeralConfigPath">Path to ephemeral environment configuration file (optional)</param>
    /// <param name="optional">Whether business configuration file is optional</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddAevatarSecureConfiguration(
        this IConfigurationBuilder builder,
        string systemConfigPath,
        string businessConfigPath = "appsettings.business.json",
        string ephemeralConfigPath = "appsettings.ephemeral.json",
        bool optional = true)
    {
        // Get protected keys dynamically from the provider
        var protectedKeys = ProtectedKeyConfigurationProvider.GetProtectedKeys();
        
        // 1. Add default appsettings.json (optional)
        builder.AddJsonFile("appsettings.json", optional: true);
        
        // 2. Add system configuration (required)
        builder.AddJsonFile(systemConfigPath, optional: false);
        
        // 3. Add business configuration (default supported, with protection)
        if (System.IO.File.Exists(businessConfigPath))
        {
            // Build system configuration for validation
            var systemConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile(systemConfigPath, optional: false)
                .Build();
            
            // Add business configuration
            builder.AddJsonFile(businessConfigPath, optional: optional);
            
            // Validate business configuration against protected keys
            var businessConfig = new ConfigurationBuilder()
                .AddJsonFile(businessConfigPath, optional: false)
                .Build();
            
            var validator = new ProtectedKeyConfigurationProvider(protectedKeys);
            validator.ValidateBusinessConfiguration(systemConfig, businessConfig);
        }
        
        // 4. Add ephemeral configuration (only if explicitly enabled)
        var enableEphemeralConfig = Environment.GetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG");
        if (string.Equals(enableEphemeralConfig, "true", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddJsonFile(ephemeralConfigPath, optional: true);
        }
        
        return builder;
    }
}