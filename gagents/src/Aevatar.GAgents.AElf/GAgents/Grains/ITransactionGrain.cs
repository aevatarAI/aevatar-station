using System.Threading.Tasks;
using Aevatar.GAgents.AElf.Dto;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.Grains;

public interface ITransactionGrain:IGrainWithGuidKey
{
    Task <TransactionDto> SendAElfTransactionAsync(SendTransactionDto sendTransactionDto);
    Task <TransactionDto> LoadAElfTransactionResultAsync(QueryTransactionDto queryTransactionDto);
    
    Task <TransactionDto> GetAElfTransactionAsync(QueryTransactionDto queryTransactionDto);
}