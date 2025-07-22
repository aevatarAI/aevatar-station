# Agent Property Schema 默认值设计文档

## 概述

为了提升用户体验，在Agent配置界面提供默认值和预设选项，本文档定义了在PropertyJsonSchema中添加默认值支持的设计方案。

## 设计原理

### 核心思想
- 统一使用 `values` 字段表示默认值列表
- `values[0]` 始终作为默认值
- 单个默认值：`values` 为单元素数组
- 多个选项：`values` 为多元素数组

### 设计优势
1. **统一性**: 所有属性都使用相同的字段结构
2. **简洁性**: 只需要一个字段，减少冗余
3. **扩展性**: 可以轻松从单值扩展到多值
4. **兼容性**: 不影响现有Schema结构

## Schema结构

### 基本结构
```json
{
  "propertyName": {
    "type": "string",
    "values": ["默认值1", "选项2", "选项3"]
  }
}
```

### 完整示例

#### 原始Schema（无默认值）
```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "ChatAIGAgentConfigDto",
  "type": "object",
  "properties": {
    "instructions": {
      "type": "string"
    },
    "systemLLM": {
      "type": "string"
    }
  }
}
```

#### 增强Schema（包含默认值）
```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "ChatAIGAgentConfigDto",
  "type": "object",
  "properties": {
    "memberName": {
      "type": "string",
      "values": [""]
    },
    "correlationId": {
      "type": ["null", "string"],
      "format": "guid",
      "values": [null]
    },
    "instructions": {
      "type": "string",
      "values": [
        "你是一个helpful AI助手",
        "你是专业的代码分析专家",
        "你是友好的客服机器人",
        "你是技术文档写作助手"
      ]
    },
    "systemLLM": {
      "type": "string",
      "values": [
        "gpt-3.5-turbo",
        "gpt-4",
        "claude-3-sonnet",
        "gemini-pro"
      ]
    },
    "enableLogging": {
      "type": "boolean",
      "values": [true]
    },
    "maxRetries": {
      "type": "integer",
      "values": [3, 5, 10, 20]
    },
    "timeoutSeconds": {
      "type": "number",
      "values": [30.0, 60.0, 120.0, 300.0]
    }
  }
}
```

## API接口影响

### 接口路径
`GET /api/agent/agent-type-info-list`

### 返回格式
```json
{
  "agentType": "Aevatar.GAgents.Twitter.GAgents.ChatAIAgent.ChatAIGAgent",
  "fullName": "Aevatar.GAgents.Twitter.GAgents.ChatAIAgent.ChatAIGAgent",
  "description": "Represents an AI chat agent capable of participating in workflows and handling conversations.",
  "agentParams": [
    {
      "name": "Instructions",
      "type": "System.String"
    },
    {
      "name": "SystemLLM", 
      "type": "System.String"
    }
  ],
  "propertyJsonSchema": "{\n  \"$schema\": \"http://json-schema.org/draft-04/schema#\",\n  \"title\": \"ChatAIGAgentConfigDto\",\n  \"type\": \"object\",\n  \"properties\": {\n    \"instructions\": {\n      \"type\": \"string\",\n      \"values\": [\"你是一个helpful AI助手\", \"你是专业的代码分析专家\"]\n    },\n    \"systemLLM\": {\n      \"type\": \"string\",\n      \"values\": [\"gpt-3.5-turbo\", \"gpt-4\"]\n    }\n  }\n}"
}
```

## 前端实现指南

### 解析逻辑
```javascript
function renderPropertyInput(property) {
  const { type, values } = property;
  
  if (!values || values.length === 0) {
    // 无默认值，渲染空输入框
    return renderInput('', type);
  }
  
  if (values.length === 1) {
    // 单个默认值，渲染输入框并预填充
    return renderInput(values[0], type);
  } else {
    // 多个选项，渲染下拉选择框
    return renderSelect(values, values[0], type);
  }
}
```

### 不同类型处理
```javascript
function renderInput(defaultValue, type) {
  switch (type) {
    case 'string':
      return `<input type="text" value="${defaultValue || ''}" />`;
    case 'integer':
    case 'number':
      return `<input type="number" value="${defaultValue || 0}" />`;
    case 'boolean':
      return `<input type="checkbox" ${defaultValue ? 'checked' : ''} />`;
    default:
      return `<input type="text" value="${defaultValue || ''}" />`;
  }
}

function renderSelect(options, defaultValue, type) {
  const optionsHtml = options.map(option => 
    `<option value="${option}" ${option === defaultValue ? 'selected' : ''}>${option}</option>`
  ).join('');
  
  return `<select>${optionsHtml}</select>`;
}
```

