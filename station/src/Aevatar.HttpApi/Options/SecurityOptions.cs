namespace Aevatar.Options;

/// <summary>
/// Security configuration options
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public ReCAPTCHAOptions ReCAPTCHA { get; set; } = new();
    public AppleDeviceCheckOptions AppleDeviceCheck { get; set; } = new();
    public PlayIntegrityOptions PlayIntegrity { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
    public SecuritySwitchOptions Switch { get; set; } = new();
}

/// <summary>
/// reCAPTCHA configuration options
/// </summary>
public class ReCAPTCHAOptions
{
    public string SiteKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Version { get; set; } = "v2";
    public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// Apple DeviceCheck configuration options
/// </summary>
public class AppleDeviceCheckOptions
{
    public bool EnableValidation { get; set; } = false;
    public string TeamId { get; set; } = "";
    public string KeyId { get; set; } = "";
    public string PrivateKey { get; set; } = "";
}

/// <summary>
/// Google Play Integrity configuration options
/// </summary>
public class PlayIntegrityOptions
{
    public bool EnableValidation { get; set; } = false;
    public string ProjectId { get; set; } = "";
    public string ServiceAccountKey { get; set; } = "";
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    public int FreeRequestsPerDay { get; set; } = 5;
    public int WindowHours { get; set; } = 24;
    public string CacheKeyPrefix { get; set; } = "SendRegCode:";
}

/// <summary>
/// Security feature switches
/// </summary>
public class SecuritySwitchOptions
{
    public bool EnableReCAPTCHA { get; set; } = false;
    public bool EnableRateLimit { get; set; } = true;
}
