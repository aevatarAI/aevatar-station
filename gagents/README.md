# aevatar-gagents

## 🚀 Introduction
 **Aevatar GAgents** is a custom intelligent agent solution designed to enable developers to customize agents and quickly create, manage, and deploy them on **Aevatar Station**.
 
## Prerequisites
### 1. Tech Stack
- .NET 8.0 SDK
- ABP 8.2.0
- Orleans 7.0
- Orleans Event Sourcing
- Orleans Stream
### 2. Dependency package
- dotnet add package Aevatar.Core --version 1.0.2 --source https://www.myget.org/F/aelf-project-dev/api/v3/index.json
- dotnet add package Aevatar.EventSourcing.Core --version 1.0.2 --source https://www.myget.org/F/aelf-project-dev/api/v3/index.json
- dotnet add package Aevatar.Core.Abstractions --version 1.0.2 --source https://www.myget.org/F/aelf-project-dev/api/v3/index.json

## How to create an Agent?

### 1. Create a class for Agent storage, and the class must inherit from StateBase.
**For example:**
```csharp
[GenerateSerializer]
public class TwitterGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Id(1)] public string UserId { get; set; }
    [Id(2)] public string Token { get; set; }
    [Id(3)] public string TokenSecret { get; set; }
    [Id(4)] public Dictionary<string, string> RepliedTweets { get; set; }
    [Id(5)] public string UserName { get; set; }
    ....
}
```

### 2. Create a class for EventSourcing **RaiseEvent**,and the class must inherit from SEventBase.
**For example:**
```csharp
public class TweetGEvent : SEventBase
{
    [Id(0)] public string Text { get; set; }
}
```

### 3. Create a class for the Agent to receive external messages. and the class must inherit from EventBase.
**For example:**
```csharp
[GenerateSerializer]
public class CreateTweetGEvent:EventBase
{
    [Id(0)]  public string Text { get; set; }
}
```
⚠️⚠️ '[GenerateSerializer]' GenerateSerializerAttribute and ‘[Id(0)]’ IdAttribute is necessary.⚠️⚠️

### 4. Create an Agent and inherit from GAgentBase<TState, TEvent>
**For example:**
``` csharp
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TwitterGAgent : GAgentBase<TwitterGAgentState, TweetGEvent>, ITwitterGAgent
{
    private readonly ILogger<TwitterGAgent> _logger;

    public TwitterGAgent(ILogger<TwitterGAgent> logger) : base(logger)
    {
        _logger = logger;
    }
    
    [EventHandler]
    public async Task HandleEventAsync(CreateTweetGEvent @event)
    {
        _logger.LogDebug("HandleEventAsync CreateTweetEvent, text: {text}", @event.Text);
        if (@event.Text.IsNullOrEmpty())
        {
            return;
        }
        
        if (State.UserId.IsNullOrEmpty())
        {
            _logger.LogDebug("HandleEventAsync SocialResponseEvent null userId");
            return;
        }
        
        await PublishAsync(new SocialGEvent()
        {
            Content = @event.Text
        });
    }
}
```
Explanation:
- TwitterGAgentState: Data that needs to be stored by TwitterGAgent
- TweetGEvent: Types of Event Sourcing
- Function 'HandleEventAsync(CreateTweetGEvent @event)' Used to handle 'CreateTweetGEvent'.
⚠️⚠️'[EventHandler]‘ EventHandlerAttribute is necessary ⚠️⚠️

## License
Distributed under the MIT License. See [License](LICENSE) for more information.