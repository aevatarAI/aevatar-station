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
/// Rate limiting configuration options - 2-level configuration
/// </summary>
public class RateOptions
{
    public const string SectionName = "RateLimit";
    
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Number of free requests allowed per 10-minute window before requiring verification.
    /// For example: if set to 5, requests 1-5 are free, request 6+ require verification.
    /// </summary>
    public int FreeRequestsPerDay { get; set; } = 5;
}


