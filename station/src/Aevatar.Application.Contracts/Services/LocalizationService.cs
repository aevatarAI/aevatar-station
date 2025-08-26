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
    public string GetLocalizedMessage(string key, GodGPTChatLanguage language)
    {
        return GetTranslation(key, language, "messages");
    }

    /// <summary>
    /// Get localized exception message by exception key and language
    /// </summary>
    public string GetLocalizedException(string exceptionKey, GodGPTChatLanguage language)
    {
        return GetTranslation(exceptionKey, language, "exceptions");
    }

    /// <summary>
    /// Get localized validation message by validation key and language
    /// </summary>
    public string GetLocalizedValidationMessage(string validationKey, GodGPTChatLanguage language)
    {
        return GetTranslation(validationKey, language, "validation");
    }
    
    /// <summary>
    /// Get localized exception message with parameter replacement
    /// </summary>
    public string GetLocalizedException(string exceptionKey, GodGPTChatLanguage language, Dictionary<string, string> parameters)
    {
        var message = GetTranslation(exceptionKey, language, "exceptions");
        return ReplaceParameters(message, parameters);
    }
    
    /// <summary>
    /// Get localized message with parameter replacement
    /// </summary>
    public string GetLocalizedMessage(string key, GodGPTChatLanguage language, Dictionary<string, string> parameters)
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
    private string GetTranslation(string key, GodGPTChatLanguage language, string category)
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
            if (language != GodGPTChatLanguage.English)
            {
                _logger.LogWarning("Translation not found for key: {Key}, language: {Language}, category: {Category}. Falling back to English.", 
                    key, language, category);
                return GetTranslation(key, GodGPTChatLanguage.English, category);
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
    private string GetLanguageCode(GodGPTChatLanguage language)
    {
        return language switch
        {
            GodGPTChatLanguage.English => "en",
            GodGPTChatLanguage.TraditionalChinese => "zh-tw",
            GodGPTChatLanguage.Spanish => "es",
            GodGPTChatLanguage.CN => "zh",
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
                ["en.UnableToLoadConversation"] = "Unable to load conversation {sessionId}",
                ["en.NoActiveGuestSession"] = "No active guest session. Please create a session first.",
                ["en.InsufficientCredits"] = "Insufficient credits.",
                ["en.RateLimitExceeded"] = "Rate limit exceeded.",
                ["en.DailyChatLimitExceeded"] = "Daily chat limit exceeded.",
                ["en.InvalidRequest"] = "Invalid request.",
                ["en.InvalidRequestBody"] = "Invalid request body.",
                ["en.InvalidCaptchaCode"] = "Invalid captcha code.",
                ["en.EmailIsRequired"] = "Email is required.",
                ["en.UserUnRegister"] = "User not registered.",
                ["en.TooManyFiles"] = "Too many files. Maximum {TooManyFiles} images per upload.",
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
                ["en.WebhookValidatingError"] = "Error validating webhook",
                ["en.InvalidShare"] = "Invalid Share string",
                ["en.EmailFrequently"] = "Email sent too frequently. Please try again later.",
                ["en.InvalidUserName"] = "Username can only contain letters or digits..",

                // Security Verification - English (SecurityVerificationRequired is used as frontend signal, always English)
                ["en.SecurityVerificationRequired"] = "Security verification is required. Please complete the verification and try again.",
                ["en.SecurityVerificationFailed"] = "Security verification failed: {reason}",
                ["en.RecaptchaVerificationFailed"] = "reCAPTCHA verification failed. Please try again.",




                // Traditional Chinese translations
                ["zh-tw.Unauthorized"] = "未授權：使用者未通過身份驗證。 ",
                ["zh-tw.UserNotAuthenticated"] = "未授权：用户未通过身份验证。",
                ["zh-tw.UnableToRetrieveUserId"] = "未授權：無法檢索 UserId。  ",
                ["zh-tw.SessionNotFound"] = "会话未找到或访问被拒绝。",
                ["zh-tw.SessionInfoIsNull"] = "会话信息为空。",
                ["zh-tw.UnableToLoadConversation"] = "無法載入對話  {sessionId}",
                ["zh-tw.NoActiveGuestSession"] = "無活躍的訪客會話。請先創建一個會話。 ",
                ["zh-tw.InsufficientCredits"] = "积分不足。",
                ["zh-tw.RateLimitExceeded"] = "超出速率限制。",
                ["zh-tw.DailyChatLimitExceeded"] = "每日聊天次數限制已超出。",
                ["zh-tw.InvalidRequest"] = "无效请求。",
                ["zh-tw.InvalidRequestBody"] = "無效的請求主體。",
                ["zh-tw.InvalidCaptchaCode"] = "無效的驗證碼",
                ["zh-tw.EmailIsRequired"] = "需要提供電子郵件。",
                ["zh-tw.UserUnRegister"] = "使用者未註冊.",
                ["zh-tw.TooManyFiles"] = "檔案過多。每次上傳最多{TooManyFiles}張圖片。",
                ["zh-tw.FileTooLarge"] = "檔案過大，最大限制為 {MaxSizeBytes} 位字节。",
                ["zh-tw.MaximumFileSizeExceeded"] = "超出最大文件大小。",
                ["zh-tw.InternalServerError"] = "內部伺服器錯誤",
                ["zh-tw.ServiceUnavailable"] = "服务暂时不可用。",
                ["zh-tw.OperationFailed"] = "操作失败。",
                ["zh-tw.ChatLimitExceeded"] = "超出聊天限制。",
                ["zh-tw.GuestChatLimitExceeded"] = "超出访客聊天限制。",
                ["zh-tw.UnsetLanguage"] = "未設置語言的請求主體。 ",
                ["zh-tw.VoiceLanguageNotSet"] = "语音语言未设置。",
                ["zh-tw.HasBeenRegistered"] = "電子郵件：｛input.email｝已注册。",
                ["zh-tw.WebhookValidatingError"] = " 驗證 webhook 失敗 ",
                ["zh-tw.InvalidShare"] = "共亯字串無效",
                ["zh-tw.EmailFrequently"] = "電子郵件發送過於頻繁，請稍後再試。",
                ["zh-tw.InvalidUserName"] = "用戶名只能包含字母或數位。",

                // Security Verification - Traditional Chinese
                ["zh-tw.SecurityVerificationRequired"] = "需要進行安全驗證。 請完成驗證並重試。",
                ["zh-tw.SecurityVerificationFailed"] = "安全驗證失敗：{reason}",
                ["zh-tw.RecaptchaVerificationFailed"] = "reCAPTCHA 驗證失敗。請重試。",




                // Spanish translations
                ["es.Unauthorized"] = "No autorizado: El usuario no está autenticado. ",
                ["es.UserNotAuthenticated"] = "No autorizado: El usuario no está autenticado.",
                ["es.UnableToRetrieveUserId"] = "No autorizado: No se pudo recuperar el UserId. ",
                ["es.SessionNotFound"] = "Sesión no encontrada o acceso denegado.",
                ["es.SessionInfoIsNull"] = "La información de la sesión es nula.",
                ["es.UnableToLoadConversation"] = "No se pudo cargar la conversación  {sessionId}",
                ["es.NoActiveGuestSession"] = "No hay sesión de invitado activa. Por favor, crea una sesión primero. ",
                ["es.InsufficientCredits"] = "Créditos insuficientes.",
                ["es.RateLimitExceeded"] = "Límite de velocidad excedido.",
                ["es.DailyChatLimitExceeded"] = "Límite diario de chats excedido. ",
                ["es.InvalidRequest"] = "Solicitud inválida.",
                ["es.InvalidRequestBody"] = "Cuerpo de la solicitud inválido. ",
                ["es.InvalidCaptchaCode"] = "Código de captcha inválido ",
                ["es.EmailIsRequired"] = "Se requiere un correo electrónico.",
                ["es.UserUnRegister"] = "Usuario no registrado.",
                ["es.TooManyFiles"] = "Demasiados archivos. Máximo {TooManyFiles} imágenes por carga.",
                ["es.FileTooLarge"] = "El archivo es demasiado grande, con un máximo de {MaxSizeBytes} bytes.",
                ["es.MaximumFileSizeExceeded"] = "Tamaño máximo de archivo excedido.",
                ["es.InternalServerError"] = "Error interno del servidor",
                ["es.ServiceUnavailable"] = "Servicio temporalmente no disponible.",
                ["es.OperationFailed"] = "Operación fallida.",
                ["es.ChatLimitExceeded"] = "Límite de chat excedido.",
                ["es.GuestChatLimitExceeded"] = "Límite de chat de invitado excedido.",
                ["es.UnsetLanguage"] = "Cuerpo de la solicitud sin idioma establecido. ",
                ["es.VoiceLanguageNotSet"] = "Idioma de voz no establecido.",
                ["es.HasBeenRegistered"] = "El correo electrónico: {input.Email} ha sido registrado.",
                ["es.WebhookValidatingError"] = "Error al validar el webhook ",
                ["es.InvalidShare"] = "Cadena de compartir inválida",
                ["es.EmailFrequently"] = "El correo electrónico se envía con demasiada frecuencia. Por favor, intenta de nuevo más tarde.",
                ["es.InvalidUserName"] = "El nombre de usuario solo puede contener letras o números.",

                // Security Verification - Spanish (SecurityVerificationRequired always uses English as frontend signal)
                ["es.SecurityVerificationRequired"] = "Se requiere verificación de seguridad.", // Not used - frontend signal
                ["es.SecurityVerificationFailed"] = "Falló la verificación de seguridad: {reason}",
                ["es.RecaptchaVerificationFailed"] = "Falló la verificación de reCAPTCHA. Inténtalo de nuevo.",
                
                //zh
                ["zh.Unauthorized"] = "未授权：用户未通过身份验证。",
                ["zh.UserNotAuthenticated"] = "未授权：用户未通过身份验证。",
                ["zh.UnableToRetrieveUserId"] = "未授权：无法获取 UserId。",
                ["zh.SessionNotFound"] = "找不到会话或拒绝访问。",
                ["zh.SessionInfoIsNull"] = "会话信息为空。",
                ["zh.UnableToLoadConversation"] = "无法加载会话 {sessionId}",
                ["zh.NoActiveGuestSession"] = "没有活跃的游客会话。请先创建会话。",
                ["zh.InsufficientCredits"] = "credits 不足。",
                ["zh.RateLimitExceeded"] = "请求频率超限。",
                ["zh.DailyChatLimitExceeded"] = "已超出每日聊天次数限制。",
                ["zh.InvalidRequest"] = "无效的请求体。",
                ["zh.InvalidRequestBody"] = "无效的请求体。",
                ["zh.InvalidCaptchaCode"] = "无效的验证码",
                ["zh.EmailIsRequired"] = "邮箱是必填项",
                ["zh.UserUnRegister"] = "用户未注册",
                ["zh.TooManyFiles"] = "文件过多。每次最多上传 {TooManyFiles} 张图片。",
                ["zh.FileTooLarge"] = "文件太大，最大限制为 {MaxSizeBytes} 字节。",
                ["zh.MaximumFileSizeExceeded"] = "超过最大文件大小。",
                ["zh.InternalServerError"] = "内部服务器错误。",
                ["zh.ServiceUnavailable"] = "服务暂时不可用，请稍后再试。",
                ["zh.OperationFailed"] = "操作失败。",
                ["zh.ChatLimitExceeded"] = "已超出聊天限制。",
                ["zh.GuestChatLimitExceeded"] = "已超出访客聊天限制",
                ["zh.UnsetLanguage"] = "Unset language request body.",
                ["zh.VoiceLanguageNotSet"] = "语言请求体未设置。",
                ["zh.HasBeenRegistered"] = "邮箱: {input.Email} 已注册。",
                ["zh.WebhookValidatingError"] = "验证 webhook 时出错",
                ["zh.InvalidShare"] = "分享的字符串无效。",
                ["zh.EmailFrequently"] = "电子邮件发送过于频繁，请稍后再试。",
                ["zh.InvalidUserName"] = "用户名只能包含字母或数字。",

                // Security Verification - Traditional Chinese
                ["zh.SecurityVerificationRequired"] = "需要进行安全验证。请完成验证并重试。",
                ["zh.SecurityVerificationFailed"] = "安全验证失败：{reason}",
                ["zh.RecaptchaVerificationFailed"] = "reCAPTCHA 验证失败。请重试。"




            },
            
            ["validation"] = new Dictionary<string, string>
            {
                // Add validation messages here if needed
                ["en.Required"] = "This field is required.",
                ["zh-tw.Required"] = "此字段为必填项。",
                ["es.Required"] = "Este campo es requerido.",
                ["zh.Required"] = "此字段为必填项。"

            },
            
            ["messages"] = new Dictionary<string, string>
            {
                // Add general messages here if needed
                ["en.Success"] = "Operation completed successfully.",
                ["zh-tw.Success"] = "操作成功完成。",
                ["es.Success"] = "Operación completada exitosamente.",
                ["en.Success"] = "操作已成功完成。"

            }
        };

        return translations;
    }
} 