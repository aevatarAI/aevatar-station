# 前端AJV参数校验集成指南

## 概述

本文档详细介绍如何在前端使用AJV库对Aevatar Station中的GAgent配置参数进行校验。通过现有的JSON Schema生成机制，前端可以获得严格的业务级验证，而无需任何后端代码改造。

## 目录

- [架构原理](#架构原理)
- [基本类型检查 vs 完整验证](#基本类型检查-vs-完整验证)
- [TwitterGAgent实战示例](#twittergagent实战示例)
- [前端集成步骤](#前端集成步骤)
- [验证效果对比](#验证效果对比)
- [最佳实践](#最佳实践)

## 架构原理

### 现有支持情况

Aevatar Station **原生支持AJV**，无需任何改造：

1. **NJsonSchema兼容性** ✅
   - 生成标准JSON Schema格式
   - 完全符合AJV规范
   - 支持Draft-04到Draft-2020-12

2. **现有API接口** ✅
   - `/api/agent/agent-type-info-list` 返回完整schema
   - `PropertyJsonSchema` 字段包含所有验证信息
   - DataAnnotations自动转换为JSON Schema约束

3. **验证流程** ✅
   ```
   前端请求 → AgentService → SchemaProvider → NJsonSchema → 标准JSON Schema → AJV验证
   ```

## 基本类型检查 vs 完整验证

### 原始状态（基本类型检查）

没有DataAnnotations的DTO类生成的schema：

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "type": "object",
  "properties": {
    "name": {
      "type": ["null", "string"]
    },
    "apiKey": {
      "type": ["null", "string"]
    },
    "maxRetries": {
      "type": "integer"
    }
  }
}
```

**特点：**
- ❌ 没有 `required` 字段约束
- ❌ 没有长度限制
- ❌ 没有格式验证
- ❌ 所有字段都允许null值
- ✅ 基本类型检查正常

### 完整验证（添加DataAnnotations）

添加验证属性后的schema：

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "type": "object",
  "required": ["name", "apiKey"],
  "properties": {
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 100
    },
    "apiKey": {
      "type": "string",
      "minLength": 10,
      "maxLength": 50,
      "pattern": "^[A-Za-z0-9]+$"
    },
    "maxRetries": {
      "type": "integer",
      "minimum": 1,
      "maximum": 10,
      "default": 3
    }
  }
}
```

**特点：**
- ✅ 严格的必填字段验证
- ✅ 长度和范围约束
- ✅ 正则表达式格式验证
- ✅ 默认值支持
- ✅ 完整的业务规则校验

### DataAnnotations映射关系

| C# DataAnnotations | JSON Schema | 说明 |
|-------------------|-------------|------|
| `[Required]` | `required: ["field"]` | 必填字段 |
| `[StringLength(max, MinimumLength=min)]` | `minLength: min, maxLength: max` | 字符串长度 |
| `[Range(min, max)]` | `minimum: min, maximum: max` | 数值范围 |
| `[RegularExpression(pattern)]` | `pattern: "regex"` | 正则验证 |
| `[EmailAddress]` | `format: "email"` | 邮箱格式 |
| `[Url]` | `format: "uri"` | URL格式 |

## TwitterGAgent实战示例

### 配置类定义

```csharp
[GenerateSerializer]
public class TwitterConfig : ConfigurationBase
{
    [Id(0)]
    [Required(ErrorMessage = "API Key is required")]
    [StringLength(50, MinimumLength = 10, ErrorMessage = "API Key must be between 10 and 50 characters")]
    public string ApiKey { get; set; }

    [Id(1)]
    [Required(ErrorMessage = "API Secret is required")]
    [StringLength(100, MinimumLength = 20)]
    public string ApiSecret { get; set; }

    [Id(2)]
    [Range(1, 3200, ErrorMessage = "Max tweets must be between 1 and 3200")]
    public int MaxTweets { get; set; } = 100;

    [Id(3)]
    [RegularExpression(@"^@?[A-Za-z0-9_]{1,15}$", ErrorMessage = "Invalid Twitter handle format")]
    public string UserHandle { get; set; }

    [Id(4)]
    [StringLength(500, ErrorMessage = "Search query cannot exceed 500 characters")]
    [RegularExpression(@"^[^<>""'&]*$", ErrorMessage = "Search query contains invalid characters")]
    public string SearchQuery { get; set; }
}
```

### 生成的JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "type": "object",
  "required": ["apiKey", "apiSecret"],
  "properties": {
    "apiKey": {
      "type": "string",
      "minLength": 10,
      "maxLength": 50
    },
    "apiSecret": {
      "type": "string",
      "minLength": 20,
      "maxLength": 100
    },
    "maxTweets": {
      "type": "integer",
      "minimum": 1,
      "maximum": 3200,
      "default": 100
    },
    "userHandle": {
      "type": "string",
      "pattern": "^@?[A-Za-z0-9_]{1,15}$"
    },
    "searchQuery": {
      "type": "string",
      "maxLength": 500,
      "pattern": "^[^<>\"'&]*$"
    }
  }
}
```

## 前端集成步骤

### 1. 安装AJV依赖

```bash
npm install ajv
```

### 2. 创建验证器类

```javascript
import Ajv from 'ajv';

class AgentConfigValidator {
  constructor() {
    this.ajv = new Ajv({
      allErrors: true,      // 获取所有错误
      strict: false,        // 兼容性模式
      removeAdditional: true // 移除额外属性
    });
    this.validators = new Map();
  }

  // 获取指定Agent类型的schema
  async loadAgentSchema(agentType) {
    try {
      const response = await fetch('/api/agent/agent-type-info-list');
      const agents = await response.json();
      
      const agent = agents.find(a => 
        a.agentType === agentType || 
        a.agentType.includes(agentType)
      );
      
      if (!agent || !agent.propertyJsonSchema) {
        throw new Error(`Agent schema not found: ${agentType}`);
      }
      
      const schema = JSON.parse(agent.propertyJsonSchema);
      const validator = this.ajv.compile(schema);
      
      this.validators.set(agentType, {
        schema,
        validator,
        agentInfo: agent
      });
      
      return schema;
    } catch (error) {
      console.error('Failed to load agent schema:', error);
      throw error;
    }
  }

  // 验证配置数据
  validateConfig(agentType, configData) {
    const validatorInfo = this.validators.get(agentType);
    if (!validatorInfo) {
      throw new Error(`Schema not loaded for: ${agentType}`);
    }

    const { validator } = validatorInfo;
    const isValid = validator(configData);
    
    if (!isValid) {
      return {
        valid: false,
        errors: this.formatErrors(validator.errors),
        data: configData
      };
    }
    
    return { 
      valid: true, 
      errors: [], 
      data: configData 
    };
  }

  // 格式化错误信息
  formatErrors(ajvErrors) {
    return ajvErrors.map(error => ({
      field: this.getFieldName(error),
      message: this.getErrorMessage(error),
      value: error.data,
      constraint: error.params
    }));
  }

  getFieldName(error) {
    if (error.instancePath) {
      return error.instancePath.substring(1); // 移除开头的 '/'
    }
    if (error.params?.missingProperty) {
      return error.params.missingProperty;
    }
    return 'unknown';
  }

  getErrorMessage(error) {
    const messages = {
      'required': `${error.params?.missingProperty || 'Field'} is required`,
      'minLength': `Must be at least ${error.params.limit} characters`,
      'maxLength': `Must not exceed ${error.params.limit} characters`,
      'minimum': `Must be at least ${error.params.limit}`,
      'maximum': `Must not exceed ${error.params.limit}`,
      'pattern': 'Invalid format',
      'type': `Must be of type ${error.params.type}`,
      'enum': `Must be one of: ${error.params.allowedValues?.join(', ')}`
    };
    
    return messages[error.keyword] || error.message || 'Invalid value';
  }

  // 获取默认值
  getDefaultValues(agentType) {
    const validatorInfo = this.validators.get(agentType);
    if (!validatorInfo) return {};

    const { schema } = validatorInfo;
    const defaults = {};
    
    if (schema.properties) {
      Object.entries(schema.properties).forEach(([key, prop]) => {
        if (prop.default !== undefined) {
          defaults[key] = prop.default;
        }
      });
    }
    
    return defaults;
  }
}
```

### 3. Twitter Agent专用验证器

```javascript
class TwitterAgentValidator extends AgentConfigValidator {
  constructor() {
    super();
    this.agentType = 'TwitterGAgent';
  }

  async initialize() {
    await this.loadAgentSchema(this.agentType);
    return this;
  }

  validateTwitterConfig(configData) {
    return this.validateConfig(this.agentType, configData);
  }

  // Twitter特定的验证增强
  validateTwitterHandle(handle) {
    if (!handle) return { valid: true };
    
    // 标准化处理
    const normalizedHandle = handle.startsWith('@') ? handle : `@${handle}`;
    
    // Twitter用户名规则
    const isValid = /^@[A-Za-z0-9_]{1,15}$/.test(normalizedHandle);
    
    return {
      valid: isValid,
      normalizedValue: isValid ? normalizedHandle : handle,
      message: isValid ? '' : 'Invalid Twitter handle format'
    };
  }

  // 实时验证
  validateField(fieldName, value) {
    const tempConfig = { [fieldName]: value };
    const result = this.validateTwitterConfig(tempConfig);
    
    const fieldError = result.errors.find(error => error.field === fieldName);
    return {
      valid: !fieldError,
      message: fieldError?.message || ''
    };
  }
}
```

### 4. 表单集成示例

```html
<!DOCTYPE html>
<html>
<head>
    <title>Twitter Agent Configuration</title>
    <style>
        .form-group { margin-bottom: 15px; }
        .error { color: red; font-size: 12px; }
        .valid { border-color: green; }
        .invalid { border-color: red; }
        .required::after { content: " *"; color: red; }
    </style>
</head>
<body>
    <form id="twitter-config-form">
        <h2>Configure Twitter Agent</h2>
        
        <div class="form-group">
            <label class="required">API Key</label>
            <input type="text" name="apiKey" id="apiKey" 
                   placeholder="Enter your Twitter API Key" />
            <div class="error" id="apiKey-error"></div>
        </div>
        
        <div class="form-group">
            <label class="required">API Secret</label>
            <input type="password" name="apiSecret" id="apiSecret" 
                   placeholder="Enter your Twitter API Secret" />
            <div class="error" id="apiSecret-error"></div>
        </div>
        
        <div class="form-group">
            <label>Max Tweets</label>
            <input type="number" name="maxTweets" id="maxTweets" 
                   min="1" max="3200" value="100" />
            <div class="error" id="maxTweets-error"></div>
            <small>Number of tweets to fetch (1-3200)</small>
        </div>
        
        <div class="form-group">
            <label>User Handle</label>
            <input type="text" name="userHandle" id="userHandle" 
                   placeholder="@username or username" />
            <div class="error" id="userHandle-error"></div>
        </div>
        
        <div class="form-group">
            <label>Search Query</label>
            <input type="text" name="searchQuery" id="searchQuery" 
                   placeholder="Enter search terms" />
            <div class="error" id="searchQuery-error"></div>
            <small>Twitter search query (no special characters)</small>
        </div>
        
        <button type="submit" id="submit-btn" disabled>Create Twitter Agent</button>
        <div id="form-errors"></div>
    </form>

    <script type="module">
        // 初始化验证器
        const validator = new TwitterAgentValidator();
        await validator.initialize();

        const form = document.getElementById('twitter-config-form');
        const submitBtn = document.getElementById('submit-btn');
        
        // 实时验证
        form.addEventListener('input', (e) => {
            validateField(e.target);
            updateSubmitButton();
        });

        // 表单提交
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            await submitForm();
        });

        function validateField(input) {
            const { name, value } = input;
            const result = validator.validateField(name, value);
            
            const errorElement = document.getElementById(`${name}-error`);
            
            if (result.valid) {
                input.classList.remove('invalid');
                input.classList.add('valid');
                errorElement.textContent = '';
            } else {
                input.classList.remove('valid');
                input.classList.add('invalid');
                errorElement.textContent = result.message;
            }
        }

        function updateSubmitButton() {
            const formData = new FormData(form);
            const config = Object.fromEntries(formData);
            const result = validator.validateTwitterConfig(config);
            
            submitBtn.disabled = !result.valid;
        }

        async function submitForm() {
            const formData = new FormData(form);
            const config = Object.fromEntries(formData);
            
            // 最终验证
            const result = validator.validateTwitterConfig(config);
            
            if (!result.valid) {
                displayFormErrors(result.errors);
                return;
            }

            try {
                // 提交到后端
                const response = await fetch('/api/agent', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        agentType: 'Aevatar.GAgents.Twitter.TwitterGAgent',
                        name: `Twitter Agent ${Date.now()}`,
                        properties: config
                    })
                });

                if (response.ok) {
                    alert('Twitter Agent created successfully!');
                    form.reset();
                } else {
                    const error = await response.json();
                    alert(`Failed to create agent: ${error.message}`);
                }
            } catch (error) {
                alert(`Error: ${error.message}`);
            }
        }

        function displayFormErrors(errors) {
            const formErrorsElement = document.getElementById('form-errors');
            formErrorsElement.innerHTML = errors.map(error => 
                `<div class="error">• ${error.field}: ${error.message}</div>`
            ).join('');
        }

        // 初始化默认值
        const defaults = validator.getDefaultValues('TwitterGAgent');
        Object.entries(defaults).forEach(([key, value]) => {
            const input = document.getElementById(key);
            if (input && !input.value) {
                input.value = value;
            }
        });
    </script>
