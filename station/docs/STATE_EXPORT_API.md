# State Export API 开发文档

## 概述

在 `Aevatar.HttpApi.Admin` 项目中添加异步导出 API，用于将 GAgent State 数据导出为 JSON 格式，供迁移工具使用。

---

## 1. 新建文件清单

```
station/src/Aevatar.HttpApi.Admin/
├── Controllers/
│   └── StateExportController.cs   # 新增
├── Models/
│   └── ExportModels.cs            # 新增
└── Services/
    ├── IStateExportService.cs     # 新增
    └── StateExportService.cs      # 新增
```

---

## 2. 实现代码

### 2.1 Models/ExportModels.cs

```csharp
using System;
using System.Collections.Generic;

namespace Aevatar.Admin.Models;

/// <summary>
/// 导出任务状态
/// </summary>
public class ExportTask
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public string? Error { get; set; }
    public List<ExportedRecord>? Data { get; set; }
}

/// <summary>
/// 导出的单条记录
/// </summary>
public class ExportedRecord
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? State { get; set; }
}

/// <summary>
/// 启动导出请求
/// </summary>
public class StartExportRequest
{
    /// <summary>
    /// 要导出的 State 类型列表，为空则导出全部
    /// </summary>
    public List<string>? Types { get; set; }
}

/// <summary>
/// 启动导出响应
/// </summary>
public class StartExportResponse
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 查询状态响应
/// </summary>
public class ExportStatusResponse
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public string? Error { get; set; }
}
```

### 2.2 Services/IStateExportService.cs

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Admin.Models;

namespace Aevatar.Admin.Services;

public interface IStateExportService
{
    /// <summary>
    /// 启动异步导出任务
    /// </summary>
    Task<string> StartExportAsync(List<string>? types);
    
    /// <summary>
    /// 获取导出任务状态
    /// </summary>
    ExportTask? GetTaskStatus(string taskId);
    
