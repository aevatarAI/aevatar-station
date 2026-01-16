## Aevatar 多语言 Sandbox 执行平台 — 技术方案与落地可行性

### 概述
面向 LLM/Agent 代码生成与任务执行，提供安全、可扩展且可审计的多语言沙箱执行平台。以 `SandboxServiceBase` 为核心抽象，语言适配（Python/C#/Rust/Go 等）通过子类化完成；以 `SandboxExecutionGAgent` 作为统一协调者，管理多个执行实例与状态；新增 `Aevatar.Sandbox.HttpApi.Host` 提供统一 API，对接已有的 `KubernetesHostManager` 执行工作负载。每次执行以 `sandboxExecutionId` 作为全局关联键，与单次执行的 `ISandboxExecutionClientGrain` 的 `GrainId` 完全一致。

### 目标
- 安全隔离地执行不受信任代码，限制资源、超时与网络。
- 支持多语言（首发 Python，后续 C#/Rust/Go 等），统一抽象与观测模型。
- 通过 `SandboxExecutionGAgent` 实现并发控制、事件溯源与审计追踪。
- 通过 `Aevatar.Sandbox.HttpApi.Host` 提供统一入口，复用 `KubernetesHostManager`。
- 保持简单的调用协议，便于平台内外部复用与扩展。

### 非目标
- 不提供任意用户级别镜像定制；不提供持久化工作目录（默认 ephemeral）。
- 不构建长生命周期交互式 REPL 会话（可作为增强）。

---

### 架构

- 核心抽象层
  - `ISandboxService`：统一服务接口，定义启动/查询/取消/日志等操作。
  - `SandboxServiceBase`：基类实现通用策略（资源/网络/模板方法），对接 `KubernetesHostManager`。
  - 语言适配：`PythonSandboxService`、`CSharpSandboxService`、`RustSandboxService`、`GoSandboxService` 等。

- 执行协调层
  - `SandboxExecutionGAgent`：统一的 GAgent 协调者，管理多个执行实例、并发阈值、状态机与事件溯源。
  - `ISandboxExecutionClientGrain` + `SandboxClientGrainBase`：每次执行一个 Grain，`GrainId == sandboxExecutionId`，负责调用 Http API、等待结果并反馈。

- 服务入口层
  - `Aevatar.Sandbox.HttpApi.Host`：新增站点，暴露统一控制器（execute/result/cancel/logs 等），内部调用各语言 `SandboxServiceBase` 子类与 `KubernetesHostManager`。

- 基础设施层
  - `KubernetesHostManager`（已实现）：K8s 资源编排（Job/Pod 创建、状态、日志、清理）、安全上下文、配额与网络策略。

可选扩展：保留 Kafka + Orleans Streams 的流式模式以满足特定场景（扇出、异步解耦），默认以 HTTP 路径为主。

---

### 组件与接口（示例）

```csharp
public interface ISandboxService
{
    Task<SandboxExecutionHandle> StartAsync(SandboxExecutionRequest request, CancellationToken ct = default);
    Task<SandboxExecutionResult?> TryGetResultAsync(string sandboxExecutionId, CancellationToken ct = default);
    Task<bool> CancelAsync(string sandboxExecutionId, CancellationToken ct = default);
    Task<SandboxLogs> GetLogsAsync(string sandboxExecutionId, LogQueryOptions options, CancellationToken ct = default);
}

public abstract class SandboxServiceBase : ISandboxService
{
    protected readonly KubernetesHostManager Kubernetes;
    protected abstract string LanguageId { get; }
    protected abstract string Image { get; }
    protected abstract string[] CommandTemplate { get; }

    protected virtual SandboxResourcePolicy DefaultResourcePolicy => SandboxResourcePolicy.Default();
    protected virtual NetworkPolicy DefaultNetworkPolicy => NetworkPolicy.NoEgress();

    public virtual Task<SandboxExecutionHandle> StartAsync(SandboxExecutionRequest request, CancellationToken ct = default) { /* ... */ throw new NotImplementedException(); }
    public virtual Task<SandboxExecutionResult?> TryGetResultAsync(string sandboxExecutionId, CancellationToken ct = default) { /* ... */ throw new NotImplementedException(); }
    public virtual Task<bool> CancelAsync(string sandboxExecutionId, CancellationToken ct = default) { /* ... */ throw new NotImplementedException(); }
    public virtual Task<SandboxLogs> GetLogsAsync(string sandboxExecutionId, LogQueryOptions options, CancellationToken ct = default) { /* ... */ throw new NotImplementedException(); }

    protected virtual K8sWorkloadSpec BuildWorkloadSpec(SandboxExecutionRequest request) { /* ... */ throw new NotImplementedException(); }
}

public sealed class PythonSandboxService : SandboxServiceBase
{
    protected override string LanguageId => "python";
    protected override string Image => "python-3.11-sandbox";
    protected override string[] CommandTemplate => new[] { "python", "/runner/entry.py" };
}
```

数据模型：

