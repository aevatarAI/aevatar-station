// ABOUTME: This file contains tests for ProtectedKeyConfigurationProvider
// ABOUTME: Validates that business configurations cannot override protected system keys

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;
using Aevatar.Domain.Shared.Configuration;

namespace Aevatar.Domain.Shared.Tests.Configuration;

public class ProtectedKeyConfigurationProviderTests
{
    [Fact]
    public void Should_PreventOverride_When_BusinessConfigContainsProtectedKey()
    {
        // Arrange - Setup configuration with system and business configs
        var systemConfig = new Dictionary<string, string>
        {
            ["Orleans:ClusterId"] = "TestCluster",
            ["Serilog:MinimumLevel:Default"] = "Information"
        };
        
        var businessConfig = new Dictionary<string, string>
        {
            ["Orleans:ClusterId"] = "HackedCluster", // Attempting to override system key
            ["Business:CustomSetting"] = "Allowed"
        };

        var protectedKeys = new[] { "Orleans", "Serilog" };
        var provider = new ProtectedKeyConfigurationProvider(protectedKeys);
        
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
        {
            var systemConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(systemConfig)
                .Build();
                
            var businessConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(businessConfig)
                .Build();
                
            provider.ValidateBusinessConfiguration(systemConfiguration, businessConfiguration);
        });
        
