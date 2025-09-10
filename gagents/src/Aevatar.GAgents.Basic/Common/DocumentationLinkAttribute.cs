using System;

namespace Aevatar.GAgents.Basic.Common;

/// <summary>
/// 为配置属性提供官方文档链接的特性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DocumentationLinkAttribute : Attribute
{
    /// <summary>
    /// 官方文档链接URL
    /// </summary>
    public string DocumentationUrl { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="documentationUrl">官方文档链接URL</param>
    public DocumentationLinkAttribute(string documentationUrl)
    {
        if (string.IsNullOrWhiteSpace(documentationUrl))
            throw new ArgumentException("Documentation URL cannot be null or empty", nameof(documentationUrl));
        
        DocumentationUrl = documentationUrl;
    }
}