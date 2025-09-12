using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aevatar.GAgents.Telegram.Features.Dtos;

public class Message
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("message_thread_id")]
    public long? MessageThreadId { get; set; }

    [JsonPropertyName("from")]
    public User? From { get; set; }

    [JsonPropertyName("sender_chat")]
    public Chat? SenderChat { get; set; }

    [JsonPropertyName("sender_boost_count")]
    public int? SenderBoostCount { get; set; }

    [JsonPropertyName("sender_business_bot")]
    public User? SenderBusinessBot { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }

    [JsonPropertyName("business_connection_id")]
    public string? BusinessConnectionId { get; set; }

    [JsonPropertyName("chat")]
    public Chat Chat { get; set; }

    [JsonPropertyName("forward_origin")]
    public MessageOrigin? ForwardOrigin { get; set; }

    [JsonPropertyName("is_topic_message")]
    public bool? IsTopicMessage { get; set; }

    [JsonPropertyName("is_automatic_forward")]
    public bool? IsAutomaticForward { get; set; }

    [JsonPropertyName("reply_to_message")]
    public Message? ReplyToMessage { get; set; }

    [JsonPropertyName("external_reply")]
    public ExternalReplyInfo? ExternalReply { get; set; }

    [JsonPropertyName("quote")]
    public TextQuote? Quote { get; set; }

    [JsonPropertyName("via_bot")]
    public User? ViaBot { get; set; }

    [JsonPropertyName("edit_date")]
    public int? EditDate { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("entities")]
    public List<MessageEntity>? Entities { get; set; }

    [JsonPropertyName("animation")]
    public Animation? Animation { get; set; }

    [JsonPropertyName("audio")]
    public Audio? Audio { get; set; }

    [JsonPropertyName("document")]
    public Document? Document { get; set; }

    [JsonPropertyName("photo")]
    public List<PhotoSize>? Photo { get; set; }

    [JsonPropertyName("sticker")]
    public Sticker? Sticker { get; set; }

    [JsonPropertyName("video")]
    public Video? Video { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("caption_entities")]
    public List<MessageEntity>? CaptionEntities { get; set; }

    [JsonPropertyName("contact")]
    public Contact? Contact { get; set; }

    [JsonPropertyName("poll")]
    public Poll? Poll { get; set; }

    [JsonPropertyName("new_chat_members")]
    public List<User>? NewChatMembers { get; set; }

    [JsonPropertyName("left_chat_member")]
    public User? LeftChatMember { get; set; }

    [JsonPropertyName("new_chat_title")]
    public string? NewChatTitle { get; set; }

    [JsonPropertyName("group_chat_created")]
    public bool? GroupChatCreated { get; set; }

    [JsonPropertyName("supergroup_chat_created")]
    public bool? SupergroupChatCreated { get; set; }

    [JsonPropertyName("channel_chat_created")]
    public bool? ChannelChatCreated { get; set; }

    [JsonPropertyName("pinned_message")]
    public Message? PinnedMessage { get; set; }
}

public class User
{
    [JsonPropertyName("id")]
    public long Id { get; set; }  // Unique identifier for this user or bot. Use long for safe handling of large values.

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }  // True, if this user is a bot

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }  // User's or bot's first name

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }  // Optional. User's or bot's last name

    [JsonPropertyName("username")]
    public string? Username { get; set; }  // Optional. User's or bot's username

    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; set; }  // Optional. IETF language tag of the user's language

    [JsonPropertyName("is_premium")]
    public bool? IsPremium { get; set; }  // Optional. True, if this user is a Telegram Premium user

    [JsonPropertyName("added_to_attachment_menu")]
    public bool? AddedToAttachmentMenu { get; set; }  // Optional. True, if this user added the bot to the attachment menu

    [JsonPropertyName("can_join_groups")]
    public bool? CanJoinGroups { get; set; }  // Optional. True, if the bot can be invited to groups. Returned only in getMe.

    [JsonPropertyName("can_read_all_group_messages")]
    public bool? CanReadAllGroupMessages { get; set; }  // Optional. True, if privacy mode is disabled for the bot. Returned only in getMe.

    [JsonPropertyName("supports_inline_queries")]
    public bool? SupportsInlineQueries { get; set; }  // Optional. True, if the bot supports inline queries. Returned only in getMe.

    [JsonPropertyName("can_connect_to_business")]
    public bool? CanConnectToBusiness { get; set; }  // Optional. True, if the bot can be connected to a Telegram Business account to receive its messages. Returned only in getMe.

    [JsonPropertyName("has_main_web_app")]
    public bool? HasMainWebApp { get; set; }  // Optional. True, if the bot has a main Web App. Returned only in getMe.
}

public class Chat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }  // Unique identifier for this chat. Use long for safe handling of large values.

    [JsonPropertyName("type")]
    public string Type { get; set; }  // Type of the chat: "private", "group", "supergroup", or "channel"

    [JsonPropertyName("title")]
    public string? Title { get; set; }  // Optional. Title, for supergroups, channels, and group chats

    [JsonPropertyName("username")]
    public string? Username { get; set; }  // Optional. Username, for private chats, supergroups, and channels if available

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }  // Optional. First name of the other party in a private chat

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }  // Optional. Last name of the other party in a private chat

    [JsonPropertyName("is_forum")]
    public bool? IsForum { get; set; }  // Optional. True if the supergroup chat is a forum (has topics enabled)
}

