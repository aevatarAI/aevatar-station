using System;
using System.Collections.Generic;
using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GEvents;

// Base event for all Twitter Web API events
[GenerateSerializer]
[Description("Base event class for all Twitter Web API operations. Contains common properties used across all Twitter events.")]
public abstract class TwitterWebApiEvent : EventBase
{
    [Id(0)]
    [Description("The Twitter user ID associated with this event. If not provided, the authenticated user's ID will be used.")]
    public string? UserId { get; set; }
}

// Tweet Management Events
[GenerateSerializer]
[Description("Event to delete a specific tweet. Returns TweetDeleted event with success status.")]
public class DeleteTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to delete. Must be a tweet owned by the authenticated user.")]
    public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to retrieve a specific tweet's details. Returns the tweet data including text, metrics, and metadata.")]
public class GetTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to retrieve. Can be any public tweet or a private tweet the user has access to.")]
    public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to retrieve multiple tweets in a single request. Returns a list of tweet data.")]
public class GetMultipleTweetsEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("List of tweet IDs to retrieve. Maximum 100 IDs per request.")]
    public List<string> TweetIds { get; set; } = new();
}

[GenerateSerializer]
[Description("Event to quote an existing tweet with additional text. Returns TweetPosted event with the new tweet details.")]
public class QuoteTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to quote. Must be a public tweet or one the user has access to.")]
    public string QuotedTweetId { get; set; } = string.Empty;

    [Id(2)]
    [Description("The text to add as a quote. Maximum 280 characters.")]
    public string Text { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to create a thread of multiple tweets. Returns multiple TweetPosted events in sequence.")]
public class CreateThreadEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("List of tweet texts to post as a thread. Each text maximum 280 characters.")]
    public List<string> TweetTexts { get; set; } = new();
}

// User Interaction Events
[GenerateSerializer]
[Description("Event to like a tweet. Returns TweetLiked event with success status.")]
public class LikeTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to like. Must be a public tweet or one the user has access to.")]
    public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to unlike a previously liked tweet. Returns TweetUnliked event with success status.")]
public class UnlikeTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to unlike. Must be a tweet previously liked by the user.")]
    public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to retweet a tweet. Returns Retweeted event with success status.")]
public class RetweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to retweet. Must be a public tweet or one the user has access to.")]
    public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to undo a retweet. Returns Unretweeted event with success status.")]
public class UnretweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to unretweet. Must be a tweet previously retweeted by the user.")]
    public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to bookmark a tweet for later reference. Returns success status.")]
public class BookmarkTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to bookmark. Must be a public tweet or one the user has access to.")]
    public string TweetId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to remove a tweet from bookmarks. Returns success status.")]
public class RemoveBookmarkEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to remove from bookmarks. Must be a previously bookmarked tweet.")]
    public string TweetId { get; set; } = string.Empty;
}

// User Profile Events
[GenerateSerializer]
[Description("Event to retrieve a user's profile by their username. Returns user profile data.")]
public class GetUserByUsernameEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The username to look up (without @ symbol). Must be a valid Twitter username.")]
    public string Username { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to retrieve a user's profile by their user ID. Returns user profile data.")]
public class GetUserByIdEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The Twitter user ID to look up. Must be a valid user ID.")]
    public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to retrieve the authenticated user's profile. Returns current user's profile data.")]
public class GetMyProfileEvent : TwitterWebApiEvent
{
}

[GenerateSerializer]
[Description("Event to update the authenticated user's profile. Returns updated profile data.")]
public class UpdateProfileEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("New display name. Maximum 50 characters.")]
    public string? Name { get; set; }

    [Id(2)]
    [Description("New profile description/bio. Maximum 160 characters.")]
    public string? Description { get; set; }

    [Id(3)]
    [Description("New location information. Maximum 30 characters.")]
    public string? Location { get; set; }

    [Id(4)]
    [Description("New website URL. Must be a valid URL.")]
    public string? Url { get; set; }
}

// Relationship Events
[GenerateSerializer]
[Description("Event to follow a user. Returns UserFollowed event with success status.")]
public class FollowUserEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user to follow. Must be a valid user ID.")]
    public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to unfollow a user. Returns UserUnfollowed event with success status.")]
public class UnfollowUserEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user to unfollow. Must be a user currently being followed.")]
    public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to retrieve a user's followers. Returns list of user profiles.")]
public class GetFollowersEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("Optional user ID to get followers for. If not provided, gets authenticated user's followers.")]
    public string? TargetUserId { get; set; }

    [Id(2)]
    [Description("Maximum number of followers to return per request. Default 100, maximum 1000.")]
    public int MaxResults { get; set; } = 100;
}

