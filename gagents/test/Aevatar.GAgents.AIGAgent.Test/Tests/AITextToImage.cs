using System.Net.Mime;
using System.Text;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;
using Shouldly;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.Tests;

public class AITextToImage : AevatarAIGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public AITextToImage()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    //[Fact]
    public async Task GenerateContentTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAITextToImage" }
        });

        var response =
            await chatAgent.GenerateImageAsync("I want a cat riding on an elephant's head.");

        response.ShouldNotBeNull();
        response.Count.ShouldBeGreaterThan(0);
        response[0].ResponseType.ShouldBe(TextToImageResponseType.Base64Content);
    }

    //[Fact]
    public async Task GenerateImageUrlTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAITextToImage" }
        });

        var response =
            await chatAgent.GenerateImageAsync("I want a cat riding on an elephant's head.",
                new TextToImageOption() { ResponseType = TextToImageResponseType.Url });

        response.ShouldNotBeNull();
        response.Count.ShouldBeGreaterThan(0);
        response[0].ResponseType.ShouldBe(TextToImageResponseType.Url);
    }

    //[Fact]
    public async Task TextToImageAsyncTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<IChatAIGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a nba player",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAITextToImage" }
        });

        await chatAgent.TextToImageAsync("I want a cat riding on an elephant's head.",
            new TextToImageOption() { ResponseType = TextToImageResponseType.Url });
        await Task.Delay(TimeSpan.FromSeconds(60));

        var state = await chatAgent.GetStateAsync();
        state.TextToImageResponses.Count.ShouldBeGreaterThan(0);
    }
}