using System;
using System.Collections.Generic;
using Aevatar.AI.Brain;
using Aevatar.AI.BrainProvider;
using Aevatar.AI.Dtos;
using Microsoft.Extensions.Logging;

namespace Aevatar.AI.BrainFactory;

public class BrainFactory : IBrainFactory
{
    private readonly Logger<BrainFactory> _logger;
    private readonly IEnumerable<IBrainProvider> _brainProviders;

    public BrainFactory(IEnumerable<IBrainProvider> brainProviders, Logger<BrainFactory> logger)
    {
        _brainProviders = brainProviders;
        _logger = logger;
    }

    public IBrain? GetBrain(Guid guid, InitializeDto dto)
    {
        try
        {
            foreach (var provider in _brainProviders)
            {
                if (provider.GetBrainType() == dto.LLM)
                {
                    return provider.GetBrain(guid, dto);
                }
            }
            throw new ArgumentException($"Unsupported LLM type: {dto.LLM}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when constructing the brain.");
            return null;
        }
    }
}