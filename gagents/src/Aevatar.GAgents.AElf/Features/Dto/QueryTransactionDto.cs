using System;
using Google.Protobuf;
using Orleans;

namespace Aevatar.GAgents.AElf.Dto;

[GenerateSerializer]
public class QueryTransactionDto
{
    [Id(1)] public string ChainId { get; set; }
    
    [Id(6)] public bool IsSuccess   { get; set; }
    
    [Id(7)] public string TransactionId { get; set; }
    [Id(8)] public Guid CreateTransactionGEventId { get; set; }
}