using System.Threading.Tasks;

namespace Aevatar.GAgents.PumpFun.Provider;

public interface IPumpFunProvider
{
    public Task SendMessageAsync(string replyId, string replyMessage);
}