using Microsoft.Extensions.Logging;
using Orleans;

namespace SimpleAIGAgent.Grains.Agents;

public interface IHello : IGrainWithIntegerKey
{
    ValueTask<string> SayHello(string greeting);
}

public class HelloGrain : Grain, IHello
{
    private readonly ILogger _logger;

    public HelloGrain(ILogger<HelloGrain> logger) => _logger = logger;

    ValueTask<string> IHello.SayHello(string greeting)
    {
        _logger.LogInformation("""
                               SayHello message received: greeting = "{Greeting}"
                               """,
            greeting);
        
        return ValueTask.FromResult($"""

                                     Client said: "{greeting}", so HelloGrain says: Hello!
                                     """);
    }
}