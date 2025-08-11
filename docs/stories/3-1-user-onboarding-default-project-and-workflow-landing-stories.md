---
Epic: 3. User Onboarding: Default Project Creation and Workflow Landing
---

# 1. First-Time Default Project Creation

## User Story
As a newly signed-in user, I want a default project to be created automatically so that I can start using the platform immediately without manual setup.

**Version:** v0.6

**Estimated Time:** 8 hours

### Acceptance Criteria
**Given** I sign in for the first time and have no projects
**When** the authentication flow completes
**Then** a default project is created and owned by me

**Given** project creation might be retried
**When** the creation is triggered multiple times concurrently
**Then** exactly one default project exists for my account

**Given** the default project is created
**When** I am redirected post-auth
**Then** I land on the default project’s Workflow page


# 2. Unique Slug and Name Generation

## User Story
As a user, I want my default project to have a readable name and unique slug so that links are stable and human-friendly.

**Version:** v0.6

**Estimated Time:** 4 hours

### Acceptance Criteria
**Given** a project with the base name exists (e.g., "My First Project")
**When** a new default project is created for another user
**Then** the slug is unique and may use a hash suffix (e.g., "my-first-project-198fh131d")

**Given** multiple projects with similar names
**When** a new slug is generated
**Then** it is unique and URL-safe


# 3. Owner Role and Permissions Initialization

## User Story
As a user, I want to be assigned Owner of my default project so that I have full permissions to manage it.

**Version:** v0.6

**Estimated Time:** 5 hours

### Acceptance Criteria
**Given** a default project is created
**When** the project is persisted
**Then** I am assigned the Owner role with full permissions

**Given** role/ACLs are initialized
**When** I access project administration actions
**Then** they are permitted for Owners and restricted for non-owners


# 4. Seed Starter Workflow

## User Story
As a user, I want a starter workflow to be available in my default project so that I can quickly understand and begin building.

**Version:** v0.6

**Estimated Time:** 6 hours

### Acceptance Criteria
**Given** a default project is created
**When** initialization completes
**Then** a starter workflow (empty canvas or template) exists in the project

**Given** the starter workflow exists
**When** I open the Workflow page
**Then** I can see and edit the starter workflow


# 5. Login Landing Behavior and Precedence

## User Story
As a returning user, I want to land on my last opened project’s Workflow page so that I can continue where I left off; if I have a deep link, it should take precedence.

**Version:** v0.6

**Estimated Time:** 8 hours

### Acceptance Criteria
**Given** I have a `lastOpenedProjectId`
**When** I log in without a deep link
**Then** I am redirected to `/projects/{lastOpenedProjectId}/workflows`

**Given** I log in with a valid `redirect_uri` deep link
**When** post-auth redirect occurs
**Then** the deep link is honored over `lastOpenedProjectId`

**Given** my last opened project is unavailable
**When** I log in without a valid deep link
**Then** I am redirected to the most recently accessed project, or the most recently created project, or a new default project is created and used as a fallback


# 6. Persist Last Opened Project

## User Story
As a user, I want the system to remember my last opened project so that I am redirected to it on future logins.

**Version:** v0.6

**Estimated Time:** 4 hours

### Acceptance Criteria
**Given** I visit a project’s Workflow page
**When** the page loads successfully
**Then** my `lastOpenedProjectId` is updated to that project

**Given** I subsequently log in
**When** the redirect logic runs
**Then** it uses `lastOpenedProjectId` unless overridden by a deep link


# 7. Onboarding UI States and Messaging

## User Story
As a user, I want clear UI during onboarding so that I understand the setup progress and outcomes.

**Version:** v0.6

**Estimated Time:** 6 hours

### Acceptance Criteria
**Given** I log in for the first time
**When** the system is setting up my environment
**Then** I see a loading state with the message "Initialising workspace…"

**Given** my default project is created
**When** I land on the Workflow page
**Then** I see a notice: "We’ve created ‘My First Project’ to get you started."

**Given** auto-creation fails
**When** I land post-auth
**Then** I see a non-blocking error with a one-click retry and a "Create Project" option


# 8. Observability and Auditability

## User Story
As a platform operator, I want metrics and structured logs for onboarding so that I can monitor success rates and diagnose issues.

**Version:** v0.6

**Estimated Time:** 5 hours

### Acceptance Criteria
**Given** users complete first login
**When** onboarding runs
**Then** metrics are emitted for first login, default project creation, and landing success

**Given** onboarding events occur
**When** logs are generated
**Then** structured audit entries exist for `first_login`, `default_project_created`, and `landing_redirected` with correlation IDs


# 9. Concurrency and Idempotency Guarantees

## User Story
As a platform operator, I want onboarding to be safe under concurrency so that duplicate default projects are never created.

**Version:** v0.6

**Estimated Time:** 6 hours

### Acceptance Criteria
**Given** multiple concurrent first login attempts for the same user
**When** project creation is triggered
**Then** only one default project is created

**Given** creation is retried due to transient errors
**When** retries occur
**Then** the operation remains idempotent and does not create duplicates


# 10. Invite/Org Flow and Deleted-Only-Project Edge Cases

## User Story
As a user joining via invite or after deleting my only project, I want onboarding to behave appropriately so that I land on the correct destination without unintended new projects.

**Version:** v0.6

**Estimated Time:** 6 hours

### Acceptance Criteria
**Given** I join via an invite to an existing org/project
**When** I complete authentication
**Then** I am redirected to the invited resource and no default project is auto-created

**Given** I previously deleted my only project
**When** I next log in without other projects
**Then** a new default project is created and I am redirected to its Workflow page


# 11. Feature Flag Control

## User Story
As an administrator, I want to enable or disable onboarding auto-creation per tenant so that I can control rollout safely.

**Version:** v0.6

**Estimated Time:** 4 hours

### Acceptance Criteria
**Given** the feature flag is disabled for a tenant
**When** a new user logs in
**Then** no default project is created and the user lands on the project list with a "Create Project" CTA

**Given** the feature flag is enabled
**When** users log in for the first time
**Then** the default project creation and landing behavior occur as specified


