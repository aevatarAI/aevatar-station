using System;
using Orleans;

namespace Aevatar.GAgents.AElf.Agent.Grains;

[GenerateSerializer]
public class AElfTransactionState
{
    [Id(0)]  public  Guid Id { get; set; }
    [Id(1)] public string ChainId { get; set; }
    [Id(2)] public string SenderName{ get; set; }
    [Id(3)] public  string ContractAddress { get; set; }
    [Id(4)] public  string MethodName { get; set; }
    [Id(5)] public  string Param { get; set; }
    [Id(6)]  public  string  Status { get; set; }
    [Id(7)]  public  string  TransactionId { get; set; }
    [Id(8)]  public  string  Error { get; set; }
    
    [Id(9)] public bool IsSuccess   { get; set; }
}