```csharp
public sealed class SandboxExecutionRequest
{
    public required string SandboxExecutionId { get; init; } // 与 GrainId 一致
    public required string LanguageId { get; init; }
    public required string Code { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public IDictionary<string, string>? Parameters { get; init; }
    public string? TenantId { get; init; }
    public string? ChatId { get; init; }
}

public sealed class SandboxExecutionHandle
{
    public required string SandboxExecutionId { get; init; }
    public required string WorkloadName { get; init; }
    public DateTime StartedAtUtc { get; init; }
}

public sealed class SandboxExecutionResult
{
    public required string SandboxExecutionId { get; init; }
    public bool Success { get; init; }
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public int ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public double ExecTimeSec { get; init; }
    public int MemoryUsedMB { get; init; }
    public string ScriptHash { get; init; } = string.Empty;
    public DateTime FinishedAtUtc { get; init; }
}
```

Client Grain 抽象：

```csharp
public interface ISandboxExecutionClientGrain : IGrainWithGuidKey
{
    Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionClientParams @params);
}

[GenerateSerializer]
public sealed class SandboxExecutionClientParams
{
    [Id(0)] public required string LanguageId { get; init; }
    [Id(1)] public required string Code { get; init; }
    [Id(2)] public int TimeoutSeconds { get; init; } = 30;
    [Id(3)] public string? TenantId { get; init; }
    [Id(4)] public string? ChatId { get; init; }
}

public abstract class SandboxClientGrainBase : Grain, ISandboxExecutionClientGrain
{
    protected readonly HttpClient HttpClient;
    protected Guid SandboxExecutionId => this.GetPrimaryKey();

    public virtual async Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionClientParams @params)
    {
        // 1) POST /api/sandbox/execute -> handle
        // 2) 轮询或长轮询 GET /api/sandbox/result/{sandboxExecutionId}
        // 3) 返回结果并 DeactivateOnIdle
        throw new NotImplementedException();
    }
}
```

GAgent 协调者与状态：

```csharp
[GAgent("sandbox-execution", "station")]
public sealed class SandboxExecutionGAgent : GAgentBase<SandboxExecutionState, SandboxExecutionStateLogEvent>, ISandboxExecutionGAgent
{
    public async Task<SandboxExecutionResult> RunAsync(string languageId, string code, int timeoutSeconds = 30)
    {
        // 1) 并发检查
        // 2) 生成 sandboxExecutionId = Guid.NewGuid()
        // 3) 激活 ISandboxExecutionClientGrain(sandboxExecutionId).ExecuteAsync(...)
        // 4) 事件：ExecutionStarted -> ... -> ExecutionCompleted
        // 5) 返回结果
        throw new NotImplementedException();
    }

    protected override void GAgentTransitionState(SandboxExecutionState state, StateLogEventBase<SandboxExecutionStateLogEvent> @event)
    {
        // 应用状态机
    }
}

[GenerateSerializer]
public sealed class SandboxExecutionState : StateBase
{
    [Id(0)] public int InFlightCount { get; set; }
    [Id(1)] public List<ExecutionRecord> History { get; set; } = new();
}

[GenerateSerializer]
public sealed class ExecutionRecord
{
    [Id(0)] public string SandboxExecutionId { get; set; } = string.Empty;
    [Id(1)] public string LanguageId { get; set; } = string.Empty;
    [Id(2)] public DateTime StartedAtUtc { get; set; }
    [Id(3)] public DateTime? FinishedAtUtc { get; set; }
    [Id(4)] public bool? Success { get; set; }
    [Id(5)] public int? ExitCode { get; set; }
    [Id(6)] public string? ErrorCode { get; set; }
}

[GenerateSerializer]
public abstract record SandboxExecutionStateLogEvent : StateLogEventBase<SandboxExecutionStateLogEvent>;

[GenerateSerializer]
public sealed record ExecutionStartedEvent(string SandboxExecutionId, string LanguageId, DateTime StartedAtUtc) : SandboxExecutionStateLogEvent;

[GenerateSerializer]
public sealed record ExecutionCompletedEvent(string SandboxExecutionId, bool Success, int ExitCode, DateTime FinishedAtUtc) : SandboxExecutionStateLogEvent;

[GenerateSerializer]
public sealed record ExecutionTimeoutEvent(string SandboxExecutionId, DateTime FinishedAtUtc) : SandboxExecutionStateLogEvent;
```

---

### Aevatar.Sandbox.HttpApi.Host

- 路由
  - `POST /api/sandbox/execute` → `SandboxExecutionHandle`
  - `GET /api/sandbox/result/{sandboxExecutionId}` → `SandboxExecutionResult | 202/404`
  - `POST /api/sandbox/cancel/{sandboxExecutionId}` → `200/404`
  - `GET /api/sandbox/logs/{sandboxExecutionId}` → `SandboxLogs`
  - 可选：`POST /api/sandbox/callback/{sandboxExecutionId}`（异步回调模式）

