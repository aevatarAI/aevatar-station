# ElasticSearch 性能观测与Tracing增强 Feature Planning Artifacts

---

## Problem Statement
- 当前ElasticSearch作为索引与日志核心，缺乏细粒度性能指标与分布式链路追踪，导致性能瓶颈难以定位，异常难以溯源。
- 没有可观测性，团队无法及时发现和解决性能问题，影响系统稳定性与用户体验，阻碍后续优化与扩展。
- 研发团队（定位与优化瓶颈）、运维团队（监控与报警）、业务方（系统稳定性）、最终用户（响应速度与可靠性）均受影响。

---

## Goals & Success Criteria
- 在ElasticIndexingService与LogElasticSearchService关键操作插入分布式Tracing与自定义Metrics埋点。
- 采集并上报如下指标：操作耗时、批量写入文档数、查询命中数、成功/失败率、异常信息。
- TraceId/SpanId贯穿调用链，trace与日志可关联。
- 指标可在Prometheus、ElasticSearch、Jaeger等平台可视化与查询。
- 插桩后系统性能无明显下降（<3%）。
- 通过Prometheus/Jaeger/ElasticSearch可实时查询和追踪ElasticSearch相关操作的性能与trace。
- 研发/运维可基于trace和metrics定位性能瓶颈与异常。
- 相关文档、监控面板、报警规则齐备。
- 通过回归测试与性能测试，确保功能正确、性能达标。

---

## Stakeholder List
- 研发负责人 | 技术方案设计与实现 | Y
- 运维负责人 | 监控、报警、运维流程 | Y
- QA/测试 | 回归与性能测试 | Y
- 产品经理 | 需求确认与验收 | Y
- 业务代表 | 业务影响评估 | N
- 架构师 | 技术架构把关 | Y

---

## Requirements
- Functional Requirements:
  - 在ElasticIndexingService与LogElasticSearchService关键方法插入OpenTelemetry Activity与自定义metrics埋点。
  - 采集并上报操作耗时、批量写入文档数、查询命中数、成功/失败率、异常信息等指标。
  - TraceId/SpanId贯穿调用链，trace与日志可关联。
  - 指标与trace可在Prometheus、ElasticSearch、Jaeger等平台可视化与查询。
  - 插桩代码需有单元测试与集成测试覆盖。
  - 提供相关文档与监控面板配置示例。
- Non-Functional Requirements:
  - 插桩后系统性能下降不超过3%。
  - 代码遵循SOLID原则，便于后续扩展。
  - 埋点与trace不影响主业务流程的可用性与稳定性。
  - 方案兼容现有OpenTelemetry与日志体系。
  - 监控与报警规则可灵活配置。

---

## Constraints & Assumptions
- Constraints:
  - 必须兼容现有OpenTelemetry与日志集成方式。
  - 仅允许在ElasticIndexingService与LogElasticSearchService等ElasticSearch相关服务插桩。
  - 插桩代码需通过所有现有单元测试与集成测试。
  - 监控与trace数据需符合公司安全与合规要求。
  - 需遵循自动化开发与分支管理规范（flow.mdc）。
- Assumptions:
  - OpenTelemetry Collector、Prometheus、Jaeger、ElasticSearch等观测组件已部署并可用。
  - 研发与运维团队具备相关观测平台的使用经验。
  - 业务高峰期允许短暂重启服务以部署新埋点。
  - 未来可能扩展更多ElasticSearch相关服务的观测需求。

---

## Solution Sketch

### 高层架构图（文字描述）
```
[调用方/Controller]
      |
      v
[ElasticIndexingService/LogElasticSearchService]
      |
      v
[OpenTelemetry Activity & Metrics 埋点]
      |
      v
[Elasticsearch 客户端]
      |
      v
[OpenTelemetry Collector] <-> [Prometheus/Jaeger/ElasticSearch]
```

