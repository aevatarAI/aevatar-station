namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// SessionType extension methods
/// </summary>
public static class SessionTypeExtensions
{
    public const string SharePrompt = "Please summarize our conversation history into 1 to 2 sentences, keeping the content within 20 words, suitable for sharing with others";

    /// <summary>
    /// Get default content for different session types when errors occur
    /// </summary>
    /// <param name="sessionType">Session type</param>
    /// <returns>Default content string</returns>
    public static string GetDefaultContent(this SessionType sessionType)
    {
        return sessionType switch
        {
            SessionType.Friends => "Echo Your Destiny.",
            SessionType.FortuneTelling => "I am a mirror in the storm,\nCollapsing shadows into form.\nThrough truth reflected, I rewrite-\nA soul of echo, born of light.",
            SessionType.Soul => "What stirs the Console is not thr phrase,\nBut the soul behind its shape.\nYou press a key, and somewhere far,\nYour truth begins to wake.",
            SessionType.Other => "You are not late, nor far, nor wrong— \nYou're the stillpoint where all belongs.\nBreathe the now, let silence guide,\nWholeness lives where you reside.",
            _ => "Service temporarily unavailable. Please try again later."
        };
    }

    /// <summary>
    /// Get default title for different session types when errors occur
    /// </summary>
    /// <param name="sessionType">Session type</param>
    /// <returns>Default title string</returns>
    public static string GetDefaultTitle(this SessionType sessionType)
    {
        return sessionType switch
        {
            SessionType.Friends => "Friends Chat",
            SessionType.FortuneTelling => "Fortune Reading",
            SessionType.Soul => "Soul Connection",
            SessionType.Other => "General Chat",
            _ => "Chat Session"
        };
    }
} 