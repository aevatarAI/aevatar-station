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
}