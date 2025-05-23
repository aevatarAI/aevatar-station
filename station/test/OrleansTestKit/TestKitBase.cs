using Aevatar.Agents;
using Aevatar.Application.Grains.Agents.Group;
using Aevatar.Application.Grains.Agents.Publisher;
using Aevatar.Sender;

namespace Orleans.TestKit;

/// <summary>A unit test base class that provides a default mock grain activation context.</summary>
public abstract class TestKitBase
{
    protected TestKitSilo Silo { get; } = new();
}