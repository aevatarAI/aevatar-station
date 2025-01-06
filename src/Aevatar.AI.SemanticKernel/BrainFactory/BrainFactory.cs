using System;
using Aevatar.AI.Brain;
using Aevatar.AI.Dtos;
using Microsoft.Extensions.Logging;

namespace Aevatar.AI.BrainFactory;

public class BrainFactory : IBrainFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Logger<BrainFactory> _logger;

    public BrainFactory(IServiceProvider serviceProvider, Logger<BrainFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IBrain? GetBrain(Guid guid, InitializeDto dto)
    {
        try
        {
            switch (dto.LLM)
            {
                case "AzureOpenAI":
                    return new AzureOpenAIBrain(_serviceProvider, guid, dto.Instructions);
                // Add more cases here for other LLM types if needed
                default:
                    throw new ArgumentException($"Unsupported LLM type: {dto.LLM}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when constructing the brain.");
            return null;
        }
    }
}