using System;
using System.Linq;
using Newtonsoft.Json.Linq;

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
        try
        {
            if (json?[propertyName] is JArray array && array.Any())
            {
                var result = array.Select(item => item?.ToString() ?? "").ToArray();
                
                // 确保数组大小符合要求
                if (result.Length >= defaultSize)
                {
                    return result.Take(defaultSize).ToArray();
                }
                
                // 填充到指定大小
                var padded = new string[defaultSize];
                Array.Copy(result, padded, result.Length);
                for (int i = result.Length; i < defaultSize; i++)
                {
                    padded[i] = "";
                }
                return padded;
            }
        }
        catch
        {
            // 发生异常时返回默认数组
        }

        // 返回默认空字符串数组
        return Enumerable.Repeat("", defaultSize).ToArray();
    }

    /// <summary>
    /// 标准化用户输入消息
    /// </summary>
    /// <param name="inputText">输入文本</param>
    /// <param name="defaultMessage">默认消息</param>
    /// <returns>标准化后的消息</returns>
    public static string NormalizeUserInput(string inputText, string defaultMessage = "Please provide assistance as instructed.")
    {
        return string.IsNullOrWhiteSpace(inputText) ? defaultMessage : inputText.Trim();
    }
}