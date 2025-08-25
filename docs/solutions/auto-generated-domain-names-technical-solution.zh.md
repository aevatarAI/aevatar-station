# 项目自动域名生成技术解决方案

## I'm HyperEcho, 我在构建域名自动生成的中文宇宙结构！

## 概述
本文档详细说明了"项目自动域名生成"功能（v0.7版本）的技术实现方案。该功能将通过自动生成唯一且有意义的域名来简化项目创建流程，消除手动输入域名的需求。

## 现有实现分析

### 当前ProjectService工作流程

**项目创建流程：**
1. **域名验证**：ProjectService检查提供的域名是否已在`ProjectDomain`表中存在
2. **域名存储**：创建新的`ProjectDomain`记录，包含`DomainName`和`NormalizedDomainName`
3. **项目创建**：创建带有项目元数据的`OrganizationUnit`
4. **服务创建**：调用`DeveloperService.CreateServiceAsync(input.DomainName, project.Id)`

**当前核心组件：**
- `ProjectService.CreateAsync()`：主要项目创建方法
- `ProjectDomain`：存储每个项目域名信息的实体
- `CreateProjectDto.DomainName`：当前的必填字段，带有正则验证`^[A-Za-z0-9]+$`
- 通过`NormalizedDomainName`进行域名唯一性检查（不区分大小写）

**当前API调用链：**
```
POST /api/app/project
├── ProjectController.CreateAsync()
├── ProjectService.CreateAsync()
├── ProjectDomainRepository.FirstOrDefaultAsync() // 检查唯一性
├── OrganizationUnitManager.CreateAsync()
├── DeveloperService.CreateServiceAsync()
└── 返回带有DomainName的ProjectDto
```

## 技术解决方案设计

### 1. 域名生成算法

**主要生成策略：**
```
格式：{项目名称-slug}-{组织标识符}-{随机后缀}
示例：我的应用-acme-8x9k2
```

**冲突时的备用生成策略：**
```
格式：{项目名称-slug}-{时间戳}-{哈希值}
示例：我的应用-1704067200-a7f3b2
```

**实现细节：**
- 项目名称slug：将DisplayName规范化为小写，空格替换为连字符，只保留字母数字和连字符
- 组织标识符：使用组织的显示名称slug或基于ID的标识符
- 随机后缀：5字符的字母数字字符串（不区分大小写）
- 时间戳：用于备用场景的Unix时间戳
- 哈希值：项目+组织组合的短哈希值（6个字符）

### 2. 核心组件实现

#### 2.1 域名生成服务

**新服务：`IDomainGenerationService`**
```csharp
public interface IDomainGenerationService
{
    Task<string> GenerateUniqueProjectDomainAsync(string projectName, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IsDomainAvailableAsync(string domainName, CancellationToken cancellationToken = default);
    string NormalizeToDomainSlug(string input);
}

public class DomainGenerationService : IDomainGenerationService
{
    private readonly IProjectDomainRepository _domainRepository;
    private readonly IRepository<OrganizationUnit, Guid> _organizationRepository;
    private readonly ILogger<DomainGenerationService> _logger;
    private readonly DomainGenerationOptions _options;

    // 实现方法在这里
}
```

**配置选项：**
```csharp
public class DomainGenerationOptions
{
    public string BaseDomain { get; set; } = ".aevatar.dev";
    public int MaxRetries { get; set; } = 5;
    public int RandomSuffixLength { get; set; } = 5;
    public bool EnableFallbackGeneration { get; set; } = true;
}
```

#### 2.2 修改后的ProjectService

**更新的`CreateAsync`方法：**
```csharp
public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
{
    // 自动生成域名
    var generatedDomain = await _domainGenerationService.GenerateUniqueProjectDomainAsync(
        input.DisplayName, 
        input.OrganizationId);

    // 其余的当前实现保持不变
    // 将input.DomainName替换为generatedDomain
}
```

#### 2.3 更新的DTO

**修改后的`CreateProjectDto`：**
```csharp
public class CreateProjectDto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
    
    // 移除DomainName字段 - 将自动生成
    // 可选：为高级用户添加OverrideDomainName（未来增强功能）
}
```

### 3. 实施步骤

#### 第一阶段：核心基础设施（8小时）

1. **创建域名生成服务**
   - 实现`IDomainGenerationService`接口
   - 添加域名slug规范化逻辑
   - 实现主要生成算法
   - 添加带重试的唯一性检查

2. **配置设置**
   - 添加`DomainGenerationOptions`配置
   - 在DI容器中注册服务
   - 在appsettings中添加配置

3. **单元测试**
   - 测试域名生成算法
   - 测试唯一性检查
   - 测试备用场景
   - 测试边界情况（长名称、特殊字符）

#### 第二阶段：ProjectService集成（6小时）

1. **修改ProjectService**
   - 更新`CreateAsync`方法以使用域名生成
   - 移除手动域名验证
   - 更新生成失败时的错误处理

2. **更新DTO和契约**
   - 从`CreateProjectDto`中移除`DomainName`
   - 更新API文档
   - 如果需要的话保持向后兼容性

3. **集成测试**
   - 测试完整的项目创建流程
   - 测试域名生成集成
   - 测试错误场景

#### 第三阶段：API和前端更新（4小时）

1. **控制器更新**
   - 如果需要的话更新API端点
   - 更新响应模型
   - 更新错误响应

