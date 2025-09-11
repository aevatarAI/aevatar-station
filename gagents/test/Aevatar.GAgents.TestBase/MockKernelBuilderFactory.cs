using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

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
            .ReturnsAsync(new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "Mock Content")
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