using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BlobStoring;

namespace Aevatar.GAgents.TestBase;

public class MockBrainFactory : IBrainFactory
{
    private readonly IKernelBuilderFactory _kernelBuilderFactory;
    private readonly ILogger<MockBrainFactory> _logger;
    private readonly IOptions<RagConfig> _options;
    private readonly IBlobContainer _blobContainer;

    public MockBrainFactory(IKernelBuilderFactory kernelBuilderFactory, ILogger<MockBrainFactory> logger,
        IOptions<RagConfig> options, IBlobContainer blobContainer)
    {
        _kernelBuilderFactory = kernelBuilderFactory;
        _logger = logger;
        _options = options;
        _blobContainer = blobContainer;
    }

    public IBrain? CreateBrain(LLMProviderConfig llmProviderConfig)
    {
        return new MockBrain(_kernelBuilderFactory, _logger, _options, _blobContainer);
    }

    public IChatBrain? GetChatBrain(LLMProviderConfig llmProviderConfig)
    {
        return new MockBrain(_kernelBuilderFactory, _logger, _options, _blobContainer);
    }

    public ITextToImageBrain? GetTextToImageBrain(LLMProviderConfig llmProviderConfig)
    {
        throw new NotImplementedException();
    }
}