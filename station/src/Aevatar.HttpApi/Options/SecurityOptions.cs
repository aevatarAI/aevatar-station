namespace Aevatar.Options;

/// <summary>
/// Security configuration options
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public RecaptchaOptions Recaptcha { get; set; } = new();
    public AppleDeviceCheckOptions AppleDeviceCheck { get; set; } = new();
    public PlayIntegrityOptions PlayIntegrity { get; set; } = new();
    public RateOptions Rate { get; set; } = new();
    public SecuritySwitchOptions Switch { get; set; } = new();
}

/// <summary>
/// reCAPTCHA configuration options
/// </summary>
public class RecaptchaOptions
{
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
public class RateOptions
{
    public int FreeRequestsPerDay { get; set; } = 5;
}

/// <summary>
/// Security feature switches
/// </summary>
public class SecuritySwitchOptions
{
    public bool EnableRecaptcha { get; set; } = false;
    public bool EnableRateLimit { get; set; } = true;
}