</body>
</html>
```

## 验证效果对比

### 测试数据

```javascript
const testData = {
  apiKey: "abc",                    // 太短 ❌
  apiSecret: "",                    // 空值 ❌  
  maxTweets: 5000,                 // 超出范围 ❌
  userHandle: "invalid@user!",      // 格式错误 ❌
  searchQuery: "<script>alert('xss')</script>" // 包含危险字符 ❌
};
```

### 原始状态验证结果

```javascript
// 基本类型检查 - 几乎都通过
{
  valid: true,
  errors: []
}
```

**问题：**
- 无法检测API Key太短
- 不验证必填字段
- 不限制数值范围
- 不验证特殊格式
- 容易产生安全漏洞

### 增强后验证结果

```javascript
// 完整验证 - 发现所有问题
{
  valid: false,
  errors: [
    {
      field: "apiKey",
      message: "Must be at least 10 characters",
      value: "abc",
      constraint: { limit: 10 }
    },
    {
      field: "apiSecret", 
      message: "apiSecret is required",
      value: "",
      constraint: { missingProperty: "apiSecret" }
    },
    {
      field: "maxTweets",
      message: "Must not exceed 3200", 
      value: 5000,
      constraint: { limit: 3200 }
    },
    {
      field: "userHandle",
      message: "Invalid format",
      value: "invalid@user!",
      constraint: { pattern: "^@?[A-Za-z0-9_]{1,15}$" }
    },
    {
      field: "searchQuery",
      message: "Invalid format",
      value: "<script>alert('xss')</script>",
      constraint: { pattern: "^[^<>\"'&]*$" }
    }
  ]
}
```

**优势：**
- ✅ 严格的业务规则验证
- ✅ 防止XSS攻击
- ✅ 符合Twitter API规范
- ✅ 提供详细错误信息
- ✅ 实时用户反馈

## 最佳实践

### 1. 性能优化

```javascript
// 缓存schema避免重复请求
const schemaCache = new Map();