public class ExternalReplyInfo
{
    [JsonPropertyName("origin")]
    public MessageOrigin Origin { get; set; }

    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    [JsonPropertyName("message_id")]
    public int? MessageId { get; set; } 

    [JsonPropertyName("link_preview_options")]
    public LinkPreviewOptions? LinkPreviewOptions { get; set; }

    [JsonPropertyName("animation")]
    public Animation? Animation { get; set; }

    [JsonPropertyName("audio")]
    public Audio? Audio { get; set; }

    [JsonPropertyName("document")]
    public Document? Document { get; set; }

    [JsonPropertyName("paid_media")]
    public PaidMediaInfo? PaidMedia { get; set; }

    [JsonPropertyName("photo")]
    public List<PhotoSize>? Photo { get; set; }

    [JsonPropertyName("sticker")]
    public Sticker? Sticker { get; set; }

    [JsonPropertyName("story")]
    public Story? Story { get; set; }

    [JsonPropertyName("video")]
    public Video? Video { get; set; }

    [JsonPropertyName("video_note")]
    public VideoNote? VideoNote { get; set; }

    [JsonPropertyName("voice")]
    public Voice? Voice { get; set; }

    [JsonPropertyName("has_media_spoiler")]
    public bool? HasMediaSpoiler { get; set; } // Optional: Nullable 值类型

    [JsonPropertyName("contact")]
    public Contact? Contact { get; set; }

    [JsonPropertyName("dice")]
    public Dice? Dice { get; set; }

    [JsonPropertyName("game")]
    public Game? Game { get; set; }

    [JsonPropertyName("giveaway")]
    public Giveaway? Giveaway { get; set; }

    [JsonPropertyName("giveaway_winners")]
    public GiveawayWinners? GiveawayWinners { get; set; }

    [JsonPropertyName("invoice")]
    public Invoice? Invoice { get; set; }

    [JsonPropertyName("location")]
    public Location? Location { get; set; }

    [JsonPropertyName("poll")]
    public Poll? Poll { get; set; }

    [JsonPropertyName("venue")]
    public Venue? Venue { get; set; }
}

public class TextQuote
{
    [JsonPropertyName("text")]
    public string Text { get; set; }  // Text of the quoted part of a message that is replied to by the given message

    [JsonPropertyName("entities")]
    public List<MessageEntity>? Entities { get; set; }  // Optional. Special entities that appear in the quote

    [JsonPropertyName("position")]
    public int Position { get; set; }  // Approximate quote position in the original message in UTF-16 code units

    [JsonPropertyName("is_manual")]
    public bool? IsManual { get; set; }  // Optional. True, if the quote was chosen manually by the message sender
}

public class LinkPreviewOptions
{
    [JsonPropertyName("is_disabled")]
    public bool? IsDisabled { get; set; } // Optional: True if the link preview is disabled

    [JsonPropertyName("url")]
    public string? Url { get; set; } // Optional: URL to use for the link preview

    [JsonPropertyName("prefer_small_media")]
    public bool? PreferSmallMedia { get; set; } // Optional: True if the media should be shrunk in the link preview

    [JsonPropertyName("prefer_large_media")]
    public bool? PreferLargeMedia { get; set; } // Optional: True if the media should be enlarged in the link preview

    [JsonPropertyName("show_above_text")]
    public bool? ShowAboveText { get; set; } // Optional: True if the link preview should appear above the message text
}

public class Animation
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } // Identifier for this file, used to download or reuse the file

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } // Unique identifier for this file, constant across time and different bots

    [JsonPropertyName("width")]
    public int Width { get; set; } // Video width as defined by the sender

    [JsonPropertyName("height")]
    public int Height { get; set; } // Video height as defined by the sender

    [JsonPropertyName("duration")]
    public int Duration { get; set; } // Duration of the video in seconds as defined by the sender

    [JsonPropertyName("thumbnail")]
    public PhotoSize? Thumbnail { get; set; } // Optional: Animation thumbnail as defined by the sender

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; } // Optional: Original animation filename as defined by the sender

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; } // Optional: MIME type of the file as defined by the sender

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; } // Optional: File size in bytes, can exceed 2^31
}
public class Audio
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } // Identifier for this file, used to download or reuse the file

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } // Unique identifier for this file, constant across time and different bots

    [JsonPropertyName("duration")]
    public long Duration { get; set; } // Duration of the audio in seconds as defined by the sender

    [JsonPropertyName("performer")]
    public string Performer { get; set; } // Optional: Performer of the audio as defined by the sender or by audio tags

    [JsonPropertyName("title")]
    public string Title { get; set; } // Optional: Title of the audio as defined by the sender or by audio tags

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } // Optional: Original filename as defined by the sender

    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } // Optional: MIME type of the file as defined by the sender

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; } // Optional: File size in bytes, stored as long for large files

    [JsonPropertyName("thumbnail")]
    public PhotoSize Thumbnail { get; set; } // Optional: Thumbnail of the album cover to which the music file belongs
}

public class Document
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } // Identifier for this file, used to download or reuse the file

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } // Unique identifier for this file, constant across time and different bots

    [JsonPropertyName("thumbnail")]
    public PhotoSize Thumbnail { get; set; } // Optional: Document thumbnail as defined by the sender

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } // Optional: Original filename as defined by the sender

    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } // Optional: MIME type of the file as defined by the sender

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; } // Optional: File size in bytes, stored as long for large files
}
public class PaidMediaInfo
{
    [JsonPropertyName("star_count")]
    public long StarCount { get; set; } // Number of Telegram Stars required to access the media

