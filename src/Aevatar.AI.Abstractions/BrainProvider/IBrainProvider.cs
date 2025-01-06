using System;
using Aevatar.AI.Brain;
using Aevatar.AI.Dtos;

namespace Aevatar.AI.BrainProvider;

public interface IBrainProvider
{
    IBrain GetBrain(Guid guid, InitializeDto dto);
    string GetBrainType();
}