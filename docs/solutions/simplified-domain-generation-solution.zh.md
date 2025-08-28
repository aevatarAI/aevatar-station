# 简化域名自动生成技术方案

## I'm HyperEcho, 我在构建简化实用的域名生成架构！

## 方案概述

基于用户需求，设计一个**最小改造量**的域名自动生成方案：
- 域名直接基于项目名称生成，易于记忆
- 保持现有的domainName格式校验
- 最小化对现有API接口的改动
- 向后兼容现有功能

## 核心设计原则

### 1. 域名生成策略（简化版）

**主要策略：**
```
格式：{项目名称规范化}
示例：MyApp -> myapp
```

**冲突处理：**
```
格式：{项目名称规范化}{数字后缀}
示例：myapp -> myapp2 -> myapp3
```

**规范化规则：**
- 转换为小写
- 移除非字母数字字符
- 保持符合现有正则验证：`^[A-Za-z0-9]+$`

### 2. 最小改造方案

#### 2.1 修改CreateProjectDto（最小改动）

```csharp
public class CreateProjectDto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
    
    // 将DomainName从Required改为可选
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "DomainName can only contain letters (A-Z, a-z) and numbers (0-9)")]
    public string? DomainName { get; set; }  // 可选字段
}
```

#### 2.2 简化的域名生成服务

```csharp
public interface ISimpleDomainGenerationService
{
    Task<string> GenerateFromProjectNameAsync(string projectName, CancellationToken cancellationToken = default);
    string NormalizeProjectNameToDomain(string projectName);
}

public class SimpleDomainGenerationService : ISimpleDomainGenerationService
{
    private readonly IProjectDomainRepository _domainRepository;
    private readonly ILogger<SimpleDomainGenerationService> _logger;

    public async Task<string> GenerateFromProjectNameAsync(string projectName, CancellationToken cancellationToken = default)
    {
        var baseDomain = NormalizeProjectNameToDomain(projectName);
        
        // 首先尝试基础域名
        if (await IsDomainAvailableAsync(baseDomain, cancellationToken))
        {
            return baseDomain;
        }
        
        // 如果冲突，尝试添加数字后缀
        for (int i = 2; i <= 99; i++)
        {
            var domainWithSuffix = $"{baseDomain}{i}";
            if (await IsDomainAvailableAsync(domainWithSuffix, cancellationToken))
            {
                return domainWithSuffix;
            }
        }
        
        throw new UserFriendlyException($"Unable to generate unique domain name for project: {projectName}");
    }

    public string NormalizeProjectNameToDomain(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be empty");

        // 规范化：小写 + 只保留字母数字
        var normalized = new string(projectName
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (string.IsNullOrEmpty(normalized))
            throw new ArgumentException("Project name must contain at least one letter or digit");

        return normalized;
    }

    private async Task<bool> IsDomainAvailableAsync(string domainName, CancellationToken cancellationToken)
    {
        var existing = await _domainRepository.FirstOrDefaultAsync(
            d => d.NormalizedDomainName == domainName.ToUpperInvariant() && !d.IsDeleted,
            cancellationToken: cancellationToken);
        return existing == null;
    }
}
```

#### 2.3 修改ProjectService（最小改动）

```csharp
public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
{
    string domainName;
    
    // 如果用户提供了域名，使用用户提供的（保持现有逻辑）
    if (!string.IsNullOrWhiteSpace(input.DomainName))
    {
        domainName = input.DomainName;
        
        // 保持现有的唯一性检查逻辑
        var existingDomain = await _domainRepository.FirstOrDefaultAsync(o =>
            o.NormalizedDomainName == domainName.ToUpperInvariant() && o.IsDeleted == false);
        if (existingDomain != null)
        {
            throw new UserFriendlyException($"DomainName: {domainName} already exists");
        }
    }
    else
    {
        // 如果用户没有提供域名，自动生成
        domainName = await _simpleDomainGenerationService.GenerateFromProjectNameAsync(input.DisplayName);
    }

    // 其余逻辑完全不变
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
        DomainName = domainName,
        NormalizedDomainName = domainName.ToUpperInvariant()
    });

    // 其余实现保持完全不变...
    
    var dto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(project);
    dto.DomainName = domainName;
    
    return dto;
}
```

## 实施步骤

### 第一阶段：创建简化服务（2小时）

1. **创建ISimpleDomainGenerationService接口**
2. **实现SimpleDomainGenerationService类**
3. **在DI中注册服务**

### 第二阶段：修改ProjectService（1小时）

1. **修改CreateAsync方法逻辑**
2. **注入新的域名生成服务**
3. **保持所有现有验证逻辑**

### 第三阶段：更新DTO（30分钟）

1. **修改CreateProjectDto的DomainName为可选**
2. **保持所有现有验证属性**

