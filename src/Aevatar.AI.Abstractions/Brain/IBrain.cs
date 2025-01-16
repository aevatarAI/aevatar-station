using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.AI.Brain;

public interface IBrain
{
    Task<bool> InitializeAsync(string id, string promptTemplate, List<File>? files = null);
    Task<string?> InvokePromptAsync(string prompt);
}