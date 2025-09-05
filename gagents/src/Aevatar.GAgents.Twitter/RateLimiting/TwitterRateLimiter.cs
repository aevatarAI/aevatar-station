using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Twitter.RateLimiting;

/// <summary>
/// Implements Twitter API rate limiting according to official limits
/// </summary>
public class TwitterRateLimiter : ITwitterRateLimiter
{
    private readonly ILogger<TwitterRateLimiter> _logger;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets;

    public TwitterRateLimiter(ILogger<TwitterRateLimiter> logger)
    {
        _logger = logger;
        _buckets = new ConcurrentDictionary<string, RateLimitBucket>();
        InitializeDefaultBuckets();
    }

    private void InitializeDefaultBuckets()
    {
        // Twitter API v2 Rate Limits
        // Tweet endpoints
        _buckets["POST /2/tweets"] = new RateLimitBucket(200, TimeSpan.FromMinutes(15)); // 200 per 15 min per user
        _buckets["DELETE /2/tweets/*"] = new RateLimitBucket(50, TimeSpan.FromMinutes(15)); // 50 per 15 min
        
        // Like endpoints  
        _buckets["POST /2/users/*/likes"] = new RateLimitBucket(1000, TimeSpan.FromHours(24)); // 1000 per 24 hours
        _buckets["DELETE /2/users/*/likes/*"] = new RateLimitBucket(1000, TimeSpan.FromHours(24));
        
        // Retweet endpoints
        _buckets["POST /2/users/*/retweets"] = new RateLimitBucket(300, TimeSpan.FromHours(3)); // 300 per 3 hours
        _buckets["DELETE /2/users/*/retweets/*"] = new RateLimitBucket(300, TimeSpan.FromHours(3));
        
        // Follow endpoints
        _buckets["POST /2/users/*/following"] = new RateLimitBucket(400, TimeSpan.FromHours(24)); // 400 per 24 hours
        _buckets["DELETE /2/users/*/following/*"] = new RateLimitBucket(400, TimeSpan.FromHours(24));
        
        // Search endpoints
        _buckets["GET /2/tweets/search/recent"] = new RateLimitBucket(180, TimeSpan.FromMinutes(15)); // 180 per 15 min
        
        // Timeline endpoints
        _buckets["GET /2/users/*/timelines/reverse_chronological"] = new RateLimitBucket(180, TimeSpan.FromMinutes(15));
        _buckets["GET /2/users/*/tweets"] = new RateLimitBucket(900, TimeSpan.FromMinutes(15));
        
        // User lookup endpoints
        _buckets["GET /2/users/by/username/*"] = new RateLimitBucket(900, TimeSpan.FromMinutes(15));
        _buckets["GET /2/users/me"] = new RateLimitBucket(75, TimeSpan.FromMinutes(15));
        _buckets["GET /2/users/*"] = new RateLimitBucket(900, TimeSpan.FromMinutes(15));
        
        // Followers/Following endpoints
        _buckets["GET /2/users/*/followers"] = new RateLimitBucket(15, TimeSpan.FromMinutes(15));
        _buckets["GET /2/users/*/following"] = new RateLimitBucket(15, TimeSpan.FromMinutes(15));
    }

    public async Task<bool> TryConsumeAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var bucket = GetBucketForEndpoint(endpoint);
        if (bucket == null)
        {
            _logger.LogWarning("No rate limit bucket found for endpoint: {Endpoint}", endpoint);
            return true; // Allow if no bucket configured
        }

        var canConsume = await bucket.TryConsumeAsync();
        if (!canConsume)
        {
            _logger.LogWarning("Rate limit exceeded for endpoint: {Endpoint}. Limit: {Limit}/{Window}", 
                endpoint, bucket.Limit, bucket.Window);
        }

