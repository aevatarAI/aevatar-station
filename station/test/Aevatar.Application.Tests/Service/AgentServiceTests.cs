using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Service;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service;

public class AgentServiceTests
{
    [Fact]
    public void AgentService_TypeExists_ShouldPass()
    {
        // Arrange & Act
        var serviceType = typeof(AgentService);
        
        // Assert - Simply verify the type exists and has expected methods
        serviceType.ShouldNotBeNull();
        serviceType.GetMethod("GetAllAgents").ShouldNotBeNull();
        serviceType.GetMethod("CreateAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("GetAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("UpdateAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("DeleteAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("RemoveAllSubAgentAsync").ShouldNotBeNull();
    }

    [Fact]
    public void AgentService_Constructor_RequiresDependencies()
    {
        // Arrange & Act
        var constructors = typeof(AgentService).GetConstructors();
        
        // Assert - Verify constructor exists and has expected parameters
        constructors.Length.ShouldBe(1);
        var constructor = constructors[0];
        var parameters = constructor.GetParameters();
        
        // Should have 10 parameters based on AgentService constructor
        parameters.Length.ShouldBe(10);
        
        // Verify some key parameter types exist
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("IClusterClient"));
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("ICQRSProvider"));
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("ILogger"));
    }
} 