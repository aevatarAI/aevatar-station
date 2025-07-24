using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Aevatar.Domain.Shared;
namespace Aevatar.Application.Contracts.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _translations;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;
        _translations = LoadTranslations();
    }

    /// <summary>
    /// Get localized message by key and language
    /// </summary>
    public string GetLocalizedMessage(string key, GodGPTLanguage language)
    {
        return GetTranslation(key, language, "messages");
    }

    /// <summary>
    /// Get localized exception message by exception key and language
    /// </summary>
    public string GetLocalizedException(string exceptionKey, GodGPTLanguage language)
    {
        return GetTranslation(exceptionKey, language, "exceptions");
    }

    /// <summary>
    /// Get localized validation message by validation key and language
    /// </summary>
    public string GetLocalizedValidationMessage(string validationKey, GodGPTLanguage language)
    {
        return GetTranslation(validationKey, language, "validation");
    }
    
    /// <summary>
    /// Get localized exception message with parameter replacement
    /// </summary>
    public string GetLocalizedException(string exceptionKey, GodGPTLanguage language, Dictionary<string, string> parameters)
    {
        var message = GetTranslation(exceptionKey, language, "exceptions");
        return ReplaceParameters(message, parameters);
    }
    
    /// <summary>
    /// Get localized message with parameter replacement
    /// </summary>
    public string GetLocalizedMessage(string key, GodGPTLanguage language, Dictionary<string, string> parameters)
    {
        var message = GetTranslation(key, language, "messages");
        return ReplaceParameters(message, parameters);
    }
    
    /// <summary>
    /// Replace parameters in message template using {parameterName} format
    /// </summary>
    /// <param name="message">Message template</param>
    /// <param name="parameters">Parameters to replace</param>
    /// <returns>Message with parameters replaced</returns>
    private string ReplaceParameters(string message, Dictionary<string, string> parameters)
    {
        if (parameters == null || !parameters.Any())
            return message;
            
        var result = message;
        foreach (var parameter in parameters)
        {
            result = result.Replace($"{{{parameter.Key}}}", parameter.Value);
        }
        return result;
    }

    /// <summary>
    /// Get translation for specific key, language and category
    /// </summary>
    private string GetTranslation(string key, GodGPTLanguage language, string category)
    {
        try
        {
            var languageCode = GetLanguageCode(language);
            
            if (_translations.TryGetValue(category, out var categoryTranslations) &&
                categoryTranslations.TryGetValue($"{languageCode}.{key}", out var translation))
            {
                return translation;
            }

            // Fallback to English if translation not found
            if (language != GodGPTLanguage.English)
            {
                _logger.LogWarning("Translation not found for key: {Key}, language: {Language}, category: {Category}. Falling back to English.", 
                    key, language, category);
                return GetTranslation(key, GodGPTLanguage.English, category);
            }

            // Final fallback to key itself
            _logger.LogError("Translation not found for key: {Key}, language: {Language}, category: {Category}. Using key as fallback.", 
                key, language, category);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting translation for key: {Key}, language: {Language}, category: {Category}", 
                key, language, category);
            return key;
        }
    }

    /// <summary>
    /// Get language code for the enum
    /// </summary>
    private string GetLanguageCode(GodGPTLanguage language)
    {
        return language switch
        {
            GodGPTLanguage.English => "en",
            GodGPTLanguage.TraditionalChinese => "zh-tw",
            GodGPTLanguage.Spanish => "es",
            _ => "en"
        };
    }

    /// <summary>
    /// Load translations from embedded resources or configuration
    /// </summary>
    private Dictionary<string, Dictionary<string, string>> LoadTranslations()
    {
        var translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["exceptions"] = new Dictionary<string, string>
            {
                // English translations
                ["en.Unauthorized"] = "Unauthorized: User is not authenticated.",
                ["en.UserNotAuthenticated"] = "Unauthorized: User is not authenticated.",
                ["en.UnableToRetrieveUserId"] = "Unauthorized: Unable to retrieve UserId.",
                ["en.SessionNotFound"] = "Session not found or access denied.",
                ["en.SessionInfoIsNull"] = "Session information is null.",
                ["en.UnableToLoadConversation"] = "Unable to load conversation {0}",
                ["en.NoActiveGuestSession"] = "No active guest session. Please create a session first.",
                ["en.InsufficientCredits"] = "Insufficient credits.",
                ["en.RateLimitExceeded"] = "Rate limit exceeded.",
                ["en.DailyChatLimitExceeded"] = "Daily chat limit exceeded.",
                ["en.InvalidRequest"] = "Invalid request.",
                ["en.InvalidRequestBody"] = "Invalid request body.",
                ["en.InvalidCaptchaCode"] = "Invalid captcha code.",
                ["en.EmailIsRequired"] = "Email is required.",
                ["en.TooManyFiles"] = "Too many files. Maximum {0} images per upload.",
                ["en.FileTooLarge"] = "The file is too large,with a maximum of {MaxSizeBytes} bytes.",
                ["en.MaximumFileSizeExceeded"] = "Maximum file size exceeded.",
                ["en.InternalServerError"] = "Internal server error.",
                ["en.ServiceUnavailable"] = "Service temporarily unavailable.",
                ["en.OperationFailed"] = "Operation failed.",
                ["en.ChatLimitExceeded"] = "Chat limit exceeded.",
                ["en.GuestChatLimitExceeded"] = "Guest chat limit exceeded.",
                ["en.UnsetLanguage"] = "Unset language request body.",
                ["en.VoiceLanguageNotSet"] = "Voice language not set.",
                ["en.HasBeenRegistered"] = "The email: {input.Email} has been registered.",

                // Traditional Chinese translations
                ["zh-tw.Unauthorized"] = "未授权：用户未通过身份验证。",
                ["zh-tw.UserNotAuthenticated"] = "未授权：用户未通过身份验证。",
                ["zh-tw.UnableToRetrieveUserId"] = "未授权：无法获取用户ID。",
                ["zh-tw.SessionNotFound"] = "会话未找到或访问被拒绝。",
                ["zh-tw.SessionInfoIsNull"] = "会话信息为空。",
                ["zh-tw.UnableToLoadConversation"] = "无法加载对话 {0}",
                ["zh-tw.NoActiveGuestSession"] = "没有活跃的访客会话。请先创建会话。",
                ["zh-tw.InsufficientCredits"] = "积分不足。",
                ["zh-tw.RateLimitExceeded"] = "超出速率限制。",
                ["zh-tw.DailyChatLimitExceeded"] = "超出每日聊天限制。",
                ["zh-tw.InvalidRequest"] = "无效请求。",
                ["zh-tw.InvalidRequestBody"] = "无效的请求体。",
                ["zh-tw.InvalidCaptchaCode"] = "無效的驗證碼",
                ["zh-tw.EmailIsRequired"] = "邮箱是必需的。",
                ["zh-tw.TooManyFiles"] = "文件过多。最多 {0} 张图片。",
                ["zh-tw.FileTooLarge"] = "檔案過大，最大限制為 {MaxSizeBytes} 位字节。",
                ["zh-tw.MaximumFileSizeExceeded"] = "超出最大文件大小。",
                ["zh-tw.InternalServerError"] = "内部服务器错误。",
                ["zh-tw.ServiceUnavailable"] = "服务暂时不可用。",
                ["zh-tw.OperationFailed"] = "操作失败。",
                ["zh-tw.ChatLimitExceeded"] = "超出聊天限制。",
                ["zh-tw.GuestChatLimitExceeded"] = "超出访客聊天限制。",
                ["zh-tw.UnsetLanguage"] = "未设置语言请求体。",
                ["zh-tw.VoiceLanguageNotSet"] = "语音语言未设置。",
                ["zh-tw.HasBeenRegistered"] = "電子郵件：｛input.email｝已注册。",

                // Spanish translations
                ["es.Unauthorized"] = "No autorizado: El usuario no está autenticado.",
                ["es.UserNotAuthenticated"] = "No autorizado: El usuario no está autenticado.",
                ["es.UnableToRetrieveUserId"] = "No autorizado: No se puede recuperar el ID del usuario.",
                ["es.SessionNotFound"] = "Sesión no encontrada o acceso denegado.",
                ["es.SessionInfoIsNull"] = "La información de la sesión es nula.",
                ["es.UnableToLoadConversation"] = "No se puede cargar la conversación {0}",
                ["es.NoActiveGuestSession"] = "No hay sesión de invitado activa. Por favor, cree una sesión primero.",
                ["es.InsufficientCredits"] = "Créditos insuficientes.",
                ["es.RateLimitExceeded"] = "Límite de velocidad excedido.",
                ["es.DailyChatLimitExceeded"] = "Límite diario de chat excedido.",
                ["es.InvalidRequest"] = "Solicitud inválida.",
                ["es.InvalidRequestBody"] = "Cuerpo de solicitud inválido.",
                ["es.InvalidCaptchaCode"] = "Código de captcha inválido ",
                ["es.EmailIsRequired"] = "El correo electrónico es requerido.",
                ["es.TooManyFiles"] = "Demasiados archivos. Máximo {0} imágenes por carga.",
                ["es.FileTooLarge"] = "El archivo es demasiado grande, con un máximo de {MaxSizeBytes} bytes.",
                ["es.MaximumFileSizeExceeded"] = "Tamaño máximo de archivo excedido.",
                ["es.InternalServerError"] = "Error interno del servidor.",
                ["es.ServiceUnavailable"] = "Servicio temporalmente no disponible.",
                ["es.OperationFailed"] = "Operación fallida.",
                ["es.ChatLimitExceeded"] = "Límite de chat excedido.",
                ["es.GuestChatLimitExceeded"] = "Límite de chat de invitado excedido.",
                ["es.UnsetLanguage"] = "Cuerpo de solicitud de idioma no establecido.",
                ["es.VoiceLanguageNotSet"] = "Idioma de voz no establecido.",
                ["es.HasBeenRegistered"] = "El correo electrónico: {input.Email} ha sido registrado."

            },
            
            ["validation"] = new Dictionary<string, string>
            {
                // Add validation messages here if needed
                ["en.Required"] = "This field is required.",
                ["zh-tw.Required"] = "此字段为必填项。",
                ["es.Required"] = "Este campo es requerido."
            },
            
            ["messages"] = new Dictionary<string, string>
            {
                // Add general messages here if needed
                ["en.Success"] = "Operation completed successfully.",
                ["zh-tw.Success"] = "操作成功完成。",
                ["es.Success"] = "Operación completada exitosamente."
            }
        };

        return translations;
    }
} 