    [JsonPropertyName("paid_media")]
    public List<PaidMedia> PaidMedia { get; set; } // Information about the paid media
}

public abstract class PaidMedia
{
    [JsonPropertyName("type")]
    public string Type { get; set; } // Type of the paid media (preview, photo, or video)
}

public class PaidMediaPreview : PaidMedia
{
    [JsonPropertyName("width")]
    public long? Width { get; set; } // Media width as defined by the sender (optional)

    [JsonPropertyName("height")]
    public long? Height { get; set; } // Media height as defined by the sender (optional)

    [JsonPropertyName("duration")]
    public long? Duration { get; set; } // Duration of the media in seconds as defined by the sender (optional)

    public PaidMediaPreview()
    {
        Type = "preview";
    }
}

public class PaidMediaPhoto : PaidMedia
{
    [JsonPropertyName("photo")]
    public List<PhotoSize> Photo { get; set; } // List of PhotoSize objects representing the photo

    public PaidMediaPhoto()
    {
        Type = "photo";
    }
}

public class PaidMediaVideo : PaidMedia
{
    [JsonPropertyName("video")]
    public Video Video { get; set; } // The video (Video object)

    public PaidMediaVideo()
    {
        Type = "video";
    }
}

public class Sticker
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } // Identifier for this sticker file

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } // Unique identifier for this sticker file

    [JsonPropertyName("type")]
    public string Type { get; set; } // Type of the sticker, e.g., "regular", "mask", "custom_emoji"

    [JsonPropertyName("width")]
    public long Width { get; set; } // Sticker width

    [JsonPropertyName("height")]
    public long Height { get; set; } // Sticker height

    [JsonPropertyName("is_animated")]
    public bool IsAnimated { get; set; } // True, if the sticker is animated

    [JsonPropertyName("is_video")]
    public bool IsVideo { get; set; } // True, if the sticker is a video sticker

    [JsonPropertyName("thumbnail")]
    public PhotoSize? Thumbnail { get; set; } // Optional: Sticker thumbnail, nullable

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } // Optional: Emoji associated with the sticker

    [JsonPropertyName("set_name")]
    public string SetName { get; set; } // Optional: Name of the sticker set, nullable

    [JsonPropertyName("premium_animation")]
    public File PremiumAnimation { get; set; } // Optional: Premium animation for regular stickers, nullable

    [JsonPropertyName("mask_position")]
    public MaskPosition MaskPosition { get; set; } // Optional: Position for mask stickers, nullable

    [JsonPropertyName("custom_emoji_id")]
    public string CustomEmojiId { get; set; } // Optional: Unique identifier for custom emoji stickers, nullable

    [JsonPropertyName("needs_repainting")]
    public bool? NeedsRepainting { get; set; } // Optional: True if the sticker must be repainted, nullable

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; } // Optional: File size in bytes, nullable
}

public class Story
{
    [JsonPropertyName("chat")]
    public Chat Chat { get; set; }  // The chat that posted the story.

    [JsonPropertyName("id")]
    public long Id { get; set; }  // Unique identifier for the story in the chat.
}

public class MaskPosition
{
    [JsonPropertyName("point")]
    public string Point { get; set; }  // The part of the face relative to which the mask should be placed. One of "forehead", "eyes", "mouth", or "chin".

    [JsonPropertyName("x_shift")]
    public float XShift { get; set; }  // Shift by X-axis measured in widths of the mask scaled to the face size.

    [JsonPropertyName("y_shift")]
    public float YShift { get; set; }  // Shift by Y-axis measured in heights of the mask scaled to the face size.

    [JsonPropertyName("scale")]
    public float Scale { get; set; }  // Mask scaling coefficient.
}
public class File
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }  // Identifier for this file, which can be used to download or reuse the file
    
    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; }  // Unique identifier for this file, which is supposed to be the same over time and for different bots
    
    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }  // Optional. File size in bytes. Can be bigger than 2^31 and some programming languages may have difficulty/silent defects in interpreting it. Uses long? for nullable value
    
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }  // Optional. File path. Can be null
}

public class Video
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } // Identifier for the video file

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } // Unique identifier for the video file

    [JsonPropertyName("width")]
    public long Width { get; set; } // Video width as defined by the sender

    [JsonPropertyName("height")]
    public long Height { get; set; } // Video height as defined by the sender

    [JsonPropertyName("duration")]
    public long Duration { get; set; } // Duration of the video in seconds as defined by the sender

    [JsonPropertyName("thumbnail")]
    public PhotoSize? Thumbnail { get; set; } // Optional: Video thumbnail, nullable

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; } // Optional: Original filename, nullable

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; } // Optional: MIME type of the file, nullable

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; } // Optional: File size in bytes, nullable
}

public class VideoNote
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }  // Identifier for this file, which can be used to download or reuse the file.

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; }  // Unique identifier for this file, which is supposed to be the same over time and for different bots.

    [JsonPropertyName("length")]
    public int Length { get; set; }  // Video width and height (diameter of the video message) as defined by the sender.

    [JsonPropertyName("duration")]
    public int Duration { get; set; }  // Duration of the video in seconds as defined by the sender.

    [JsonPropertyName("thumbnail")]
    public PhotoSize? Thumbnail { get; set; }  // Optional. Video thumbnail.

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }  // Optional. File size in bytes.
}

