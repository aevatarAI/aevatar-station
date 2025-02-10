using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Common;

namespace Aevatar.AI.Brain;

public interface IBrain
{
    Task InitBrainAsync(string id, string description, bool ifSupportKnowledge = false);
    Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null);
    // Task<string?> ChatAsync(string content);
    Task<List<ChatMessage>> ChatWithHistoryAsync(List<ChatMessage>? history, string content);
}