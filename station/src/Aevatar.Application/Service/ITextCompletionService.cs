using System.Threading.Tasks;

namespace Aevatar.Service;

/// <summary>
/// 文本补全服务接口
/// </summary>
public interface ITextCompletionService
{
    /// <summary>
    /// 生成文本补全
    /// </summary>
    /// <param name="request">补全请求</param>
    /// <returns>补全结果</returns>
    Task<TextCompletionResponseDto> GenerateCompletionsAsync(TextCompletionRequestDto request);
} 