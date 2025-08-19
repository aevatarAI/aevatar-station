using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.Projects;

/// <summary>
/// 简化的域名生成服务接口
/// 基于项目名称直接生成易记的域名
/// </summary>
public interface ISimpleDomainGenerationService
{
    /// <summary>
    /// 基于项目名称生成唯一的域名
    /// </summary>
    /// <param name="projectName">项目名称</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>生成的唯一域名</returns>
    Task<string> GenerateFromProjectNameAsync(string projectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将项目名称规范化为域名格式
    /// 规则：小写 + 只保留字母数字
    /// </summary>
    /// <param name="projectName">项目名称</param>
    /// <returns>规范化的域名</returns>
    string NormalizeProjectNameToDomain(string projectName);
}