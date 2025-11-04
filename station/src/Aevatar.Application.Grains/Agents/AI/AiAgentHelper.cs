using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// AI代理助手类 - 提供通用的AI代理工具方法
/// </summary>
public static class AiAgentHelper
{
    /// <summary>
    /// 清理JSON内容（移除markdown标记等）
    /// </summary>
    /// <param name="jsonContent">原始JSON内容</param>
    /// <param name="fallbackValue">当内容为空时的回退值</param>
    /// <returns>清理后的JSON字符串</returns>
    public static string CleanJsonContent(string jsonContent, string fallbackValue = "")
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return fallbackValue;

        var cleaned = jsonContent.Trim();

        // 移除markdown代码块标记
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }

        return cleaned.Trim();
    }

    /// <summary>
    /// 验证JSON内容是否有效
    /// </summary>
    /// <param name="jsonContent">JSON内容</param>
    /// <returns>是否为有效JSON</returns>
    public static bool IsValidJson(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        try
        {
            var cleaned = CleanJsonContent(jsonContent);
            JToken.Parse(cleaned);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 安全解析JSON对象
    /// </summary>
    /// <param name="jsonContent">JSON内容</param>
    /// <returns>JObject实例，失败时返回null</returns>
    public static JObject? SafeParseJson(string jsonContent)
    {
        try
        {
            var cleaned = CleanJsonContent(jsonContent);
            return JObject.Parse(cleaned);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从JSON对象中安全获取字符串数组
    /// </summary>
    /// <param name="json">JSON对象</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="defaultSize">默认数组大小</param>
    /// <returns>字符串数组</returns>
    public static string[] SafeGetStringArray(JObject? json, string propertyName, int defaultSize = 5)
    {
        // 优化：先获取有效项，再统一填充到指定大小
        var items = (json?[propertyName] as JArray)?.Select(item => item?.ToString() ?? "").Take(defaultSize).ToList() 
                   ?? new List<string>();
        
        // 填充到指定大小，避免重复遍历
        while (items.Count < defaultSize)
        {
            items.Add("");
        }
        
        return items.ToArray();
    }

    /// <summary>
    /// 标准化用户输入消息
    /// </summary>
    /// <param name="inputText">输入文本</param>
    /// <param name="defaultMessage">默认消息</param>
    /// <returns>标准化后的消息</returns>
    public static string NormalizeUserInput(string inputText,
        string defaultMessage = "Please provide assistance as instructed.")
    {
        return string.IsNullOrWhiteSpace(inputText) ? defaultMessage : inputText.Trim();
    }

    /// <summary>
    /// 简化的AI响应处理 - 将AI聊天结果转换为字符串，统一处理空值情况
    /// </summary>
    /// <param name="chatResult">AI聊天结果列表</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="fallbackGenerator">回退内容生成函数</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>处理后的AI响应内容或回退内容</returns>
    public static string ProcessAiChatResult<T>(IList<T> chatResult, ILogger logger,
        Func<string, string> fallbackGenerator, string operationName)
        where T : class
    {
        // 统一处理空结果
        if (chatResult?.Any() != true)
        {
            logger.LogWarning("AI service returned null or empty result for {Operation}", operationName);
            return fallbackGenerator("AI service returned empty result");
        }

        // 通过反射获取Content属性（避免强类型依赖）
        var content = chatResult[0].GetType().GetProperty("Content")?.GetValue(chatResult[0])?.ToString();

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("AI returned empty content for {Operation}", operationName);
            return fallbackGenerator("AI service returned empty content");
        }

        logger.LogDebug("AI {Operation} response received, length: {Length} characters", operationName, content.Length);
        return content;
    }
}