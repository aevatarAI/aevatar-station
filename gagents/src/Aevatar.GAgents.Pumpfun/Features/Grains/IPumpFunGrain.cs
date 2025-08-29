using System.Threading.Tasks;
using Orleans;

namespace Aevatar.GAgents.PumpFun.Grains;

public interface IPumpFunGrain : IGrainWithGuidKey
{
    public Task SendMessageAsync(string replyId, string? replyMessage);
   
}