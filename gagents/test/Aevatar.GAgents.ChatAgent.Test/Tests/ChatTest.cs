using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent;
using Aevatar.GAgents.ChatAgent.Test.GAgents;
using Shouldly;
using Volo.Abp.BlobStoring;

namespace Aevatar.GAgents.ChatAgent.Test.Tests;

public sealed class ChatTest : AevatarChatGAgentTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly IBlobContainer _blobContainer;

    public ChatTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _blobContainer = GetRequiredService<IBlobContainer>();
    }

    [Fact]
    public async Task ImageTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<ITestChatGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a image reader",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });
        
        var result =
            await chatAgent.ChatAsync("Hello");
        result[0].Content.ShouldBe("Mock Content");

        var history = (await chatAgent.GetStateAsync()).ChatHistory;
        history.Count.ShouldBe(2);
        history[0].Content.ShouldBe("Hello");
        history[0].ImageKeys.ShouldBeNull();

        result =
            await chatAgent.ChatAsync("Obtain the content of the picture", imageKeys: new List<string> { "image1.png" });
        result[0].Content.ShouldBe("Mock Content");

        history = (await chatAgent.GetStateAsync()).ChatHistory;
        history.Count.ShouldBe(4);
        history[2].Content.ShouldBe("Obtain the content of the picture");
        history[2].ImageKeys.Count.ShouldBe(1);
        history[2].ImageKeys[0].ShouldBe("image1.png");
    }

    [Fact]
    public async Task ImageStreamTest()
    {
        var chatAgent = await _agentFactory.GetGAgentAsync<ITestChatGAgent>(Guid.NewGuid());
        await chatAgent.InitializeAsync(new InitializeDto()
        {
            Instructions = "you are a image reader",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });

        var result =
            await chatAgent.ChatWithStreamAsync("Hello",
                new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() });
        result.ShouldBe(true);
        var history = (await chatAgent.GetStateAsync()).ChatHistory;
        history.Count.ShouldBe(1);
        await Task.Delay(TimeSpan.FromSeconds(1));

        history = (await chatAgent.GetStateAsync()).ChatHistory;
        history.Count.ShouldBe(2);
        history[0].Content.ShouldBe("Hello");
        history[0].ImageKeys.ShouldBeNull();
        
        result =
            await chatAgent.ChatWithStreamAsync("Obtain the content of the picture",
                new AIChatContextDto() { ChatId = Guid.NewGuid().ToString() }, imageKeys: new List<string> { "image1.png" });
        result.ShouldBe(true);
        history = (await chatAgent.GetStateAsync()).ChatHistory;
        history.Count.ShouldBe(3);
        await Task.Delay(TimeSpan.FromSeconds(1));

        history = (await chatAgent.GetStateAsync()).ChatHistory;
        history.Count.ShouldBe(4);
        history[2].Content.ShouldBe("Obtain the content of the picture");
        history[2].ImageKeys.Count.ShouldBe(1);
        history[2].ImageKeys[0].ShouldBe("image1.png");
    }
}