async function getCachedSchema(agentType) {
  if (schemaCache.has(agentType)) {
    return schemaCache.get(agentType);
  }
  
  const schema = await loadAgentSchema(agentType);
  schemaCache.set(agentType, schema);
  return schema;
}

// 防抖验证避免频繁校验
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

const debouncedValidate = debounce(validateField, 300);
```

### 2. 错误处理

```javascript
class ValidationError extends Error {
  constructor(message, field, value) {
    super(message);
    this.name = 'ValidationError';
    this.field = field;
    this.value = value;
  }
}

try {
  const result = validator.validateConfig(agentType, config);
  if (!result.valid) {
    throw new ValidationError(
      'Validation failed', 
      result.errors[0].field, 
      result.errors[0].value
    );
  }
} catch (error) {
  if (error instanceof ValidationError) {
    // 处理验证错误
    console.error(`Validation error in field ${error.field}:`, error.message);
  } else {
    // 处理其他错误
    console.error('Unexpected error:', error);
  }
}
```

### 3. 国际化支持

```javascript
const errorMessages = {
  'en': {
    'required': 'This field is required',
    'minLength': 'Must be at least {limit} characters',
    'maxLength': 'Must not exceed {limit} characters',
    'pattern': 'Invalid format'
  },
  'zh': {
    'required': '此字段为必填项',
    'minLength': '至少需要{limit}个字符',
    'maxLength': '不能超过{limit}个字符',
    'pattern': '格式无效'
  }
};