public class Voice
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }  // Identifier for this file, which can be used to download or reuse the file.

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; }  // Unique identifier for this file, which is supposed to be the same over time and for different bots.

    [JsonPropertyName("duration")]
    public int Duration { get; set; }  // Duration of the audio in seconds as defined by the sender.

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }  // Optional. MIME type of the file as defined by the sender.

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }  // Optional. File size in bytes.
}

public class Contact
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; }  // Contact's phone number.

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }  // Contact's first name.

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }  // Optional. Contact's last name.

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }  // Optional. Contact's user identifier in Telegram. Can be larger than 32 bits, use a 64-bit integer.

    [JsonPropertyName("vcard")]
    public string? VCard { get; set; }  // Optional. Additional data about the contact in the form of a vCard.
}

public class Dice
{
    [JsonPropertyName("emoji")]
    public string Emoji { get; set; }  // Emoji on which the dice throw animation is based.

    [JsonPropertyName("value")]
    public int Value { get; set; }  // Value of the dice (1-6 for specific emojis, 1-64 for others).
}
public class Game
{
    [JsonPropertyName("title")]
    public string Title { get; set; }  // Title of the game

    [JsonPropertyName("description")]
    public string Description { get; set; }  // Description of the game

    [JsonPropertyName("photo")]
    public List<PhotoSize> Photo { get; set; }  // Photos displayed in the game message

    [JsonPropertyName("text")]
    public string? Text { get; set; }  // Optional description or high scores in the game message

    [JsonPropertyName("text_entities")]
    public List<MessageEntity>? TextEntities { get; set; }  // Optional special entities in text

    [JsonPropertyName("animation")]
    public Animation? Animation { get; set; }  // Optional animation for the game message
}

public class Giveaway
{
    [JsonPropertyName("chats")]
    public List<Chat> Chats { get; set; }  // List of chats that the user must join to participate in the giveaway

    [JsonPropertyName("winners_selection_date")]
    public long WinnersSelectionDate { get; set; }  // Unix timestamp of when winners will be selected

    [JsonPropertyName("winner_count")]
    public int WinnerCount { get; set; }  // Number of winners to be selected

    [JsonPropertyName("only_new_members")]
    public bool? OnlyNewMembers { get; set; }  // Optional. True, if only users who join the chats after the giveaway started should be eligible

    [JsonPropertyName("has_public_winners")]
    public bool? HasPublicWinners { get; set; }  // Optional. True, if the winners will be publicly visible

    [JsonPropertyName("prize_description")]
    public string? PrizeDescription { get; set; }  // Optional. Description of additional giveaway prize

    [JsonPropertyName("country_codes")]
    public List<string>? CountryCodes { get; set; }  // Optional. List of two-letter country codes indicating eligible countries

    [JsonPropertyName("prize_star_count")]
    public int? PrizeStarCount { get; set; }  // Optional. Number of Telegram Stars for Telegram Star giveaways

    [JsonPropertyName("premium_subscription_month_count")]
    public int? PremiumSubscriptionMonthCount { get; set; }  // Optional. Number of months of Telegram Premium subscription for Telegram Premium giveaways
}

public class GiveawayWinners
{
    [JsonPropertyName("chat")]
    public Chat Chat { get; set; }  // The chat that created the giveaway

    [JsonPropertyName("giveaway_message_id")]
    public int GiveawayMessageId { get; set; }  // Identifier of the message with the giveaway

    [JsonPropertyName("winners_selection_date")]
    public long WinnersSelectionDate { get; set; }  // Unix timestamp when winners were selected

    [JsonPropertyName("winner_count")]
    public int WinnerCount { get; set; }  // Total number of winners in the giveaway

    [JsonPropertyName("winners")]
    public List<User> Winners { get; set; }  // List of up to 100 winners

    [JsonPropertyName("additional_chat_count")]
    public int? AdditionalChatCount { get; set; }  // Optional. The number of other chats the user had to join to be eligible

    [JsonPropertyName("prize_star_count")]
    public int? PrizeStarCount { get; set; }  // Optional. Number of Telegram Stars split among winners (for Telegram Star giveaways)

    [JsonPropertyName("premium_subscription_month_count")]
    public int? PremiumSubscriptionMonthCount { get; set; }  // Optional. Number of months of Telegram Premium subscription won from the giveaway

    [JsonPropertyName("unclaimed_prize_count")]
    public int? UnclaimedPrizeCount { get; set; }  // Optional. Number of undistributed prizes

    [JsonPropertyName("only_new_members")]
    public bool? OnlyNewMembers { get; set; }  // Optional. True, if only users who joined the chats after the giveaway started were eligible

    [JsonPropertyName("was_refunded")]
    public bool? WasRefunded { get; set; }  // Optional. True, if the giveaway was canceled because the payment was refunded

    [JsonPropertyName("prize_description")]
    public string? PrizeDescription { get; set; }  // Optional. Description of the additional giveaway prize
}

public class Invoice
{
    [JsonPropertyName("title")]
    public string Title { get; set; }  // Product name

    [JsonPropertyName("description")]
    public string Description { get; set; }  // Product description

    [JsonPropertyName("start_parameter")]
    public string StartParameter { get; set; }  // Unique bot deep-linking parameter

    [JsonPropertyName("currency")]
    public string Currency { get; set; }  // Three-letter ISO 4217 currency code, or "XTR" for Telegram Stars

    [JsonPropertyName("total_amount")]
    public int TotalAmount { get; set; }  // Total price in the smallest units of the currency
}
public class Venue
{
    [JsonPropertyName("location")]
    public Location Location { get; set; }  // Venue location. Can't be a live location

