// Integration test for dynamic configuration protection
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;
using Aevatar.Domain.Shared.Configuration;

namespace Aevatar.Domain.Shared.Tests.Configuration;

public class DynamicConfigProtectionIntegrationTest
{
    [Fact]
    public void DynamicProtection_Should_PreventBusinessOverride_Of_SystemKeys()
    {
        // Arrange - Create temporary config files
        var tempDir = Path.GetTempPath();
        var systemConfigPath = Path.Combine(tempDir, $"system-{Guid.NewGuid()}.json");
        var businessConfigPath = Path.Combine(tempDir, $"business-{Guid.NewGuid()}.json");
        
        try
        {
            // System config contains Redis, MongoDB, etc. settings
            var systemConfigContent = @"{
  ""Redis"": {
    ""Configuration"": ""localhost:6379""
  },
  ""MongoDB"": {
    ""ConnectionString"": ""mongodb://localhost:27017""
  },
  ""CustomSystemService"": {
    ""Endpoint"": ""https://system.internal.com""
  }
}";
            
            // Business config tries to override system keys (should fail)
            var businessConfigContent = @"{
  ""Redis"": {
    ""Configuration"": ""hacker-redis:6379""
  },
  ""Business"": {
    ""ApiKey"": ""business-key-123""
  }
}";
            
            File.WriteAllText(systemConfigPath, systemConfigContent);
            File.WriteAllText(businessConfigPath, businessConfigContent);
            
            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() =>
            {
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddAevatarSecureConfiguration(
                    systemConfigPaths: new[] { systemConfigPath },
                    businessConfigPath: businessConfigPath,
                    optional: false);
                
                configBuilder.Build();
            });
            
            // Should detect Redis override attempt
            exception.Message.ShouldContain("Redis");
            exception.Message.ShouldContain("protected");
        }
        finally
        {
            // Cleanup
            if (File.Exists(systemConfigPath)) File.Delete(systemConfigPath);
            if (File.Exists(businessConfigPath)) File.Delete(businessConfigPath);
        }
    }

    [Fact]
    public void DynamicProtection_Should_Allow_OnlyBusinessKeys()
    {
        // Arrange - Create temporary config files
        var tempDir = Path.GetTempPath();
        var systemConfigPath = Path.Combine(tempDir, $"system-{Guid.NewGuid()}.json");
        var businessConfigPath = Path.Combine(tempDir, $"business-{Guid.NewGuid()}.json");
        
        try
        {
            // System config
            var systemConfigContent = @"{
  ""Orleans"": {
    ""ClusterId"": ""TestCluster""
  },
  ""Serilog"": {
    ""MinimumLevel"": ""Information""
  }
}";
            
            // Business config with only allowed keys
            var businessConfigContent = @"{
  ""Business"": {
    ""ApiKey"": ""business-key-123"",
    ""Timeout"": 30
  },
  ""Features"": {
    ""EnableNewFeature"": true
  }
}";
            
            File.WriteAllText(systemConfigPath, systemConfigContent);
            File.WriteAllText(businessConfigPath, businessConfigContent);
            
            // Act - Should not throw
            var config = new ConfigurationBuilder()
                .AddAevatarSecureConfiguration(
                    systemConfigPaths: new[] { systemConfigPath },
                    businessConfigPath: businessConfigPath,
                    optional: false)
                .Build();
            
            // Assert - Business config values should be available
            config["Business:ApiKey"].ShouldBe("business-key-123");
            config["Features:EnableNewFeature"].ShouldBe("True"); // JSON deserializes boolean as "True"
            
            // System config values should still be preserved
            config["Orleans:ClusterId"].ShouldBe("TestCluster");
            config["Serilog:MinimumLevel"].ShouldBe("Information");
        }
        finally
        {
            // Cleanup
            if (File.Exists(systemConfigPath)) File.Delete(systemConfigPath);
            if (File.Exists(businessConfigPath)) File.Delete(businessConfigPath);
        }
    }
    
    [Fact]
    public void DynamicProtection_Should_Work_WithMultipleSystemConfigs()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var systemConfig1Path = Path.Combine(tempDir, $"system1-{Guid.NewGuid()}.json");
        var systemConfig2Path = Path.Combine(tempDir, $"system2-{Guid.NewGuid()}.json");
        var businessConfigPath = Path.Combine(tempDir, $"business-{Guid.NewGuid()}.json");
        
        try
        {
            // First system config
            var systemConfig1Content = @"{
  ""Orleans"": {
    ""ClusterId"": ""TestCluster""
  }
}";
            
            // Second system config
            var systemConfig2Content = @"{
  ""Serilog"": {
    ""MinimumLevel"": ""Information""
  },
  ""HttpApi"": {
    ""Port"": 8080
  }
}";
            
            // Business config trying to override keys from both system configs
            var businessConfigContent = @"{
  ""Orleans"": {
    ""ServiceId"": ""HackedService""
  },
  ""HttpApi"": {
    ""Port"": 9999
  },
  ""Business"": {
    ""Setting"": ""allowed""
  }
}";
            
            File.WriteAllText(systemConfig1Path, systemConfig1Content);
            File.WriteAllText(systemConfig2Path, systemConfig2Content);
            File.WriteAllText(businessConfigPath, businessConfigContent);
            
            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() =>
            {
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddAevatarSecureConfiguration(
                    systemConfigPaths: new[] { systemConfig1Path, systemConfig2Path },
                    businessConfigPath: businessConfigPath,
                    optional: false);
                
                configBuilder.Build();
            });
            
            // Should detect both Orleans and HttpApi override attempts
            exception.Message.ShouldContain("Orleans");
            exception.Message.ShouldContain("HttpApi");
        }
        finally
        {
            // Cleanup
            if (File.Exists(systemConfig1Path)) File.Delete(systemConfig1Path);
            if (File.Exists(systemConfig2Path)) File.Delete(systemConfig2Path);
            if (File.Exists(businessConfigPath)) File.Delete(businessConfigPath);
        }
    }
} 