### 关键组件与交互
- 关键方法（如SaveOrUpdateStateIndexBatchAsync、QueryWithLuceneAsync等）起始/结束处插入OpenTelemetry Activity。
- 用Meter记录自定义metrics（写入耗时、bulk大小、命中数等）。
- 异常时记录Event，trace中标记error。
- 日志中补充traceId，便于日志与trace关联。
- 观测数据通过OpenTelemetry Collector汇聚，分别导出到Prometheus（metrics）、Jaeger（trace）、ElasticSearch（trace/日志）。

### 伪代码示例
```csharp
public async Task SaveOrUpdateStateIndexBatchAsync(IEnumerable<SaveStateCommand> commands)
{
    using var activity = _activitySource.StartActivity("ElasticSaveOrUpdateBatch");
    var stopwatch = Stopwatch.StartNew();
    try
    {
        // ...批量写入逻辑...
        _meter.Record("aevatar.elasticsearch.write.bulk_size", commands.Count());
        activity?.SetTag("bulk_size", commands.Count());
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        _meter.Record("aevatar.elasticsearch.error.count", 1);
        throw;
    }
    finally
    {
        stopwatch.Stop();
        _meter.Record("aevatar.elasticsearch.write.duration", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
    }
}
```

---

## Risk Assessment
- Risk | Likelihood | Impact | Mitigation
- 插桩导致性能下降 | 中 | 中 | 逐步灰度上线，性能回归测试，监控延迟指标
- 埋点遗漏或trace链断裂 | 中 | 高 | 代码评审，自动化测试覆盖traceId/metrics
- OpenTelemetry/观测平台配置错误 | 低 | 高 | 预先在测试环境全链路验证，文档完善
- 观测数据量过大导致存储/查询压力 | 中 | 中 | 指标采样、trace采样、定期清理历史数据
- 团队对新观测体系不熟悉 | 中 | 中 | 培训、文档、运维手册同步交付
- 依赖组件（如otel-collector、Prometheus、ES）异常 | 低 | 高 | 健康检查、报警、降级方案

---

## Milestones & Timeline
- Milestone | Description | Target Date
- 需求澄清与方案评审 | 完成所有artifact并人类审批 | T+2天
- feature分支创建与环境准备 | 按flow.mdc规范建分支、配置观测平台 | T+3天
- 插桩开发与单元测试 | 关键方法插入Activity/metrics，补充测试 | T+7天
- 集成测试与性能回归 | 验证trace/metrics可用性与性能影响 | T+10天
- 监控面板与报警配置 | Prometheus/Jaeger/ES仪表盘与报警规则 | T+12天
- 代码评审与主干合并 | 评审通过后合并dev，准备上线 | T+13天
- 上线与回滚预案 | 灰度发布，监控观测，准备回滚方案 | T+14天
- 项目复盘与文档交付 | 总结经验，完善文档与运维手册 | T+16天

---

## Review Checklist
- [ ] Problem Statement reviewed by human
- [ ] Goals & Success Criteria reviewed by human
- [ ] Stakeholder List reviewed by human
- [ ] Requirements reviewed by human
- [ ] Constraints & Assumptions reviewed by human
- [ ] Solution Sketch reviewed by human
- [ ] Risk Assessment reviewed by human
- [ ] Milestones & Timeline reviewed by human
- [ ] All artifacts approved before implementation

---

## Feature Tracker Artifact
- **Feature Name/ID**: ElasticSearch 性能观测与Tracing增强
- **Status**: planned
- **Owner(s)**: 研发负责人（指定）、运维负责人（指定）
- **Planning Artifacts**:
  - Problem Statement（已生成，待审批）
  - Goals & Success Criteria（已生成，待审批）
  - Stakeholder List（已生成，待审批）
  - Requirements（已生成，待审批）
  - Constraints & Assumptions（已生成，待审批）
  - Solution Sketch（已生成，待审批）
  - Risk Assessment（已生成，待审批）
  - Milestones & Timeline（已生成，待审批）
  - Review Checklist（已生成，待审批）
- **Date of Approval**: _待全部artifact人类审批后填写_
- **Notes/Dependencies**:
  - 需依赖OpenTelemetry、Prometheus、Jaeger、ElasticSearch等观测平台部署与可用性
  - 需遵循flow.mdc与m1.feature-planning.mdc规范
  - 相关文档与监控面板需同步交付 