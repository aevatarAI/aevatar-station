using System;
using Google.Protobuf;
using Orleans;

namespace Aevatar.GAgents.AElf.Dto;

[GenerateSerializer]
public class SendTransactionDto
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string ChainId { get; set; }
    [Id(2)] public string SenderName{ get; set; }
    [Id(3)] public  string ContractAddress { get; set; }
    [Id(4)] public  string MethodName { get; set; }
    [Id(5)] public  string Param { get; set; }
    
    [Id(6)] public bool IsSuccess   { get; set; }
    
    [Id(7)] public string TransactionId { get; set; }
    
    [Id(8)] public string CreateTransactionGEventId { get; set; }
}