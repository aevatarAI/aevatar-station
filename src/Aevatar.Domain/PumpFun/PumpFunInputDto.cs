using System.Collections.Generic;
using Aevatar.Dto;
using System.Text.Json.Serialization;

namespace Aevatar.PumpFun;

public class PumpFunInputDto 
{
    public string? ChatId { get; set; } 
    
    public string? AgentId { get; set; } 
    
    public string? RequestMessage { get; set; } 
    
    public string? ReplyId { get; set; } 
    
}
