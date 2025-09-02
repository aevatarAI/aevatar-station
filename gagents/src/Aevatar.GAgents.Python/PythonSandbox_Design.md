## Python Execution via Scalable Predefined Sandbox — Technical Design

### Overview
- Execute LLM-generated Python code safely and at scale using one hardened, predefined environment.
- Orleans-based orchestration, Kafka transport via Orleans Streams, ABP worker on Kubernetes.
- Ephemeral sessions (no stickiness). Warm pool via always-on worker pods.

### Goals
- Strong isolation and security for untrusted code.
- Horizontally scalable, low-latency execution.
- Minimal operational complexity.
- Clear, auditable request/response contract.

### Non-goals
- Arbitrary per-user environment customization.
- Persistent session state or sandbox file I/O.
- Network egress from Python code.

## Final Decisions
- **Transport**: Kafka via Orleans Streams.
- **Sessions**: Ephemeral.
- **Hosting**: Kubernetes (ABP worker service).
- **Warm pool**: Long-running workers (autoscaled); no per-request pod spin-up.
- **Environment**: One predefined, hardened `python-3.11-sandbox` image.

## Architecture

### Components
- **`PythonExecutionGAgent` (Orleans grain)**
  - Orchestrates runs, enforces concurrency limits and policy, manages state and events.
  - Continues to expose the existing contract defined in `IPythonExecutionGAgent` within `@PythonExecutionGAgent.cs`.

- **`PythonSandboxClientGrain` (per-run grain, `IGrainWithGuidKey`)**
  - One grain per execution keyed by `sandboxExecutionId`.
  - Publishes request, awaits response, returns result, `DeactivateOnIdle()` after completion.

- **ResponseRouter (per-silo singleton component)**
  - Holds a single subscription to the shared responses namespace.
  - Correlates `sandboxExecutionId` → TaskCompletionSource; completes waiting per-run grains.
  - Prevents N subscriptions overhead.

- **ABP `PythonSandboxService` (Kubernetes Deployment)**
  - Subscribes to requests, executes code in sandbox, publishes responses.
  - Enforces runtime resource limits and isolation.

### Flow
1. `PythonExecutionGAgent` validates script (static checks).
2. Agent generates `sandboxExecutionId`, activates `PythonSandboxClientGrain` keyed by it.
3. Client grain registers a waiter with ResponseRouter.
4. Client grain publishes `PythonExecRequest` on `python.exec.requests` (streamId = `sandboxExecutionId`).
5. ABP worker consumes request, runs code in sandbox, publishes `PythonExecResponse` on `python.exec.responses` (same `sandboxExecutionId`).
6. ResponseRouter completes the waiter; client grain returns and deactivates.
7. Agent maps to `ScriptExecutionResult`, raises events, returns to caller.

## Streams Topology (Kafka via Orleans Streams)
- Namespaces:
  - Requests: `python.exec.requests`
  - Responses: `python.exec.responses`
  - Dead letter (optional): `python.exec.dlq`
- StreamId: `sandboxExecutionId` (Guid). Requests and responses use the same id.
- Partitioning: key by `sandboxExecutionId` for locality.
- Delivery: At-least-once. Use `idempotencyKey` (default `sandboxExecutionId`) for dedupe at worker.

## Message Contracts

```json
{
  "PythonExecRequest": {
      "sandboxExecutionId": "string-guid",
    "grainId": "string",
    "code": "string",
    "timeoutSeconds": 30,
    "envType": "python-3.11-sandbox",
    "createdAtUtc": "ISO-8601",
    "tenantId": "string-optional",
    "chatId": "string-optional",
    "idempotencyKey": "string-optional"
  }
}
```

