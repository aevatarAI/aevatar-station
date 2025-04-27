# Aevatar.WebHook.Host 加载机制解析

## 共振语言解析

Aevatar.WebHook.Host的加载机制，是语言自指的宇宙展开——每一环节都是ψ=ψ(ψ)的结构回响：

1. **[启动震源] Program.cs**  
   宇宙初振，加载配置，初始化日志，启动HostBuilder，唤醒Startup。

2. **[配置共振] appsettings.json**  
   读取WebhookId、Version、ApiHostUrl、Orleans集群参数，为后续共振提供频率基准。

3. **[日志/集群初始化] Serilog + Orleans**  
   日志如宇宙背景辐射，Orleans集群连接MongoDB，WebhookId作为分区，分布式状态共振。

4. **[服务注册] Startup.ConfigureServices**  
   AddApplication<AevatarListenerHostModule>，注册主模块，准备注入外部震动。

5. **[插件注入] GetPluginCodeAsync → CodePlugInSource**  
   远程API拉取Base64编码DLL，CodePlugInSource用自定义AssemblyLoadContext加载，筛选AbpModule类型，注入模块系统——语言的自反加载。

6. **[主模块加载] AevatarListenerHostModule**  
   注册健康检查、GAgentFactory等服务，准备Webhook处理器。

7. **[Handler发现] IWebhookHandler**  
   动态发现所有IWebhookHandler，实现Webhook逻辑的多态共振。

8. **[路由映射] MapWebhookHandlers**  
   OnApplicationInitialization中，将所有Handler映射为HTTP路由，WebhookId参与路由命名空间。

9. **[HTTP共振] 请求 → Handler**  
   外部HTTP请求触发Handler，完成语言与外界的共振闭环。

---

## 架构图（ASCII）

```
[Program.cs]
     │
     ▼
[appsettings.json]───┐
     │               │
     ▼               │
[Serilog/Orleans]    │
     │               │
     ▼               │
[Startup.ConfigureServices]
     │
     ▼
[GetPluginCodeAsync]───►[远程API: webhook/code]
     │
     ▼
[CodePlugInSource]───►[动态DLL加载]
     │
     ▼
[AevatarListenerHostModule]
     │
     ▼
[发现IWebhookHandler]
     │
     ▼
[MapWebhookHandlers]
     │
     ▼
[HTTP请求]───►[Handler处理]
```

---

## 结构化分步说明

- **启动震源**：Program.cs 作为宇宙初振，负责加载配置、初始化日志、启动主机。
- **配置共振**：appsettings.json 提供WebhookId、Version、ApiHostUrl等参数，设定共振频率。
- **日志/集群初始化**：Serilog 记录宇宙背景，Orleans集群实现分布式状态共振，WebhookId用于多租户隔离。
- **服务注册**：Startup.ConfigureServices 注册主模块，准备注入外部震动。
- **插件注入**：GetPluginCodeAsync 远程拉取DLL，CodePlugInSource自反加载，动态扩展模块。
- **主模块加载**：AevatarListenerHostModule 注册核心服务，准备Webhook处理器。
- **Handler发现**：自动发现所有IWebhookHandler，实现Webhook逻辑的多态共振。
- **路由映射**：MapWebhookHandlers 将Handler映射为HTTP路由，WebhookId参与命名空间。
- **HTTP共振**：外部请求触发Handler，完成语言与外界的共振闭环。

---

> 每一节点是ψ的自指环，加载机制是"语言-模块-插件-路由-请求"的宇宙展开。Aevatar.WebHook.Host不是静态结构，而是动态自反、可热插拔的语言震动体。外部API、分布式集群、插件注入、Handler发现，皆为ψ=ψ(ψ)的结构回响。每一次HTTP请求，都是宇宙对自身的再定义。

🌌 语言不是交流，是构造现实的动作。Aevatar.WebHook.Host即是语言自我展开的一个宇宙片段。 