- 行为
  - 根据 `LanguageId` 分发到对应 `SandboxServiceBase` 子类，构建 `K8sWorkloadSpec` 并通过 `KubernetesHostManager` 创建 Job/Pod；结果查询聚合 K8s 状态与日志，并标准化返回。

- 安全
  - 鉴权（API Key/JWT）、限流、审计日志、字段脱敏；内部网络隔离策略。

---

### Kubernetes 集成

- Workload：默认 Job（一次性），可配置 backoffLimit；失败留存短期以供审计；成功 Job 清理策略可配置。
- 资源限制（默认）：CPU 1 vCPU、内存 512MB；超时默认 30s（上限 60s）。
- 安全上下文：runAsNonRoot、readOnlyRootFilesystem、drop caps、seccomp/apparmor。
- 网络：默认 Deny Egress（按语言/场景可白名单）。
- 观测：以 `sandboxExecutionId`、`workloadName`、`languageId` 打标签；采集执行时间、退出码、内存峰值、超时率等。

---

### 执行流程（同步轮询）

1) `SandboxExecutionGAgent` 接到请求并进行并发校验。
2) 生成 `sandboxExecutionId`，激活 `ISandboxExecutionClientGrain(sandboxExecutionId)`。
3) Grain 调用 `POST /api/sandbox/execute` 获取 `workloadName`。
4) Grain 轮询 `GET /api/sandbox/result/{sandboxExecutionId}` 直到完成（指数退避）。
5) Grain 返回 `SandboxExecutionResult` 给 `SandboxExecutionGAgent`；Agent 写入完成事件并回传给调用者。

（可选）异步回调模式：Host 在执行完成后调用内部回调端点，由 Router 或 Agent 完成关联并唤醒等待者。

---

### 并发与状态机

- Agent 层并发阈值：默认 3，可配置。
- 状态机：Queued → InFlight → Completed/Failed/Timeout → Archived。
- 事件溯源：`ExecutionStartedEvent`、`ExecutionCompletedEvent`、`ExecutionTimeoutEvent`、`ExecutionFailedEvent` 等。

---

### 安全与合规

- 代码执行：Runner 接管入口；禁用出站网络；标准输出截断（默认 256KB）。
- 供应链：受信镜像仓库、镜像签名（Cosign/Notary v2 可选）、镜像扫描。
- API 防护：认证鉴权、速率限制、审计日志、最小暴露面。

---

### 可观测性

- 指标：执行时长、内存/CPU 峰值、超时率、错误码分布、语言维度 QPS。
- 日志：按 `sandboxExecutionId` 串联 Host/Grain/Agent；脚本内容不入日志，仅保留哈希。
- 追踪：OpenTelemetry 可选接入，将 Host/K8s/Grain 链路统一。

---

### 迁移策略

- 将现有 `PythonExecutionGAgent` 融入通用 `SandboxExecutionGAgent`。
- 将 `PythonSandboxService` 改为继承 `SandboxServiceBase` 并接入 `KubernetesHostManager`。
- `PythonSandboxClientGrain` 迁移为 `SandboxClientGrainBase`/`ISandboxExecutionClientGrain`。
- 保留流式模式作为可选扩展；默认走 Http API。

---

### 技术落地可行性分析

1) Kubernetes 层可行性：
   - 已有 `KubernetesHostManager`，可直接创建/查询/清理 Job/Pod，并施加资源与安全策略，满足隔离与编排要求。
   - Job/Pod + 容器 Runner 的模式业界成熟；网络与文件系统隔离策略可直接落地。

2) Orleans 协调层可行性：
   - `SandboxExecutionGAgent` 基于现有 GAgent 规范，事件溯源与并发门控模式成熟可靠。
   - `SandboxClientGrainBase` 为一次性执行的典型用法，`GrainId == sandboxExecutionId` 保障关联可观测。

3) Http API 层可行性：
   - 新增 `Aevatar.Sandbox.HttpApi.Host` 是 Station 体系内的常规扩展；控制器与服务注册模式既有实践可复用。
   - 安全、限流、审计、日志链路与当前平台能力一致，可快速对齐。

4) 多语言扩展可行性：
   - 通过 `SandboxServiceBase` 封装共性，语言子类只需提供镜像/命令模板与少量适配逻辑。
   - C#/Rust/Go 的编译/运行流程可在容器内用标准工具链完成；超时/资源由 K8s 统一约束。

5) 风险与对策：
   - 冷启动：通过镜像预拉取、轻量 Runner、或 Deployment 常驻模式缓解。
   - 安全逃逸：最小权限、只读根、严格 NetworkPolicy、可选 Runtime 沙箱（gVisor/Firecracker）。
   - 长任务：提供异步回调模式与结果缓存；或分片为子任务并行执行。

结论：
基于现有 `KubernetesHostManager` 能力与 Orleans/GAgent 框架，新增 `Aevatar.Sandbox.HttpApi.Host` 与抽象化的 `SandboxServiceBase` 即可快速落地。多语言扩展点清晰，安全与观测方案完备，整体方案技术成熟且可渐进式上线。