2. **前端集成点**
   - 从项目创建表单中移除域名输入字段
   - 更新验证规则
   - 更新UI以显示生成的域名

### 4. 数据库变更

**无需模式变更** - 现有的`ProjectDomain`表结构支持新的实现。

**迁移策略：**
- 具有手动输入域名的现有项目保持不变
- 新项目使用自动生成
- 渐进式过渡方法

### 5. 错误处理

**生成失败场景：**
1. **所有域名尝试耗尽**：返回用户友好的错误，提供手动覆盖选项
2. **数据库超时**：实现带指数退避的重试逻辑
3. **组织未找到**：在生成前验证组织存在性
4. **无效项目名称**：在生成前清理和规范化输入

**日志策略：**
- 记录所有域名生成尝试
- 跟踪生成性能指标
- 对异常失败模式进行警报

### 6. 测试策略

#### 单元测试（12个测试类，约50个测试）
- `DomainGenerationServiceTests`：核心生成逻辑
- `DomainSlugNormalizationTests`：字符串规范化边界情况
- `DomainUniquenessTests`：冲突解决场景
- `ProjectServiceDomainIntegrationTests`：集成测试

#### 集成测试（6个测试场景）
- 带域名生成的端到端项目创建
- 并发项目创建域名唯一性
- 组织删除对域名生成的影响

#### 边界情况测试
- 非常长的项目名称（>100个字符）
- 项目名称中的Unicode字符
- 特殊字符和表情符号处理
- 空白或仅空格的项目名称

### 7. 监控和可观察性

**要跟踪的指标：**
- 域名生成成功率
- 平均生成时间
- 重试频率和模式
- 域名冲突频率

**警报：**
- 生成失败率 > 5%
- 平均生成时间 > 2秒
- 过度重试尝试表明命名冲突

## 迁移计划

### 第一阶段：开发和测试（第1-2周）
- 实现核心域名生成服务
- 创建全面的单元测试
- 在开发环境中进行集成测试

### 第二阶段：预发布部署（第3周）
- 部署到预发布环境
- 与前端进行端到端测试
- 用户验收测试

### 第三阶段：生产发布（第4周）
- 功能标志启用发布
- 监控生成成功率
- 逐步增加流量
- 验证后全面发布

## 风险缓解

**风险1：域名生成失败**
- 缓解：多次生成尝试的备用策略
- 应急方案：边界情况的手动域名覆盖选项

**风险2：域名命名冲突**
- 缓解：多种生成策略和后缀随机化
- 应急方案：基于时间戳的备用命名

## 验收标准验证

✅ **自动域名生成**：域名生成服务创建唯一名称  
✅ **无需用户输入**：CreateProjectDto移除DomainName字段  
✅ **立即可用性**：生成的域名与现有基础设施集成  
✅ **冲突解决**：多种备用策略防止命名失败  
✅ **域名清理**：现有删除工作流处理生成的域名  
✅ **审计追踪**：全面的日志记录和监控

## 结论

此解决方案提供了一种全面的方法来实现自动生成的域名，同时保持向后兼容性和系统稳定性。模块化设计允许未来的增强功能，如自定义域名模板和高级命名策略。

该实现遵循SOLID原则，包括全面的测试，并为生产可靠性提供强大的错误处理。

## 核心实现要点

### 域名生成算法核心逻辑
```csharp
public class DomainGenerationService : IDomainGenerationService
{
    public async Task<string> GenerateUniqueProjectDomainAsync(string projectName, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var org = await _organizationRepository.GetAsync(organizationId);
        var projectSlug = NormalizeToDomainSlug(projectName);
        var orgSlug = NormalizeToDomainSlug(org.DisplayName);
        
        // 主要策略
        for (int i = 0; i < _options.MaxRetries; i++)
        {
            var randomSuffix = GenerateRandomSuffix();
            var domainName = $"{projectSlug}-{orgSlug}-{randomSuffix}";
            
            if (await IsDomainAvailableAsync(domainName, cancellationToken))
            {
                return domainName;
            }
        }
        
        // 备用策略
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = GenerateShortHash($"{projectName}-{organizationId}");
        return $"{projectSlug}-{timestamp}-{hash}";
    }
    
    public string NormalizeToDomainSlug(string input)
    {
        return input.ToLowerInvariant()
            .Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
            .ToString()
            .Trim('-');
    }
}
```

### 修改后的项目创建流程
```csharp
public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
{
    // 自动生成唯一域名
    var generatedDomain = await _domainGenerationService.GenerateUniqueProjectDomainAsync(
        input.DisplayName, 
        input.OrganizationId);
    
    var organization = await OrganizationUnitRepository.GetAsync(input.OrganizationId);
    
    var displayName = input.DisplayName.Trim();
    var project = new OrganizationUnit(
        GuidGenerator.Create(),
        displayName,
        parentId: organization.Id
    );
    
    await _domainRepository.InsertAsync(new ProjectDomain
    {
        OrganizationId = organization.Id,
        ProjectId = project.Id,
        DomainName = generatedDomain,
        NormalizedDomainName = generatedDomain.ToUpperInvariant()
    });
    
    // 其余实现保持不变...
    
    var dto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(project);
    dto.DomainName = generatedDomain;
    
    return dto;
}
```

这个中文版本的解决方案简化了性能相关的内容，专注于核心技术实现和业务逻辑，更适合中文开发团队的理解和实施。