function getLocalizedMessage(keyword, params, locale = 'en') {
  const template = errorMessages[locale]?.[keyword] || errorMessages['en'][keyword];
  return template.replace(/\{(\w+)\}/g, (match, key) => params[key] || match);
}
```

### 4. 安全考虑

```javascript
// 输入清理
function sanitizeInput(value, type) {
  if (type === 'string') {
    return value.trim().replace(/[<>\"'&]/g, '');
  }
  return value;
}

// CSP策略
const cspPolicy = {
  'default-src': "'self'",
  'script-src': "'self' 'unsafe-inline'",
  'style-src': "'self' 'unsafe-inline'"
};
```

## 结论

通过本文档介绍的方法，开发者可以：

1. **无缝集成AJV验证** - 现有架构完全支持，无需后端改造
2. **获得严格的业务级验证** - 从基本类型检查升级到完整规则校验  
3. **提升用户体验** - 实时反馈，减少无效提交
4. **增强安全性** - 防止XSS攻击和数据注入
5. **保证前后端一致性** - 使用相同的验证规则

**推荐的实施步骤：**

1. 为现有GAgent配置类添加DataAnnotations
2. 前端集成AJV验证器
3. 实现实时验证和错误提示
4. 逐步覆盖所有Agent类型
5. 监控验证效果和用户反馈

这种方案既利用了现有技术栈的优势，又为系统带来了显著的功能和体验提升。

---

*文档版本：v1.0*  
*最后更新：2025年1月*  
*作者：Aevatar Station Team*