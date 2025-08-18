# Real-Time Option Validation Solution Design

## 需求概述

基于用户故事 #4 "Real-time Option Validation"，实现以下核心功能：

1. **即时兼容性检查**：选择不兼容选项时立即显示警告
2. **动态依赖更新**：参数变更时自动更新依赖参数的选项列表  
3. **无效选项验证**：显示清晰错误信息并提供有效替代建议

**估算时间**：14小时
**目标版本**：v0.6

## 系统架构设计

### 核心组件

```
┌─────────────────────────────────────────────────────────┐
│                   Frontend (React/Vue)                  │
├─────────────────────────────────────────────────────────┤
│  NodeParameterInput  │  ValidationDisplay  │ OptionDropdown │
└─────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────┐
│                    HTTP API Layer                       │
├─────────────────────────────────────────────────────────┤
│           RealtimeValidationController                  │
└─────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                      │
├─────────────────────────────────────────────────────────┤
│ IRealTimeValidationService │ IParameterDependencyService │
│ IDynamicOptionService      │ IValidationCacheService     │
└─────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────┐
│                    Domain Layer                         │
├─────────────────────────────────────────────────────────┤
│ ValidationEngine │ DependencyResolver │ OptionProvider  │
└─────────────────────────────────────────────────────────┘
```

## 数据模型设计

### 1. 验证请求模型

```csharp
// Application.Contracts/Validation/RealTimeValidationDto.cs
public class NodeParameterValidationDto
{
    public string NodeId { get; set; }
    public string ParameterName { get; set; }
    public object ParameterValue { get; set; }
    public Dictionary<string, object> AllParameters { get; set; }
    public string NodeType { get; set; }
    public WorkflowContextDto Context { get; set; }
}

public class WorkflowContextDto
{
    public string WorkflowId { get; set; }
    public List<NodeConfigurationDto> AllNodes { get; set; }
    public Dictionary<string, object> GlobalSettings { get; set; }
}
```

### 2. 验证结果模型

```csharp
// Application.Contracts/Validation/ValidationResultDto.cs
public class ValidationResultDto
{
    public bool IsValid { get; set; }
    public ValidationLevel Level { get; set; }
    public string Message { get; set; }
    public List<string> Suggestions { get; set; }
    public Dictionary<string, List<DynamicOptionDto>> UpdatedDependentOptions { get; set; }
    public TimeSpan ValidationDuration { get; set; }
}

public enum ValidationLevel
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public class DynamicOptionDto
{
    public string Value { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public bool IsDeprecated { get; set; }
    public bool IsRecommended { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### 3. 依赖关系模型

```csharp
// Domain/Validation/ParameterDependency.cs
public class ParameterDependency
{
    public string SourceParameter { get; set; }
    public string TargetParameter { get; set; }
    public DependencyType Type { get; set; }
    public string ConditionExpression { get; set; }
    public List<DependencyRule> Rules { get; set; }
}

public enum DependencyType
{
    OptionsFilter,      // 过滤可用选项
    ValueValidation,    // 验证值的有效性
    ConditionalDisplay, // 条件显示/隐藏
    DefaultValue       // 设置默认值
}

