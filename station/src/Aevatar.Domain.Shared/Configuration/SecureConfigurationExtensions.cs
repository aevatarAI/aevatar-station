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
    /// Adds secure configuration for Aevatar platform with predefined protected keys
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="systemConfigPath">Path to system configuration file</param>
    /// <param name="businessConfigPath">Path to business configuration file</param>
    /// <param name="optional">Whether business configuration file is optional</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddAevatarSecureConfiguration(
        this IConfigurationBuilder builder,
        string systemConfigPath,
        string businessConfigPath = "appsettings.business.json",
        bool optional = true)
    {
        var protectedKeys = new[]
        {
            "Orleans",
            "Serilog", 
            "OpenTelemetry",
            "ConnectionStrings",
            "Redis",
            "MongoDB"
        };
        
        return builder.AddSecureConfiguration(systemConfigPath, businessConfigPath, optional, protectedKeys);
    }
}