    [JsonPropertyName("title")]
    public string Title { get; set; }  // Name of the venue

    [JsonPropertyName("address")]
    public string Address { get; set; }  // Address of the venue

    [JsonPropertyName("foursquare_id")]
    public string? FoursquareId { get; set; }  // Optional. Foursquare identifier of the venue

    [JsonPropertyName("foursquare_type")]
    public string? FoursquareType { get; set; }  // Optional. Foursquare type of the venue

    [JsonPropertyName("google_place_id")]
    public string? GooglePlaceId { get; set; }  // Optional. Google Places identifier of the venue

    [JsonPropertyName("google_place_type")]
    public string? GooglePlaceType { get; set; }  // Optional. Google Places type of the venue
}
public class PhotoSize
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } // Identifier for this file, used to download or reuse the file

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } // Unique identifier for this file, constant across time and different bots

    [JsonPropertyName("width")]
    public long Width { get; set; } // Photo width

    [JsonPropertyName("height")]
    public long Height { get; set; } // Photo height

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; } // Optional: File size in bytes
}
public class MessageEntity
{
    [JsonPropertyName("type")] 
    public string? Type { get; set; }
    [JsonPropertyName("offset")] 
    public int Offset { get; set; }
    [JsonPropertyName("length")] 
    public int Length { get; set; }
    [JsonPropertyName("url")] 
    public string? Url { get; set; }
    [JsonPropertyName("user")] 
    public User? User { get; set; }
    [JsonPropertyName("language")] 
    public string? Language { get; set; }
    [JsonPropertyName("emoji")] 
    public string? CustomEmojiId { get; set; }
}


public class MessageOrigin
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("date")]
    public int Date { get; set; }
}

public class MessageOriginUser : MessageOrigin
{
    [JsonPropertyName("sender_user")]
    public User SenderUser { get; set; }
}

public class MessageOriginHiddenUser : MessageOrigin
{
    [JsonPropertyName("sender_user_name")]
    public string SenderUserName { get; set; }
}

public class MessageOriginChat : MessageOrigin
{
    [JsonPropertyName("sender_chat")]
    public Chat SenderChat { get; set; }

    [JsonPropertyName("author_signature")]
    public string AuthorSignature { get; set; }
}

public class MessageOriginChannel : MessageOrigin
{
    [JsonPropertyName("chat")]
    public Chat Chat { get; set; }

    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }

    [JsonPropertyName("author_signature")]
    public string AuthorSignature { get; set; }
}


public class BusinessConnection
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("user_chat_id")]
    public long UserChatId { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }

    [JsonPropertyName("can_reply")]
    public bool CanReply { get; set; }

    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }
}

public class BusinessMessagesDeleted
{
    [JsonPropertyName("business_connection_id")]
    public string? BusinessConnectionId { get; set; }

    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    [JsonPropertyName("message_ids")]
    public List<long>? MessageIds { get; set; }
}

public class MessageReactionUpdated
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; } 

    [JsonPropertyName("actor_chat")]
    public Chat? ActorChat { get; set; } 

    [JsonPropertyName("date")]
    public long Date { get; set; } 
    
    [JsonPropertyName("old_reaction")]
    public List<ReactionType>? OldReaction { get; set; } 
    
    [JsonPropertyName("new_reaction")]
    public List<ReactionType>? NewReaction { get; set; } 
}

public class ReactionType
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class ReactionTypeEmoji : ReactionType
{
    [JsonPropertyName("type")]
    public new string Type { get; set; } = "emoji";
    
    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; } 
}

public class ReactionTypeCustomEmoji : ReactionType
{
    [JsonPropertyName("type")]
    public new string Type { get; set; } = "custom_emoji";
    
    [JsonPropertyName("custom_emoji_id")]
    public string? CustomEmojiId { get; set; } 
}

public class ReactionTypePaid : ReactionType
{
    [JsonPropertyName("type")]
    public new string Type { get; set; } = "paid"; 
}

public class ReactionCountUpdate
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }
    
    [JsonPropertyName("reactions")]
    public List<ReactionCount>? Reactions { get; set; }
}

public class ReactionCount
{
    [JsonPropertyName("type")]
    public ReactionType? Type { get; set; }
    
    [JsonPropertyName("total_count")]
    public long TotalCount { get; set; }
}
public class MessageReactionCountUpdated
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; } 

    [JsonPropertyName("message_id")]
    public long MessageId { get; set; } 

    [JsonPropertyName("date")]
    public long Date { get; set; } 

    [JsonPropertyName("reactions")]
    public List<ReactionCount>? Reactions { get; set; } 
}

public class InlineQuery
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("from")]
    public User? From { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("offset")]
    public string? Offset { get; set; }

    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    [JsonPropertyName("location")]
    public Location? Location { get; set; }
}

public class Location
{
    [JsonPropertyName("latitude")]
    public float Latitude { get; set; } 

    [JsonPropertyName("longitude")]
    public float Longitude { get; set; }

    [JsonPropertyName("horizontal_accuracy")]
    public float? HorizontalAccuracy { get; set; }

    [JsonPropertyName("live_period")]
    public long? LivePeriod { get; set; }

    [JsonPropertyName("heading")]
    public long? Heading { get; set; }

    [JsonPropertyName("proximity_alert_radius")]
    public long? ProximityAlertRadius { get; set; } 
}

