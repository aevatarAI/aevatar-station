using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Json.Schema.Generation;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

[Description("AgentTest")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class AgentTest : GAgentBase<FrontAgentState, FrontTestEvent, EventBase>, IFrontAgentTest
{
    private readonly ILogger<AgentTest> _logger;

    public AgentTest(ILogger<AgentTest> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("this is used for front test");
    }

    public async Task PublishEventAsync(FrontTestCreateEvent @event)
    {
        _logger.LogInformation("FrontTestCreateEvent: {name}", @event.PromptTemplate);
        RaiseEvent(new FrontTestCreateSEvent
        {
            Id = Guid.NewGuid(),
            LLM = new LLMConfig
            {
                ProviderEnum = LLMProviderEnum.Azure,
                ModelIdEnum = ModelIdEnum.OpenAI,
                ModelName = "Test",
                Endpoint = "Http://localhost",
                ApiKey = "qwe"
            },
            PromptTemplate = @event.PromptTemplate,
            IfUpsertKnowledge = true,
            InputTokenUsage = 10.9,
            OutTokenUsage = 10,
            TotalTokenUsage = 10,
            Time = DateTime.Today,
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleFrontTestCreateEvent(FrontTestCreateEvent @event)
    {
        _logger.LogInformation("FrontTestCreateEvent: {name}", @event.PromptTemplate);
        RaiseEvent(new FrontTestCreateSEvent
        {
            Id = Guid.NewGuid(),
            LLM = new LLMConfig
            {
                ProviderEnum = LLMProviderEnum.Azure,
                ModelIdEnum = ModelIdEnum.OpenAI,
                ModelName = "Test",
                Endpoint = "Http://localhost",
                ApiKey = "qwe"
            },
            PromptTemplate = @event.PromptTemplate,
            IfUpsertKnowledge = true,
            InputTokenUsage = 10,
            OutTokenUsage = 10,
            TotalTokenUsage = 10,
            Time = DateTime.Today,
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(FrontAgentState state, StateLogEventBase<FrontTestEvent> @event)
    {
        switch (@event)
        {
            case FrontTestCreateSEvent frontTestCreateSEvent:
                state.PromptTemplate = frontTestCreateSEvent.PromptTemplate;
                state.IfUpsertKnowledge = frontTestCreateSEvent.IfUpsertKnowledge;
                state.InputTokenUsage = frontTestCreateSEvent.InputTokenUsage;
                state.OutTokenUsage = frontTestCreateSEvent.OutTokenUsage;
                state.Time = frontTestCreateSEvent.Time;
                state.LLM = frontTestCreateSEvent.LLM;
                state.Id = this.GetPrimaryKey();
                state.ApiCount = (decimal)9.01;
                state.LLM.Memo = new Dictionary<string, object>()
                {
                    { "qwe", "sda" }
                };

                break;
        }
    }
}

public interface IFrontAgentTest : IGAgent
{
    Task PublishEventAsync(FrontTestCreateEvent frontTestCreateEvent);
}

[GenerateSerializer]
public class FrontAgentState : StateBase
{
    [Id(0)] public LLMConfig? LLM { get; set; }
    [Id(1)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(2)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(3)] public double InputTokenUsage { get; set; } = 0;
    [Id(4)] public int OutTokenUsage { get; set; } = 0;
    [Id(5)] public int TotalTokenUsage { get; set; } = 0;
    [Id(6)] public DateTime Time { get; set; }
    [Id(7)] public Guid Id { get; set; }
    [Id(8)] public decimal ApiCount { get; set; }
    [Id(9)] public LLMProviderEnum ProviderEnum { get; set; }
}

[GenerateSerializer]
public class FrontTestEvent : StateLogEventBase<FrontTestEvent>
{
}

[GenerateSerializer]
public class FrontTestCreateSEvent : FrontTestEvent
{
    [Id(0)] public LLMConfig? LLM { get; set; }
    [Id(1)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(2)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(3)] public double InputTokenUsage { get; set; } = 0;
    [Id(4)] public int OutTokenUsage { get; set; } = 0;
    [Id(5)] public int TotalTokenUsage { get; set; } = 0;
    [Id(6)] public DateTime Time { get; set; }
}

[GenerateSerializer]
public class FrontTestCreateEvent : EventBase
{
    [Id(0)] public string PromptTemplate { get; set; } = string.Empty;
}