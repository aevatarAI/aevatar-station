---
Epic: 13. Workflow Error Visibility During Execution
---

# 1. Per-Node Error Indicators on Failure

## User Story
As a workflow designer, I want clear per-node error indicators when a node fails so that I can immediately identify where execution broke and investigate the issue.

**Version:** v0.6

**Estimated Time:** 10 hours

### Acceptance Criteria
**Given** I run a workflow and a node fails
**When** the failure occurs
**Then** the node displays a distinct error state (icon and color) and a tooltip with a short error summary

**Given** a node shows an error state
**When** I hover or focus the node with keyboard navigation
**Then** I can read a concise error summary via tooltip and screen reader labels

**Given** a new run starts or the node is retried successfully
**When** execution proceeds without error
**Then** the node's error indicator clears automatically


