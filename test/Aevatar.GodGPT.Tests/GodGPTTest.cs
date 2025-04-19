using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.GodGPT.Tests;

public class GodGPTTest : AevatarOrleansTestBase<AevatarGodGPTTestsMoudle>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _agentFactory;
    
    public GodGPTTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }
    
    [Fact]
    public async Task ChatAsync_Test()
    {
        var grainId = Guid.NewGuid();
        _testOutputHelper.WriteLine($"Chat Manager GrainId: {grainId.ToString()}");
        
        var chatManagerGAgent = await _agentFactory.GetGAgentAsync<IChatManagerGAgent>();
        var godGAgentId = await chatManagerGAgent.CreateSessionAsync("OpenAI", string.Empty, new UserProfileDto
        {
            Gender = "Male",
            BirthDate = DateTime.UtcNow,
            BirthPlace = "BeiJing",
            FullName = "Test001"
        });
        _testOutputHelper.WriteLine($"God GAgent GrainId: {godGAgentId.ToString()}");

        var chatId = Guid.NewGuid();
        _testOutputHelper.WriteLine($"ChatId: {chatId.ToString()}");
        
        var godChat = await _agentFactory.GetGAgentAsync<IGodChat>(godGAgentId);
        await godChat.GodStreamChatAsync(grainId, "OpenAI", true, "Who are you",
            chatId.ToString(), null);
        await Task.Delay(TimeSpan.FromSeconds(2000));
        var chatMessage = await godChat.GetChatMessageAsync();
        _testOutputHelper.WriteLine($"chatMessage: {JsonConvert.SerializeObject(chatMessage)}");
        chatMessage.ShouldNotBeEmpty();
        chatMessage.Count.ShouldBe(2);
    }
}