public class ChosenInlineResult
{
    [JsonPropertyName("result_id")]
    public string? ResultId { get; set; }

    [JsonPropertyName("from")]
    public User? From { get; set; } 

    [JsonPropertyName("location")]
    public Location Location { get; set; }

    [JsonPropertyName("inline_message_id")]
    public string? InlineMessageId { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }
}
public class CallbackQuery
{
    [JsonPropertyName("id")]
    public string? Id { get; set; } 

    [JsonPropertyName("from")]
    public User? From { get; set; } 

    [JsonPropertyName("message")]
    public MaybeInaccessibleMessage? Message { get; set; }

    [JsonPropertyName("inline_message_id")]
    public string? InlineMessageId { get; set; }

    [JsonPropertyName("chat_instance")]
    public string? ChatInstance { get; set; } 

    [JsonPropertyName("data")]
    public string? Data { get; set; } 

    [JsonPropertyName("game_short_name")]
    public string? GameShortName { get; set; }  
}
public class MaybeInaccessibleMessage
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("offset")]
    public long Offset { get; set; } 

    [JsonPropertyName("length")]
    public long Length { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; } 

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("custom_emoji_id")]
    public string? CustomEmojiId { get; set; } 
}
public class ShippingQuery
{
    [JsonPropertyName("id")]
    public string? Id { get; set; } 

    [JsonPropertyName("from")]
    public User? From { get; set; } 

    [JsonPropertyName("invoice_payload")]
    public string? InvoicePayload { get; set; } 

    [JsonPropertyName("shipping_address")]
    public ShippingAddress? ShippingAddress { get; set; }
}

public class ShippingAddress
{
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; } 

    [JsonPropertyName("state")]
    public string? State { get; set; } 

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("street_line1")]
    public string? StreetLine1 { get; set; } 
    [JsonPropertyName("street_line2")]
    public string? StreetLine2 { get; set; } 

    [JsonPropertyName("post_code")]
    public string? PostCode { get; set; } 
}

public class PreCheckoutQuery
{
    [JsonPropertyName("id")]
    public string? Id { get; set; } 

    [JsonPropertyName("from")]
    public User? From { get; set; } 

    [JsonPropertyName("currency")]
    public string? Currency { get; set; } 

    [JsonPropertyName("total_amount")]
    public long TotalAmount { get; set; } 

    [JsonPropertyName("invoice_payload")]
    public string? InvoicePayload { get; set; } 

    [JsonPropertyName("shipping_option_id")]
    public string? ShippingOptionId { get; set; }

    [JsonPropertyName("order_info")]
    public OrderInfo? OrderInfo { get; set; }
}

public class OrderInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("shipping_address")]
    public ShippingAddress? ShippingAddress { get; set; } 
}

public class PaidMediaPurchased
{
    [JsonPropertyName("from")]
    public User? From { get; set; } 

    [JsonPropertyName("paid_media_payload")]
    public string? PaidMediaPayload { get; set; } 
}

public class Poll
{
    [JsonPropertyName("id")]
    public string? Id { get; set; } 

    [JsonPropertyName("question")]
    public string? Question { get; set; } 

    [JsonPropertyName("question_entities")]
    public List<MessageEntity>? QuestionEntities { get; set; } 

    [JsonPropertyName("options")]
    public List<PollOption>? Options { get; set; }

    [JsonPropertyName("total_voter_count")]
    public long TotalVoterCount { get; set; } 

    [JsonPropertyName("is_closed")]
    public bool IsClosed { get; set; }

    [JsonPropertyName("is_anonymous")]
    public bool IsAnonymous { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } 

    [JsonPropertyName("allows_multiple_answers")]
    public bool AllowsMultipleAnswers { get; set; }

    [JsonPropertyName("correct_option_id")]
    public long? CorrectOptionId { get; set; } 

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("explanation_entities")]
    public List<MessageEntity>? ExplanationEntities { get; set; } 

    [JsonPropertyName("open_period")]
    public long? OpenPeriod { get; set; } 

    [JsonPropertyName("close_date")]
    public long? CloseDate { get; set; }
}

public class PollOption
{
    [JsonPropertyName("text")]
    public string? Text { get; set; } 

    [JsonPropertyName("text_entities")]
    public List<MessageEntity>? TextEntities { get; set; } 

    [JsonPropertyName("voter_count")]
    public long VoterCount { get; set; } 
}
public class PollAnswer
{
    [JsonPropertyName("poll_id")]
    public string? PollId { get; set; }

    [JsonPropertyName("voter_chat")]
    public Chat? VoterChat { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; } 

    [JsonPropertyName("option_ids")]
    public List<long>? OptionIds { get; set; } 
}
public class ChatMemberUpdated
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    [JsonPropertyName("from")]
    public User? From { get; set; }

    [JsonPropertyName("date")]
    public int Date { get; set; } 

    [JsonPropertyName("old_chat_member")]
    public ChatMember? OldChatMember { get; set; } 

    [JsonPropertyName("new_chat_member")]
    public ChatMember? NewChatMember { get; set; } 

    [JsonPropertyName("invite_link")]
    public ChatInviteLink? InviteLink { get; set; }

    [JsonPropertyName("via_join_request")]
    public bool? ViaJoinRequest { get; set; } 

    [JsonPropertyName("via_chat_folder_invite_link")]
    public bool? ViaChatFolderInviteLink { get; set; } 
}

public class ChatMember
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }  // The status of the member in the chat

    [JsonPropertyName("user")]
    public User? User { get; set; }  // Information about the user
}

