using Newtonsoft.Json;

namespace Aevatar.GAgents.Telegram.Features.Dtos;

public class MessageParamsRequest
{
    [JsonProperty("chat_id")]
    public string ChatId { get; set; }
    
    [JsonProperty("text")]
    public string Text { get; set; }
    
    [JsonProperty("reply_parameters")]
    public ReplyParameters ReplyParameters { get; set; }
}

public class ReplyParameters
{
    [JsonProperty("message_id")]
    public string MessageId { get; set; }
   
}