using System.ComponentModel;
using Aevatar.Code;
using Aevatar.Code.GEvents;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Code;

[Description("Handle Agent Combination")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class CodeGAgent : GAgentBase<CodeGAgentState, CodeAgentGEvent>, ICodeGAgent
{
    public CodeGAgent(ILogger<CodeGAgent> logger) 
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This agent is used to store the code needed to create a webhook.");
    }

    public async Task UploadCodeAsync(string webhookId, string version, byte[] codeBytes)
    {
        var addCodeAgentGEvent  = new AddCodeAgentGEvent
        {
            Ctime = DateTime.UtcNow,
            WebhookId = webhookId,
            WebhookVersion = version,
            Code = codeBytes
        };
        RaiseEvent(addCodeAgentGEvent);
        await ConfirmEvents();
    }
}

public interface ICodeGAgent : IStateGAgent<CodeGAgentState>
{
    Task UploadCodeAsync(string webhookId, string version, byte[] codeBytes);
}
