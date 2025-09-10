using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.Twitter.GEvents;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Twitter.GAgents;

/// <summary>
/// Twitter Web API GAgent using modular components
/// </summary>
[GAgent("twitter", AevatarGAgentsConstants.ToolGAgentNamespace)]
public class TwitterWebApiGAgent :
    MemberGAgentBase<TwitterWebApiGAgentState, TwitterWebApiStateLogEvent, EventBase, TwitterWebApiGAgentConfiguration>,
    ITwitterWebApiGAgent
{
    private Client.ITwitterApiClient? _apiClient;
    private Authentication.ITwitterAuthenticationHandler? _authHandler;
    private RateLimiting.ITwitterRateLimiter? _rateLimiter;

    // Lazy-loaded services
    private Client.ITwitterApiClient ApiClient => _apiClient ??= CreateApiClient();
    private Authentication.ITwitterAuthenticationHandler AuthHandler => _authHandler ??= CreateAuthHandler();
    private RateLimiting.ITwitterRateLimiter RateLimiter => _rateLimiter ??= CreateRateLimiter();

    #region Service Creation

    private Client.ITwitterApiClient CreateApiClient()
    {
        var httpClient = ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        httpClient.BaseAddress = new Uri(State.Configuration?.BaseApiUrl ?? "https://api.twitter.com/2");

        var config = new Client.TwitterApiClientConfiguration
        {
            BaseApiUrl = State.Configuration?.BaseApiUrl ?? "https://api.twitter.com/2",
            RequestTimeoutSeconds = State.Configuration?.RequestTimeoutSeconds ?? 30,
            BearerToken = State.Configuration?.BearerToken,
            ConsumerKey = State.Configuration?.ConsumerKey,
            ConsumerSecret = State.Configuration?.ConsumerSecret,
            OAuthToken = State.Configuration?.OAuthToken,
            OAuthTokenSecret = State.Configuration?.OAuthTokenSecret
        };

        return new Client.TwitterApiClient(
            httpClient,
            AuthHandler,
            RateLimiter,
            ServiceProvider.GetRequiredService<ILogger<Client.TwitterApiClient>>(),
            config);
    }

    private Authentication.ITwitterAuthenticationHandler CreateAuthHandler()
    {
        return new Authentication.TwitterAuthenticationHandler(
            ServiceProvider.GetRequiredService<ILogger<Authentication.TwitterAuthenticationHandler>>());
    }

    private RateLimiting.ITwitterRateLimiter CreateRateLimiter()
    {
        return new RateLimiting.TwitterRateLimiter(
            ServiceProvider.GetRequiredService<ILogger<RateLimiting.TwitterRateLimiter>>());
    }

    #endregion

    #region GAgent Overrides

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(1);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        return Task.FromResult(new ChatResponse
        {
            Skip = true,
            Continue = false
        });
    }

    protected override async Task PerformConfigAsync(TwitterWebApiGAgentConfiguration configuration)
    {
        RaiseEvent(new ConfigurationSetLogEvent
        {
            ConfigurationJson = JsonSerializer.Serialize(configuration),
            ConfiguredAt = DateTime.UtcNow
        });

        await ConfirmEvents();

        // Re-create services with new configuration
        _apiClient = null;
        _authHandler = null;
        _rateLimiter = null;
    }

    protected override void GAgentTransitionState(TwitterWebApiGAgentState state,
        StateLogEventBase<TwitterWebApiStateLogEvent> @event)
    {
        switch (@event)
        {
            case ConfigurationSetLogEvent e:
                state.Configuration = JsonSerializer.Deserialize<TwitterWebApiGAgentConfiguration>(e.ConfigurationJson);
                state.LastOperationUtc = e.ConfiguredAt;
                break;

            case TweetPostedLogEvent e:
                state.TweetsPosted.Add(e.TweetId);
                IncrementOperationCount(state, "tweets_posted");
                state.LastOperationUtc = e.PostedAt;
                break;

            case TweetDeletedLogEvent e:
                state.TweetsPosted.Remove(e.TweetId);
                IncrementOperationCount(state, "tweets_deleted");
                state.LastOperationUtc = e.DeletedAt;
                break;

            case TweetInteractionLogEvent e:
                if (e.InteractionType == "like")
                    state.LikedTweets.Add(e.TweetId);
                else if (e.InteractionType == "unlike")
                    state.LikedTweets.Remove(e.TweetId);
                else if (e.InteractionType == "retweet")
                    state.RetweetedTweets.Add(e.TweetId);
                else if (e.InteractionType == "unretweet")
                    state.RetweetedTweets.Remove(e.TweetId);
                IncrementOperationCount(state, $"tweets_{e.InteractionType}d");
                state.LastOperationUtc = e.InteractedAt;
                break;

            case UserRelationshipLogEvent e:
                if (e.RelationshipAction == "follow")
                {
                    state.FollowingUsers.Add(e.TargetUserId);
                    IncrementOperationCount(state, "users_followed");
                }
                else if (e.RelationshipAction == "unfollow")
                {
                    state.FollowingUsers.Remove(e.TargetUserId);
                    IncrementOperationCount(state, "users_unfollowed");
                }

                state.LastOperationUtc = e.ActionAt;
                break;

            case UserProfileUpdatedLogEvent e:
                state.UserId = e.UserId;
                state.LastOperationUtc = e.UpdatedAt;
                break;
        }
    }

    private void IncrementOperationCount(TwitterWebApiGAgentState state, string operation)
    {
        if (!state.OperationCounts.ContainsKey(operation))
            state.OperationCounts[operation] = 0;
        state.OperationCounts[operation]++;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(@"Twitter Web API GAgent (Refactored) - Modular Twitter/X API v2 Integration

ARCHITECTURE:
• TwitterApiClient - Handles HTTP communication with rate limiting
• TwitterAuthenticationHandler - Manages OAuth 1.0a and Bearer token auth
• TwitterRateLimiter - Implements token bucket rate limiting

CAPABILITIES:
• Tweet Management (post, reply, quote, delete, search)
• User Interactions (like, retweet, follow)
• Timeline Operations (home, mentions, user timelines)
• User Profile Management
• Relationship Management

All operations use event sourcing for state management and support both
interface methods and event handlers for maximum flexibility.");
    }

    #endregion

    #region Tweet Management

    public async Task<TweetDto> PostTweetAsync(string text, List<string>? mediaIds = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Tweet text cannot be empty", nameof(text));

        object payload;
        if (mediaIds is { Count: > 0 })
        {
            payload = new { text, media = new { media_ids = mediaIds } };
        }
        else
        {
            payload = new { text };
        }

        var response = await ApiClient.SendRequestAsync<TweetResponseDto>(
            HttpMethod.Post,
            "/tweets",
            payload);

        RaiseEvent(new TweetPostedLogEvent
        {
            TweetId = response.Data.Id,
            Text = text,
            PostedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        return response.Data;
    }

    public async Task<TweetDto> ReplyToTweetAsync(string tweetId, string text)
    {
        if (string.IsNullOrWhiteSpace(tweetId))
            throw new ArgumentException("Tweet ID cannot be empty", nameof(tweetId));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Reply text cannot be empty", nameof(text));

        var payload = new
        {
            text,
            reply = new { in_reply_to_tweet_id = tweetId }
        };

        var response = await ApiClient.SendRequestAsync<TweetResponseDto>(
            HttpMethod.Post,
            "/tweets",
            payload);

        return response.Data;
    }

    public async Task<TweetDto> QuoteTweetAsync(string tweetId, string text)
    {
        if (string.IsNullOrWhiteSpace(tweetId))
            throw new ArgumentException("Tweet ID cannot be empty", nameof(tweetId));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Quote text cannot be empty", nameof(text));

        var payload = new
        {
            text,
            quote_tweet_id = tweetId
        };

        var response = await ApiClient.SendRequestAsync<TweetResponseDto>(
            HttpMethod.Post,
            "/tweets",
            payload);

        return response.Data;
    }

    public async Task<bool> DeleteTweetAsync(string tweetId)
    {
        try
        {
            await ApiClient.SendRequestAsync(
                HttpMethod.Delete,
                $"/tweets/{tweetId}");

            RaiseEvent(new TweetDeletedLogEvent
            {
                TweetId = tweetId,
                DeletedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete tweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<TweetDto?> GetTweetByIdAsync(string tweetId)
    {
        try
        {
            var response = await ApiClient.SendRequestAsync<TweetResponseDto>(
                HttpMethod.Get,
                $"/tweets/{tweetId}");

            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get tweet {TweetId}", tweetId);
            return null;
        }
    }

    public async Task<List<TweetDto>> SearchRecentTweetsAsync(string query, int maxResults = 10)
    {
        try
        {
            var response = await ApiClient.SendRequestAsync<TweetsResponseDto>(
                HttpMethod.Get,
                $"/tweets/search/recent?query={Uri.EscapeDataString(query)}&max_results={maxResults}");

            return response.Data ?? new List<TweetDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to search tweets with query: {Query}", query);
            return new List<TweetDto>();
        }
    }

    #endregion

    #region User Interactions

    public async Task<bool> LikeTweetAsync(string tweetId)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            await ApiClient.SendRequestAsync(
                HttpMethod.Post,
                $"/users/{userId}/likes",
                new { tweet_id = tweetId });

            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "like",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to like tweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<bool> UnlikeTweetAsync(string tweetId)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            await ApiClient.SendRequestAsync(
                HttpMethod.Delete,
                $"/users/{userId}/likes/{tweetId}");

            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "unlike",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unlike tweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<bool> RetweetAsync(string tweetId)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            await ApiClient.SendRequestAsync(
                HttpMethod.Post,
                $"/users/{userId}/retweets",
                new { tweet_id = tweetId });

            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "retweet",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<bool> UnretweetAsync(string tweetId)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            await ApiClient.SendRequestAsync(
                HttpMethod.Delete,
                $"/users/{userId}/retweets/{tweetId}");

            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "unretweet",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unretweet {TweetId}", tweetId);
            return false;
        }
    }

    #endregion

    #region User Management

    public async Task<UserProfileDto?> GetUserByUsernameAsync(string username)
    {
        try
        {
            var response = await ApiClient.SendRequestAsync<UserResponseDto>(
                HttpMethod.Get,
                $"/users/by/username/{username}");

            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get user by username: {Username}", username);
            return null;
        }
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var response = await ApiClient.SendRequestAsync<UserResponseDto>(
                HttpMethod.Get,
                $"/users/{userId}");

            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get user by ID: {UserId}", userId);
            return null;
        }
    }

    public async Task<UserProfileDto?> GetMyProfileAsync()
    {
        try
        {
            var response = await ApiClient.SendRequestAsync<UserResponseDto>(
                HttpMethod.Get,
                "/users/me");

            if (response.Data != null && State.UserId != response.Data.Id)
            {
                RaiseEvent(new UserProfileUpdatedLogEvent
                {
                    UserId = response.Data.Id,
                    UpdatedAt = DateTime.UtcNow
                });
                await ConfirmEvents();
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get my profile");
            return null;
        }
    }

    #endregion

    #region Relationship Management

    public async Task<bool> FollowUserAsync(string targetUserId)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            await ApiClient.SendRequestAsync(
                HttpMethod.Post,
                $"/users/{userId}/following",
                new { target_user_id = targetUserId });

            RaiseEvent(new UserRelationshipLogEvent
            {
                TargetUserId = targetUserId,
                RelationshipAction = "follow",
                ActionAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to follow user {UserId}", targetUserId);
            return false;
        }
    }

    public async Task<bool> UnfollowUserAsync(string targetUserId)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            await ApiClient.SendRequestAsync(
                HttpMethod.Delete,
                $"/users/{userId}/following/{targetUserId}");

            RaiseEvent(new UserRelationshipLogEvent
            {
                TargetUserId = targetUserId,
                RelationshipAction = "unfollow",
                ActionAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unfollow user {UserId}", targetUserId);
            return false;
        }
    }

    public async Task<List<UserProfileDto>> GetFollowersAsync(string? userId = null, int maxResults = 100)
    {
        try
        {
            userId ??= await EnsureUserIdAsync();

            var response = await ApiClient.SendRequestAsync<UsersResponseDto>(
                HttpMethod.Get,
                $"/users/{userId}/followers?max_results={maxResults}");

            return response.Data ?? new List<UserProfileDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get followers for user {UserId}", userId);
            return new List<UserProfileDto>();
        }
    }

    public async Task<List<UserProfileDto>> GetFollowingAsync(string? userId = null, int maxResults = 100)
    {
        try
        {
            userId ??= await EnsureUserIdAsync();

            var response = await ApiClient.SendRequestAsync<UsersResponseDto>(
                HttpMethod.Get,
                $"/users/{userId}/following?max_results={maxResults}");

            return response.Data ?? new List<UserProfileDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get following for user {UserId}", userId);
            return new List<UserProfileDto>();
        }
    }

    #endregion

    #region Timeline Operations

    public async Task<List<TweetDto>> GetHomeTimelineAsync(int maxResults = 10)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            var response = await ApiClient.SendRequestAsync<TweetsResponseDto>(
                HttpMethod.Get,
                $"/users/{userId}/timelines/reverse_chronological?max_results={maxResults}");

            return response.Data ?? new List<TweetDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get home timeline");
            return new List<TweetDto>();
        }
    }

    public async Task<List<TweetDto>> GetUserTimelineAsync(string userId, int maxResults = 10)
    {
        try
        {
            var response = await ApiClient.SendRequestAsync<TweetsResponseDto>(
                HttpMethod.Get,
                $"/users/{userId}/tweets?max_results={maxResults}");

            return response.Data ?? new List<TweetDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get timeline for user {UserId}", userId);
            return new List<TweetDto>();
        }
    }

    public async Task<List<TweetDto>> GetMentionsTimelineAsync(int maxResults = 10)
    {
        try
        {
            var userId = await EnsureUserIdAsync();

            var response = await ApiClient.SendRequestAsync<TweetsResponseDto>(
                HttpMethod.Get,
                $"/users/{userId}/mentions?max_results={maxResults}");

            return response.Data ?? new List<TweetDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get mentions timeline");
            return new List<TweetDto>();
        }
    }

    #endregion

    #region Event Handlers

    [EventHandler]
    public async Task HandlePostTweetEventAsync(PostTweetEvent @event)
    {
        await PostTweetAsync(@event.Text, @event.MediaIds);
    }

    [EventHandler]
    public async Task HandleReplyToTweetEventAsync(ReplyToTweetEvent @event)
    {
        await ReplyToTweetAsync(@event.InReplyToTweetId, @event.Text);
    }

    [EventHandler]
    public async Task HandleQuoteTweetEventAsync(QuoteTweetEvent @event)
    {
        await QuoteTweetAsync(@event.QuotedTweetId, @event.Text);
    }

    [EventHandler]
    public async Task HandleDeleteTweetEventAsync(DeleteTweetEvent @event)
    {
        await DeleteTweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleLikeTweetEventAsync(LikeTweetEvent @event)
    {
        await LikeTweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleUnlikeTweetEventAsync(UnlikeTweetEvent @event)
    {
        await UnlikeTweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleRetweetEventAsync(RetweetEvent @event)
    {
        await RetweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleUnretweetEventAsync(UnretweetEvent @event)
    {
        await UnretweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleFollowUserEventAsync(FollowUserEvent @event)
    {
        await FollowUserAsync(@event.TargetUserId);
    }

    [EventHandler]
    public async Task HandleUnfollowUserEventAsync(UnfollowUserEvent @event)
    {
        await UnfollowUserAsync(@event.TargetUserId);
    }

    #endregion

    #region Helper Methods

    private async Task<string> EnsureUserIdAsync()
    {
        if (!string.IsNullOrEmpty(State.UserId))
            return State.UserId;

        var profile = await GetMyProfileAsync();
        if (profile == null)
            throw new InvalidOperationException("Failed to get user profile");

        return profile.Id;
    }

    #endregion
}

// Response DTOs
public class TweetResponseDto
{
    public TweetDto Data { get; set; } = new();
}

public class TweetsResponseDto
{
    public List<TweetDto>? Data { get; set; }
}

public class UserResponseDto
{
    public UserProfileDto Data { get; set; } = new();
}

public class UsersResponseDto
{
    public List<UserProfileDto>? Data { get; set; }
}