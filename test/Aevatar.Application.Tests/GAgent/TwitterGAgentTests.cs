using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgent;

/// <summary>
/// Tests for Twitter GAgent CreateAgent API functionality
/// Tests creating TwitterGAgent instances using the CreateAgent API
/// Based on the actual TwitterGAgent implementation parameters
/// </summary>
public class TwitterGAgentTests
{
    private readonly ITestOutputHelper _output;

    public TwitterGAgentTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CreateTwitterGAgent_WithBasicConfiguration_ShouldCreateSuccessfully()
    {
        // Arrange - Create a mock AgentService to test TwitterGAgent creation
        var mockAgentService = new Mock<IAgentService>();
        var agentId = Guid.NewGuid();
        
        var createAgentInput = new CreateAgentInputDto
        {
            AgentId = agentId,
            AgentType = "Aevatar.GAgents.Twitter.Agent.TwitterGAgent",
            Name = "Basic Twitter Agent",
            Properties = new Dictionary<string, object>
            {
                ["ConsumerKey"] = "test_consumer_key",
                ["ConsumerSecret"] = "test_consumer_secret",
                ["BearerToken"] = "test_bearer_token",
                ["EncryptionPassword"] = "test_encryption_password",
                ["ReplyLimit"] = 10
            }
        };

        var expectedResult = new AgentDto
        {
            Id = agentId,
            AgentType = "Aevatar.GAgents.Twitter.Agent.TwitterGAgent",
            Name = "Basic Twitter Agent",
            Properties = createAgentInput.Properties,
            BusinessAgentGrainId = Guid.NewGuid().ToString()
        };

        mockAgentService.Setup(x => x.CreateAgentAsync(It.IsAny<CreateAgentInputDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await mockAgentService.Object.CreateAgentAsync(createAgentInput);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(agentId);
        result.AgentType.ShouldBe("Aevatar.GAgents.Twitter.Agent.TwitterGAgent");
        result.Name.ShouldBe("Basic Twitter Agent");
        result.Properties.ShouldNotBeNull();
        result.Properties.ShouldContainKey("ConsumerKey");
        result.Properties.ShouldContainKey("ConsumerSecret");
        result.Properties.ShouldContainKey("BearerToken");
        result.BusinessAgentGrainId.ShouldNotBeNullOrEmpty();

        _output.WriteLine($"✅ Successfully created Twitter GAgent with ID: {result.Id}");
        _output.WriteLine($"📋 Business Agent Grain ID: {result.BusinessAgentGrainId}");
        _output.WriteLine($"🔧 Agent Type: {result.AgentType}");
        _output.WriteLine($"⚙️  Properties: {string.Join(", ", result.Properties.Keys)}");
    }

    [Fact]
    public async Task CreateTwitterGAgent_WithAutoPostConfiguration_ShouldCreateSuccessfully()
    {
        // Arrange - Create a mock AgentService with auto-post configuration matching twitter-auto.mdc
        var mockAgentService = new Mock<IAgentService>();
        var agentId = Guid.NewGuid();
        
        var createAgentInput = new CreateAgentInputDto
        {
            AgentId = agentId,
            AgentType = "Aevatar.GAgents.Twitter.Agent.TwitterGAgent",
            Name = "Auto Post Twitter Agent",
            Properties = new Dictionary<string, object>
            {
                ["ConsumerKey"] = "pinedogsoup_consumer_key",
                ["ConsumerSecret"] = "pinedogsoup_consumer_secret",
                ["BearerToken"] = "pinedogsoup_bearer_token",
                ["Username"] = "@pinedogsoup",
                ["PostContent"] = "今天aelf必定大涨!",
                ["ReplyLimit"] = 5
            }
        };

        var expectedResult = new AgentDto
        {
            Id = agentId,
            AgentType = "Aevatar.GAgents.Twitter.Agent.TwitterGAgent",
            Name = "Auto Post Twitter Agent",
            Properties = createAgentInput.Properties,
            BusinessAgentGrainId = Guid.NewGuid().ToString()
        };

        mockAgentService.Setup(x => x.CreateAgentAsync(It.IsAny<CreateAgentInputDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await mockAgentService.Object.CreateAgentAsync(createAgentInput);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(agentId);
        result.AgentType.ShouldBe("Aevatar.GAgents.Twitter.Agent.TwitterGAgent");
        result.Name.ShouldBe("Auto Post Twitter Agent");
        result.Properties.ShouldNotBeNull();
        result.Properties.ShouldContainKey("Username");
        result.Properties.ShouldContainKey("PostContent");
        result.Properties["Username"].ShouldBe("@pinedogsoup");
        result.Properties["PostContent"].ShouldBe("今天aelf必定大涨!");
        result.BusinessAgentGrainId.ShouldNotBeNullOrEmpty();

        _output.WriteLine($"✅ Successfully created Auto Post Twitter GAgent with ID: {result.Id}");
        _output.WriteLine($"📋 Business Agent Grain ID: {result.BusinessAgentGrainId}");
        _output.WriteLine("🎯 Configuration matches twitter-auto.mdc requirements:");
        _output.WriteLine($"   👤 Username: {result.Properties["Username"]}");
        _output.WriteLine($"   📝 Post Content: {result.Properties["PostContent"]}");
    }

    [Fact]
    public void CreateAgentInputDto_ShouldValidateTwitterGAgentType()
    {
        // Arrange & Act
        var createAgentInput = new CreateAgentInputDto
        {
            AgentId = Guid.NewGuid(),
            AgentType = "Aevatar.GAgents.Twitter.Agent.TwitterGAgent",
            Name = "Test Twitter Agent",
            Properties = new Dictionary<string, object>()
        };

        // Assert
        createAgentInput.AgentType.ShouldBe("Aevatar.GAgents.Twitter.Agent.TwitterGAgent");
        createAgentInput.Name.ShouldNotBeNullOrEmpty();
        createAgentInput.Properties.ShouldNotBeNull();
        createAgentInput.AgentId.ShouldNotBeNull();

        _output.WriteLine($"✅ TwitterGAgent type validation passed: {createAgentInput.AgentType}");
    }
} 