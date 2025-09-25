using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using System.Linq;

namespace Aevatar.GAgents.TestBase;

public class MockKernelBuilderFactory : IKernelBuilderFactory
{
    private readonly Mock<IChatCompletionService> _mockChatService;

    public MockKernelBuilderFactory()
    {
        _mockChatService = new Mock<IChatCompletionService>();
        SetupDefaultMockBehavior();
    }

    public Mock<IChatCompletionService> MockChatService => _mockChatService;

    public IKernelBuilder GetKernelBuilder(string id)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton(typeof(IChatCompletionService), _mockChatService.Object);
        
        return kernelBuilder;
    }

    private void SetupDefaultMockBehavior()
    {
        _mockChatService.Setup(x => x.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatHistory history, PromptExecutionSettings settings, Kernel kernel, CancellationToken token) =>
            {
                string responseContent = "Mock Content";
                
                // Check if this is a router prompt by examining the chat history
                var isRouterPrompt = history.Any(msg => 
                    msg.Content != null && (
                        msg.Content.Contains("task routing assistant") || 
                        msg.Content.Contains("Available Agents and Their Events")
                    ));

                if (isRouterPrompt)
                {
                    // Check for specific test scenarios
                    var hasTwitterAndBitcoin = history.Any(msg => 
                        msg.Content != null && 
                        msg.Content.Contains("bitcoin") && 
                        msg.Content.Contains("tweet"));

                    if (hasTwitterAndBitcoin)
                    {
                        // Return mock RouterOutputSchema JSON for bitcoin tweet scenario
                        responseContent = @"{
  ""agentName"": ""Proxy_IBlockChainGAgent"",
  ""eventName"": ""GetBitcoinPriceGEvent"",
  ""parameters"": ""{}"",
  ""complete"": false,
  ""terminated"": false,
  ""reason"": ""Need to get the current bitcoin price before posting the tweet""
}";
                    }
                    else
                    {
                        // Generic completed response
                        responseContent = @"{
  ""agentName"": """",
  ""eventName"": """",
  ""parameters"": """",
  ""complete"": true,
  ""terminated"": false,
  ""reason"": ""Task has been completed successfully""
}";
                    }
                }

                return new List<ChatMessageContent>
                {
                    new ChatMessageContent(AuthorRole.Assistant, responseContent)
                };
            });

        _mockChatService.Setup(x => x.GetStreamingChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(GetMockStreamingResponse());
    }

    private async IAsyncEnumerable<StreamingChatMessageContent> GetMockStreamingResponse()
    {
        var responses = new[] { "Mock", "Streaming", "Content"};
        foreach (var response in responses)
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, response);
            await Task.Delay(50);
        }
    }
}