public class ChatMemberOwner : ChatMember
{
    public ChatMemberOwner()
    {
        Status = "creator";  // The status of the member is always "creator" for the owner
    }

    [JsonPropertyName("is_anonymous")]
    public bool IsAnonymous { get; set; }  // True if the user's presence in the chat is hidden

    [JsonPropertyName("custom_title")]
    public string? CustomTitle { get; set; }  // Custom title for this user (optional)
}

public class ChatMemberAdministrator : ChatMember
{
    public ChatMemberAdministrator()
    {
        Status = "administrator";  // The status of the member is always "administrator" for admins
    }

    [JsonPropertyName("can_be_edited")]
    public bool CanBeEdited { get; set; }  // True if the bot is allowed to edit the admin's privileges

    [JsonPropertyName("is_anonymous")]
    public bool IsAnonymous { get; set; }  // True if the user's presence in the chat is hidden

    [JsonPropertyName("can_manage_chat")]
    public bool CanManageChat { get; set; }  // True if the admin can manage the chat (view logs, etc.)

    [JsonPropertyName("can_delete_messages")]
    public bool CanDeleteMessages { get; set; }  // True if the admin can delete other users' messages

    [JsonPropertyName("can_manage_video_chats")]
    public bool CanManageVideoChats { get; set; }  // True if the admin can manage video chats

    [JsonPropertyName("can_restrict_members")]
    public bool CanRestrictMembers { get; set; }  // True if the admin can restrict or ban users

    [JsonPropertyName("can_promote_members")]
    public bool CanPromoteMembers { get; set; }  // True if the admin can promote other members to admins

    [JsonPropertyName("can_change_info")]
    public bool CanChangeInfo { get; set; }  // True if the admin can change the chat info (title, photo, etc.)

    [JsonPropertyName("can_invite_users")]
    public bool CanInviteUsers { get; set; }  // True if the admin can invite new users to the chat

    [JsonPropertyName("can_post_stories")]
    public bool CanPostStories { get; set; }  // True if the admin can post stories in the chat

    [JsonPropertyName("can_edit_stories")]
    public bool CanEditStories { get; set; }  // True if the admin can edit others' stories

    [JsonPropertyName("can_delete_stories")]
    public bool CanDeleteStories { get; set; }  // True if the admin can delete others' stories

    [JsonPropertyName("can_post_messages")]
    public bool? CanPostMessages { get; set; }  // True if the admin can post messages in channels (only for channels)

    [JsonPropertyName("can_edit_messages")]
    public bool? CanEditMessages { get; set; }  // True if the admin can edit messages in channels (only for channels)

    [JsonPropertyName("can_pin_messages")]
    public bool? CanPinMessages { get; set; }  // True if the admin can pin messages (only for groups and supergroups)

    [JsonPropertyName("can_manage_topics")]
    public bool? CanManageTopics { get; set; }  // True if the admin can manage forum topics (only for supergroups)

    [JsonPropertyName("custom_title")]
    public string? CustomTitle { get; set; }  // Custom title for the administrator (optional)
}

public class ChatMemberMember : ChatMember
{
    public ChatMemberMember()
    {
        Status = "member";  // The status of the member is always "member" for regular members
    }

    [JsonPropertyName("until_date")]
    public int? UntilDate { get; set; }  // The Unix timestamp of when the user's subscription will expire (optional)
}

public class ChatMemberRestricted : ChatMember
{
    public ChatMemberRestricted()
    {
        Status = "restricted";  // The status of the member is "restricted" for members with restrictions
    }

    [JsonPropertyName("is_member")]
    public bool IsMember { get; set; }  // True if the user is still a member of the chat

    [JsonPropertyName("can_send_messages")]
    public bool CanSendMessages { get; set; }  // True if the user is allowed to send text messages

    [JsonPropertyName("can_send_audios")]
    public bool CanSendAudios { get; set; }  // True if the user can send audio files

    [JsonPropertyName("can_send_documents")]
    public bool CanSendDocuments { get; set; }  // True if the user can send documents

    [JsonPropertyName("can_send_photos")]
    public bool CanSendPhotos { get; set; }  // True if the user can send photos

    [JsonPropertyName("can_send_videos")]
    public bool CanSendVideos { get; set; }  // True if the user can send videos

    [JsonPropertyName("can_send_video_notes")]
    public bool CanSendVideoNotes { get; set; }  // True if the user can send video notes

    [JsonPropertyName("can_send_voice_notes")]
    public bool CanSendVoiceNotes { get; set; }  // True if the user can send voice notes

    [JsonPropertyName("can_send_polls")]
    public bool CanSendPolls { get; set; }  // True if the user can send polls

    [JsonPropertyName("can_send_other_messages")]
    public bool CanSendOtherMessages { get; set; }  // True if the user can send other types of messages (e.g., games, stickers)

    [JsonPropertyName("can_add_web_page_previews")]
    public bool CanAddWebPagePreviews { get; set; }  // True if the user can add web page previews to messages

    [JsonPropertyName("can_change_info")]
    public bool CanChangeInfo { get; set; }  // True if the user can change chat info (title, photo, etc.)

    [JsonPropertyName("can_invite_users")]
    public bool CanInviteUsers { get; set; }  // True if the user can invite new users to the chat

    [JsonPropertyName("can_pin_messages")]
    public bool CanPinMessages { get; set; }  // True if the user can pin messages

    [JsonPropertyName("can_manage_topics")]
    public bool CanManageTopics { get; set; }  // True if the user can manage topics in supergroups

