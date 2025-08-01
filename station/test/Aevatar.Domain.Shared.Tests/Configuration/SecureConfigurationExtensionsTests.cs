// ABOUTME: This file contains tests for SecureConfigurationExtensions
// ABOUTME: Tests the configuration loading hierarchy with business and ephemeral configurations

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;
using Aevatar.Domain.Shared.Configuration;

namespace Aevatar.Domain.Shared.Tests.Configuration;

public class SecureConfigurationExtensionsTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        // Clean up temp files
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public void AddAevatarSecureConfiguration_Should_LoadDefaultAppsettings()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var defaultConfigPath = Path.Combine(currentDir, "appsettings.json");
        
        // Create appsettings.json in current directory 
        var defaultJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["Default:Setting"] = "FromDefault"
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        
        File.WriteAllText(defaultConfigPath, defaultJson);
        _tempFiles.Add(defaultConfigPath);

        var systemConfig = CreateTempConfigFile("appsettings.system.json", new Dictionary<string, object>
        {
            ["System:Setting"] = "FromSystem"
        });

        // Act
        var configuration = new ConfigurationBuilder()
            .AddAevatarSecureConfiguration(systemConfigPaths: new[] { systemConfig })
            .Build();

        // Assert
        configuration["Default:Setting"].ShouldBe("FromDefault");
        configuration["System:Setting"].ShouldBe("FromSystem");
    }

    [Fact]
    public void AddAevatarSecureConfiguration_Should_LoadBusinessConfig_When_FileExists()
    {
        // Arrange
        var systemConfig = CreateTempConfigFile("appsettings.system.json", new Dictionary<string, object>
        {
            ["System:Setting"] = "FromSystem"
        });

        var businessConfig = CreateTempConfigFile("appsettings.business.json", new Dictionary<string, object>
        {
            ["Business:Setting"] = "FromBusiness"
        });

        // Act
        var configuration = new ConfigurationBuilder()
            .AddAevatarSecureConfiguration(systemConfigPaths: new[] { systemConfig }, businessConfigPath: businessConfig)
            .Build();

        // Assert
        configuration["System:Setting"].ShouldBe("FromSystem");
        configuration["Business:Setting"].ShouldBe("FromBusiness");
    }

    [Fact]
    public void AddAevatarSecureConfiguration_Should_LoadEphemeralConfig_When_Enabled()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG", "true");
        
        var systemConfig = CreateTempConfigFile("appsettings.system.json", new Dictionary<string, object>
        {
            ["System:Setting"] = "FromSystem"
        });

        var ephemeralConfig = CreateTempConfigFile("appsettings.ephemeral.json", new Dictionary<string, object>
        {
            ["System:Setting"] = "OverriddenByEphemeral",
            ["Ephemeral:Setting"] = "FromEphemeral"
        });

        try
        {
            // Act
            var configuration = new ConfigurationBuilder()
                .AddAevatarSecureConfiguration(systemConfigPaths: new[] { systemConfig }, ephemeralConfigPath: ephemeralConfig)
                .Build();

            // Assert
            configuration["System:Setting"].ShouldBe("OverriddenByEphemeral");
            configuration["Ephemeral:Setting"].ShouldBe("FromEphemeral");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG", null);
        }
    }

    [Fact]
    public void AddAevatarSecureConfiguration_Should_NotLoadEphemeralConfig_When_Disabled()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG", "false");
        
        var systemConfig = CreateTempConfigFile("appsettings.system.json", new Dictionary<string, object>
        {
            ["System:Setting"] = "FromSystem"
        });

        var ephemeralConfig = CreateTempConfigFile("appsettings.ephemeral.json", new Dictionary<string, object>
        {
            ["System:Setting"] = "OverriddenByEphemeral"
        });

        try
        {
            // Act
            var configuration = new ConfigurationBuilder()
                .AddAevatarSecureConfiguration(systemConfigPaths: new[] { systemConfig }, ephemeralConfigPath: ephemeralConfig)
                .Build();

            // Assert
            configuration["System:Setting"].ShouldBe("FromSystem"); // Should not be overridden
        }
        finally
        {
            Environment.SetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG", null);
        }
    }

    [Fact]
    public void AddAevatarSecureConfiguration_Should_ValidateBusinessConfig_AgainstProtectedKeys()
    {
        // Arrange
        var systemConfig = CreateTempConfigFile("appsettings.system.json", new Dictionary<string, object>
        {
            ["Orleans:ClusterId"] = "SystemCluster"
        });

        var businessConfig = CreateTempConfigFile("appsettings.business.json", new Dictionary<string, object>
        {
            ["Orleans:ClusterId"] = "BusinessCluster" // This should be rejected
        });

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
        {
            new ConfigurationBuilder()
                .AddAevatarSecureConfiguration(systemConfigPaths: new[] { systemConfig }, businessConfigPath: businessConfig)
                .Build();
        });
    }

    [Fact]
    public void AddAevatarSecureConfiguration_Should_RespectConfigurationPriority()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG", "true");
        Environment.SetEnvironmentVariable("Test__Priority", "FromEnvironment");
        
        var defaultConfig = CreateTempConfigFile("appsettings.json", new Dictionary<string, object>
        {
            ["Test:Priority"] = "FromDefault"
        });

        var systemConfig = CreateTempConfigFile("appsettings.system.json", new Dictionary<string, object>
        {
            ["SystemOnly:Setting"] = "FromSystem"
        });

        var businessConfig = CreateTempConfigFile("appsettings.business.json", new Dictionary<string, object>
        {
            ["Test:Priority"] = "FromBusiness"  // This won't conflict since Test is not in system config
        });

        var ephemeralConfig = CreateTempConfigFile("appsettings.ephemeral.json", new Dictionary<string, object>
        {
            ["Test:Priority"] = "FromEphemeral"
        });

        try
        {
            // Act
            var configuration = new ConfigurationBuilder()
                .AddAevatarSecureConfiguration(systemConfigPaths: new[] { systemConfig }, businessConfigPath: businessConfig, ephemeralConfigPath: ephemeralConfig)
                .AddEnvironmentVariables()
                .Build();

            // Assert - Environment variable should have highest priority
            configuration["Test:Priority"].ShouldBe("FromEnvironment");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ENABLE_EPHEMERAL_CONFIG", null);
            Environment.SetEnvironmentVariable("Test__Priority", null);
        }
    }

    private string CreateTempConfigFile(string fileName, Dictionary<string, object> config)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);
        _tempFiles.Add(tempPath);

        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(tempPath, json);
        return tempPath;
    }
}