## 后端实现方案

### 1. 扩展SchemaProvider

```csharp
public class DefaultValueProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        if (context.Schema.Properties != null)
        {
            var typeProperties = context.Type.GetProperties();
            
            foreach (var property in typeProperties)
            {
                var propertyName = GetJsonPropertyName(property.Name);
                
                if (context.Schema.Properties.TryGetValue(propertyName, out var propertySchema))
                {
                    // 添加values字段
                    var values = GetPropertyValues(property);
                    if (values.Any())
                    {
                        propertySchema.ExtensionData["values"] = JArray.FromObject(values);
                    }
                }
            }
        }
    }
    
    private List<object> GetPropertyValues(PropertyInfo property)
    {
        // 1. 检查自定义特性
        var valuesAttr = property.GetCustomAttribute<DefaultValuesAttribute>();
        if (valuesAttr != null)
        {
            return valuesAttr.Values.ToList();
        }
        
        // 2. 检查DefaultValue特性
        var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultValueAttr != null)
        {
            return new List<object> { defaultValueAttr.Value };
        }
        
        // 3. 尝试从实例获取默认值
        try
        {
            var instance = Activator.CreateInstance(property.DeclaringType);
            var value = property.GetValue(instance);
            if (value != null)
            {
                return new List<object> { value };
            }
        }
        catch { }
        
        return new List<object>();
    }
}
```

### 2. 自定义特性

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class DefaultValuesAttribute : Attribute
{
    public object[] Values { get; }
    
    public DefaultValuesAttribute(params object[] values)
    {
        Values = values ?? new object[0];
    }
}
```

### 3. 配置类示例

```csharp
[GenerateSerializer]
public class ChatAIGAgentConfigDto : ConfigurationBase
{
    [Id(0)]
    [DefaultValues("你是一个helpful AI助手", "你是专业的代码分析专家", "你是友好的客服机器人")]
    public string Instructions { get; set; } = "你是一个helpful AI助手";

    [Id(1)]
    [DefaultValues("gpt-3.5-turbo", "gpt-4", "claude-3-sonnet")]
    public string SystemLLM { get; set; } = "gpt-3.5-turbo";
    
    [Id(2)]
    [DefaultValues(true)]
    public bool EnableLogging { get; set; } = true;
    
    [Id(3)]
    [DefaultValues(3, 5, 10, 20)]
    public int MaxRetries { get; set; } = 3;
}
```

## 数据类型支持

| 类型 | 示例values | 说明 |
|------|------------|------|
| string | `["option1", "option2"]` | 字符串选项 |
| integer | `[1, 5, 10]` | 整数选项 |
| number | `[1.5, 3.0, 5.5]` | 浮点数选项 |
| boolean | `[true]` 或 `[false]` | 布尔值（通常单值） |
| null | `[null]` | 空值 |

## 兼容性考虑

### 向后兼容
- 现有Schema不受影响
- 前端需要容错处理缺少values字段的情况
- 新字段为可选扩展

### 渐进式迁移
1. **阶段1**: 只为新Agent添加values支持
2. **阶段2**: 逐步为现有Agent添加默认值
3. **阶段3**: 全面推广使用

## 测试用例

### 单元测试
```csharp
[Test]
public void Should_Generate_Values_For_Property_With_DefaultValues_Attribute()
{
    // Arrange
    var schema = _schemaProvider.GetTypeSchema(typeof(TestConfigDto));
    
    // Act
    var instructionsProperty = schema.Properties["instructions"];
    
    // Assert
    Assert.IsTrue(instructionsProperty.ExtensionData.ContainsKey("values"));
    var values = instructionsProperty.ExtensionData["values"] as JArray;
    Assert.AreEqual(3, values.Count);
    Assert.AreEqual("option1", values[0].Value<string>());
}
```

### 集成测试
```csharp
[Test]
public async Task GetAllAgents_Should_Return_Schema_With_Values()
{
    // Arrange & Act
    var result = await _agentService.GetAllAgents();
    
    // Assert
    var chatAgent = result.First(x => x.AgentType.Contains("ChatAIGAgent"));
    var schema = JObject.Parse(chatAgent.PropertyJsonSchema);
    var instructionsProperty = schema["properties"]["instructions"];
    
    Assert.IsNotNull(instructionsProperty["values"]);
}
```

## 总结

本设计方案通过统一的 `values` 字段为Agent配置提供默认值支持，既保持了简洁性又确保了扩展性。前端可以根据values数组长度灵活渲染不同的UI组件，为用户提供更好的配置体验。

---

**文档版本**: 1.0  
**创建日期**: 2025-01-27  
**最后更新**: 2025-01-27 