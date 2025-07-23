using Aevatar.Domain.Shared;

namespace Aevatar.Application.Contracts.Services;

/// <summary>
/// Localization service interface for internationalization support
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Get localized message by key and language
    /// </summary>
    /// <param name="key">Message key</param>
    /// <param name="language">Target language</param>
    /// <returns>Localized message</returns>
    string GetLocalizedMessage(string key, GodGPTLanguage language);
    
    /// <summary>
    /// Get localized exception message by exception key and language
    /// </summary>
    /// <param name="exceptionKey">Exception message key</param>
    /// <param name="language">Target language</param>
    /// <returns>Localized exception message</returns>
    string GetLocalizedException(string exceptionKey, GodGPTLanguage language);
    
    /// <summary>
    /// Get localized validation message by validation key and language
    /// </summary>
    /// <param name="validationKey">Validation message key</param>
    /// <param name="language">Target language</param>
    /// <returns>Localized validation message</returns>
    string GetLocalizedValidationMessage(string validationKey, GodGPTLanguage language);
} 