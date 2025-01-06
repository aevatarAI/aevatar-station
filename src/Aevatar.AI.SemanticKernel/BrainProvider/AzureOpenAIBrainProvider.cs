using System;
using Aevatar.AI.Brain;
using Aevatar.AI.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.AI.BrainProvider;

public class AzureOpenAIBrainProvider : IBrainProvider
{
    private readonly IServiceProvider _serviceProvider;

    public AzureOpenAIBrainProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBrain GetBrain(Guid guid, InitializeDto dto)
    {
        return ActivatorUtilities.CreateInstance<AzureOpenAIBrain>(
            _serviceProvider, guid, dto.Instructions);
    }

    public string GetBrainType()
    {
        return "AzureOpenAI";
    }
}