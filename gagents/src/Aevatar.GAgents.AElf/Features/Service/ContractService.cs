using System.Threading.Tasks;
using Aevatar.GAgents.AElf.Provider;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace Aevatar.GAgents.AElf.Service;

public class ContractService : IContractService
{
    private readonly IAElfNodeProvider _AElfNodeProvider;

    public  ContractService(IAElfNodeProvider AElfNodeProvider)
    {
        _AElfNodeProvider = AElfNodeProvider;
    }

    public async Task<string> SendTransferAsync(string chainId, string senderName, TransferInput transferInput)
    {
      var transaction =  await  _AElfNodeProvider.CreateTransactionAsync(chainId, senderName, "", "Transfer",
            transferInput);
      var sendTransactionOutput = await _AElfNodeProvider.SendTransactionAsync(chainId, transaction);
      return sendTransactionOutput.TransactionId;
    }
}