public class DependencyRule
{
    public string Condition { get; set; }
    public List<string> AllowedValues { get; set; }
    public List<string> DisallowedValues { get; set; }
    public string ValidationMessage { get; set; }
}
```

## 服务接口设计

### 1. 实时验证服务

```csharp
// Application.Contracts/Services/IRealTimeValidationService.cs
public interface IRealTimeValidationService : IApplicationService
{
    Task<ValidationResultDto> ValidateParameterAsync(NodeParameterValidationDto request);
    Task<ValidationResultDto> ValidateAllParametersAsync(NodeConfigurationDto nodeConfig);
    Task<Dictionary<string, List<DynamicOptionDto>>> GetDependentOptionsAsync(
        string nodeType, string parameterName, object parameterValue);
    Task RegisterValidationRuleAsync(string nodeType, ValidationRuleDefinition rule);
}
```

### 2. 参数依赖服务

```csharp
// Application.Contracts/Services/IParameterDependencyService.cs
public interface IParameterDependencyService : IApplicationService
{
    Task<List<ParameterDependency>> GetDependenciesAsync(string nodeType);
    Task<bool> HasDependenciesAsync(string nodeType, string parameterName);
    Task<List<string>> GetDependentParametersAsync(string nodeType, string sourceParameter);
    Task UpdateDependencyGraphAsync(string nodeType, List<ParameterDependency> dependencies);
}
```

### 3. 动态选项服务

```csharp
// Application.Contracts/Services/IDynamicOptionService.cs
public interface IDynamicOptionService : IApplicationService
{
    Task<List<DynamicOptionDto>> GetOptionsAsync(string nodeType, string parameterName);
    Task<List<DynamicOptionDto>> GetFilteredOptionsAsync(
        string nodeType, string parameterName, Dictionary<string, object> context);
    Task<DynamicOptionDto> GetOptionMetadataAsync(string nodeType, string parameterName, string optionValue);
    Task RefreshOptionsAsync(string nodeType, string parameterName);
}
```

## 实现计划

### 阶段一：基础验证框架 (4小时)

1. **扩展SchemaProvider**
   ```csharp
   // Application/Schema/IValidationSchemaProvider.cs
   public interface IValidationSchemaProvider : ISchemaProvider
   {
       Task<ValidationSchema> GetValidationSchemaAsync(Type nodeType);
       Task<List<ValidationRule>> GetValidationRulesAsync(Type nodeType, string propertyName);
   }
   ```

2. **实现ValidationService核心逻辑**
   ```csharp
   // Application/Services/RealTimeValidationService.cs
   public class RealTimeValidationService : ApplicationService, IRealTimeValidationService
   {
       private readonly IValidationSchemaProvider _schemaProvider;
       private readonly IParameterDependencyService _dependencyService;
       private readonly IDynamicOptionService _optionService;
       private readonly IValidationCacheService _cacheService;

       public async Task<ValidationResultDto> ValidateParameterAsync(NodeParameterValidationDto request)
       {
           // 1. 基础类型验证
           // 2. 业务规则验证  
           // 3. 依赖关系验证
           // 4. 生成验证结果
       }
   }
   ```

### 阶段二：依赖关系管理 (4小时)

1. **实现DependencyResolver**
   ```csharp
   // Domain/Validation/DependencyResolver.cs
   public class DependencyResolver
   {
       public async Task<DependencyGraph> BuildDependencyGraphAsync(string nodeType)
       {
           // 构建参数依赖关系图
       }

       public async Task<List<string>> ResolveDependentParametersAsync(
           DependencyGraph graph, string parameterName)
       {
           // 解析受影响的依赖参数
       }
   }
   ```

2. **实现条件表达式评估器**
   ```csharp
   // Domain/Validation/ExpressionEvaluator.cs
   public class ExpressionEvaluator
   {
       public bool EvaluateCondition(string expression, Dictionary<string, object> context);
       public List<string> GetFilteredOptions(List<string> options, string filterExpression, Dictionary<string, object> context);
   }
   ```

### 阶段三：动态选项更新 (4小时)

1. **实现DynamicOptionService**
   ```csharp
   // Application/Services/DynamicOptionService.cs
   public class DynamicOptionService : ApplicationService, IDynamicOptionService
   {
       public async Task<List<DynamicOptionDto>> GetFilteredOptionsAsync(
           string nodeType, string parameterName, Dictionary<string, object> context)
       {
           // 1. 获取基础选项列表
           // 2. 应用依赖过滤规则
           // 3. 标记弃用选项
           // 4. 添加元数据信息
       }
   }
   ```

2. **集成选项缓存机制**
   ```csharp
   // Application/Services/ValidationCacheService.cs
   public class ValidationCacheService : IValidationCacheService, ISingletonDependency
   {
       private readonly IMemoryCache _memoryCache;
       private readonly IDistributedCache _distributedCache;

       public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
   }
   ```

### 阶段四：API集成和优化 (2小时)

1. **创建HTTP API控制器**
   ```csharp
   // HttpApi/Controllers/RealtimeValidationController.cs
   [ApiController]
   [Route("api/validation")]
   public class RealtimeValidationController : AevatarController
   {
       [HttpPost("validate-parameter")]
       public async Task<ValidationResultDto> ValidateParameterAsync([FromBody] NodeParameterValidationDto request)
       {
           return await _validationService.ValidateParameterAsync(request);
       }

       [HttpPost("get-dependent-options")]
       public async Task<Dictionary<string, List<DynamicOptionDto>>> GetDependentOptionsAsync([FromBody] DependentOptionsRequest request)
       {
           return await _validationService.GetDependentOptionsAsync(request.NodeType, request.ParameterName, request.ParameterValue);
       }
   }
   ```

## 性能优化策略

### 1. 缓存策略

```csharp
// 多层缓存配置
public class ValidationCacheOptions
{
    public TimeSpan ValidationRuleCacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan DependencyGraphCacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan OptionsCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxCacheSize { get; set; } = 1000;
}
```

### 2. 防抖动机制

```typescript
// Frontend防抖动实现
class ValidationDebouncer {
    private timeoutId: number | null = null;
    private readonly delay: number = 300;

    debounce(fn: Function, ...args: any[]) {
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
        }
        this.timeoutId = setTimeout(() => fn(...args), this.delay);
    }
}
```

### 3. 批量验证

```csharp
// 批量验证支持
public async Task<Dictionary<string, ValidationResultDto>> ValidateBatchAsync(
    List<NodeParameterValidationDto> requests)
{
    var tasks = requests.Select(async request => 
        new KeyValuePair<string, ValidationResultDto>(
            $"{request.NodeId}.{request.ParameterName}",
            await ValidateParameterAsync(request)));
    
    var results = await Task.WhenAll(tasks);
    return results.ToDictionary(r => r.Key, r => r.Value);
}
```

## 测试策略

### 1. 单元测试覆盖

```csharp
// Test/Validation/RealTimeValidationServiceTests.cs
public class RealTimeValidationServiceTests : AevatarApplicationTestBase
{
    [Fact]
    public async Task ValidateParameterAsync_WithValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var request = new NodeParameterValidationDto
        {
            NodeId = "test-node",
            ParameterName = "model",
            ParameterValue = "gpt-4o",
            NodeType = "AIAgentNode"
        };

