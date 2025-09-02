using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aevatar.GAgents.Telegram.Features.Dtos;

public class ReplyParamDto
{
    [JsonPropertyName("message_id")] public long MessageId { get; set; }
    [JsonPropertyName("chat_id")] public long? ChainId { get; set; }

    [JsonPropertyName("allow_sending_without_reply")]
    public bool? AllowSendingWithoutReply { get; set; }

    [JsonPropertyName("quote")] public string? Quote { get; set; }
    [JsonPropertyName("quote_parse_mode")] public string? QuoteParseMode { get; set; }
    [JsonPropertyName("quote_entities")] public List<MessageEntity>? QuoteEntities { get; set; }
    [JsonPropertyName("quote_position")] public int? QuotePosition { get; set; }
}

