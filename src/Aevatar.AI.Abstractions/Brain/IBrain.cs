using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.AI.Brain;

public interface IBrain
{
    bool Initialize(Guid guid, string promptTemplate, List<File> files);
    Task<string?> InvokePromptAsync(string prompt);
}