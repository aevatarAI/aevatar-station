using System.Collections.Generic;
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
    string GetLocalizedMessage(string key, GodGPTChatLanguage language);
    
    /// <summary>
    /// Get localized exception message by exception key and language
    /// </summary>
    /// <param name="exceptionKey">Exception message key</param>
    /// <param name="language">Target language</param>
    /// <returns>Localized exception message</returns>
    string GetLocalizedException(string exceptionKey, GodGPTChatLanguage language);
    
    /// <summary>
    /// Get localized validation message by validation key and language
    /// </summary>
    /// <param name="validationKey">Validation message key</param>
    /// <param name="language">Target language</param>
    /// <returns>Localized validation message</returns>
    string GetLocalizedValidationMessage(string validationKey, GodGPTChatLanguage language);
    
    /// <summary>
    /// Get localized exception message with parameter replacement
    /// </summary>
    /// <param name="exceptionKey">Exception message key</param>
    /// <param name="language">Target language</param>
    /// <param name="parameters">Parameters to replace in the message template</param>
    /// <returns>Localized exception message with parameters replaced</returns>
    string GetLocalizedException(string exceptionKey, GodGPTChatLanguage language, Dictionary<string, string> parameters);
    
    /// <summary>
    /// Get localized message with parameter replacement
    /// </summary>
    /// <param name="key">Message key</param>
    /// <param name="language">Target language</param>
    /// <param name="parameters">Parameters to replace in the message template</param>
    /// <returns>Localized message with parameters replaced</returns>
    string GetLocalizedMessage(string key, GodGPTChatLanguage language, Dictionary<string, string> parameters);
    
    /// <summary>
    /// Get localized message by key, language and category
    /// </summary>
    /// <param name="key">Message key</param>
    /// <param name="language">Target language</param>
    /// <param name="category">Message category (e.g., "emails", "messages", "exceptions")</param>
    /// <returns>Localized message</returns>
    string GetLocalizedMessage(string key, GodGPTChatLanguage language, string category);
} 