        return canConsume;
    }

    public async Task WaitForAvailabilityAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var bucket = GetBucketForEndpoint(endpoint);
        if (bucket == null)
        {
            return; // No rate limit
        }

        while (!await bucket.TryConsumeAsync())
        {
            var waitTime = bucket.GetTimeUntilNextAvailable();
            if (waitTime > TimeSpan.Zero)
            {
                _logger.LogInformation("Rate limit reached for {Endpoint}. Waiting {WaitTime:g} before retry.", 
                    endpoint, waitTime);
                await Task.Delay(waitTime, cancellationToken);
            }
        }
    }

    public RateLimitStatus GetStatus(string endpoint)
    {
        var bucket = GetBucketForEndpoint(endpoint);
        if (bucket == null)
        {
            return new RateLimitStatus
            {
                Endpoint = endpoint,
                Limit = int.MaxValue,
                Remaining = int.MaxValue,
                ResetsAt = DateTime.UtcNow.AddHours(1)
            };
        }

        return new RateLimitStatus
        {
            Endpoint = endpoint,
            Limit = bucket.Limit,
            Remaining = bucket.GetRemaining(),
            ResetsAt = bucket.GetResetTime()
        };
    }

    private RateLimitBucket? GetBucketForEndpoint(string endpoint)
    {
        // Normalize endpoint for matching
        var normalizedEndpoint = NormalizeEndpoint(endpoint);
        
        // Try exact match first
        if (_buckets.TryGetValue(normalizedEndpoint, out var bucket))
        {
            return bucket;
        }

        // Try pattern matching for parameterized endpoints
        foreach (var kvp in _buckets)
        {
            if (MatchesPattern(normalizedEndpoint, kvp.Key))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private string NormalizeEndpoint(string endpoint)
    {
        // Remove query parameters
        var questionIndex = endpoint.IndexOf('?');
        if (questionIndex >= 0)
        {
            endpoint = endpoint.Substring(0, questionIndex);
        }

        // Ensure it starts with method if provided
        if (!endpoint.StartsWith("GET ") && !endpoint.StartsWith("POST ") && 
            !endpoint.StartsWith("DELETE ") && !endpoint.StartsWith("PUT "))
        {
            endpoint = "GET " + endpoint;
        }

        return endpoint;
    }

    private bool MatchesPattern(string endpoint, string pattern)
    {
        // Simple wildcard matching for paths like /2/users/*/likes
        var patternParts = pattern.Split('/');
        var endpointParts = endpoint.Split('/');

        if (patternParts.Length != endpointParts.Length)
        {
            return false;
        }

        for (int i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i] == "*")
            {
                continue; // Wildcard matches anything
            }

            if (patternParts[i] != endpointParts[i])
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Represents a rate limit bucket with token bucket algorithm
/// </summary>
public class RateLimitBucket
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<DateTime> _consumptionTimes;
    private readonly object _lock = new();

    public int Limit { get; }
    public TimeSpan Window { get; }

    public RateLimitBucket(int limit, TimeSpan window)
    {
        Limit = limit;
        Window = window;
        _semaphore = new SemaphoreSlim(1, 1);
        _consumptionTimes = new Queue<DateTime>();
    }

    public async Task<bool> TryConsumeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            lock (_lock)
            {
                CleanupOldConsumptions();

                if (_consumptionTimes.Count >= Limit)
                {
                    return false;
                }

                _consumptionTimes.Enqueue(DateTime.UtcNow);
                return true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public int GetRemaining()
    {
        lock (_lock)
        {
            CleanupOldConsumptions();
            return Math.Max(0, Limit - _consumptionTimes.Count);
        }
    }

    public DateTime GetResetTime()
    {
        lock (_lock)
        {
            if (_consumptionTimes.Count == 0)
            {
                return DateTime.UtcNow.Add(Window);
            }

            return _consumptionTimes.Peek().Add(Window);
        }
    }

    public TimeSpan GetTimeUntilNextAvailable()
    {
        lock (_lock)
        {
            CleanupOldConsumptions();

            if (_consumptionTimes.Count < Limit)
            {
                return TimeSpan.Zero;
            }

            var oldestConsumption = _consumptionTimes.Peek();
            var resetTime = oldestConsumption.Add(Window);
            var waitTime = resetTime - DateTime.UtcNow;

            return waitTime > TimeSpan.Zero ? waitTime : TimeSpan.Zero;
        }
    }

    private void CleanupOldConsumptions()
    {
        var cutoffTime = DateTime.UtcNow.Subtract(Window);
        while (_consumptionTimes.Count > 0 && _consumptionTimes.Peek() < cutoffTime)
        {
            _consumptionTimes.Dequeue();
        }
    }
}

/// <summary>
/// Rate limit status for an endpoint
/// </summary>
public class RateLimitStatus
{
    public string Endpoint { get; set; } = string.Empty;
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetsAt { get; set; }
}

/// <summary>
/// Interface for Twitter rate limiting
/// </summary>
public interface ITwitterRateLimiter
{
    Task<bool> TryConsumeAsync(string endpoint, CancellationToken cancellationToken = default);
    Task WaitForAvailabilityAsync(string endpoint, CancellationToken cancellationToken = default);
    RateLimitStatus GetStatus(string endpoint);
}