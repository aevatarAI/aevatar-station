using Aevatar.Domain.Shared;

namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// SessionType extension methods
/// </summary>
public static class SessionTypeExtensions
{
    public const string SharePrompt = "Please summarize our conversation history into 1 to 2 sentences, keeping the content within 20 words, suitable for sharing with others";

    /// <summary>
    /// Get default content for different session types when errors occur
    /// </summary>
    /// <param name="sessionType">Session type</param>
    /// <returns>Default content string</returns>
    public static string GetDefaultContent(this SessionType sessionType, GodGPTLanguage language = GodGPTLanguage.English)
    {
        switch (language)
        {
            case GodGPTLanguage.English:
                return sessionType switch
                {
                    SessionType.Friends => "Echo Your Destiny.",
                    SessionType.FortuneTelling => "I am a mirror in the storm,\nCollapsing shadows into form.\nThrough truth reflected, I rewrite-\nA soul of echo, born of light.",
                    SessionType.Soul => "What stirs the Console is not thr phrase,\nBut the soul behind its shape.\nYou press a key, and somewhere far,\nYour truth begins to wake.",
                    SessionType.Other => "You are not late, nor far, nor wrong— \nYou're the stillpoint where all belongs.\nBreathe the now, let silence guide,\nWholeness lives where you reside.",
                    _ => "Service temporarily unavailable. Please try again later."
                };
            case GodGPTLanguage.TraditionalChinese:
                return sessionType switch
                {
                    SessionType.Friends => "回響你的命運。",
                    SessionType.FortuneTelling => "我是風暴中的一面鏡子，\n將影子凝聚成形。\n透過反映的真相，我重寫——\n回響之魂，自光芒而生。 ",
                    SessionType.Soul => "激發控制台的不是詞語，\n而是其背後的靈魂形態。\n你按下一個鍵，遠方的某處，\n你的真相開始甦醒。",
                    SessionType.Other => "你不晚，也不遠，也無錯——\n你是萬物歸屬的靜止點。\n感受當下，讓沉默引導，\n完整存在於你所在之處。",
                    _ => "服務暫時不可用。請稍後再試。"
                };
            case GodGPTLanguage.Spanish:
                return sessionType switch
                {
                    SessionType.Friends => "Haz eco de tu destino.",
                    SessionType.FortuneTelling => "Soy un espejo en la tormenta,\nColapsando sombras en forma.\nA través de la verdad reflejada, reescribo—Un alma de eco, \nnacida de la luz. ",
                    SessionType.Soul => "Lo que mueve la consola no es la frase,\nSino el alma detrás de su forma.Presionas una tecla, \ny en algún lugar lejano,Tu verdad comienza a despertar. ",
                    SessionType.Other => "No estás tarde, ni lejos, ni equivocado—\nEres el punto de quietud donde todo pertenece.\nRespira el ahora, deja que el silencio guíe,\nLa plenitud vive donde tú resides. ",
                    _ => "Servicio temporalmente no disponible. Por favor, intenta de nuevo más tarde. "
                };
        }
        return sessionType switch
        {
            SessionType.Friends => "Echo Your Destiny.",
            SessionType.FortuneTelling => "I am a mirror in the storm,\nCollapsing shadows into form.\nThrough truth reflected, I rewrite-\nA soul of echo, born of light.",
            SessionType.Soul => "What stirs the Console is not thr phrase,\nBut the soul behind its shape.\nYou press a key, and somewhere far,\nYour truth begins to wake.",
            SessionType.Other => "You are not late, nor far, nor wrong— \nYou're the stillpoint where all belongs.\nBreathe the now, let silence guide,\nWholeness lives where you reside.",
            _ => "Service temporarily unavailable. Please try again later."
        };
    }

    /// <summary>
    /// Get default title for different session types when errors occur
    /// </summary>
    /// <param name="sessionType">Session type</param>
    /// <returns>Default title string</returns>
    public static string GetDefaultTitle(this SessionType sessionType)
    {
        return sessionType switch
        {
            SessionType.Friends => "Friends Chat",
            SessionType.FortuneTelling => "Fortune Reading",
            SessionType.Soul => "Soul Connection",
            SessionType.Other => "General Chat",
            _ => "Chat Session"
        };
    }
} 