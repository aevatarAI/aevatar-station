# State Export API 使用文档

## 概述

State Export API 提供了从MongoDB导出所有GAgent State数据的功能，支持批量导出和分析。API使用Orleans的`HybridGrainStateSerializer`进行反序列化，确保数据格式正确。

## 功能特性

✅ **支持所有Agent类型**：自动识别并反序列化所有GAgent State  
✅ **生产环境优化**：使用分页API，避免超时问题  
✅ **类型安全**：使用Silo端的`HybridGrainStateSerializer`确保反序列化正确  
✅ **自动类型发现**：从collection名称自动推断State类型  
✅ **详细日志**：提供反序列化过程的详细日志  

## API端点

### 1. 获取可用的State集合列表

**端点**: `GET /api/admin/export/collections`

**认证**: 需要Admin权限（Bearer Token）

**响应示例**:
```json
{
    "code": "20000",
    "data": [
        {
            "collectionName": "StreamgodgptAevatar.Application.Grains.UserStatistics.UserStatisticsGAgent",
            "typeName": "UserStatisticsGAgent",
            "count": 10
        },
        {
            "collectionName": "StreamgodgptAevatar.Application.Grains.Agents.ChatManager.Chat.GodChatGAgent",
            "typeName": "GodChatGAgent",
            "count": 1980
        }
    ]
}
```

### 2. 分页导出State数据

**端点**: `GET /api/admin/export/grain`

**认证**: 需要Admin权限（Bearer Token）

**查询参数**:
- `collection` (必需): MongoDB集合名称，例如 `StreamgodgptAevatar.Application.Grains.UserStatistics.UserStatisticsGAgent`
- `skip` (可选): 跳过的记录数，默认0
- `limit` (可选): 每页返回的记录数，默认1000，最大5000

**响应示例**:
```json
{
    "code": "20000",
    "data": {
        "collectionName": "StreamgodgptAevatar.Application.Grains.UserStatistics.UserStatisticsGAgent",
        "typeName": "UserStatisticsGAgent",
        "skip": 0,
        "limit": 1000,
        "totalCount": 10,
        "hasMore": false,
        "records": [
            {
                "id": "Aevatar.Application.Grains.UserStatistics.UserStatisticsGAgent/2e08d2bc65ad4c579afee776ba45d0d6",
                "eTag": "4d73e095-34c2-48c2-ad23-4e873ab66823",
                "state": {
                    "UserId": "00000000-0000-0000-0000-000000000000",
                    "IsInitialized": false,
                    "AppRatings": null,
                    "IsRealUser": false,
                    "Children": null,
                    "Parent": null,
                    "GAgentCreator": null
                }
            }
        ]
    }
}
```

## 使用示例

### 1. 获取Token

```bash
TOKEN=$(curl -s -X POST "http://localhost:8001/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=password&client_id=AevatarAuthServer&username=admin&password=YOUR_PASSWORD&scope=Aevatar" \
    | jq -r '.access_token')
```

### 2. 获取所有集合列表

```bash
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8001/api/admin/export/collections" | jq
```

### 3. 导出单个集合的所有数据（分页循环）

```bash
#!/bin/bash
COLLECTION="StreamgodgptAevatar.Application.Grains.UserStatistics.UserStatisticsGAgent"
SKIP=0
LIMIT=1000
HAS_MORE=true

while [ "$HAS_MORE" = "true" ]; do
    RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN" \
        "http://localhost:8001/api/admin/export/grain?collection=${COLLECTION}&skip=${SKIP}&limit=${LIMIT}")
    
    # 保存数据
    echo "$RESPONSE" | jq '.data.records' >> "export_${COLLECTION}.json"
    
    # 检查是否还有更多数据
    HAS_MORE=$(echo "$RESPONSE" | jq -r '.data.hasMore')
    SKIP=$((SKIP + LIMIT))
    
    echo "Exported ${SKIP} records..."
done

echo "Export completed!"
```

### 4. Python示例

