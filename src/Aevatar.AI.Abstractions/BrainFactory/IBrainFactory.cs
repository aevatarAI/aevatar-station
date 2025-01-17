using System;
using Aevatar.AI.Brain;

namespace Aevatar.AI.BrainFactory;

public interface IBrainFactory
{
    IBrain? GetBrain(string llm);
}