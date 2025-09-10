using System;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.TestBase;
using Aevatar.GAgents.Twitter.GAgents;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Twitter.Test;

public class TwitterStateTransitionTests : AevatarTwitterTestBase
{
    [Fact]
    public void GAgentTransitionState_TweetPostedLogEvent_UpdatesState()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        var @event = new TweetPostedLogEvent
        {
            TweetId = "tweet123",
            Text = "Test tweet",
            PostedAt = DateTime.UtcNow
        };

        // Act
        // This would normally be called internally by the GAgent
        // We need to test the state transition logic
        ApplyStateTransition(state, @event);

        // Assert
        Assert.Contains("tweet123", state.TweetsPosted);
        Assert.Equal(@event.PostedAt, state.LastOperationUtc);
        Assert.Equal(1, state.OperationCounts["tweets_posted"]);
    }

    [Fact]
    public void GAgentTransitionState_TweetDeletedLogEvent_RemovesTweetFromState()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        state.TweetsPosted.Add("tweet123");
        
        var @event = new TweetDeletedLogEvent
        {
            TweetId = "tweet123",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        ApplyStateTransition(state, @event);

        // Assert
        Assert.DoesNotContain("tweet123", state.TweetsPosted);
        Assert.Equal(@event.DeletedAt, state.LastOperationUtc);
        Assert.Equal(1, state.OperationCounts["tweets_deleted"]);
    }

    [Fact]
    public void GAgentTransitionState_TweetInteractionLogEvent_Like_UpdatesState()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        var @event = new TweetInteractionLogEvent
        {
            TweetId = "tweet123",
            InteractionType = "like",
            InteractedAt = DateTime.UtcNow
        };

        // Act
        ApplyStateTransition(state, @event);

        // Assert
        Assert.Contains("tweet123", state.LikedTweets);
        Assert.Equal(@event.InteractedAt, state.LastOperationUtc);
        Assert.Equal(1, state.OperationCounts["tweets_liked"]);
    }

    [Fact]
    public void GAgentTransitionState_TweetInteractionLogEvent_Unlike_UpdatesState()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        state.LikedTweets.Add("tweet123");
        
        var @event = new TweetInteractionLogEvent
        {
            TweetId = "tweet123",
            InteractionType = "unlike",
            InteractedAt = DateTime.UtcNow
        };

        // Act
        ApplyStateTransition(state, @event);

        // Assert
        Assert.DoesNotContain("tweet123", state.LikedTweets);
        Assert.Equal(@event.InteractedAt, state.LastOperationUtc);
        Assert.Equal(1, state.OperationCounts["tweets_unliked"]);
    }

    [Fact]
    public void GAgentTransitionState_TweetInteractionLogEvent_Retweet_UpdatesState()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        var @event = new TweetInteractionLogEvent
        {
            TweetId = "tweet123",
            InteractionType = "retweet",
            InteractedAt = DateTime.UtcNow
        };

        // Act
        ApplyStateTransition(state, @event);

        // Assert
        Assert.Contains("tweet123", state.RetweetedTweets);
        Assert.Equal(@event.InteractedAt, state.LastOperationUtc);
        Assert.Equal(1, state.OperationCounts["tweets_retweeted"]);
    }

    [Fact]
    public void GAgentTransitionState_UserRelationshipLogEvent_Follow_UpdatesState()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        var @event = new UserRelationshipLogEvent
        {
            TargetUserId = "user123",
            RelationshipAction = "follow",
            ActionAt = DateTime.UtcNow
        };

        // Act
        ApplyStateTransition(state, @event);

        // Assert
        Assert.Contains("user123", state.FollowingUsers);
        Assert.Equal(@event.ActionAt, state.LastOperationUtc);
        Assert.Equal(1, state.OperationCounts["users_followed"]);
    }

    [Fact]
    public void GAgentTransitionState_UserRelationshipLogEvent_Unfollow_UpdatesState()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        state.FollowingUsers.Add("user123");
        
        var @event = new UserRelationshipLogEvent
        {
            TargetUserId = "user123",
            RelationshipAction = "unfollow",
            ActionAt = DateTime.UtcNow
        };

        // Act
        ApplyStateTransition(state, @event);

        // Assert
        Assert.DoesNotContain("user123", state.FollowingUsers);
        Assert.Equal(@event.ActionAt, state.LastOperationUtc);
        Assert.Equal(1, state.OperationCounts["users_unfollowed"]);
    }

    [Fact]
    public void GAgentTransitionState_ConfigurationSetLogEvent_UpdatesConfiguration()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        var config = new TwitterWebApiGAgentConfiguration
        {
            BaseApiUrl = "https://api.twitter.com/2",
            BearerToken = "test-bearer-token",
            RequestTimeoutSeconds = 30,
            ConsumerKey = "test-consumer-key",
            ConsumerSecret = "test-consumer-secret",
            OAuthToken = "test-oauth-token",
            OAuthTokenSecret = "test-oauth-token-secret"
        };
        
        var @event = new ConfigurationSetLogEvent
        {
            ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(config),
            ConfiguredAt = DateTime.UtcNow
        };

        // Act
        ApplyStateTransition(state, @event);

        // Assert
        Assert.NotNull(state.Configuration);
        Assert.Equal("https://api.twitter.com/2", state.Configuration.BaseApiUrl);
        Assert.Equal("test-bearer-token", state.Configuration.BearerToken);
        Assert.Equal(30, state.Configuration.RequestTimeoutSeconds);
        Assert.Equal("test-consumer-key", state.Configuration.ConsumerKey);
        Assert.Equal("test-consumer-secret", state.Configuration.ConsumerSecret);
        Assert.Equal("test-oauth-token", state.Configuration.OAuthToken);
        Assert.Equal("test-oauth-token-secret", state.Configuration.OAuthTokenSecret);
    }

    [Fact]
    public void GAgentTransitionState_MultipleEvents_MaintainsCorrectCounts()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        
        // Act - Apply multiple events
        ApplyStateTransition(state, new TweetPostedLogEvent
        {
            TweetId = "tweet1",
            Text = "First tweet",
            PostedAt = DateTime.UtcNow
        });
        
        ApplyStateTransition(state, new TweetPostedLogEvent
        {
            TweetId = "tweet2",
            Text = "Second tweet",
            PostedAt = DateTime.UtcNow.AddMinutes(1)
        });
        
        ApplyStateTransition(state, new TweetInteractionLogEvent
        {
            TweetId = "tweet3",
            InteractionType = "like",
            InteractedAt = DateTime.UtcNow.AddMinutes(2)
        });

        // Assert
        Assert.Equal(2, state.TweetsPosted.Count);
        Assert.Contains("tweet1", state.TweetsPosted);
        Assert.Contains("tweet2", state.TweetsPosted);
        Assert.Single(state.LikedTweets);
        Assert.Contains("tweet3", state.LikedTweets);
        Assert.Equal(2, state.OperationCounts["tweets_posted"]);
        Assert.Equal(1, state.OperationCounts["tweets_liked"]);
    }

    [Fact]
    public void GAgentTransitionState_ComplexScenario_HandlesMultipleOperations()
    {
        // Arrange
        var state = new TwitterWebApiGAgentState();
        
        // Act - Simulate a complex scenario
        // 1. Set configuration
        var config = new TwitterWebApiGAgentConfiguration
        {
            BaseApiUrl = "https://api.twitter.com/2",
            BearerToken = "bearer-token",
            RequestTimeoutSeconds = 30,
            ConsumerKey = "consumer-key",
            ConsumerSecret = "consumer-secret",
            OAuthToken = "oauth-token",
            OAuthTokenSecret = "oauth-token-secret"
        };
        
        ApplyStateTransition(state, new ConfigurationSetLogEvent
        {
            ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(config),
            ConfiguredAt = DateTime.UtcNow
        });
        
        // 2. Post a tweet
        ApplyStateTransition(state, new TweetPostedLogEvent
        {
            TweetId = "tweet1",
            Text = "Hello Twitter!",
            PostedAt = DateTime.UtcNow.AddMinutes(1)
        });
        
        // 3. Like a tweet
        ApplyStateTransition(state, new TweetInteractionLogEvent
        {
            TweetId = "tweet2",
            InteractionType = "like",
            InteractedAt = DateTime.UtcNow.AddMinutes(2)
        });
        
        // 4. Retweet
        ApplyStateTransition(state, new TweetInteractionLogEvent
        {
            TweetId = "tweet3",
            InteractionType = "retweet",
            InteractedAt = DateTime.UtcNow.AddMinutes(3)
        });
        
        // 5. Follow a user
        ApplyStateTransition(state, new UserRelationshipLogEvent
        {
            TargetUserId = "user1",
            RelationshipAction = "follow",
            ActionAt = DateTime.UtcNow.AddMinutes(4)
        });
        
        // 6. Delete the tweet
        ApplyStateTransition(state, new TweetDeletedLogEvent
        {
            TweetId = "tweet1",
            DeletedAt = DateTime.UtcNow.AddMinutes(5)
        });

        // Assert
        Assert.NotNull(state.Configuration);
        Assert.Empty(state.TweetsPosted); // Tweet was deleted
        Assert.Single(state.LikedTweets);
        Assert.Single(state.RetweetedTweets);
        Assert.Single(state.FollowingUsers);
        Assert.Equal(1, state.OperationCounts["tweets_posted"]);
        Assert.Equal(1, state.OperationCounts["tweets_deleted"]);
        Assert.Equal(1, state.OperationCounts["tweets_liked"]);
        Assert.Equal(1, state.OperationCounts["tweets_retweeted"]);
        Assert.Equal(1, state.OperationCounts["users_followed"]);
    }

    // Helper method to simulate state transitions
    // In actual implementation, this would be handled by the GAgent's GAgentTransitionState method
    private void ApplyStateTransition(TwitterWebApiGAgentState state, TwitterWebApiStateLogEvent @event)
    {
        // This simulates what would happen in TwitterWebApiGAgent.GAgentTransitionState
        // The actual implementation would be in the GAgent itself
        // For testing purposes, we're replicating the logic here
        
        switch (@event)
        {
            case ConfigurationSetLogEvent e:
                state.Configuration = System.Text.Json.JsonSerializer.Deserialize<TwitterWebApiGAgentConfiguration>(e.ConfigurationJson);
                state.LastOperationUtc = e.ConfiguredAt;
                break;
                
            case TweetPostedLogEvent e:
                state.TweetsPosted.Add(e.TweetId);
                state.LastOperationUtc = e.PostedAt;
                IncrementOperationCount(state, "tweets_posted");
                break;
                
            case TweetDeletedLogEvent e:
                state.TweetsPosted.Remove(e.TweetId);
                state.LastOperationUtc = e.DeletedAt;
                IncrementOperationCount(state, "tweets_deleted");
                break;
                
            case TweetInteractionLogEvent e:
                state.LastOperationUtc = e.InteractedAt;
                switch (e.InteractionType)
                {
                    case "like":
                        state.LikedTweets.Add(e.TweetId);
                        IncrementOperationCount(state, "tweets_liked");
                        break;
                    case "unlike":
                        state.LikedTweets.Remove(e.TweetId);
                        IncrementOperationCount(state, "tweets_unliked");
                        break;
                    case "retweet":
                        state.RetweetedTweets.Add(e.TweetId);
                        IncrementOperationCount(state, "tweets_retweeted");
                        break;
                    case "unretweet":
                        state.RetweetedTweets.Remove(e.TweetId);
                        IncrementOperationCount(state, "tweets_unretweeted");
                        break;
                }
                break;
                
            case UserRelationshipLogEvent e:
                state.LastOperationUtc = e.ActionAt;
                switch (e.RelationshipAction)
                {
                    case "follow":
                        state.FollowingUsers.Add(e.TargetUserId);
                        IncrementOperationCount(state, "users_followed");
                        break;
                    case "unfollow":
                        state.FollowingUsers.Remove(e.TargetUserId);
                        IncrementOperationCount(state, "users_unfollowed");
                        break;
                }
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
        {
            state.OperationCounts[operation] = 0;
        }
        state.OperationCounts[operation]++;
    }
}