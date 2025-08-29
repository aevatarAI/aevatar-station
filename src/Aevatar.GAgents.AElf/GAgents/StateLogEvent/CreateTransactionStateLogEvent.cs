using Orleans;

namespace Aevatar.GAgents.AElf.Agent.GEvents;

[GenerateSerializer]
public  class CreateTransactionStateLogEvent: TransactionStateLogEvent
{
    [Id(1)] public string ChainId { get; set; }
    [Id(2)] public string SenderName{ get; set; }
    [Id(3)] public  string ContractAddress { get; set; }
    [Id(4)] public  string MethodName { get; set; }
    [Id(5)] public  string Param { get; set; }
    
    [Id(6)] public bool IsSuccess   { get; set; }
    
    [Id(7)] public string TransactionId { get; set; }
}