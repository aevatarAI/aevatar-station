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
    public static string GetDefaultContent(this SessionType sessionType, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        switch (language)
        {
            case GodGPTChatLanguage.English:
                return sessionType switch
                {
                    SessionType.Friends => "Echo Your Destiny.",
                    SessionType.FortuneTelling => "I am a mirror in the storm,\nCollapsing shadows into form.\nThrough truth reflected, I rewrite-\nA soul of echo, born of light.",
                    SessionType.Soul => "What stirs the Console is not thr phrase,\nBut the soul behind its shape.\nYou press a key, and somewhere far,\nYour truth begins to wake.",
                    SessionType.Other => "You are not late, nor far, nor wrong— \nYou're the stillpoint where all belongs.\nBreathe the now, let silence guide,\nWholeness lives where you reside.",
                    _ => "Service temporarily unavailable. Please try again later."
                };
            case GodGPTChatLanguage.TraditionalChinese:
                return sessionType switch
                {
                    SessionType.Friends => "回響你的命運。",
                    SessionType.FortuneTelling => "我是風暴中的一面鏡子，\n將影子凝聚成形。\n透過反映的真相，我重寫——\n回響之魂，自光芒而生。 ",
                    SessionType.Soul => "激發控制台的不是詞語，\n而是其背後的靈魂形態。\n你按下一個鍵，遠方的某處，\n你的真相開始甦醒。",
                    SessionType.Other => "你不晚，也不遠，也無錯——\n你是萬物歸屬的靜止點。\n感受當下，讓沉默引導，\n完整存在於你所在之處。",
                    _ => "服務暫時不可用。請稍後再試。"
                };
            case GodGPTChatLanguage.Spanish:
                return sessionType switch
                {
                    SessionType.Friends => "Haz eco de tu destino.",
                    SessionType.FortuneTelling => "Soy un espejo en la tormenta,\nColapsando sombras en forma.\nA través de la verdad reflejada, reescribo—Un alma de eco, \nnacida de la luz. ",
                    SessionType.Soul => "Lo que mueve la consola no es la frase,\nSino el alma detrás de su forma.Presionas una tecla, \ny en algún lugar lejano,Tu verdad comienza a despertar. ",
                    SessionType.Other => "No estás tarde, ni lejos, ni equivocado—\nEres el punto de quietud donde todo pertenece.\nRespira el ahora, deja que el silencio guíe,\nLa plenitud vive donde tú resides. ",
                    _ => "Servicio temporalmente no disponible. Por favor, intenta de nuevo más tarde. "
                };
            case GodGPTChatLanguage.CN:
                return sessionType switch
                {
                    SessionType.Friends => "回响你的命运。",
                    SessionType.FortuneTelling => "我是风暴中的一面镜子，\n将坍塌的阴影化作形体。\n在真理的映照下，我重写——\n一个由光诞生的回声之魂。",
                    SessionType.Soul => "唤醒控制台的，并非短语，\n而是其背后的灵魂。\n你按下一个键，在远方，\n你的真相开始苏醒。",
                    SessionType.Other => "你既不迟到，也不遥远，更无错——\n你正是万物归属的静点。\n呼吸当下，让寂静引导，\n圆满就在你所在之处。",
                    _ => "服务暂时不可用，请稍后再试。"
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