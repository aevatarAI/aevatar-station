using System.Threading.Tasks;
using Aevatar.AI.Dtos;

namespace Aevatar.AI.Agent;

public interface IAIGAgent
{
    Task<bool> InitializeAsync(InitializeDto dto);
}