using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Common;

namespace Aevatar.AI.Brain;

public interface IBrain
{
    Task InitializeAsync(string id, string description, bool ifSupportKnowledge = false);

    Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null);

    Task<List<ChatMessage>> InvokePromptAsync(string content, List<ChatMessage>? history = null);
}