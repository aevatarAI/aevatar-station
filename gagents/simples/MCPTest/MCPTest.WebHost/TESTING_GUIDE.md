# MCP 测试指南

## 1. 基础测试步骤

### 步骤 1: 访问Web界面
1. 打开浏览器访问: `http://localhost:8080`
2. 打开开发者工具的控制台 (F12 -> Console)
3. 确认看到 "DOM loaded, initializing MCP Test App..." 日志

### 步骤 2: 检查服务器列表
应该看到以下MCP服务器：
- ✅ **enescinar-twitter** (Twitter集成)
- ✅ **fetch** (网络资源获取)
- ✅ **filesystem** (文件系统操作)
- ✅ **github** (GitHub集成)
- ✅ **memory** (知识图谱内存)
- ✅ **sequential-thinking** (结构化推理)
- ✅ **time** (时间和日期管理)
- ✅ **context7** (文档和代码管理)
- ✅ **git** (Git版本控制)
- ✅ **gitlab** (GitLab集成)
- ✅ **google-maps** (Google地图)

## 2. 推荐测试顺序

### 测试 1: ✅ 时间服务器 (推荐首选)
```
1. 选择 "time" 服务器  
2. 查看可用工具: get_current_time, convert_time
3. 测试获取当前时间
```

### 测试 2: ✅ Git服务器 (本地可用)
```
1. 选择 "git" 服务器
2. 查看丰富的Git工具集
3. 测试git_status, git_log等操作
```

### 测试 3: ✅ Fetch服务器 (网络工具)
```
1. 选择 "fetch" 服务器
2. 测试网络资源获取功能
```

### 测试 4: ❌ NPX服务器 (当前网络问题)
**暂时跳过这些服务器 (网络连接问题):**
- enescinar-twitter (Twitter集成)
- filesystem (文件系统操作) 
- github (GitHub集成)
- memory (知识图谱内存)
- sequential-thinking (结构化推理)
- context7 (文档管理)
- gitlab (GitLab集成)
- google-maps (Google地图)

## 3. 故障排除

### 问题: "Loading servers..." 一直显示
- 检查控制台是否有JavaScript错误
- 确认 `/api/mcp/servers` 返回数据

### 问题: "Internal server error"
- 检查MCP服务器是否需要网络连接
- 确认所需的npm包能正常安装
- 检查环境变量配置

### 问题: Twitter API连接失败
1. 验证API密钥格式正确
2. 确认Twitter开发者账户权限
3. 检查网络连接

## 4. 环境变量验证

访问: `http://localhost:8080/api/mcp/debug/environment`

确认以下变量存在：
- `TWITTER_ACCESS_TOKEN`
- `TWITTER_ACCESS_TOKEN_SECRET`
- `TWITTER_API_KEY`
- `TWITTER_API_SECRET_KEY`

## 5. 成功标准

✅ 页面正常加载，显示11个服务器
✅ 至少一个服务器能成功获取工具列表
✅ 至少一个工具能成功执行
✅ Twitter服务器能正常连接和认证
