using AElf.Client.Dto;
using AElf.Types;

namespace Aevatar.GAgents.AElf.Provider;
using System.Threading.Tasks;
using Google.Protobuf;

public interface IAElfNodeProvider
{
   
    Task<Transaction> CreateTransactionAsync(string chainId, string senderName, string contractAddress, string methodName, IMessage param);

  
    Task<SendTransactionOutput> SendTransactionAsync(string chainId, Transaction transaction);

   
    Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId);

   
    Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class, IMessage<T>, new();
}