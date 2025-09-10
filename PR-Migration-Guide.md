# Direct PR Migration Guide

## Overview

This guide explains how to migrate individual Pull Requests from the **aevatar-gagents** repository into the **aevatar-station** repository after the subtree merge has been completed.

## Prerequisites

- ✅ The aevatar-gagents repository has been merged as a subtree in the `gagents/` directory
- ✅ The `gagents` remote is configured: `https://github.com/aevatarAI/aevatar-gagents.git`
- ✅ You have access to both repositories

## Current Status

- **Source Repository**: [aevatar-gagents](https://github.com/aevatarAI/aevatar-gagents) 
- **Target Directory**: `gagents/` in aevatar-station
- **Open PRs**: 11 PRs ready for migration
- **Remote Name**: `gagents`

## Migration Process

### Step 1: List Available PRs

First, check what PRs are available in the source repository:

```bash
# View open PRs (using GitHub CLI if available)
gh pr list --repo aevatarAI/aevatar-gagents

# Or visit: https://github.com/aevatarAI/aevatar-gagents/pulls
```

### Step 2: Fetch PR Branch

For each PR you want to migrate, fetch the PR branch locally:

```bash
# Fetch specific PR by number (replace PR_NUMBER with actual number)
git fetch gagents pull/PR_NUMBER/head:pr-PR_NUMBER

# Example: For PR #25
git fetch gagents pull/25/head:pr-25
```

### Step 3: Create Migration Branch

Create a new branch for the migrated PR:

```bash
# Create and checkout new branch
git checkout dev  # or your base branch
git checkout -b feature/migrated-pr-PR_NUMBER

# Example:
git checkout -b feature/migrated-pr-25
```

### Step 4: Apply PR Changes via Subtree

Use git subtree to pull the PR changes into your gagents directory:

```bash
# Pull PR changes into gagents/ subdirectory
git subtree pull --prefix=gagents gagents pr-PR_NUMBER

# Example:
git subtree pull --prefix=gagents gagents pr-25
```

### Step 5: Verify and Test

```bash
# Check what files were changed
git diff HEAD~1 --name-only

# Verify the changes are in the gagents/ directory
git diff HEAD~1 --stat gagents/

# Build and test if applicable
dotnet build gagents/
dotnet test gagents/
```

### Step 6: Create New PR

```bash
# Push the migrated branch
git push origin feature/migrated-pr-PR_NUMBER

# Create PR using GitHub CLI or web interface
gh pr create --title "Migrate PR #PR_NUMBER: [Original PR Title]" \
             --body "Migrated from aevatarAI/aevatar-gagents#PR_NUMBER"
```

## Batch Migration Script

For migrating multiple PRs efficiently:

```bash
#!/bin/bash
# migrate-prs.sh

PR_NUMBERS=(25 26 27)  # Replace with actual PR numbers

for pr_num in "${PR_NUMBERS[@]}"; do
    echo "Migrating PR #$pr_num..."
    
    # Fetch PR branch
    git fetch gagents pull/$pr_num/head:pr-$pr_num
    
    # Create migration branch
    git checkout dev
    git checkout -b feature/migrated-pr-$pr_num
    
    # Apply PR changes
    git subtree pull --prefix=gagents gagents pr-$pr_num
    
    # Push branch
    git push origin feature/migrated-pr-$pr_num
    
    echo "✅ PR #$pr_num migration branch created"
done
```

Make it executable and run:
```bash
chmod +x migrate-prs.sh
./migrate-prs.sh
```

## Advanced Scenarios

### Migrating Specific Commits

If you only want specific commits from a PR:

```bash
# Fetch the PR branch
git fetch gagents pull/PR_NUMBER/head:pr-PR_NUMBER

# View commits in the PR
git log gagents/dev..pr-PR_NUMBER --oneline

# Cherry-pick specific commits into subtree
git checkout feature/migrated-pr-PR_NUMBER
git subtree pull --prefix=gagents gagents COMMIT_HASH
```

### Resolving Merge Conflicts

If conflicts occur during subtree pull:

```bash
# View conflict status
git status

# Resolve conflicts in gagents/ directory files
# Edit conflicted files manually

# After resolving conflicts
git add gagents/
git commit -m "Resolve merge conflicts for PR #PR_NUMBER"
```

### Updating Existing Migrated PRs

To sync a migrated PR with upstream changes:

```bash
# Checkout existing migration branch
git checkout feature/migrated-pr-PR_NUMBER

# Pull latest changes from the source PR
git subtree pull --prefix=gagents gagents pr-PR_NUMBER
```

## Best Practices

### 1. **Consistent Naming Convention**
- Use format: `feature/migrated-pr-{PR_NUMBER}`
- Include original PR number in commit messages
- Reference original PR in new PR description

### 2. **Preserve Attribution**
- Mention original PR author in new PR description
- Link back to original PR: `aevatarAI/aevatar-gagents#PR_NUMBER`
- Preserve original commit messages where possible

### 3. **Testing Strategy**
```bash
# Test before creating PR
dotnet build gagents/
dotnet test gagents/

# Run station-wide tests if changes affect integration
dotnet build
dotnet test
```

### 4. **Documentation Updates**
- Update relevant documentation in `gagents/` if needed
- Ensure README files reflect the new structure
- Update any references to the old repository structure

## Troubleshooting

### Common Issues

**Issue**: `fatal: refusing to merge unrelated histories`
```bash
# Solution: Use --allow-unrelated-histories
git subtree pull --prefix=gagents gagents pr-PR_NUMBER --allow-unrelated-histories
```

**Issue**: `Working tree has modifications`
```bash
# Solution: Commit or stash changes first
git stash
git subtree pull --prefix=gagents gagents pr-PR_NUMBER
git stash pop
```

**Issue**: `prefix 'gagents' already exists`
```bash
# This shouldn't happen in migration, but if it does:
git subtree pull --prefix=gagents gagents pr-PR_NUMBER
# (use pull instead of add)
```

## Verification Checklist

After each migration:

- [ ] PR changes are correctly placed in `gagents/` directory
- [ ] No changes outside `gagents/` directory (unless intended)
- [ ] Code builds successfully
- [ ] Tests pass
- [ ] Original functionality is preserved
- [ ] New PR created with proper references
- [ ] Original PR author is credited

## Integration with Existing Workflow

Update your existing subtree workflow in `station/README.md`:

```bash
# Regular updates (add to existing workflow)
git subtree pull --prefix=gagents gagents dev --squash

# After PR merges in source repo, sync here:
git subtree pull --prefix=gagents gagents dev --squash
```

## Monitoring Progress

Keep track of migrated PRs:

| Original PR | Status | Migration Branch | New PR | Notes |
|-------------|--------|-----------------|--------|--------|
| #25 | ✅ Migrated | feature/migrated-pr-25 | #1234 | Completed |
| #26 | 🔄 In Progress | feature/migrated-pr-26 | - | Testing |
| #27 | ⏳ Pending | - | - | Waiting for dependencies |

## Future Maintenance

Once all PRs are migrated:

1. **Keep the remote active** for future updates
2. **Regularly sync** with upstream changes:
   ```bash
   git subtree pull --prefix=gagents gagents dev --squash
   ```
3. **Monitor new PRs** in the source repository
4. **Consider archiving** the source repository once fully integrated

## Contact & Support

- Original repository: [aevatar-gagents](https://github.com/aevatarAI/aevatar-gagents)
- Current integration: `gagents/` subtree in aevatar-station
- Remote configuration: `gagents` -> `https://github.com/aevatarAI/aevatar-gagents.git`

---

*This guide was created after successfully merging aevatar-gagents repository as a subtree on $(date). Last updated: $(date)*
