using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aevatar.GAgents.Telegram.Features.Dtos;

public class GetUpdatedDto
{
    public bool Ok { get; set; }
    public List<TelegramUpdateDto> Result { get; set; }
}
public class TelegramUpdateDto 
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; } // Update's unique identifier
    
    [JsonPropertyName("message")]
    public Message? Message { get; set; } // New incoming message (optional)
    
    [JsonPropertyName("edited_message")]
    public Message? EditedMessage { get; set; } // Edited message (optional)
    
    [JsonPropertyName("channel_post")]
    public Message? ChannelPost { get; set; } // New incoming channel post (optional)
    
    [JsonPropertyName("edited_channel_post")]
    public Message? EditedChannelPost { get; set; } // Edited channel post (optional)
    
  
    [JsonPropertyName("business_message")]
    public Message? BusinessMessage { get; set; } // New message from a connected business account (optional)
    
    [JsonPropertyName("edited_business_message")]
    public Message? EditedBusinessMessage { get; set; } // Edited message from a connected business account (optional)
    
    [JsonPropertyName("deleted_business_messages")]
    public BusinessMessagesDeleted? DeletedBusinessMessages { get; set; } // Messages deleted from a business account (optional)
    
    [JsonPropertyName("message_reaction")]
    public MessageReactionUpdated? MessageReaction { get; set; } // Reaction to a message updated (optional)
    
    [JsonPropertyName("message_reaction_count")]
    public MessageReactionCountUpdated? MessageReactionCount { get; set; } // Reactions to a message updated (optional)
}
