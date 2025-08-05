using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Service;

/// <summary>
/// 工作流生成请求DTO
/// </summary>
public class GenerateWorkflowRequestDto
{
    /// <summary>
    /// 用户目标描述（最少15个字符）
    /// </summary>
    [Required(ErrorMessage = "User goal is required")]
    [MinLength(15, ErrorMessage = "User goal must be at least 15 characters long")]
    public string UserGoal { get; set; } = string.Empty;
}

/// <summary>
/// 文本补全请求DTO
/// </summary>
public class TextCompletionRequestDto
{
    /// <summary>
    /// 用户目标描述（最少15个字符）
    /// </summary>
    [Required(ErrorMessage = "User goal is required")]
    [MinLength(15, ErrorMessage = "User goal must be at least 15 characters long")]
    public string UserGoal { get; set; } = string.Empty;
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
} 