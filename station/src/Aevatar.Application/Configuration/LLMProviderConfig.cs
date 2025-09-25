using System.Collections.Generic;

namespace Aevatar.Application.Configuration;

/// <summary>
/// LLM Provider configuration model
/// LLM提供商配置模型
/// </summary>
public class LLMProviderConfig
{
    /// <summary>
    /// Unique provider identifier
    /// 唯一提供商标识符
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider type (e.g., "AzureOpenAI", "AWSBedrock", "OpenAI")
    /// 提供商类型（例如："AzureOpenAI", "AWSBedrock", "OpenAI"）
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific settings (endpoint, region, etc.)
    /// 提供商特定设置（端点、区域等）
    /// </summary>
    public Dictionary<string, string> ProviderSettings { get; set; } = new();

    /// <summary>
    /// Whether this provider is enabled
    /// 此提供商是否已启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Provider display name
    /// 提供商显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Provider description
    /// 提供商描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// LLM Model configuration model
/// LLM模型配置模型
/// </summary>
public class LLMModelConfig
{
    /// <summary>
    /// Unique model identifier
    /// 唯一模型标识符
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Model display name
    /// 模型显示名称
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the provider ID
    /// 对提供商ID的引用
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Model-specific parameters (temperature, max_tokens, etc.)
    /// 模型特定参数（temperature、max_tokens等）
    /// </summary>
    public Dictionary<string, object> ModelParameters { get; set; } = new();

    /// <summary>
    /// Whether this model is enabled
    /// 此模型是否已启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Model description
    /// 模型描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Model capabilities (e.g., "chat", "completion", "embedding")
    /// 模型能力（例如："chat", "completion", "embedding"）
    /// </summary>
    public List<string> Capabilities { get; set; } = new();
}
