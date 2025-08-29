using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Aevatar.GAgents.AElf.Options;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using ByteArrayHelper = AElf.ByteArrayHelper;

namespace Aevatar.GAgents.AElf.Provider;

public class AElfNodeProvider: IAElfNodeProvider,ISingletonDependency
{
   
    private readonly IOptionsMonitor<ChainOptions> _chainOption;
    private readonly Dictionary<string, AElfClient> _clients = new();
    private readonly ILogger<AElfNodeProvider> _logger;
    public AElfNodeProvider(IOptionsMonitor<ChainOptions> chainOption,ILogger<AElfNodeProvider> logger)
    {
        _logger = logger;
        _chainOption = chainOption;
        InitAElfClient();
    }
    
    private void InitAElfClient()
    {
        if (_chainOption.CurrentValue.ChainNodeHosts.IsNullOrEmpty())
        {
            return;
        }
        foreach (var node in _chainOption.CurrentValue.ChainNodeHosts)
        {
            _clients[node.Key] = new AElfClient(node.Value);
            _logger.LogDebug("init AElfClient: {ChainId}, {Node}", node.Key, node.Value);
        }
    }
    
    public  async Task<Transaction> CreateTransactionAsync(string chainId, string senderName, string contractAddress,
        string methodName,
        IMessage param)
    {
        var privateKey = GetAccount(senderName);
        var transaction = await Client(chainId).GenerateTransactionAsync(
            Client(chainId).GetAddressFromPrivateKey(privateKey),
            contractAddress,
            methodName, param);
        
       return Client(chainId).SignTransaction(privateKey, transaction);
    }
    
    public async Task<SendTransactionOutput> SendTransactionAsync(string chainId, Transaction transaction)
    {
        return await Client(chainId).SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });
    }
    
    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        return await Client(chainId).GetTransactionResultAsync(transactionId);
    }

    public async Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction)
        where T : class, IMessage<T>, new()
    {
        var result = await Client(chainId)
            .ExecuteTransactionAsync(new ExecuteTransactionDto() { RawTransaction = 
                transaction.ToByteArray().ToHex()
            });
        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
        return value;
    }
    
    private AElfClient Client(string chainId)
    {
        return _clients[chainId];
    }
    
    private string GetAccount(string accountName)
    {
        var optionExists = _chainOption.CurrentValue.AccountDictionary.TryGetValue(accountName, out var account);
        if (!optionExists)
        {
            throw new UserFriendlyException($"Account {accountName} not found");
        }
        return account;
    }
}