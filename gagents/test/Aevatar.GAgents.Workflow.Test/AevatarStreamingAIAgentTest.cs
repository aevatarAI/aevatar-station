using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.SocialChat.GAgent;
using Aevatar.GAgents.Workflow.Test.TestGAgents;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Workflow.Test;

public class AevatarStreamingAIAgentTest : AevatarWorkflowTestBase
{
    private readonly IGAgentFactory _gAgentFactory;


    public AevatarStreamingAIAgentTest(ITestOutputHelper outputHelper)
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact(Skip = "暂时跳过这个测试，存在问题需要后续修复")]
    public async Task StreamingAIGagentTest()
    {
        var socialAgent = await _gAgentFactory.GetGAgentAsync<ISocialGAgent>(Guid.NewGuid());
        await socialAgent.ConfigAsync(new ChatConfigDto()
        {
            Instructions = "You are a social agent",
            MaxHistoryCount = 10,
            StreamingModeEnabled = true,
            StreamingConfig = new StreamingConfig()
            {
                BufferingSize = 64,
                TimeOutInternal = 300000
            },
            LLMConfig = new LLMConfigDto() {
                SystemLLM = "OpenAI",
            }
        });
        
        var groupGAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        await groupGAgent.RegisterAsync(socialAgent);
        
        var testAIStreamingGAgent = await _gAgentFactory.GetGAgentAsync<IAIStreamingGAgent>(Guid.NewGuid());
        await groupGAgent.RegisterAsync(testAIStreamingGAgent);

        var requestId = Guid.NewGuid();
        var messageId = "testMessageId";
        var chatId = "test";

        var response = await socialAgent.ChatAsync("Who are you? And how old are you?", aiChatContextDto: new AIChatContextDto()
        {
            RequestId = requestId,
            MessageId = messageId,
            ChatId = chatId
        });
        await Task.Delay(3000);
        var content = await testAIStreamingGAgent.GetContent(requestId);
        content.ShouldBe("Mock Content");
    }
}