    /// <summary>
    /// 获取导出数据并清理任务
    /// </summary>
    List<ExportedRecord>? GetAndRemoveTaskData(string taskId);
}
```

### 2.3 Services/StateExportService.cs

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Admin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Serialization;
using Orleans.Storage;

namespace Aevatar.Admin.Services;

public class StateExportService : IStateExportService
{
    private readonly ILogger<StateExportService> _logger;
    private readonly IMongoDatabase _database;
    private readonly IGrainStateSerializer _grainStateSerializer;
    
    // 内存存储导出任务（生产环境可改用 Redis）
    private static readonly ConcurrentDictionary<string, ExportTask> _tasks = new();
    
    // 需要导出的业务 State 类型（排除 Orleans 内部类型）
    private static readonly HashSet<string> BusinessStateTypes = new()
    {
        "UserStatistics",
        "GodChat", 
        "UserQuota",
        "UserBilling",
        "Invitation",
        "InviteCode",
        "Lumen",
        "UserInfo",
        "GoogleAuth",
        "Awakening",
        "DailyPush",
        "AnonymousUser",
        "UserFeedback",
        "FreeTrialCode"
    };

    public StateExportService(
        ILogger<StateExportService> logger,
        IConfiguration configuration,
        IGrainStateSerializer grainStateSerializer)
    {
        _logger = logger;
        _grainStateSerializer = grainStateSerializer;
        
        // 从配置读取 MongoDB 连接
        var connectionString = configuration["OrleansEventSourcing:Mongodb:Connection"];
        var databaseName = configuration["OrleansEventSourcing:Mongodb:Database"];
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public async Task<string> StartExportAsync(List<string>? types)
    {
        var taskId = Guid.NewGuid().ToString("N")[..8];
        var task = new ExportTask
        {
            TaskId = taskId,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            Data = new List<ExportedRecord>()
        };
        
        _tasks[taskId] = task;
        
        // 后台执行导出
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteExportAsync(task, types);
                task.Status = "completed";
                task.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Export task {TaskId} completed. Total: {Count} records", 
                    taskId, task.TotalCount);
            }
            catch (Exception ex)
            {
                task.Status = "failed";
                task.Error = ex.Message;
                task.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Export task {TaskId} failed", taskId);
            }
        });
        
        return taskId;
    }

    public ExportTask? GetTaskStatus(string taskId)
    {
        return _tasks.TryGetValue(taskId, out var task) ? task : null;
    }

    public List<ExportedRecord>? GetAndRemoveTaskData(string taskId)
    {
        if (_tasks.TryRemove(taskId, out var task) && task.Status == "completed")
        {
            return task.Data;
        }
        return null;
    }

    private async Task ExecuteExportAsync(ExportTask task, List<string>? requestedTypes)
    {
        // 获取所有 Stream* 集合
        var collectionNames = await _database.ListCollectionNamesAsync();
        var streamCollections = await collectionNames.ToListAsync();
        streamCollections = streamCollections
            .Where(c => c.StartsWith("Streamgodgpt"))
            .ToList();
        
        // 过滤要导出的类型
        var typesToExport = requestedTypes?.Count > 0 
            ? requestedTypes 
            : BusinessStateTypes.ToList();
        
        foreach (var collectionName in streamCollections)
        {
            // 检查是否需要导出此类型
            var matchesType = typesToExport.Any(t => 
                collectionName.Contains(t, StringComparison.OrdinalIgnoreCase));
            
            if (!matchesType) continue;
            
            await ExportCollectionAsync(task, collectionName);
        }
        
        task.TotalCount = task.Data!.Count;
    }

    private async Task ExportCollectionAsync(ExportTask task, string collectionName)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var typeName = ExtractTypeName(collectionName);
        
        _logger.LogInformation("Exporting collection: {CollectionName}", collectionName);
        
        using var cursor = await collection.Find(_ => true).ToCursorAsync();
        
        while (await cursor.MoveNextAsync())
        {
            foreach (var doc in cursor.Current)
            {
                try
                {
                    var record = DeserializeDocument(doc, typeName);
                    if (record != null)
                    {
                        task.Data!.Add(record);
                        task.ProcessedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize document {Id}", 
                        doc.GetValue("_id", BsonValue.Create("unknown")));
                }
            }
        }
    }

    private ExportedRecord? DeserializeDocument(BsonDocument doc, string typeName)
    {
        var id = doc.GetValue("_id", BsonValue.Create("")).AsString;
        
        // 获取 _doc.data 中的二进制数据
        if (!doc.Contains("_doc")) return null;
        
        var docField = doc["_doc"];
        if (!docField.IsBsonDocument) return null;
        
        var innerDoc = docField.AsBsonDocument;
        if (!innerDoc.Contains("data")) return null;
        
        var dataValue = innerDoc["data"];
        
        // 使用 Orleans 序列化器反序列化为动态对象
        // 这里返回原始 BsonDocument 让调用方处理
        // 或者使用 BinaryGrainStateSerializer 反序列化
        try
        {
            // 方式1：直接返回 BSON 数据（简单但不完美）
            // var state = BsonSerializer.Deserialize<dynamic>(dataValue.AsBsonBinaryData.Bytes);
            
            // 方式2：使用 IGrainStateSerializer（推荐）
            // 需要知道具体类型才能反序列化，这里用 object 尝试
            var state = _grainStateSerializer.Deserialize<object>(dataValue);
            
            return new ExportedRecord
            {
                Id = id,
                Type = typeName,
                State = state
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Deserialize failed for {Id}, returning raw bytes", id);
            
            // Fallback: 返回 Base64 编码的原始数据
            if (dataValue.IsBsonBinaryData)
            {
                return new ExportedRecord
                {
                    Id = id,
                    Type = typeName,
                    State = new { 
                        _rawBase64 = Convert.ToBase64String(dataValue.AsBsonBinaryData.Bytes),
                        _note = "Failed to deserialize, raw bytes provided"
                    }
                };
            }
            return null;
        }
    }

    private string ExtractTypeName(string collectionName)
    {
        // StreamgodgptAevatar.Application.Grains.UserStatistics.UserStatisticsGAgent
        // → UserStatisticsGAgent
        var parts = collectionName.Replace("Streamgodgpt", "").Split('.');
        return parts.LastOrDefault() ?? collectionName;
    }
}
```

### 2.4 Controllers/StateExportController.cs

