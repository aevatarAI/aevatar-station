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
    public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
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
    public string ApiUrl { get; set; } = "https://api.devicecheck.apple.com/v1/validate_device_token";
}

/// <summary>
/// Google Play Integrity configuration options
/// </summary>
public class PlayIntegrityOptions
{
    public bool EnableValidation { get; set; } = false;
    public string ProjectId { get; set; } = "";
    public string ServiceAccountKey { get; set; } = "";
    public string ApiUrl { get; set; } = "https://playintegrity.googleapis.com/v1";
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    public int FreeRequestsPerDay { get; set; } = 5;
}

/// <summary>
/// Security feature switches
/// </summary>
public class SecuritySwitchOptions
{
    public bool EnableReCAPTCHA { get; set; } = false;
    public bool EnableRateLimit { get; set; } = true;
}
