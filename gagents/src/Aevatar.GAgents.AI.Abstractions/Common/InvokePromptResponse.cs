using System.Collections.Generic;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AI.Common;

public class InvokePromptResponse
{
    public List<ChatMessage> ChatReponseList { get; set; }
    public TokenUsageStatistics TokenUsageStatistics { get; set; }
}