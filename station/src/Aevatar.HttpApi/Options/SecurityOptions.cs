namespace Aevatar.Options;

/// <summary>
/// reCAPTCHA configuration options - 2-level configuration
/// </summary>
public class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";
    
    public bool Enabled { get; set; } = false;
    public string SecretKey { get; set; } = "";
    public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
}

/// <summary>
/// Apple DeviceCheck configuration options - 2-level configuration  
/// </summary>
public class AppleDeviceCheckOptions
{
    public const string SectionName = "AppleDeviceCheck";
    
    public bool Enabled { get; set; } = false;
    public string TeamId { get; set; } = "";
    public string KeyId { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string ApiUrl { get; set; } = "https://api.devicecheck.apple.com/v1/validate_device_token";
}

/// <summary>
/// Google Play Integrity configuration options - 2-level configuration
/// </summary>
public class PlayIntegrityOptions
{
    public const string SectionName = "PlayIntegrity";
    
    public bool Enabled { get; set; } = false;
    public string ProjectId { get; set; } = "";
    public string ServiceAccountKey { get; set; } = "";
    public string ApiUrl { get; set; } = "https://playintegrity.googleapis.com/v1";
}

/// <summary>
/// Rate limiting configuration options - 2-level configuration
/// </summary>
public class RateOptions
{
    public const string SectionName = "RateLimit";
    
    public bool Enabled { get; set; } = true;
    public int FreeRequestsPerDay { get; set; } = 5;
}


