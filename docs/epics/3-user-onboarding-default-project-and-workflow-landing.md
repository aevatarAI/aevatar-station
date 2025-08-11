## 3. User Onboarding: Default Project Creation and Workflow Landing

**Objective:**
Ensure new users can start immediately by automatically creating a default project on their first successful sign-in and consistently landing them on a project’s Workflow page on subsequent logins.

### Key Requirements

- **First-Time Default Project Creation**
  - Trigger on the user’s first successful authentication (idempotent; safe to retry).
  - Default project naming: "My First Project" or "{FirstName}’s First Project" with a unique slug. Collisions resolved via numeric suffix (e.g., "my-first-project-2").
  - Assign the user as **Owner** with full permissions; initialize default roles/ACLs.
  - Seed a starter workflow (either an empty canvas or a "Getting Started" template).
  - Store onboarding metadata on the user profile: `firstLoginAt`, `hasDefaultProject`.
  - Multi-tenant/org-aware: if user joins via invite to an org/project, skip auto-creation.
  - Wrapped in a feature flag: `onboarding.autoCreateDefaultProject`.

- **Login Landing Behavior**
  - First-time sign-in: redirect to the newly created project’s Workflow page.
  - Returning users: redirect to the last opened project’s Workflow page.
  - Fallback order if last-opened is missing: most recently accessed project → most recently created project → create default project (if none exist) and redirect.
  - Respect explicit redirect intent (e.g., `redirect_uri` query param from invites or deep links) with higher precedence than default behavior.
  - URL format: `/projects/{projectId}/workflows` (append workflow identifier if applicable).

- **Backend/Services**
  - Service API: `OnboardingService.CreateDefaultProjectIfMissing(userId)` called during post-auth callback or first session initialization.
  - Persistence: `UserProfile.lastOpenedProjectId`, updated on project/workflow page visits.
  - Audit/Activity log entries: `first_login`, `default_project_created`, `landing_redirected`.
  - Ensure transactional creation: Project + initial Workflow are created atomically; partial failures roll back.
  - Telemetry: metrics and traces for creation latency and redirect success rate.

- **Data Model/Config**
  - Project: `Id`, `OwnerUserId`, `Name`, `Slug`, `CreatedAt`, `UpdatedAt`.
  - Workflow (seed): `Id`, `ProjectId`, `Name` (e.g., "Getting Started"), `Definition` (empty or template), `CreatedAt`.
  - UserProfile: `UserId`, `firstLoginAt`, `hasDefaultProject`, `lastOpenedProjectId`.
  - Feature flag: `onboarding.autoCreateDefaultProject` (default ON for new tenants; configurable).

- **UI/UX**
  - Post-login loading/transition state with clear messaging: "Setting up your first project…" (≤2s typical).
  - Toast/inline notice on first arrival: "We’ve created ‘My First Project’ to get you started."
  - If auto-creation fails, show a non-blocking error with a one-click retry and a manual "Create Project" option.

- **Security & Permissions**
  - Ensure new project inherits baseline org policies (if applicable).
  - Enforce that only the authenticated user becomes Owner; no unintended shared access.
  - Comply with invite/SSO flows: if user enters via invite, route to invited resource and suppress default project creation.

- **Scalability & Reliability**
  - Idempotent creation guarded by a unique constraint on `(OwnerUserId, isDefaultProject)` or a strong existence check.
  - Handle concurrent logins (multi-device) using optimistic concurrency or distributed locks.
  - Budget: median creation + redirect ≤ 2s; P95 ≤ 5s.

- **Observability**
  - Emit metrics: `onboarding.first_login.count`, `onboarding.default_project.created.count`, `onboarding.landing.success.count`.
  - Log structured events with correlation IDs spanning auth callback → project creation → redirect.
  - Add dashboards and alerts for creation failures and slow paths.

- **Edge Cases**
  - User previously deleted their only project: recreate on next login and redirect.
  - Disabled feature flag: no auto-creation; land on project list with "Create Project" CTA.
  - Auth provider delays (SSO/JIT provisioning): defer creation until user record is fully provisioned.

### Acceptance Criteria

- **Default project is created exactly once** per new user (unless the user joins via an invite), and the user is assigned Owner.
- **First login** lands the user on `/projects/{projectId}/workflows` for the default project with a visible confirmation.
- **Subsequent logins** land on the last opened project’s Workflow page; if unavailable, follow the defined fallback order.
- **Idempotency and concurrency** verified by tests simulating repeated and concurrent first-logins without duplicate projects.
- **Observability**: metrics and audit logs are produced for creation and redirect events; creation+redirect median latency ≤ 2s.
- **Feature flag** can enable/disable behavior at tenant or environment level without redeploy.

### Dependencies

- Authentication/SSO, User Profile service, Project service, Workflow service, Feature flag/config service, Telemetry/Logging.

### Rollout Plan

- Phase 1: Behind feature flag for internal/testing tenants; validate latency and success metrics.
- Phase 2: Gradual enablement across tenants; monitor dashboards and error budgets.
- Phase 3: Default ON for new tenants; provide tenant-level override.

### Open Questions

- Should the seeded workflow be empty or a guided template by default? Make configurable per tenant?
- What is the exact precedence between `redirect_uri` deep links vs. `lastOpenedProjectId` in all auth flows?
- Should we support multiple default projects in org contexts (e.g., personal vs. org space)?