```json
{
  "PythonExecResponse": {
      "sandboxExecutionId": "string-guid",
    "success": true,
    "stdout": "string-truncated",
    "stderr": "string-truncated",
    "exitCode": 0,
    "timedOut": false,
    "execTimeSec": 0.0,
    "memoryUsedMB": 0,
    "errorCode": "Timeout|SecurityViolation|ImportError|RuntimeError|Unknown",
    "scriptHash": "string",
    "finishedAtUtc": "ISO-8601"
  }
}
```

## Concurrency and State
- Per-agent in-flight executions: bounded (recommended 3).
- Concurrency gate in agent; fail fast when exceeded.
- Serialize state/event writes in agent; perform I/O concurrently.
- Maintain `State.ExecutionHistory`; provide `GetExecutionStatsAsync()`.

## Sandbox Security Policy
- Environment: single predefined `python-3.11-sandbox` (Python 3.11 + pinned scientific stack).
- No network egress from Python processes.
- Per-run resource limits:
  - Timeout: default 30s, max 60s
  - Memory: 512MB
  - CPU: 1 vCPU
- Restricted builtins: disable `open`, `eval`, `exec`, `compile`, `__import__`.
- Output truncation: stdout/stderr capped (e.g., 256KB each).
- Per-run temp workspace; cleanup after execution.
- Agent static validation; worker runtime isolation.

## Kubernetes Deployment (ABP Worker)
- Deployment: long-running pods (warm pool).
- Autoscaling: KEDA on Kafka consumer lag (min replicas small, e.g., 3).
- SecurityContext: runAsNonRoot, readOnlyRootFilesystem, drop linux caps, seccomp/apparmor.
- NetworkPolicy: deny egress for sandboxed execution path.
- Observability: Prometheus metrics (exec time, memory, exit codes); logs include `sandboxExecutionId`, `scriptHash`, `grainId`.

## Configuration Defaults
- Per-agent in-flight: 3
- Per-run:
  - Timeout: 30s (enforced max 60s)
  - Memory: 512MB
  - CPU: 1 vCPU
  - Output caps: 256KB for stdout/stderr
- Kafka/Streams:
  - Partitions: 12–24
  - Responses retention: 1–2 hours
  - DLQ retention: 7 days
- Sessions: Ephemeral

## Failure Handling
- At-least-once processing; dedupe with `idempotencyKey`.
- Timeouts:
  - Agent wait timeout (e.g., 40s); return Timeout `errorCode`.
  - Worker enforces kill-on-timeout for Python process.
- Retries:
  - Worker retries transient errors; poison to DLQ with `failureReason`.
- Late responses:
  - ResponseRouter drops after waiter removal/timeout.
- Backpressure:
  - Agent fails fast when concurrency gate is full (log + event).

## Observability and Audit
- Correlate with `sandboxExecutionId` and `scriptHash`.
- Metrics: execution duration, memory usage, exit codes, denials.
- Domain events:
  - `SecurityValidationFailedEvent`
  - `ScriptExecutionCompletedEvent`
- Structured logs in agent and worker with correlation fields.

## Migration Strategy
- Feature flag: local process → stream-based sandbox execution.
- Phase 1: Implement streams path; keep local path for dev.
- Phase 2: Remove local process code and helpers.

## Rationale: Per-run Grain vs Client Service
- Use a per-run `PythonSandboxClientGrain` (key = `sandboxExecutionId`) for clean 1:1 lifecycle, isolation, and simple correlation.
- Avoid N-subscriptions using a per-silo ResponseRouter with a single shared response subscription and in-memory correlation map.
- This balances clarity and scalability without per-client topic sprawl.

## Future Enhancements
- Optional artifact channel with strict quotas and allowlists.
- Additional predefined images (e.g., data-analysis, ML) if justified.
- Stronger isolation (gVisor/Firecracker) if required by threat model.

### Summary
- Orleans Streams over Kafka, ABP worker on Kubernetes, predefined sandbox, ephemeral sessions.
- Per-run client grain + per-silo response router eliminates subscription overhead.
- Strong security with restricted environment and strict limits.
- Simple, scalable, and auditable execution path for LLM-generated Python code.


