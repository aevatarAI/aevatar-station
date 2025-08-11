namespace Aevatar.Options;

public class FirebaseAnalyticsOptions
{
    public bool EnableAnalytics { get; set; }
    public string FirebaseAppId { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = "https://www.google-analytics.com/mp/collect";
    public int TimeoutSeconds { get; set; } = 10;
}