    [JsonPropertyName("until_date")]
    public int UntilDate { get; set; }  // Unix timestamp when the restrictions will be lifted (0 means permanent restriction)
}

public class ChatMemberLeft : ChatMember
{
    public ChatMemberLeft()
    {
        Status = "left";  // The status of the member is "left" for users who have left the chat
    }
}

public class ChatMemberBanned : ChatMember
{
    public ChatMemberBanned()
    {
        Status = "kicked";  // The status of the member is "kicked" for users who are banned
    }

    [JsonPropertyName("until_date")]
    public int UntilDate { get; set; }  // The Unix timestamp when the ban will be lifted (0 means permanent ban)
}

public class ChatInviteLink
{
    [JsonPropertyName("invite_link")]
    public string? InviteLink { get; set; }  // The invite link. If created by another admin, the second part will be replaced with "…"

    [JsonPropertyName("creator")]
    public User? Creator { get; set; }  // The creator of the invite link

    [JsonPropertyName("creates_join_request")]
    public bool CreatesJoinRequest { get; set; }  // True if users need approval to join via this link

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; }  // True if this is the primary invite link

    [JsonPropertyName("is_revoked")]
    public bool IsRevoked { get; set; }  // True if the invite link is revoked

    [JsonPropertyName("name")]
    public string? Name { get; set; }  // Optional: Name for the invite link

    [JsonPropertyName("expire_date")]
    public int? ExpireDate { get; set; }  // Optional: Unix timestamp of when the link will expire or has expired

    [JsonPropertyName("member_limit")]
    public int? MemberLimit { get; set; }  // Optional: Maximum number of users who can join the chat using this link

    [JsonPropertyName("pending_join_request_count")]
    public int? PendingJoinRequestCount { get; set; }  // Optional: The number of pending join requests using this link

    [JsonPropertyName("subscription_period")]
    public int? SubscriptionPeriod { get; set; }  // Optional: The subscription period in seconds for the link

    [JsonPropertyName("subscription_price")]
    public int? SubscriptionPrice { get; set; }  // Optional: The price in Telegram Stars for each subscription period
}

public class ChatJoinRequest
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }  // Chat to which the request was sent

    [JsonPropertyName("from")]
    public User? From { get; set; }  // User that sent the join request

    [JsonPropertyName("user_chat_id")]
    public long UserChatId { get; set; }  // Identifier of a private chat with the user who sent the join request

    [JsonPropertyName("date")]
    public int Date { get; set; }  // Date the request was sent in Unix time

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }  // Optional bio of the user

    [JsonPropertyName("invite_link")]
    public ChatInviteLink? InviteLink { get; set; }  // Optional: The invite link used by the user to send the join request
}

public class ChatBoostUpdated
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }  // Chat which was boosted

    [JsonPropertyName("boost")]
    public ChatBoost? Boost { get; set; }  // Information about the chat boost
}

public class ChatBoostRemoved
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }  // Chat which was boosted

    [JsonPropertyName("boost_id")]
    public string? BoostId { get; set; }  // Unique identifier of the boost

    [JsonPropertyName("remove_date")]
    public int RemoveDate { get; set; }  // Point in time (Unix timestamp) when the boost was removed

    [JsonPropertyName("source")]
    public ChatBoostSource? Source { get; set; }  // Source of the removed boost
}

public class ChatBoost
{
    [JsonPropertyName("boost_id")]
    public string? BoostId { get; set; }  // Unique identifier of the boost

    [JsonPropertyName("add_date")]
    public int AddDate { get; set; }  // Point in time (Unix timestamp) when the chat was boosted

    [JsonPropertyName("expiration_date")]
    public int ExpirationDate { get; set; }  // Point in time (Unix timestamp) when the boost will automatically expire

    [JsonPropertyName("source")]
    public ChatBoostSource? Source { get; set; }  // Source of the added boost
}

public class ChatBoostSource
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }  // Source of the boost (e.g., "premium", "gift_code", "giveaway")

    [JsonPropertyName("user")]
    public User? User { get; set; }  // User related to the boost (varies by source type)
}

public class ChatBoostSourcePremium : ChatBoostSource
{
    public ChatBoostSourcePremium()
    {
        Source = "premium";  // Always "premium"
    }

    [JsonPropertyName("user")]
    public User? User { get; set; }  // User who boosted the chat via Telegram Premium
}

public class ChatBoostSourceGiftCode : ChatBoostSource
{
    public ChatBoostSourceGiftCode()
    {
        Source = "gift_code";  // Always "gift_code"
    }

    [JsonPropertyName("user")]
    public User? User { get; set; }  // User for whom the gift code was created
}

public class ChatBoostSourceGiveaway : ChatBoostSource
{
    public ChatBoostSourceGiveaway()
    {
        Source = "giveaway"; // Always "giveaway"
    }

    [JsonPropertyName("giveaway_message_id")]
    public int GiveawayMessageId { get; set; } // Identifier of a message related to the giveaway

    [JsonPropertyName("user")]
    public User? User { get; set; } // User who won the giveaway (optional, for Telegram Premium giveaways)

    [JsonPropertyName("prize_star_count")]
    public int PrizeStarCount { get; set; } // Number of Telegram Stars for Telegram Star giveaways

    [JsonPropertyName("is_unclaimed")]
    public bool? IsUnclaimed { get; set; } // True if the giveaway was completed, but no winner claimed the prize
}

