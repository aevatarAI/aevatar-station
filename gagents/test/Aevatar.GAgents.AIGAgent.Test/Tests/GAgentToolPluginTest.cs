using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Plugin;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Detailed tests for GAgentToolPlugin functionality
/// </summary>
public sealed class GAgentToolPluginTest : AevatarAIGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _agentFactory;
    private readonly IGAgentService _gAgentService;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly ILogger<GAgentToolPlugin> _logger;

    public GAgentToolPluginTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentService = GetRequiredService<IGAgentService>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
        _logger = GetRequiredService<ILogger<GAgentToolPlugin>>();
    }

    //[Fact]
    public async Task InvokeGAgent_Should_Execute_Valid_Event()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Create a test agent first
        var testAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await testAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "Test agent for plugin",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        // Get available GAgents
        var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();

        // Find the ChatAIGAgent type
        GrainType? chatAgentType = null;
        foreach (var kvp in allGAgents)
        {
            if (kvp.Key.ToString().Contains("ChatAIGAgent"))
            {
                chatAgentType = kvp.Key;
                break;
            }
        }

        chatAgentType.ShouldNotBeNull();

        // Act - Try to invoke with a valid event
        var parameters = JsonSerializer.Serialize(new { Message = "Test message" });
        var result = await plugin.InvokeGAgentAsync(
            chatAgentType.Value.ToString(),
            "ChatEvent",
            parameters
        );

        // Assert
        result.ShouldNotBeNullOrEmpty();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        response.ShouldNotBeNull();
        response.ShouldContainKey("success");
    }

    //[Fact]
    public async Task InvokeGAgent_Should_Handle_Invalid_GrainType()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Act
        var result = await plugin.InvokeGAgentAsync(
            "invalid.grain.type",
            "SomeEvent",
            "{}"
        );

        // Assert
        result.ShouldNotBeNullOrEmpty();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        response.ShouldNotBeNull();
        response["success"].ToString().ShouldBe("False");
        response.ShouldContainKey("error");
    }

    //[Fact]
    public async Task InvokeGAgent_Should_Handle_Invalid_Event_Type()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Get a valid grain type
        var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
        var firstGrainType = allGAgents.Keys.First();

        // Act
        var result = await plugin.InvokeGAgentAsync(
            firstGrainType.ToString(),
            "NonExistentEvent",
            "{}"
        );

        // Assert
        result.ShouldNotBeNullOrEmpty();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        response.ShouldNotBeNull();
        response["success"].ToString().ShouldBe("False");
        response.ShouldContainKey("error");
    }

    //[Fact]
    public async Task InvokeGAgent_Should_Handle_Invalid_JSON_Parameters()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Get a valid grain type
        var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
        var firstGrainType = allGAgents.Keys.First();
        var firstEventType = allGAgents[firstGrainType][0];

        // Act
        var result = await plugin.InvokeGAgentAsync(
            firstGrainType.ToString(),
            firstEventType.Name,
            "invalid json {{"
        );

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _testOutputHelper.WriteLine(result);
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        response.ShouldNotBeNull();
        response["success"].ToString().ShouldBe("False");
        response.ShouldContainKey("error");
        response["error"].ToString().ShouldContain("Unexpected character");
    }

    //[Fact]
    public async Task ListGAgents_Should_Return_All_Available_GAgents()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Act
        var result = await plugin.ListGAgentsAsync();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _testOutputHelper.WriteLine(result);
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        response.ShouldNotBeNull();
        response["success"].ToString().ShouldBe("True");
        response.ShouldContainKey("total");
        response.ShouldContainKey("gAgents");

        // Verify it's a valid number
        var total = int.Parse(response["total"].ToString());
        total.ShouldBeGreaterThanOrEqualTo(0);
    }

    //[Fact]
    public async Task GetGAgentInfo_Should_Return_Valid_Info_For_Existing_GAgent()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Get available GAgents
        var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
        var firstGrainType = allGAgents.Keys.First();

        // Act
        var result = await plugin.GetGAgentInfoAsync(firstGrainType.ToString());

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _testOutputHelper.WriteLine(result);
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        response.ShouldNotBeNull();
        response["success"].ToString().ShouldBe("True");
        var info = response["info"].ToString();
        info.ShouldNotBeNull();
        _testOutputHelper.WriteLine(info);
        info.ShouldContain("grainType");
        info.ShouldContain("events");
        info.ShouldContain("description");
    }

    //[Fact]
    public async Task GetGAgentInfo_Should_Handle_Non_Existent_GAgent()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Act
        var result = await plugin.GetGAgentInfoAsync("non.existent.gagent");

        // Assert
        result.ShouldNotBeNullOrEmpty();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        response.ShouldNotBeNull();
        response["success"].ToString().ShouldBe("False");
        response.ShouldContainKey("error");
        response["error"].ToString().ShouldContain("No active nodes are compatible with grain");
    }

    //[Fact]
    public async Task Plugin_Should_Handle_Null_Parameters_Gracefully()
    {
        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Act & Assert - These should not throw but return error responses
        var result1 = await plugin.InvokeGAgentAsync(null!, "Event", "{}");
        result1.ShouldContain("error");

        var result2 = await plugin.InvokeGAgentAsync("grain", null!, "{}");
        result2.ShouldContain("error");

        var result3 = await plugin.InvokeGAgentAsync("grain", "Event", null!);
        result3.ShouldContain("error");
    }

    //[Fact]
    public async Task Plugin_Should_List_Multiple_Event_Types_Per_GAgent()
    {
        // Verify that GAgents with multiple event handlers are properly listed

        // Arrange
        var plugin = new GAgentToolPlugin(_gAgentExecutor, _gAgentService, _logger);

        // Act
        var listResult = await plugin.ListGAgentsAsync();
        _testOutputHelper.WriteLine(listResult);
        var listResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(listResult);
        listResponse.ShouldNotBeNull();

        // Find a GAgent with multiple events
        var gagentsElement = listResponse["gAgents"];
        gagentsElement.ShouldNotBeNull();

        // The response should contain GAgents with their event lists
        var gagentsJson = gagentsElement.ToString();
        gagentsJson.ShouldNotBeNullOrEmpty();

        // Verify structure
        gagentsJson.ShouldContain("grainType");
        gagentsJson.ShouldContain("events");
    }
}