using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.Projects;

/// <summary>
/// 简化的域名生成服务实现
/// 基于项目名称直接生成易记的域名，冲突时添加数字后缀
/// </summary>
public class SimpleDomainGenerationService : ApplicationService, ISimpleDomainGenerationService
{
    private readonly IProjectDomainRepository _domainRepository;
    private readonly ILogger<SimpleDomainGenerationService> _logger;

    public SimpleDomainGenerationService(
        IProjectDomainRepository domainRepository,
        ILogger<SimpleDomainGenerationService> logger)
    {
        _domainRepository = domainRepository;
        _logger = logger;
    }

    public async Task<string> GenerateFromProjectNameAsync(string projectName, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope("Generating domain for project: {ProjectName}", projectName);
        
        var baseDomain = NormalizeProjectNameToDomain(projectName);
        _logger.LogInformation("Base domain generated: {BaseDomain}", baseDomain);
        
        // 首先尝试基础域名
        if (await IsDomainAvailableAsync(baseDomain, cancellationToken))
        {
            _logger.LogInformation("Base domain {BaseDomain} is available", baseDomain);
            return baseDomain;
        }
        
        _logger.LogInformation("Base domain {BaseDomain} is not available, trying with suffixes", baseDomain);
        
        // 如果冲突，尝试添加数字后缀
        for (int i = 2; i <= 99; i++)
        {
            var domainWithSuffix = $"{baseDomain}{i}";
            if (await IsDomainAvailableAsync(domainWithSuffix, cancellationToken))
            {
                _logger.LogInformation("Domain {Domain} is available", domainWithSuffix);
                return domainWithSuffix;
            }
        }
        
        _logger.LogError("Unable to generate unique domain name for project: {ProjectName}", projectName);
        throw new UserFriendlyException($"Unable to generate unique domain name for project: {projectName}");
    }

    public string NormalizeProjectNameToDomain(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name cannot be empty", nameof(projectName));
        }

        // 规范化：小写 + 只保留字母数字
        var normalized = new string(projectName
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Project name must contain at least one letter or digit", nameof(projectName));
        }

        _logger.LogDebug("Normalized '{ProjectName}' to '{NormalizedDomain}'", projectName, normalized);
        return normalized;
    }

    /// <summary>
    /// 检查域名是否可用（不存在且未被删除）
    /// </summary>
    private async Task<bool> IsDomainAvailableAsync(string domainName, CancellationToken cancellationToken)
    {
        var existing = await _domainRepository.FirstOrDefaultAsync(
            d => d.NormalizedDomainName == domainName.ToUpperInvariant() && !d.IsDeleted,
            cancellationToken: cancellationToken);
            
        var isAvailable = existing == null;
        _logger.LogDebug("Domain {DomainName} availability: {IsAvailable}", domainName, isAvailable);
        
        return isAvailable;
    }
}