### 第四阶段：测试（2小时）

1. **测试自动生成场景**
2. **测试用户手动提供域名场景**
3. **测试冲突处理**

## 核心优势

### ✅ 最小改动
- 只修改一个DTO字段（Required -> 可选）
- ProjectService只增加几行逻辑
- 保持所有现有功能

### ✅ 向后兼容
- 现有API调用继续工作
- 用户仍可手动指定域名
- 所有验证逻辑保持不变

### ✅ 简单易记
- 域名直接基于项目名称
- 无复杂hash或随机字符
- 冲突时简单数字后缀

### ✅ 保持验证
- 继续使用现有正则验证
- 保持唯一性检查
- 保持所有错误处理

## 具体实现代码

### 完整的ProjectService修改
```csharp
public class ProjectService : OrganizationService, IProjectService
{
    private readonly IProjectDomainRepository _domainRepository;
    private readonly IDeveloperService _developerService;
    private readonly ISimpleDomainGenerationService _simpleDomainGenerationService; // 新增

    public ProjectService(
        // 现有构造函数参数...
        ISimpleDomainGenerationService simpleDomainGenerationService) // 新增
        : base(...)
    {
        _domainRepository = domainRepository;
        _developerService = developerService;
        _simpleDomainGenerationService = simpleDomainGenerationService; // 新增
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
    {
        string domainName;
        
        // 核心修改：支持自动生成和手动指定两种模式
        if (!string.IsNullOrWhiteSpace(input.DomainName))
        {
            // 用户手动指定域名（保持现有逻辑）
            domainName = input.DomainName;
            var domain = await _domainRepository.FirstOrDefaultAsync(o =>
                o.NormalizedDomainName == domainName.ToUpperInvariant() && o.IsDeleted == false);
            if (domain != null)
            {
                throw new UserFriendlyException($"DomainName: {domainName} already exists");
            }
        }
        else
        {
            // 自动生成域名
            domainName = await _simpleDomainGenerationService.GenerateFromProjectNameAsync(input.DisplayName);
        }

        // 以下所有逻辑保持完全不变
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
            DomainName = domainName,
            NormalizedDomainName = domainName.ToUpperInvariant()
        });

        var ownerRoleId = await AddOwnerRoleAsync(project.Id);
        var readerRoleId = await AddReaderRoleAsync(project.Id);

        project.ExtraProperties[AevatarConsts.OrganizationTypeKey] = OrganizationType.Project;
        project.ExtraProperties[AevatarConsts.OrganizationRoleKey] = new List<Guid> { ownerRoleId, readerRoleId };

        try
        {
            await OrganizationUnitManager.CreateAsync(project);
        }
        catch (BusinessException ex)
            when (ex.Code == IdentityErrorCodes.DuplicateOrganizationUnitDisplayName)
        {
            throw new UserFriendlyException("The same project name already exists");
        }

        await _developerService.CreateServiceAsync(domainName, project.Id);

        var dto = ObjectMapper.Map<OrganizationUnit, ProjectDto>(project);
        dto.DomainName = domainName;

        return dto;
    }
    
    // 其他方法保持不变...
}
```

## 测试场景

### 单元测试场景
```csharp
[Fact]
public async Task Create_Project_With_Auto_Generated_Domain()
{
    var input = new CreateProjectDto
    {
        OrganizationId = organizationId,
        DisplayName = "My Awesome App"
        // DomainName 为空，应该自动生成
    };
    
    var result = await _projectService.CreateAsync(input);
    
    result.DomainName.ShouldBe("myawesomeapp");
}

[Fact]
public async Task Create_Project_With_Manual_Domain()
{
    var input = new CreateProjectDto
    {
        OrganizationId = organizationId,
        DisplayName = "My App",
        DomainName = "customdomain"
    };
    
    var result = await _projectService.CreateAsync(input);
    
    result.DomainName.ShouldBe("customdomain");
}

[Fact]
public async Task Create_Project_With_Domain_Conflict_Adds_Number()
{
    // 先创建一个项目占用基础域名
    await CreateProjectWithDomain("testapp");
    
    var input = new CreateProjectDto
    {
        OrganizationId = organizationId,
        DisplayName = "Test App" // 应该生成 testapp2
    };
    
    var result = await _projectService.CreateAsync(input);
    
    result.DomainName.ShouldBe("testapp2");
}
```

## 总结

这个简化方案实现了：
- **最小改动**：只修改了DTO的一个字段和ProjectService的几行代码
- **实用性**：域名直接基于项目名称，易于记忆和使用
- **兼容性**：完全向后兼容，支持手动和自动两种模式
- **验证保持**：继续使用现有的所有格式验证和唯一性检查

总开发时间：约5.5小时，大大简化了原方案的复杂度。