[GenerateSerializer]
[Description("Event to retrieve users that a specified user is following. Returns list of user profiles.")]
public class GetFollowingEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("Optional user ID to get following list for. If not provided, gets authenticated user's following list.")]
    public string? TargetUserId { get; set; }

    [Id(2)]
    [Description("Maximum number of following users to return per request. Default 100, maximum 1000.")]
    public int MaxResults { get; set; } = 100;
}

[GenerateSerializer]
[Description("Event to block a user. Returns success status.")]
public class BlockUserEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user to block. Must be a valid user ID.")]
    public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to unblock a previously blocked user. Returns success status.")]
public class UnblockUserEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user to unblock. Must be a previously blocked user.")]
    public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to mute a user. Returns success status.")]
public class MuteUserEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user to mute. Must be a valid user ID.")]
    public string TargetUserId { get; set; } = string.Empty;
}

[GenerateSerializer]
[Description("Event to unmute a previously muted user. Returns success status.")]
public class UnmuteUserEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user to unmute. Must be a previously muted user.")]
    public string TargetUserId { get; set; } = string.Empty;
}

// Timeline Events
[GenerateSerializer]
[Description("Event to retrieve the authenticated user's home timeline. Returns list of tweets.")]
public class GetHomeTimelineEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("Maximum number of tweets to return per request. Default 100, maximum 800.")]
    public int MaxResults { get; set; } = 100;

    [Id(2)]
    [Description("Optional pagination token for retrieving next page of results.")]
    public string? PaginationToken { get; set; }
}

[GenerateSerializer]
[Description("Event to retrieve a user's timeline (tweets posted by the user). Returns list of tweets.")]
public class GetUserTimelineEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user whose timeline to retrieve. Must be a valid user ID.")]
    public string TargetUserId { get; set; } = string.Empty;

    [Id(2)]
    [Description("Maximum number of tweets to return per request. Default 100, maximum 800.")]
    public int MaxResults { get; set; } = 100;

    [Id(3)]
    [Description("Optional pagination token for retrieving next page of results.")]
    public string? PaginationToken { get; set; }
}

[GenerateSerializer]
[Description("Event to retrieve tweets mentioning the authenticated user. Returns list of tweets.")]
public class GetMentionsTimelineEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("Maximum number of mentions to return per request. Default 100, maximum 800.")]
    public int MaxResults { get; set; } = 100;

    [Id(2)]
    [Description("Optional pagination token for retrieving next page of results.")]
    public string? PaginationToken { get; set; }
}

// Direct Message Events
[GenerateSerializer]
[Description("Event to send a direct message to a user. Returns success status.")]
public class SendDirectMessageEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the user to send the message to. Must be a valid user ID.")]
    public string RecipientId { get; set; } = string.Empty;

    [Id(2)]
    [Description("The text content of the direct message. Maximum 10000 characters.")]
    public string Text { get; set; } = string.Empty;
}

// Media Upload Events
[GenerateSerializer]
[Description("Event to upload media for attaching to tweets. Returns media ID for use in tweet creation.")]
public class UploadMediaEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The binary data of the media file to upload.")]
    public byte[] MediaData { get; set; } = Array.Empty<byte>();

    [Id(2)]
    [Description("The MIME type of the media (e.g., 'image/jpeg', 'image/png', 'video/mp4').")]
    public string MediaType { get; set; } = string.Empty;

    [Id(3)]
    [Description("Optional alt text for accessibility. Maximum 1000 characters.")]
    public string? AltText { get; set; }
}

// Tweet Creation Events
[GenerateSerializer]
[Description("Event to post a new tweet. Returns TweetPosted event with the new tweet details.")]
public class PostTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The text content of the tweet. Maximum 280 characters.")]
    public string Text { get; set; } = string.Empty;

    [Id(2)]
    [Description("Optional list of media IDs to attach to the tweet. Maximum 4 media items.")]
    public List<string>? MediaIds { get; set; }
}

[GenerateSerializer]
[Description("Event to reply to an existing tweet. Returns TweetPosted event with the reply tweet details.")]
public class ReplyToTweetEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The ID of the tweet to reply to. Must be a public tweet or one the user has access to.")]
    public string InReplyToTweetId { get; set; } = string.Empty;

    [Id(2)]
    [Description("The text content of the reply. Maximum 280 characters.")]
    public string Text { get; set; } = string.Empty;

    [Id(3)]
    [Description("Optional list of media IDs to attach to the reply. Maximum 4 media items.")]
    public List<string>? MediaIds { get; set; }
}

[GenerateSerializer]
[Description("Event to search for recent tweets matching a query. Returns list of matching tweets.")]
public class SearchRecentTweetsEvent : TwitterWebApiEvent
{
    [Id(1)]
    [Description("The search query. Supports Twitter search operators.")]
    public string Query { get; set; } = string.Empty;

    [Id(2)]
    [Description("Maximum number of tweets to return. Default 10, maximum 100.")]
    public int MaxResults { get; set; } = 10;
}