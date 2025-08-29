using System;
using System.Threading.Tasks;
using Aevatar.GAgents.Twitter.RateLimiting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aevatar.GAgents.Twitter.Test;

public class TwitterRateLimiterTests
{
    private readonly TwitterRateLimiter _rateLimiter;
    private readonly Mock<ILogger<TwitterRateLimiter>> _loggerMock;

    public TwitterRateLimiterTests()
    {
        _loggerMock = new Mock<ILogger<TwitterRateLimiter>>();
        _rateLimiter = new TwitterRateLimiter(_loggerMock.Object);
    }

    [Fact]
    public async Task TryConsumeAsync_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var endpoint = "GET /2/tweets/search/recent";

        // Act
        var result = await _rateLimiter.TryConsumeAsync(endpoint);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryConsumeAsync_ExceedsLimit_ReturnsFalse()
    {
        // Arrange
        var endpoint = "GET /2/users/123/followers"; // Limited to 15 per 15 minutes

        // Act - Consume all available tokens
        for (int i = 0; i < 15; i++)
        {
            var consumeResult = await _rateLimiter.TryConsumeAsync(endpoint);
            Assert.True(consumeResult, $"Failed to consume token {i + 1}");
        }

        // Try to consume one more
        var result = await _rateLimiter.TryConsumeAsync(endpoint);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task WaitForAvailabilityAsync_WhenLimitExceeded_Waits()
    {
        // Arrange
        var endpoint = "GET /2/users/123/followers";

        // Consume all tokens
        for (int i = 0; i < 15; i++)
        {
            await _rateLimiter.TryConsumeAsync(endpoint);
        }

        // Act
        var waitTask = _rateLimiter.WaitForAvailabilityAsync(endpoint);

        // Assert - Should not complete immediately
        var completed = await Task.WhenAny(waitTask, Task.Delay(100)) == waitTask;
        Assert.False(completed, "WaitForAvailabilityAsync should not complete immediately when rate limited");
    }

    [Fact]
    public void GetStatus_ReturnsCorrectStatus()
    {
        // Arrange
        var endpoint = "POST /2/tweets";

        // Act
        var status = _rateLimiter.GetStatus(endpoint);

        // Assert
        Assert.Equal(endpoint, status.Endpoint);
        Assert.Equal(200, status.Limit); // 200 tweets per 15 minutes
        Assert.Equal(200, status.Remaining); // Should be full at start
        Assert.True(status.ResetsAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetStatus_AfterConsumption_UpdatesRemaining()
    {
        // Arrange
        var endpoint = "POST /2/tweets";

        // Act
        await _rateLimiter.TryConsumeAsync(endpoint);
        await _rateLimiter.TryConsumeAsync(endpoint);
        var status = _rateLimiter.GetStatus(endpoint);

        // Assert
        Assert.Equal(198, status.Remaining);
    }

    [Fact]
    public async Task RateLimiter_HandlesWildcardEndpoints()
    {
        // Arrange
        var endpoint1 = "POST /2/users/123/likes";
        var endpoint2 = "POST /2/users/456/likes";

        // Act - Both should use the same bucket
        var result1 = await _rateLimiter.TryConsumeAsync(endpoint1);
        var result2 = await _rateLimiter.TryConsumeAsync(endpoint2);

        // Assert
        Assert.True(result1);
        Assert.True(result2);

        // Both endpoints should share the same limit
        var status1 = _rateLimiter.GetStatus(endpoint1);
        var status2 = _rateLimiter.GetStatus(endpoint2);

        Assert.Equal(status1.Limit, status2.Limit);
    }

    [Fact]
    public async Task RateLimiter_ResetsAfterWindow()
    {
        // This test would need to mock time or use a shorter window for testing
        // For now, we'll test the logic
        var bucket = new RateLimitBucket(2, TimeSpan.FromMilliseconds(100));

        // Consume all tokens
        Assert.True(await bucket.TryConsumeAsync());
        Assert.True(await bucket.TryConsumeAsync());
        Assert.False(await bucket.TryConsumeAsync());

        // Wait for window to pass
        await Task.Delay(150);

        // Should be able to consume again
        Assert.True(await bucket.TryConsumeAsync());
    }
}