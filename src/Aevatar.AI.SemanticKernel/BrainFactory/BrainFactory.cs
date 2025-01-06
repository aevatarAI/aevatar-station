using System;
using Aevatar.AI.Brain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.AI.BrainFactory;

public class BrainFactory : IBrainFactory
{
    private readonly Logger<BrainFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BrainFactory(Logger<BrainFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public IBrain? GetBrain(string llm)
    {
        try
        {
            return _serviceProvider.GetRequiredKeyedService<IBrain>(llm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when constructing the brain.");
            return null;
        }
    }
}