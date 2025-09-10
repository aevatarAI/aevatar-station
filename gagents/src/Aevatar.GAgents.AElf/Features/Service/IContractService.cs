using System.Threading.Tasks;
using AElf.Contracts.MultiToken;

namespace Aevatar.GAgents.AElf.Service;

public interface IContractService
{
  
    public Task<string>  SendTransferAsync(string chainId, string senderName, TransferInput transferInput);
}