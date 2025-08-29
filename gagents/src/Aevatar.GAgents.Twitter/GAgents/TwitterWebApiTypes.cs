using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.GEvent;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents;

#region Interface

public interface ITwitterWebApiGAgent : IStateGAgent<TwitterWebApiGAgentState>
{
    // Tweet Management
    Task<TweetDto> PostTweetAsync(string text, List<string>? mediaIds = null);
    Task<TweetDto> ReplyToTweetAsync(string tweetId, string text);
    Task<TweetDto> QuoteTweetAsync(string tweetId, string text);
    Task<bool> DeleteTweetAsync(string tweetId);
    Task<TweetDto?> GetTweetByIdAsync(string tweetId);
    Task<List<TweetDto>> SearchRecentTweetsAsync(string query, int maxResults = 10);
    
    // User Interactions
    Task<bool> LikeTweetAsync(string tweetId);
    Task<bool> UnlikeTweetAsync(string tweetId);
    Task<bool> RetweetAsync(string tweetId);
    Task<bool> UnretweetAsync(string tweetId);
    
    // User Management
    Task<UserProfileDto?> GetUserByUsernameAsync(string username);
    Task<UserProfileDto?> GetUserByIdAsync(string userId);
    Task<UserProfileDto?> GetMyProfileAsync();
    
    // Relationship Management
    Task<bool> FollowUserAsync(string targetUserId);
    Task<bool> UnfollowUserAsync(string targetUserId);
    Task<List<UserProfileDto>> GetFollowersAsync(string? userId = null, int maxResults = 100);
    Task<List<UserProfileDto>> GetFollowingAsync(string? userId = null, int maxResults = 100);
    
    // Timeline Operations
    Task<List<TweetDto>> GetHomeTimelineAsync(int maxResults = 10);
    Task<List<TweetDto>> GetUserTimelineAsync(string userId, int maxResults = 10);
    Task<List<TweetDto>> GetMentionsTimelineAsync(int maxResults = 10);
}

#endregion

#region State

[GenerateSerializer]
public class TwitterWebApiGAgentState : MemberState
{
    [Id(0)] public string UserId { get; set; } = string.Empty;
    [Id(1)] public TwitterWebApiGAgentConfiguration? Configuration { get; set; }
    [Id(2)] public HashSet<string> TweetsPosted { get; set; } = new();
    [Id(3)] public HashSet<string> LikedTweets { get; set; } = new();
    [Id(4)] public HashSet<string> RetweetedTweets { get; set; } = new();
    [Id(5)] public HashSet<string> FollowingUsers { get; set; } = new();
    [Id(6)] public Dictionary<string, int> OperationCounts { get; set; } = new();
    [Id(7)] public DateTime LastOperationUtc { get; set; }
}

#endregion

#region Configuration

[GenerateSerializer]
public class TwitterWebApiGAgentConfiguration : MemberConfigDto
{
    [Id(0)] public string ConsumerKey { get; set; } = string.Empty;
    [Id(1)] public string ConsumerSecret { get; set; } = string.Empty;
    [Id(2)] public string OAuthToken { get; set; } = string.Empty;
    [Id(3)] public string OAuthTokenSecret { get; set; } = string.Empty;
    [Id(4)] public string BearerToken { get; set; } = string.Empty;
    [Id(5)] public string BaseApiUrl { get; set; } = "https://api.twitter.com/2";
    [Id(6)] public int RequestTimeoutSeconds { get; set; } = 30;
    [Id(7)] public int MaxRequestsPerWindow { get; set; } = 300;
    [Id(8)] public int RateLimitWindowMinutes { get; set; } = 15;
    [Id(9)] public bool EnableRateLimiting { get; set; } = true;
}

#endregion

#region State Log Events

[GenerateSerializer]
public abstract class TwitterWebApiStateLogEvent : StateLogEventBase<TwitterWebApiStateLogEvent>
{
}

[GenerateSerializer]
public class ConfigurationSetLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string ConfigurationJson { get; set; } = string.Empty;
    [Id(1)] public DateTime ConfiguredAt { get; set; }
}

[GenerateSerializer]
public class TweetPostedLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TweetId { get; set; } = string.Empty;
    [Id(1)] public string Text { get; set; } = string.Empty;
    [Id(2)] public DateTime PostedAt { get; set; }
}

[GenerateSerializer]
public class TweetDeletedLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TweetId { get; set; } = string.Empty;
    [Id(1)] public DateTime DeletedAt { get; set; }
}

[GenerateSerializer]
public class TweetInteractionLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TweetId { get; set; } = string.Empty;
    [Id(1)] public string InteractionType { get; set; } = string.Empty; // like, unlike, retweet, unretweet
    [Id(2)] public DateTime InteractedAt { get; set; }
}

[GenerateSerializer]
public class UserRelationshipLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TargetUserId { get; set; } = string.Empty;
    [Id(1)] public string RelationshipAction { get; set; } = string.Empty; // follow, unfollow
    [Id(2)] public DateTime ActionAt { get; set; }
}

[GenerateSerializer]
public class UserProfileUpdatedLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string UserId { get; set; } = string.Empty;
    [Id(1)] public DateTime UpdatedAt { get; set; }
}

#endregion

#region DTOs

[GenerateSerializer]
public class TweetDto
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Text { get; set; } = string.Empty;
    [Id(2)] public string AuthorId { get; set; } = string.Empty;
    [Id(3)] public DateTime CreatedAt { get; set; }
    [Id(4)] public Dictionary<string, object>? PublicMetrics { get; set; }
    [Id(5)] public List<string>? EditHistoryTweetIds { get; set; }
}

[GenerateSerializer]
public class UserProfileDto
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Username { get; set; } = string.Empty;
    [Id(2)] public string Name { get; set; } = string.Empty;
    [Id(3)] public string? Description { get; set; }
    [Id(4)] public DateTime CreatedAt { get; set; }
    [Id(5)] public Dictionary<string, object>? PublicMetrics { get; set; }
    [Id(6)] public bool? Verified { get; set; }
}

#endregion

