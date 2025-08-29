using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AI.Brain;

public interface IChatBrain : IBrain
{
    Task<InvokePromptResponse?> InvokePromptAsync(string content, List<string>? imageKeys = null, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default);

    Task<IAsyncEnumerable<object>> InvokePromptStreamingAsync(string content, List<string>? imageKeys = null, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default);

    TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList);
}