```csharp
using System.Threading.Tasks;
using Aevatar.Admin.Models;
using Aevatar.Admin.Services;
using Aevatar.Controllers;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("StateExport")]
[Route("api/admin/export")]
[Authorize(Policy = AevatarPermissions.AdminPolicy)]
public class StateExportController : AevatarController
{
    private readonly IStateExportService _exportService;
    private readonly ILogger<StateExportController> _logger;

    public StateExportController(
        IStateExportService exportService,
        ILogger<StateExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// 启动导出任务
    /// POST /api/admin/export/start
    /// </summary>
    [HttpPost("start")]
    public async Task<StartExportResponse> StartExport([FromBody] StartExportRequest? request)
    {
        _logger.LogInformation("Starting state export. Types: {Types}", 
            request?.Types != null ? string.Join(", ", request.Types) : "all");
        
        var taskId = await _exportService.StartExportAsync(request?.Types);
        
        return new StartExportResponse
        {
            TaskId = taskId,
            Status = "processing",
            Message = "Export task started. Use GET /api/admin/export/status/{taskId} to check progress."
        };
    }

    /// <summary>
    /// 查询导出任务状态
    /// GET /api/admin/export/status/{taskId}
    /// </summary>
    [HttpGet("status/{taskId}")]
    public ExportStatusResponse GetStatus(string taskId)
    {
        var task = _exportService.GetTaskStatus(taskId);
        
        if (task == null)
        {
            throw new UserFriendlyException($"Export task {taskId} not found");
        }
        
        return new ExportStatusResponse
        {
            TaskId = task.TaskId,
            Status = task.Status,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            TotalCount = task.TotalCount,
            ProcessedCount = task.ProcessedCount,
            Error = task.Error
        };
    }

    /// <summary>
    /// 下载导出数据
    /// GET /api/admin/export/download/{taskId}
    /// </summary>
    [HttpGet("download/{taskId}")]
    public IActionResult Download(string taskId)
    {
        var task = _exportService.GetTaskStatus(taskId);
        
        if (task == null)
        {
            throw new UserFriendlyException($"Export task {taskId} not found");
        }
        
        if (task.Status != "completed")
        {
            throw new UserFriendlyException($"Export task {taskId} is not ready. Current status: {task.Status}");
        }
        
        var data = _exportService.GetAndRemoveTaskData(taskId);
        
        if (data == null)
        {
            throw new UserFriendlyException($"Export data for task {taskId} not available");
        }
        
        _logger.LogInformation("Download completed for task {TaskId}. Records: {Count}", taskId, data.Count);
        
        return Ok(new
        {
            taskId,
            exportedAt = task.CompletedAt,
            totalCount = data.Count,
            records = data
        });
    }

    /// <summary>
    /// 获取可导出的 State 类型列表
    /// GET /api/admin/export/types
    /// </summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetAvailableTypes()
    {
        // 这里可以动态从数据库获取集合列表
        return Ok(new
        {
            availableTypes = new[]
            {
                "UserStatistics",
                "GodChat",
                "UserQuota", 
                "UserBilling",
                "Invitation",
                "InviteCode",
                "Lumen",
                "UserInfo",
                "GoogleAuth",
                "Awakening",
                "DailyPush",
                "AnonymousUser",
                "UserFeedback",
                "FreeTrialCode"
            }
        });
    }
}
```

---

## 3. 服务注册

在 `AevatarHttpApiAdminModule.cs` 中注册服务：

```csharp
// 在 ConfigureServices 方法中添加
context.Services.AddScoped<IStateExportService, StateExportService>();
```

---

## 4. API 使用说明

### 4.1 启动导出

```bash
# 导出全部业务数据
curl -X POST "https://api.example.com/api/admin/export/start" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'

# 导出指定类型
curl -X POST "https://api.example.com/api/admin/export/start" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"types": ["UserStatistics", "UserQuota"]}'

# 响应
{
  "taskId": "a1b2c3d4",
  "status": "processing",
  "message": "Export task started..."
}
```

### 4.2 查询状态

```bash
curl "https://api.example.com/api/admin/export/status/a1b2c3d4" \
  -H "Authorization: Bearer $TOKEN"

# 响应
{
  "taskId": "a1b2c3d4",
  "status": "completed",  // pending | processing | completed | failed
  "startedAt": "2026-01-14T12:00:00Z",
  "completedAt": "2026-01-14T12:00:45Z",
  "totalCount": 7000,
  "processedCount": 7000,
  "error": null
}
```

### 4.3 下载数据

```bash
curl "https://api.example.com/api/admin/export/download/a1b2c3d4" \
  -H "Authorization: Bearer $TOKEN" \
  -o export_data.json

# 响应结构
{
  "taskId": "a1b2c3d4",
  "exportedAt": "2026-01-14T12:00:45Z",
  "totalCount": 7000,
  "records": [
    {
      "id": "Aevatar.Application.Grains.UserStatistics.UserStatisticsGAgent/2e08d2bc...",
      "type": "UserStatisticsGAgent",
      "state": {
        "userId": "2e08d2bc-65ad-4c57-9afe-e776ba45d0d6",
        "isInitialized": true,
        "appRatings": { ... }
      }
    },
    ...
  ]
}
```

---

## 5. 配置要求

确保 `appsettings.json` 中有以下配置：

```json
{
  "OrleansEventSourcing": {
    "Mongodb": {
      "Connection": "mongodb://...",
      "Database": "GodgptDbStaging"
    }
  }
}
```

---

## 6. 安全注意事项

1. **认证授权**：API 使用 `[Authorize(Policy = AevatarPermissions.AdminPolicy)]`，仅管理员可访问
2. **一次性下载**：下载后任务数据会被清理，防止重复获取
3. **内存限制**：大量数据会占用内存，7000 条约 21MB 在可接受范围

---

## 7. 测试清单

- [ ] 启动导出任务正常返回 taskId
- [ ] 查询状态显示正确的 processing/completed
- [ ] 下载数据包含所有记录
- [ ] 类型过滤正常工作
- [ ] 无权限访问返回 401/403
- [ ] 任务不存在返回友好错误

---

## 8. 后续：MigrationTool 调用

在 MigrationTool 中添加 `import-from-api` 命令来调用此 API，详见 MigrationTool 文档。
