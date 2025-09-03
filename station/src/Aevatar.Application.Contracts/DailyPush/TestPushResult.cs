using System;

namespace Aevatar.Application.Contracts.DailyPush;

/// <summary>
/// Result of test push notification operation
/// </summary>
public class TestPushResult
{
    /// <summary>
    /// Target timezone that was tested
    /// </summary>
    public string TimeZoneId { get; set; } = "";

    /// <summary>
    /// Total number of users found in the timezone
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Total number of devices that received the push
    /// </summary>
    public int TotalDevices { get; set; }

    /// <summary>
    /// Number of successful push notifications sent
    /// </summary>
    public int SuccessfulPushes { get; set; }

    /// <summary>
    /// Number of failed push notifications
    /// </summary>
    public int FailedPushes { get; set; }

    /// <summary>
    /// Number of users that had no enabled devices
    /// </summary>
    public int UsersWithNoDevices { get; set; }

    /// <summary>
    /// Time when the test push was executed
    /// </summary>
    public DateTime ExecutionTime { get; set; }

    /// <summary>
    /// Total execution duration in milliseconds
    /// </summary>
    public long ExecutionDurationMs { get; set; }

    /// <summary>
    /// Whether the operation completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Custom title that was sent
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Custom content that was sent
    /// </summary>
    public string Content { get; set; } = "";
}