```python
import requests
import json

BASE_URL = "http://localhost:8001"
TOKEN = "your_access_token"

headers = {"Authorization": f"Bearer {TOKEN}"}

# 1. 获取集合列表
collections_response = requests.get(
    f"{BASE_URL}/api/admin/export/collections",
    headers=headers
)
collections = collections_response.json()["data"]

# 2. 导出每个集合的数据
for collection_info in collections:
    collection_name = collection_info["collectionName"]
    total_count = collection_info["count"]
    
    print(f"Exporting {collection_name} ({total_count} records)...")
    
    skip = 0
    limit = 1000
    all_records = []
    
    while True:
        response = requests.get(
            f"{BASE_URL}/api/admin/export/grain",
            headers=headers,
            params={
                "collection": collection_name,
                "skip": skip,
                "limit": limit
            }
        )
        
        data = response.json()["data"]
        all_records.extend(data["records"])
        
        if not data["hasMore"]:
            break
        
        skip += limit
        print(f"  Progress: {skip}/{total_count}")
    
    # 保存到文件
    filename = f"export_{collection_info['typeName']}.json"
    with open(filename, "w") as f:
        json.dump(all_records, f, indent=2)
    
    print(f"  Saved {len(all_records)} records to {filename}")
```

## 配置

### Silo配置 (`appsettings.json`)

```json
{
  "StateExport": {
    "ConnectionString": "mongodb://user:password@localhost:27018/GodgptDbStaging?authSource=GodgptDbStaging&directConnection=true&readPreference=secondaryPreferred",
    "Database": "GodgptDbStaging"
  }
}
```

### 生产环境配置

生产环境需要配置MongoDB连接：

1. **Kubernetes Port-Forward**（开发/测试）:
   ```bash
   kubectl -n dapp-factory-shared port-forward pod/mongo-1 27018:27017
   ```

2. **直接连接**（生产）:
   - 修改`StateExport:ConnectionString`为生产MongoDB连接字符串
   - 确保Silo可以访问MongoDB

## 工作原理

1. **类型自动发现**: 从collection名称（如`StreamgodgptXxxGAgent`）推断State类型（如`XxxState`）
2. **反序列化**: 使用`HybridGrainStateSerializer`反序列化Orleans binary格式的State
3. **JSON转换**: 将反序列化的对象转换为Dictionary，便于JSON序列化
4. **分页处理**: 使用MongoDB的`skip`和`limit`进行分页，避免内存溢出

## 性能优化

- ✅ 使用`EstimatedDocumentCountAsync`（不扫描全表）
- ✅ MongoDB projection只取必要字段（`_id`, `_doc`, `_etag`）
- ✅ 类型缓存避免重复反射查找
- ✅ 分页处理避免一次性加载大量数据

## 错误处理

- 如果State类型找不到，返回raw BSON结构
- 单个文档反序列化失败不影响整体导出
- 详细的错误日志记录在Silo日志中

## 日志

Silo日志中包含详细的导出信息：

```
🔍 Deserializing "UserStatisticsState": HasData=True, DataSize=331 bytes
✅ Deserialized "UserStatisticsState": 1/7 non-default fields
Exported 1/1 records from "Streamgodgpt..." (errors: 0)
```

## 限制

- 每页最大`limit`为5000（防止内存问题）
- 需要Admin权限访问
- 需要Silo正常运行（使用Silo端的序列化器）

## 故障排查

### 问题：返回的数据全是默认值

**原因**: 这些Grain的State确实就是默认值（新创建或未使用）

**验证**: 检查Silo日志中的`DataSize`，如果有数据但反序列化后是默认值，说明State本身就是默认状态

### 问题：连接超时

**解决**: 
1. 检查MongoDB连接是否正常
2. 减小`limit`参数（如改为100）
3. 检查port-forward是否运行

### 问题：找不到State类型

**解决**: 
1. 检查Silo日志中的类型查找信息
2. 确认State类型命名符合规范（`XxxState`或`XxxGAgentState`）
3. 如果找不到，会返回raw BSON结构作为fallback

## 导入逻辑建议

导出后的JSON格式可以直接用于导入：

```json
{
  "id": "GrainId",
  "eTag": "etag-value",
  "state": {
    "Field1": "value1",
    "Field2": "value2"
  }
}
```

导入时需要：
1. 解析JSON文件
2. 根据`id`找到对应的Grain
3. 使用`IGrainStateSerializer.Serialize`序列化State
4. 写入MongoDB的`_doc.data`字段

## 相关文件

- `station/src/Aevatar.Silo/Grains/StateExport/StateExportGrain.cs` - Grain实现
- `station/src/Aevatar.HttpApi.Admin/Controllers/StateExportController.cs` - API控制器
- `station/src/Aevatar.Application.Contracts/StateExport/IStateExportGrain.cs` - 接口定义
