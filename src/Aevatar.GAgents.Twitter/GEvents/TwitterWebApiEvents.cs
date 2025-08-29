using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GEvents;

// Base event for all Twitter Web API events
[GenerateSerializer]
public abstract class TwitterWebApiEvent : EventBase
{
    [Id(0)] public string? UserId { get; set; }
}

// Tweet Management Events
[GenerateSerializer]
public class DeleteTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class GetTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class GetMultipleTweetsEvent : TwitterWebApiEvent
{
    [Id(1)] public List<string> TweetIds { get; set; } = new();
}

[GenerateSerializer]
public class QuoteTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string QuotedTweetId { get; set; } = string.Empty;
    [Id(2)] public string Text { get; set; } = string.Empty;
}

[GenerateSerializer]
public class CreateThreadEvent : TwitterWebApiEvent
{
    [Id(1)] public List<string> TweetTexts { get; set; } = new();
}

// User Interaction Events
[GenerateSerializer]
public class LikeTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UnlikeTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class RetweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UnretweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class BookmarkTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class RemoveBookmarkEvent : TwitterWebApiEvent
{
    [Id(1)] public string TweetId { get; set; } = string.Empty;
}

// User Profile Events
[GenerateSerializer]
public class GetUserByUsernameEvent : TwitterWebApiEvent
{
    [Id(1)] public string Username { get; set; } = string.Empty;
}

[GenerateSerializer]
public class GetUserByIdEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class GetMyProfileEvent : TwitterWebApiEvent
{
}

[GenerateSerializer]
public class UpdateProfileEvent : TwitterWebApiEvent
{
    [Id(1)] public string? Name { get; set; }
    [Id(2)] public string? Description { get; set; }
    [Id(3)] public string? Location { get; set; }
    [Id(4)] public string? Url { get; set; }
}

// Relationship Events
[GenerateSerializer]
public class FollowUserEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UnfollowUserEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class GetFollowersEvent : TwitterWebApiEvent
{
    [Id(1)] public string? TargetUserId { get; set; }
    [Id(2)] public int MaxResults { get; set; } = 100;
}

[GenerateSerializer]
public class GetFollowingEvent : TwitterWebApiEvent
{
    [Id(1)] public string? TargetUserId { get; set; }
    [Id(2)] public int MaxResults { get; set; } = 100;
}

[GenerateSerializer]
public class BlockUserEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UnblockUserEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class MuteUserEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UnmuteUserEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
}

// Timeline Events
[GenerateSerializer]
public class GetHomeTimelineEvent : TwitterWebApiEvent
{
    [Id(1)] public int MaxResults { get; set; } = 100;
    [Id(2)] public string? PaginationToken { get; set; }
}

[GenerateSerializer]
public class GetUserTimelineEvent : TwitterWebApiEvent
{
    [Id(1)] public string TargetUserId { get; set; } = string.Empty;
    [Id(2)] public int MaxResults { get; set; } = 100;
    [Id(3)] public string? PaginationToken { get; set; }
}

[GenerateSerializer]
public class GetMentionsTimelineEvent : TwitterWebApiEvent
{
    [Id(1)] public int MaxResults { get; set; } = 100;
    [Id(2)] public string? PaginationToken { get; set; }
}

// Direct Message Events
[GenerateSerializer]
public class SendDirectMessageEvent : TwitterWebApiEvent
{
    [Id(1)] public string RecipientId { get; set; } = string.Empty;
    [Id(2)] public string Text { get; set; } = string.Empty;
}

// Media Upload Events
[GenerateSerializer]
public class UploadMediaEvent : TwitterWebApiEvent
{
    [Id(1)] public byte[] MediaData { get; set; } = Array.Empty<byte>();
    [Id(2)] public string MediaType { get; set; } = string.Empty;
    [Id(3)] public string? AltText { get; set; }
}

// Existing events for backward compatibility
[GenerateSerializer]
public class PostTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string Text { get; set; } = string.Empty;
    [Id(2)] public List<string>? MediaIds { get; set; }
}

[GenerateSerializer]
public class ReplyToTweetEvent : TwitterWebApiEvent
{
    [Id(1)] public string InReplyToTweetId { get; set; } = string.Empty;
    [Id(2)] public string Text { get; set; } = string.Empty;
    [Id(3)] public List<string>? MediaIds { get; set; }
}

[GenerateSerializer]
public class SearchRecentTweetsEvent : TwitterWebApiEvent
{
    [Id(1)] public string Query { get; set; } = string.Empty;
    [Id(2)] public int MaxResults { get; set; } = 10;
}