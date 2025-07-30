using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Service;

/// <summary>
/// 文本补全请求DTO
/// </summary>
public class TextCompletionRequestDto
{
    /// <summary>
    /// 需要补全的输入文本（最少15个字符）
    /// </summary>
    [Required(ErrorMessage = "Input text is required")]
    [MinLength(15, ErrorMessage = "Input text must be at least 15 characters long")]
    public string InputText { get; set; } = string.Empty;
}

/// <summary>
/// 文本补全响应DTO
/// </summary>
public class TextCompletionResponseDto
{
    /// <summary>
    /// 5个补全选项
    /// </summary>
    public List<string> Completions { get; set; } = new();

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    public string? ErrorMessage { get; set; }
} 