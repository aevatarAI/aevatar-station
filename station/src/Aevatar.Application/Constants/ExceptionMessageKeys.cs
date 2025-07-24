namespace Aevatar.Application.Constants;

/// <summary>
/// Exception message keys for internationalization
/// </summary>
public static class ExceptionMessageKeys
{
    // Authentication and Authorization
    public const string Unauthorized = "Unauthorized";
    public const string UserNotAuthenticated = "UserNotAuthenticated";
    public const string UnableToRetrieveUserId = "UnableToRetrieveUserId";
    
    // Session Management
    public const string SessionNotFound = "SessionNotFound";
    public const string SessionInfoIsNull = "SessionInfoIsNull";
    public const string UnableToLoadConversation = "UnableToLoadConversation";
    public const string NoActiveGuestSession = "NoActiveGuestSession";
    
    // Payment and Credits
    public const string InsufficientCredits = "InsufficientCredits";
    public const string RateLimitExceeded = "RateLimitExceeded";
    public const string DailyChatLimitExceeded = "DailyChatLimitExceeded";
    
    // Validation
    public const string InvalidRequest = "InvalidRequest";
    public const string InvalidRequestBody = "InvalidRequestBody";
    public const string InvalidCaptchaCode = "InvalidCaptchaCode";
    public const string EmailIsRequired = "EmailIsRequired";
    public const string UserUnRegister = "UserUnRegister";
    public const string TooManyFiles = "TooManyFiles";
    public const string HASREGISTERED = "HasBeenRegistered";

    // File Operations
    public const string FileTooLarge = "FileTooLarge";
    public const string MaximumFileSizeExceeded = "MaximumFileSizeExceeded";
    
    // General Errors
    public const string InternalServerError = "InternalServerError";
    public const string ServiceUnavailable = "ServiceUnavailable";
    public const string OperationFailed = "OperationFailed";
    
    // Guest Chat
    public const string ChatLimitExceeded = "ChatLimitExceeded";
    public const string GuestChatLimitExceeded = "GuestChatLimitExceeded";
    
    // Voice Chat
    public const string UnsetLanguage = "UnsetLanguage";
    public const string VoiceLanguageNotSet = "VoiceLanguageNotSet";
} 