        exception.Message.ShouldContain("Orleans:ClusterId");
        exception.Message.ShouldContain("protected key");
    }

    [Fact]
    public void Should_AllowBusinessKeys_When_NotProtected()
    {
        // Arrange - Only non-protected keys in business config
        var systemConfig = new Dictionary<string, string>
        {
            ["Orleans:ClusterId"] = "TestCluster"
        };
        
        var businessConfig = new Dictionary<string, string>
        {
            ["Business:CustomSetting"] = "Allowed",
            ["Features:NewFeature"] = "Enabled"
        };

        var protectedKeys = new[] { "Orleans", "Serilog" };
        var provider = new ProtectedKeyConfigurationProvider(protectedKeys);
        
        // Act
        var systemConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(systemConfig)
            .Build();
            
        var businessConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(businessConfig)
            .Build();
            
        // This should not throw because business config has no protected keys
        provider.ValidateBusinessConfiguration(systemConfiguration, businessConfiguration);
        
        var finalConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(systemConfig)
            .AddInMemoryCollection(businessConfig)
            .Build();
        
        // Assert
        finalConfiguration["Orleans:ClusterId"].ShouldBe("TestCluster");
        finalConfiguration["Business:CustomSetting"].ShouldBe("Allowed");
        finalConfiguration["Features:NewFeature"].ShouldBe("Enabled");
    }

    [Fact]
    public void Should_PreventPartialKeyOverride_When_NestedKeyIsProtected()
    {
        // Arrange
        var systemConfig = new Dictionary<string, string>
        {
            ["Orleans:Clustering:MongoDb:ConnectionString"] = "SystemConnection"
        };
        
        var businessConfig = new Dictionary<string, string>
        {
            ["Orleans:Clustering:MongoDb:ConnectionString"] = "BusinessConnection"
        };

        var protectedKeys = new[] { "Orleans" };
        var provider = new ProtectedKeyConfigurationProvider(protectedKeys);
        
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
        {
            var systemConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(systemConfig)
                .Build();
                
            var businessConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(businessConfig)
                .Build();
                
            provider.ValidateBusinessConfiguration(systemConfiguration, businessConfiguration);
        });
    }

    [Fact]
    public void GetProtectedKeys_Should_ReturnPredefinedKeys()
    {
        // Act
        var protectedKeys = ProtectedKeyConfigurationProvider.GetProtectedKeys();
        
        // Assert
        protectedKeys.ShouldNotBeNull();
        protectedKeys.ShouldNotBeEmpty();
        protectedKeys.ShouldContain("Orleans");
        protectedKeys.ShouldContain("Serilog");
        protectedKeys.ShouldContain("ConnectionStrings");
        protectedKeys.ShouldContain("Redis");
        protectedKeys.ShouldContain("MongoDB");
        protectedKeys.ShouldContain("OpenTelemetry");
    }

    [Fact]
    public void ValidateBusinessConfigurationKeys_Should_DetectConflicts()
    {
        // Arrange
        var businessConfig = new Dictionary<string, object>
        {
            ["Orleans:ClusterId"] = "TestCluster",
            ["Business:Setting"] = "Allowed",
            ["Serilog:MinimumLevel"] = "Debug",
            ["Features:NewFeature"] = true
        };

        // Act
        var conflicts = ProtectedKeyConfigurationProvider.ValidateBusinessConfigurationKeys(businessConfig);

        // Assert
        conflicts.ShouldNotBeEmpty();
        conflicts.ShouldContain("Orleans:ClusterId");
        conflicts.ShouldContain("Serilog:MinimumLevel");
        conflicts.ShouldNotContain("Business:Setting");
        conflicts.ShouldNotContain("Features:NewFeature");
    }

    [Fact]
    public void ValidateBusinessConfigurationKeys_Should_ReturnEmpty_When_NoConflicts()
    {
        // Arrange
        var businessConfig = new Dictionary<string, object>
        {
            ["Business:ApiUrl"] = "https://api.example.com",
            ["Features:EnableNewFeature"] = true,
            ["MyApp:Timeout"] = 30
        };

        // Act
        var conflicts = ProtectedKeyConfigurationProvider.ValidateBusinessConfigurationKeys(businessConfig);

        // Assert
        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateBusinessConfigurationKeys_Should_HandleNullInput()
    {
        // Act
        var conflicts = ProtectedKeyConfigurationProvider.ValidateBusinessConfigurationKeys(null);

        // Assert
        conflicts.ShouldNotBeNull();
        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateBusinessConfigurationKeys_Should_UseCustomProtectedKeys()
    {
        // Arrange
        var businessConfig = new Dictionary<string, object>
        {
            ["CustomProtected:Setting"] = "value",
            ["Orleans:ClusterId"] = "TestCluster",
            ["Business:Setting"] = "Allowed"
        };

        var customProtectedKeys = new[] { "CustomProtected" };

        // Act
        var conflicts = ProtectedKeyConfigurationProvider.ValidateBusinessConfigurationKeys(businessConfig, customProtectedKeys);

        // Assert
        conflicts.ShouldContain("CustomProtected:Setting");
        conflicts.ShouldNotContain("Orleans:ClusterId"); // Orleans not in custom protected keys
        conflicts.ShouldNotContain("Business:Setting");
    }

    [Theory]
    [InlineData("Orleans", "Orleans", true)]
    [InlineData("Orleans:ClusterId", "Orleans", true)]
    [InlineData("Orleans:Clustering:MongoDb", "Orleans", true)]
    [InlineData("Business:Setting", "Orleans", false)]
    [InlineData("OrleansCustom", "Orleans", false)]
    [InlineData("Serilog:MinimumLevel", "Serilog", true)]
    [InlineData("SerilogCustom:Level", "Serilog", false)]
    public void IsProtectedKey_Should_MatchCorrectly(string key, string protectedPrefix, bool expectedResult)
    {
        // Arrange
        var businessConfig = new Dictionary<string, object> { [key] = "value" };
        var protectedKeys = new[] { protectedPrefix };

        // Act
        var conflicts = ProtectedKeyConfigurationProvider.ValidateBusinessConfigurationKeys(businessConfig, protectedKeys);

        // Assert
        if (expectedResult)
        {
            conflicts.ShouldContain(key);
        }
        else
        {
            conflicts.ShouldNotContain(key);
        }
    }

    [Fact]
    public void ValidateBusinessConfiguration_Should_UseDynamicProtectedKeys_FromSystemConfig()
    {
        // Arrange - System config has keys that should be automatically protected
        var systemConfig = new Dictionary<string, string>
        {
            ["CustomSystemKey:Setting"] = "SystemValue",
            ["AnotherSystemKey:Value"] = "SystemValue2",
            ["Orleans:ClusterId"] = "TestCluster" // This is also in predefined protected keys
        };
        
        var businessConfig = new Dictionary<string, string>
        {
            ["CustomSystemKey:Setting"] = "BusinessValue", // Should be rejected - from system config
            ["AllowedBusinessKey:Setting"] = "BusinessValue", // Should be allowed
            ["Orleans:ServiceId"] = "BusinessService" // Should be rejected - predefined protected key
        };

        var protectedKeys = new[] { "Orleans" }; // Only explicit protected keys
        var provider = new ProtectedKeyConfigurationProvider(protectedKeys);
        
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
        {
            var systemConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(systemConfig)
                .Build();
                
            var businessConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(businessConfig)
                .Build();
                
            provider.ValidateBusinessConfiguration(systemConfiguration, businessConfiguration);
        });
        
        // Should detect both system-derived and predefined protected keys
        exception.Message.ShouldContain("CustomSystemKey:Setting");
        exception.Message.ShouldContain("Orleans:ServiceId");
        exception.Message.ShouldNotContain("AllowedBusinessKey:Setting");
    }

    [Fact]
    public void ValidateBusinessConfiguration_Should_AllowBusinessKeys_WhenNotInSystemConfig()
    {
        // Arrange - Business config with only allowed keys
        var systemConfig = new Dictionary<string, string>
        {
            ["Orleans:ClusterId"] = "TestCluster"
        };
        
        var businessConfig = new Dictionary<string, string>
        {
            ["Business:ApiUrl"] = "https://api.example.com",
            ["Features:NewFeature"] = "Enabled",
            ["CustomApp:Setting"] = "Value"
        };

        var protectedKeys = new[] { "Orleans" };
        var provider = new ProtectedKeyConfigurationProvider(protectedKeys);
        
        // Act - Should not throw
        var systemConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(systemConfig)
            .Build();
            
        var businessConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(businessConfig)
            .Build();
            
        provider.ValidateBusinessConfiguration(systemConfiguration, businessConfiguration);
        
        // Assert - If we reach here, validation passed (no exception thrown)
        Assert.True(true);
    }

    [Fact]
    public void ValidateBusinessConfiguration_Should_CombineSystemAndExplicitProtectedKeys()
    {
        // Arrange
        var systemConfig = new Dictionary<string, string>
        {
            ["SystemKey1:Value"] = "System1",
            ["SystemKey2:Value"] = "System2"
        };
        
        var businessConfig = new Dictionary<string, string>
        {
            ["SystemKey1:Value"] = "Business1", // Should be rejected - from system
            ["ExplicitKey:Value"] = "Business2", // Should be rejected - explicit protected
            ["AllowedKey:Value"] = "Business3"  // Should be allowed
        };

        var explicitProtectedKeys = new[] { "ExplicitKey" };
        var provider = new ProtectedKeyConfigurationProvider(explicitProtectedKeys);
        
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
        {
            var systemConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(systemConfig)
                .Build();
                
            var businessConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(businessConfig)
                .Build();
                
            provider.ValidateBusinessConfiguration(systemConfiguration, businessConfiguration);
        });
        
        // Should detect both system-derived and explicit protected keys
        exception.Message.ShouldContain("SystemKey1:Value");
        exception.Message.ShouldContain("ExplicitKey:Value");
        exception.Message.ShouldNotContain("AllowedKey:Value");
        exception.Message.ShouldContain("SystemKey1");
        exception.Message.ShouldContain("SystemKey2");
        exception.Message.ShouldContain("ExplicitKey");
    }
}