# Aevatar 平台 0.6 版本

## 概述
0.6 版本专注于用户引导默认设置和设计器可用性改进，包括自动项目创建、首次登录引导流程，以及节点参数的丰富选项选择。

## 包含功能

### 1. 用户引导：默认项目创建和工作流引导页面
**史诗参考：** 
- [1-node-visualizer-specifications.md](../epics/1-node-visualizer-specifications.md#9-automatic-project-creation--user-onboarding)
- [3-user-onboarding-default-project-and-workflow-landing.md](../epics/3-user-onboarding-default-project-and-workflow-landing.md)

**用户故事 (v0.6)：**
- 首次默认项目创建 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#1-first-time-default-project-creation)
- 唯一标识符和名称生成 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#2-unique-slug-and-name-generation)
- 所有者角色和权限初始化 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#3-owner-role-and-permissions-initialization)
- 种子启动器工作流 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#4-seed-starter-workflow)
- 登录引导行为和优先级 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#5-login-landing-behavior-and-precedence)
- 引导界面状态和消息 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#6-onboarding-ui-states-and-messaging)
- 可观测性和审计性 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#7-observability-and-auditability)
- 并发和幂等性保证 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#8-concurrency-and-idempotency-guarantees)
- 邀请/组织流程和已删除项目边缘情况 — [3-1-user-onboarding-default-project-and-workflow-landing-stories.md](../stories/3-1-user-onboarding-default-project-and-workflow-landing-stories.md#9-inviteorg-flow-and-deleted-only-project-edge-cases)

组织创建时的额外引导功能：
- 自动默认项目创建 — [1-9-automatic-project-creation-stories.md](../stories/1-9-automatic-project-creation-stories.md#1-automatic-default-project-creation)
- 用户导航到工作流仪表板 — [1-9-automatic-project-creation-stories.md](../stories/1-9-automatic-project-creation-stories.md#2-user-navigation-to-workflow-dashboard)
- 默认项目权限和配置 — [1-9-automatic-project-creation-stories.md](../stories/1-9-automatic-project-creation-stories.md#3-default-project-permissions-and-configuration)
- 错误恢复和降级机制 — [1-9-automatic-project-creation-stories.md](../stories/1-9-automatic-project-creation-stories.md#4-error-recovery-and-fallback-mechanisms)

### 2. 节点输入选项显示和选择
**史诗参考：** [1-node-visualizer-specifications.md](../epics/1-node-visualizer-specifications.md#11-node-input-option-display--selection)

**用户故事 (v0.6)：**
- 基础参数选项显示 — [1-11-node-input-option-display-stories.md](../stories/1-11-node-input-option-display-stories.md#1-basic-parameter-option-display)
- AI 模型选择界面 — [1-11-node-input-option-display-stories.md](../stories/1-11-node-input-option-display-stories.md#2-ai-model-selection-interface)
- 选项搜索和过滤 — [1-11-node-input-option-display-stories.md](../stories/1-11-node-input-option-display-stories.md#3-option-search-and-filtering)
- 实时选项验证 — [1-11-node-input-option-display-stories.md](../stories/1-11-node-input-option-display-stories.md#4-real-time-option-validation)
- 选项描述和元数据 — [1-11-node-input-option-display-stories.md](../stories/1-11-node-input-option-display-stories.md#5-option-descriptions-and-metadata)

### 3. 工作流执行期间的错误可见性
**史诗参考：** 史诗 13：工作流执行期间的错误可见性

**用户故事 (v0.6)：**
- 节点失败时的错误指示器 — [1-13-workflow-error-visibility-during-execution-stories.md](../stories/1-13-workflow-error-visibility-during-execution-stories.md#1-per-node-error-indicators-on-failure)