        // Act
        var result = await _validationService.ValidateParameterAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Level.ShouldBe(ValidationLevel.Info);
    }

    [Fact]
    public async Task ValidateParameterAsync_WithIncompatibleOption_ShouldReturnWarning()
    {
        // 测试不兼容选项验证
    }

    [Fact]
    public async Task GetDependentOptionsAsync_WithParameterChange_ShouldUpdateOptions()
    {
        // 测试依赖选项动态更新
    }
}
```

### 2. 集成测试

```csharp
// Test/Integration/ValidationEndToEndTests.cs
public class ValidationEndToEndTests : AevatarWebApplicationFactoryIntegratedTest<Program>
{
    [Fact]
    public async Task ValidateParameter_CompleteFlow_ShouldWork()
    {
        // 端到端验证流程测试
    }
}
```

## 监控和日志

### 1. 性能指标

```csharp
// Application/Metrics/ValidationMetrics.cs
public class ValidationMetrics
{
    private readonly ICounter _validationRequestCounter;
    private readonly IHistogram _validationDurationHistogram;
    private readonly ICounter _validationErrorCounter;

    public void RecordValidationRequest(string nodeType, string parameterName)
    {
        _validationRequestCounter.WithTags("node_type", nodeType, "parameter", parameterName).Add(1);
    }

    public void RecordValidationDuration(TimeSpan duration)
    {
        _validationDurationHistogram.Record(duration.TotalMilliseconds);
    }
}
```

### 2. 详细日志

```csharp
// 验证日志配置
services.Configure<AbpAuditingOptions>(options =>
{
    options.EntityHistorySelectors.Add(
        new NamedTypeSelector("ValidationAudit", type => typeof(ValidationAuditLog).IsAssignableFrom(type))
    );
});
```

## 前端集成示例

### React组件实现

```typescript
// components/NodeParameterInput.tsx
import React, { useState, useCallback } from 'react';
import { useValidation } from '../hooks/useValidation';

interface NodeParameterInputProps {
    nodeId: string;
    parameterName: string;
    nodeType: string;
    value: any;
    onChange: (value: any) => void;
}

export const NodeParameterInput: React.FC<NodeParameterInputProps> = ({
    nodeId, parameterName, nodeType, value, onChange
}) => {
    const { validateParameter, validationResult, isValidating } = useValidation();

    const handleValueChange = useCallback(async (newValue: any) => {
        onChange(newValue);
        
        // 触发实时验证
        await validateParameter({
            nodeId,
            parameterName,
            parameterValue: newValue,
            nodeType
        });
    }, [nodeId, parameterName, nodeType, onChange, validateParameter]);

    return (
        <div className="parameter-input">
            <input 
                value={value}
                onChange={(e) => handleValueChange(e.target.value)}
            />
            {isValidating && <span className="validating">验证中...</span>}
            {validationResult && (
                <ValidationDisplay result={validationResult} />
            )}
        </div>
    );
};
```

## 部署配置

### appsettings配置

```json
{
  "RealTimeValidation": {
    "Enabled": true,
    "ValidationTimeout": "00:00:05",
    "MaxConcurrentValidations": 100,
    "CacheOptions": {
      "ValidationRuleCacheExpiration": "01:00:00",
      "DependencyGraphCacheExpiration": "00:30:00",
      "OptionsCacheExpiration": "00:05:00"
    },
    "PerformanceThresholds": {
      "ValidationWarningThresholdMs": 1000,
      "ValidationErrorThresholdMs": 3000
    }
  }
}
```

## 验收标准验证

### ✅ AC1: 不兼容选项警告
- 当选择不兼容选项时，系统立即显示警告信息
- 警告信息清晰解释兼容性问题

### ✅ AC2: 动态依赖更新  
- 参数变更时，依赖参数选项列表自动更新
- 更新过程对用户透明且响应迅速

### ✅ AC3: 无效选项验证
- 无效或弃用选项显示清晰错误信息
- 提供有效替代选项建议

## 估算时间分解

| 阶段 | 任务 | 时间 |
|------|------|------|
| 阶段一 | 基础验证框架 | 4小时 |
| 阶段二 | 依赖关系管理 | 4小时 |
| 阶段三 | 动态选项更新 | 4小时 |
| 阶段四 | API集成优化 | 2小时 |
| **总计** | | **14小时** |

---

**注意事项**：
1. 所有Git提交信息使用英文
2. 保持framework目录不变
3. 单元测试覆盖率目标80%+
4. 遵循SOLID原则和现有架构模式
5. 实现过程中持续进行性能测试和优化