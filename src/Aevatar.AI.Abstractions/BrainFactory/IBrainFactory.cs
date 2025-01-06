using System;
using Aevatar.AI.Brain;
using Aevatar.AI.Dtos;

namespace Aevatar.AI.BrainFactory;

public interface IBrainFactory
{
    IBrain? GetBrain(Guid guid, InitializeDto dto);
}