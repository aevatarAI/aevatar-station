1、我的设计
HTTP API 接口定义
搜索会话接口
GET /api/godgpt/sessions/search?keyword={keyword}
Authorization: Bearer {jwt_token}
查询参数：
- `keyword` (string, required): 搜索关键词

响应格式：
[
  {
    "sessionId": "uuid",
    "title": "会话标题",
    "createAt": "2024-05-27T10:30:00Z",
    "guider": "引导信息",
    "content": "聊天的前60个字符...",
    "isMatch": true
  }
]
前端结果展示
- 使用与普通会话列表相同的UI组件
- 根据 `IsMatch` 字段区分搜索结果和普通列表
- 显示 `Content` 字段作为会话预览
- 关键词高亮由前端实现（简单的文本匹配加粗）
前端对错误处理
- 无搜索结果时显示 "No results"
- 搜索错误时显示友好的错误提示

Controller 层实现
GodGPTController 新增方法
[HttpGet("godgpt/sessions/search")]
public async Task<List<SessionInfoDto>> SearchSessionsAsync([FromQuery] string keyword)
{
    var stopwatch = Stopwatch.StartNew();
    var currentUserId = (Guid)CurrentUser.Id!;
    if (string.IsNullOrWhiteSpace(keyword))
    {
        return new List<SessionInfoDto>();
    }
    var searchResults = await _godGptService.SearchSessionsAsync(currentUserId, keyword);
    _logger.LogDebug("[GodGPTController][SearchSessionsAsync] userId: {0}, keyword: {1}, results: {2}, duration: {3}ms",
        currentUserId, keyword, searchResults.Count, stopwatch.ElapsedMilliseconds);
    return searchResults;
}
Service 层实现
IGodGPTService 接口新增
Task<List<SessionInfoDto>> SearchSessionsAsync(Guid userId, string keyword);

GodGPTService 实现
public async Task<List<SessionInfoDto>> SearchSessionsAsync(Guid userId, string keyword)
{
    var manager = _clusterClient.GetGrain<IChatManagerGAgent>(userId);
    return await manager.SearchSessionsAsync(keyword);
}




2、下游团队给的对接建议
## 4、上游对接标准和注意事项

### 4.1 接口调用规范

#### 4.1.1 方法签名
```csharp
[ReadOnly]
IChatManagerGAgent.SearchSessionsAsync(string keyword, int maxResults = 1000);
```

#### 4.1.2 参数验证要求
- **keyword**: 
  - 不能为 `null` 或空白字符串
  - 建议长度限制：1-200字符
  - 支持中英文、数字、常见符号
- **maxResults**: 
  - 可选参数，默认值1000
  - 有效范围：1-1000
  - 超出范围时使用默认值

#### 4.1.3 返回值处理
- 返回 `List<SessionInfoDto>` 类型
- 空搜索结果返回空列表（非null）
- 所有返回的 `SessionInfoDto` 对象的 `IsMatch` 字段均为 `true`
- `Content` 字段保证非null（可能为空字符串）

### 4.2 错误处理标准

#### 4.2.1 输入验证错误
```csharp
// 空关键词处理
if (string.IsNullOrWhiteSpace(keyword))
{
    return new List<SessionInfoDto>(); // 返回空列表，不抛异常
}

// 超长关键词处理
if (keyword.Length > 200)
{
    // 记录警告日志，返回空列表
    Logger.LogWarning($"Search keyword too long: {keyword.Length} characters");
    return new List<SessionInfoDto>();
}
```

#### 4.2.2 运行时异常处理
- 单个会话处理失败不应影响整体搜索
- 网络异常、数据访问异常应被捕获并记录
- 关键异常记录Warning级别日志
- 确保方法始终返回有效的List对象

### 4.3 性能考虑事项

#### 4.3.1 调用频率限制
- 建议实现防抖机制，避免频繁调用
- 推荐最小调用间隔：300ms
- 考虑实现客户端缓存机制

#### 4.3.2 超时设置
- 建议设置合理的超时时间：5-10秒
- 超时后应优雅降级，返回空结果

#### 4.3.3 资源使用
- 搜索限制在最近1000条会话，避免全量搜索
- 内存使用可控，单次搜索内存消耗 < 10MB
- 支持并发调用，但建议限制并发数

### 4.4 兼容性要求

#### 4.4.1 Orleans框架兼容
- 方法标记 `[ReadOnly]` 属性，确保只读操作
- `SessionInfoDto` 新增字段使用正确的序列化ID
- 支持分布式环境下的调用

#### 4.4.2 向后兼容
- 新增字段有默认值，不影响现有序列化
- 现有 `GetSessionListAsync()` 方法行为不变
- 不影响现有会话管理功能

### 4.5 最佳实践建议

#### 4.5.1 客户端实现建议
```csharp
// 推荐的调用方式
public async Task<List<SessionInfoDto>> SearchSessionsWithValidation(string keyword)
{
    // 1. 输入验证
    if (string.IsNullOrWhiteSpace(keyword))
    {
        return new List<SessionInfoDto>();
    }
    
    // 2. 长度限制
    if (keyword.Length > 200)
    {
        keyword = keyword.Substring(0, 200);
    }
    
    try
    {
        // 3. 调用搜索接口
        var results = await chatManagerGAgent.SearchSessionsAsync(keyword.Trim());
        return results ?? new List<SessionInfoDto>();
    }
    catch (Exception ex)
    {
        // 4. 异常处理
        Logger.LogError(ex, $"Search sessions failed for keyword: {keyword}");
        return new List<SessionInfoDto>();
    }
}
```

#### 4.5.2 UI交互建议
- 实现搜索防抖，用户停止输入300ms后再搜索
- 显示搜索状态（搜索中、无结果、错误状态）
- 高亮显示匹配的关键词
- 提供搜索历史功能

#### 4.5.3 日志记录建议
- Debug级别：记录搜索关键词和结果数量
- Warning级别：记录输入验证失败、单个会话处理失败
- Error级别：记录严重异常和系统错误

### 4.6 测试验证要点

#### 4.6.1 功能测试
- [ ] 空关键词处理
- [ ] 正常关键词搜索
- [ ] 超长关键词处理
- [ ] 特殊字符关键词
- [ ] 中英文混合关键词
- [ ] 大小写不敏感验证

#### 4.6.2 性能测试
- [ ] 1000条会话搜索性能
- [ ] 并发搜索测试
- [ ] 内存使用监控
- [ ] 响应时间测试

#### 4.6.3 异常测试
- [ ] 网络异常处理
- [ ] 数据访问异常
- [ ] 超时处理
- [ ] 并发安全性

---