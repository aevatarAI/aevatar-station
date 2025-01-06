using System.Threading.Tasks;

namespace Aevatar.AI.Brain;

public interface IBrain
{
    Task<string?> InvokePromptAsync(string prompt);
}