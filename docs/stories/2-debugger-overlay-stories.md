---
Epic: 2. Debugger Overlay (Read-Only State Inspection)
---

# 1. Debugger Overlay for Agent State Inspection

## Overview
Enables authenticated users to inspect the agent's state (inputs, outputs, intermediate data) after workflow execution in a read-only overlay, supporting troubleshooting and validation without allowing state modification.

### User Story
As an authenticated user, I want to enable a debug mode in the workflow designer that overlays a read-only view of agent state (including inputs, outputs, and intermediate data) after execution, so that I can troubleshoot and validate workflow behavior without risk of accidental changes.

**Time Estimate: 24 hours**

### Key Features
- Debug mode toggle in the workflow designer.
- Overlay displays agent state (inputs, outputs, intermediate data) post-execution.
- No step-through or breakpoints; read-only inspection only.
- Access restricted to authenticated users.
- Clear indication when in debug mode.
- Debug mode shows clearly the timeline of execution.

### Acceptance Criteria
1. Only authenticated users can access the debug overlay.
2. Users can view state snapshots for each node after execution.
3. No ability to modify state from the overlay.
4. Execution sequence of agents is shown in the overlay.
5. Debug mode is clearly indicated in the UI.
6. Debug mode can be toggled on/off from the workflow designer. 