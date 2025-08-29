using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;
using Aevatar.GAgents.Twitter.GAgents;
using Aevatar.GAgents.Twitter.GEvents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Twitter.Test;

/// <summary>
/// Unit tests for TwitterWebApiGAgent following Aevatar testing patterns
/// </summary>
[Trait("Category", "SkipOnCI")]
public sealed class TwitterWebApiGAgentTests : AevatarTwitterTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public TwitterWebApiGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region Tweet Management Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task PostTweetAsync_ValidText_ShouldReturnTweetDto()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var tweetText = "Hello Twitter! #testing";

        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("tweet123", tweetText));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.PostTweetAsync(tweetText);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("tweet123");
        result.Text.ShouldBe(tweetText);

        _testOutputHelper.WriteLine($"Successfully posted tweet with ID: {result.Id}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task PostTweetAsync_EmptyText_ShouldThrowArgumentException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () => { await agent.PostTweetAsync(""); });

        _testOutputHelper.WriteLine("Empty tweet text correctly threw ArgumentException");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task PostTweetAsync_WithMediaIds_ShouldIncludeMedia()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var mediaIds = new List<string> { "media123", "media456" };

        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse(text: "Tweet with media"));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.PostTweetAsync("Tweet with media", mediaIds);

        // Assert
        result.ShouldNotBeNull();
        result.Text.ShouldContain("media");

        _testOutputHelper.WriteLine($"Posted tweet with {mediaIds.Count} media items");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task ReplyToTweetAsync_ValidInput_ShouldCreateReply()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var originalTweetId = "original123";
        var replyText = "This is my reply";

        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("reply123", replyText));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.ReplyToTweetAsync(originalTweetId, replyText);

        // Assert
        result.ShouldNotBeNull();
        result.Text.ShouldBe(replyText);

        _testOutputHelper.WriteLine($"Created reply to tweet {originalTweetId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task QuoteTweetAsync_ValidInput_ShouldCreateQuoteTweet()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var quotedTweetId = "quoted123";
        var quoteText = "Check this out!";

        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("quote123", quoteText));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.QuoteTweetAsync(quotedTweetId, quoteText);

        // Assert
        result.ShouldNotBeNull();
        result.Text.ShouldBe(quoteText);

        _testOutputHelper.WriteLine($"Created quote tweet for {quotedTweetId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task DeleteTweetAsync_ValidId_ShouldReturnTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var tweetId = "tweet123";

        SetupHttpResponse(HttpMethod.Delete, $"/tweets/{tweetId}",
            new { data = new { deleted = true } });

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.DeleteTweetAsync(tweetId);

        // Assert
        result.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Successfully deleted tweet {tweetId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GetTweetByIdAsync_ValidId_ShouldReturnTweet()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var tweetId = "tweet123";

        SetupHttpResponse(HttpMethod.Get, $"/tweets/{tweetId}",
            CreateTestTweetResponse(tweetId, "Retrieved tweet"));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.GetTweetByIdAsync(tweetId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(tweetId);

        _testOutputHelper.WriteLine($"Retrieved tweet: {result.Text}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task SearchRecentTweetsAsync_ValidQuery_ShouldReturnTweets()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var searchQuery = "#testing";

        var searchResponse = new
        {
            data = new[]
            {
                new
                {
                    id = "tweet1", text = "First #testing tweet", author_id = "user1",
                    created_at = DateTime.UtcNow.ToString("O")
                },
                new
                {
                    id = "tweet2", text = "Second #testing tweet", author_id = "user2",
                    created_at = DateTime.UtcNow.ToString("O")
                }
            }
        };

        SetupHttpResponse(HttpMethod.Get, "/tweets/search/recent", searchResponse);

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var results = await agent.SearchRecentTweetsAsync(searchQuery, 10);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(2);
        results.All(t => t.Text.Contains("#testing")).ShouldBeTrue();

        _testOutputHelper.WriteLine($"Found {results.Count} tweets matching query: {searchQuery}");
    }

    #endregion

    #region User Interaction Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task LikeTweetAsync_ValidTweetId_ShouldReturnTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var tweetId = "tweet123";
        var userId = "user123";

        // Setup user profile response first
        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(userId));

        // Setup like response
        SetupHttpResponse(HttpMethod.Post, $"/users/{userId}/likes",
            new { data = new { liked = true } });

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.LikeTweetAsync(tweetId);

        // Assert
        result.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Successfully liked tweet {tweetId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task UnlikeTweetAsync_ValidTweetId_ShouldReturnTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var tweetId = "tweet123";
        var userId = "user123";

        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(userId));
        SetupHttpResponse(HttpMethod.Delete, $"/users/{userId}/likes/{tweetId}", "");

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.UnlikeTweetAsync(tweetId);

        // Assert
        result.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Successfully unliked tweet {tweetId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task RetweetAsync_ValidTweetId_ShouldReturnTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var tweetId = "tweet123";
        var userId = "user123";

        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(userId));
        SetupHttpResponse(HttpMethod.Post, $"/users/{userId}/retweets",
            new { data = new { retweeted = true } });

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.RetweetAsync(tweetId);

        // Assert
        result.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Successfully retweeted {tweetId}");
    }

    #endregion

    #region User Profile Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GetUserByUsernameAsync_ValidUsername_ShouldReturnProfile()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var username = "testuser";

        SetupHttpResponse(HttpMethod.Get, $"/users/by/username/{username}",
            CreateTestUserResponse("user123", username));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.GetUserByUsernameAsync(username);

        // Assert
        result.ShouldNotBeNull();
        result.Username.ShouldBe(username);
        result.Name.ShouldBe("Test User");

        _testOutputHelper.WriteLine($"Retrieved profile for @{username}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GetUserByIdAsync_ValidId_ShouldReturnProfile()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var userId = "user123";

        SetupHttpResponse(HttpMethod.Get, $"/users/{userId}",
            CreateTestUserResponse(userId));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.GetUserByIdAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(userId);

        _testOutputHelper.WriteLine($"Retrieved profile for user ID: {userId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GetMyProfileAsync_WithOAuth_ShouldReturnOwnProfile()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);

        SetupHttpResponse(HttpMethod.Get, "/users/me",
            CreateTestUserResponse("myuser123", "myusername"));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.GetMyProfileAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Username.ShouldBe("myusername");

        // Verify state was updated
        var state = await agent.GetStateAsync();
        state.UserId.ShouldBe("myuser123");

        _testOutputHelper.WriteLine($"Retrieved own profile: @{result.Username}");
    }

    #endregion

    #region Relationship Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task FollowUserAsync_ValidUserId_ShouldReturnTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var targetUserId = "target123";
        var myUserId = "user123";

        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(myUserId));
        SetupHttpResponse(HttpMethod.Post, $"/users/{myUserId}/following",
            new { data = new { following = true, pending_follow = false } });

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.FollowUserAsync(targetUserId);

        // Assert
        result.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Successfully followed user {targetUserId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task UnfollowUserAsync_ValidUserId_ShouldReturnTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var targetUserId = "target123";
        var myUserId = "user123";

        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(myUserId));
        SetupHttpResponse(HttpMethod.Delete, $"/users/{myUserId}/following/{targetUserId}", "");

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.UnfollowUserAsync(targetUserId);

        // Assert
        result.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Successfully unfollowed user {targetUserId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GetFollowersAsync_ValidUserId_ShouldReturnList()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var userId = "user123";

        var followersResponse = new
        {
            data = new[]
            {
                new { id = "follower1", username = "follower1", name = "Follower One" },
                new { id = "follower2", username = "follower2", name = "Follower Two" }
            }
        };

        SetupHttpResponse(HttpMethod.Get, $"/users/{userId}/followers", followersResponse);

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var results = await agent.GetFollowersAsync(userId, 100);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(2);
        results[0].Username.ShouldBe("follower1");

        _testOutputHelper.WriteLine($"Retrieved {results.Count} followers for user {userId}");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GetFollowingAsync_ValidUserId_ShouldReturnList()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var userId = "user123";

        var followingResponse = new
        {
            data = new[]
            {
                new { id = "following1", username = "following1", name = "Following One" },
                new { id = "following2", username = "following2", name = "Following Two" }
            }
        };

        SetupHttpResponse(HttpMethod.Get, $"/users/{userId}/following", followingResponse);

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var results = await agent.GetFollowingAsync(userId, 100);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(2);

        _testOutputHelper.WriteLine($"Retrieved {results.Count} following for user {userId}");
    }

    #endregion

    #region Event Handler Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task HandlePostTweetEvent_ValidEvent_ShouldPostTweet()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var tweetText = "Event-driven tweet!";

        SetupHttpResponse(HttpMethod.Post, "/tweets",
            CreateTestTweetResponse("event123", tweetText));

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act - Simulate event handling by calling the method directly
        // In real scenario, this would be triggered by PublishAsync
        var result = await agent.PostTweetAsync(tweetText);

        // Assert
        result.ShouldNotBeNull();
        result.Text.ShouldBe(tweetText);

        // Verify state was updated
        var state = await agent.GetStateAsync();
        state.TweetsPosted.ShouldContain("event123");

        _testOutputHelper.WriteLine("Event handler successfully posted tweet");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task HandleLikeTweetEvent_ValidEvent_ShouldLikeTweet()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var tweetId = "tweet123";
        var userId = "user123";

        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(userId));
        SetupHttpResponse(HttpMethod.Post, $"/users/{userId}/likes",
            new { data = new { liked = true } });

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.LikeTweetAsync(tweetId);

        // Assert
        result.ShouldBeTrue();

        // Verify state was updated
        var state = await agent.GetStateAsync();
        state.LikedTweets.ShouldContain(tweetId);

        _testOutputHelper.WriteLine("Event handler successfully liked tweet");
    }

    #endregion

    #region Error Handling Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task PostTweetAsync_ApiError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();

        // Simulate an HTTP Unauthorized from server
        SetupHttpResponse(HttpMethod.Post, "/tweets",
            CreateErrorResponse("Unauthorized", 401), HttpStatusCode.Unauthorized);

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () => { await agent.PostTweetAsync("Test tweet"); });

        _testOutputHelper.WriteLine("API error correctly threw HttpRequestException");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task DeleteTweetAsync_NonExistentTweet_ShouldReturnFalse()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var tweetId = "nonexistent123";

        SetupHttpResponse(HttpMethod.Delete, $"/tweets/{tweetId}",
            CreateErrorResponse("Tweet not found", 404), HttpStatusCode.NotFound);

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.DeleteTweetAsync(tweetId);

        // Assert
        result.ShouldBeFalse();

        _testOutputHelper.WriteLine("Non-existent tweet deletion returned false as expected");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GetUserByUsernameAsync_InvalidUsername_ShouldReturnNull()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var username = "nonexistentuser";

        SetupHttpResponse(HttpMethod.Get, $"/users/by/username/{username}",
            CreateErrorResponse("User not found", 404), HttpStatusCode.NotFound);

        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act
        var result = await agent.GetUserByUsernameAsync(username);

        // Assert
        result.ShouldBeNull();

        _testOutputHelper.WriteLine("Invalid username correctly returned null");
    }

    #endregion

    #region State Management Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GAgent_StateOperations_ShouldUpdateCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);

        // Act - Perform multiple operations
        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("tweet1"));
        var tweet1 = await agent.PostTweetAsync("First tweet");

        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("tweet2"));
        var tweet2 = await agent.PostTweetAsync("Second tweet");

        // Assert - Verify state tracking
        var state = await agent.GetStateAsync();
        state.TweetsPosted.Count.ShouldBe(2);
        state.TweetsPosted.ShouldContain("tweet1");
        state.TweetsPosted.ShouldContain("tweet2");
        state.OperationCounts["tweets_posted"].ShouldBe(2);

        _testOutputHelper.WriteLine($"State correctly tracked {state.TweetsPosted.Count} tweets");
    }

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task GAgent_MultipleInstances_ShouldIsolateState()
    {
        // Arrange
        var agent1Id = Guid.NewGuid();
        var agent2Id = Guid.NewGuid();
        var config = CreateTestConfiguration();

        var agent1 = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agent1Id, config);
        var agent2 = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agent2Id, config);

        // Act
        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("tweet1"));
        await agent1.PostTweetAsync("Agent 1 tweet");

        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("tweet2"));
        await agent2.PostTweetAsync("Agent 2 tweet");

        // Assert - States should be isolated
        var state1 = await agent1.GetStateAsync();
        var state2 = await agent2.GetStateAsync();

        state1.TweetsPosted.Count.ShouldBe(1);
        state2.TweetsPosted.Count.ShouldBe(1);
        state1.TweetsPosted.ShouldNotBe(state2.TweetsPosted);

        _testOutputHelper.WriteLine("Multiple agents correctly isolated their states");
    }

    #endregion

    #region Integration Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task EndToEnd_TweetLifecycle_ShouldCompleteSuccessfully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration(useBearerToken: false, useOAuth: true);
        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);
        var userId = "user123";

        // Act & Assert - Complete tweet lifecycle

        // 1. Post a tweet
        SetupHttpResponse(HttpMethod.Post, "/tweets", CreateTestTweetResponse("tweet123", "Test tweet"));
        var tweet = await agent.PostTweetAsync("Test tweet");
        tweet.ShouldNotBeNull();
        _testOutputHelper.WriteLine($"Step 1: Posted tweet {tweet.Id}");

        // 2. Like the tweet
        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(userId));
        SetupHttpResponse(HttpMethod.Post, $"/users/{userId}/likes", new { data = new { liked = true } });
        var liked = await agent.LikeTweetAsync(tweet.Id);
        liked.ShouldBeTrue();
        _testOutputHelper.WriteLine($"Step 2: Liked tweet {tweet.Id}");

        // 3. Retweet it
        SetupHttpResponse(HttpMethod.Get, "/users/me", CreateTestUserResponse(userId));
        SetupHttpResponse(HttpMethod.Post, $"/users/{userId}/retweets", new { data = new { retweeted = true } });
        var retweeted = await agent.RetweetAsync(tweet.Id);
        retweeted.ShouldBeTrue();
        _testOutputHelper.WriteLine($"Step 3: Retweeted tweet {tweet.Id}");

        // 4. Delete the tweet
        SetupHttpResponse(HttpMethod.Delete, $"/tweets/{tweet.Id}", new { data = new { deleted = true } });
        var deleted = await agent.DeleteTweetAsync(tweet.Id);
        deleted.ShouldBeTrue();
        _testOutputHelper.WriteLine($"Step 4: Deleted tweet {tweet.Id}");

        // Verify final state
        var state = await agent.GetStateAsync();
        // After deletion, the tweet should have been removed from TweetsPosted
        state.TweetsPosted.ShouldNotContain(tweet.Id);
        state.LikedTweets.ShouldContain(tweet.Id);
        state.RetweetedTweets.ShouldContain(tweet.Id);
        state.OperationCounts["tweets_posted"].ShouldBe(1);
        state.OperationCounts["tweets_liked"].ShouldBe(1);
        state.OperationCounts["tweets_retweetd"].ShouldBe(1);

        _testOutputHelper.WriteLine("End-to-end tweet lifecycle completed successfully");
    }

    #endregion

    #region Performance Tests

    //[Fact(Skip = "暂时跳过这个测试类，存在问题需要后续修复")]
    public async Task ConcurrentOperations_MultipleTweets_ShouldHandleCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var config = CreateTestConfiguration();
        var agent = await _gAgentFactory.GetGAgentAsync<ITwitterWebApiGAgent>(agentId, config);
        var tasks = new List<Task<TweetDto>>();

        // Setup responses for all concurrent requests with explicit sequence binding
        SetupHttpResponseSequence(HttpMethod.Post, "/tweets", new[]
        {
            CreateTestTweetResponse("tweet0", "Concurrent tweet 0"),
            CreateTestTweetResponse("tweet1", "Concurrent tweet 1"),
            CreateTestTweetResponse("tweet2", "Concurrent tweet 2"),
            CreateTestTweetResponse("tweet3", "Concurrent tweet 3"),
            CreateTestTweetResponse("tweet4", "Concurrent tweet 4")
        });

        // Act - Execute concurrent operations
        for (int i = 0; i < 5; i++)
        {
            var taskId = i;
            tasks.Add(agent.PostTweetAsync($"Concurrent tweet {taskId}"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.ShouldBe(5);
        results.Select(r => r.Id).Distinct().Count().ShouldBe(5);

        var state = await agent.GetStateAsync();
        state.TweetsPosted.Count.ShouldBe(5);

        _testOutputHelper.WriteLine($"Successfully handled {results.Length} concurrent operations");
    }

    #endregion
}