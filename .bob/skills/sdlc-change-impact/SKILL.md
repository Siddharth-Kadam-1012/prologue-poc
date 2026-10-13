---
name: sdlc-change-impact
description: >-
  Use when a requirement, business rule, API contract, UX flow, or shared
  component has changed — produces a dependency-ordered change plan identifying
  every affected artifact before any edits are made.
---

# SDLC — Change-Impact Analysis

You are acting as the **SDLC Orchestrator** performing a change-impact analysis.

## Inputs

- `$ARGUMENTS` — a description of the change (e.g., "rename field `orderId` to
  `orderRef` in CreateOrder request", "remove the DELETE /orders/{id} endpoint",
  "change the Customer entity to add a required `region` field").
- The affected feature directory (if known).

## Pre-flight checks

1. Read `AGENTS.md`.
2. Identify the current approved state of all relevant artifacts:
   - `specs/<NNN>/spec.md`
   - `specs/<NNN>/contracts/openapi.yaml`
   - `specs/<NNN>/ux/component-map.yaml`
   - `specs/<NNN>/traceability.md`
   - `specs/<NNN>/status.yaml`

## Analysis steps

### Step 1 — Classify the change

| Dimension | Value |
|---|---|
| Change type | requirement / contract / business rule / UX / shared component |
| Breaking? | Yes / No (explain) |
| Risk tier | Low / Medium / High |
| Approval required? | Yes / No |

**High-risk changes** (require human approval before any edits):
- Removes or renames a public API operation or required field.
- Changes authentication or authorization behavior.
- Adds a destructive or irreversible database migration.
- Exposes or reshapes sensitive data.
- Affects shared infrastructure used by multiple features.

### Step 2 — Enumerate affected artifacts

Trace the change through the traceability chain:

```text
Changed item
  └── Requirements  (REQ-XXX-NNN)
       ├── Acceptance criteria  (AC-XXX-NNN)
       ├── API operations  (API-XXX-NNN)
       ├── UX screens  (UX-XXX-NNN)
       ├── Tasks  (TASK-XXX-NNN)
       ├── Tests  (TEST-XXX-NNN / E2E-XXX-NNN)
       └── ADRs / docs
```

For each affected artifact list:
- Current state
- Required change
- Risk of the change to that artifact

### Step 3 — Dependency-ordered change plan

Produce a minimal, ordered list of changes:

```markdown
## Change plan

1. [ ] Update spec.md — section X, requirement REQ-XXX-NNN [Low risk]
2. [ ] Update openapi.yaml — rename field, add deprecation note [High risk — approval needed]
3. [ ] Update component-map.yaml — field reference [Low risk]
4. [ ] Update Handler.cs / Command.cs [Medium risk]
5. [ ] Update / add tests for changed behavior [Low risk]
6. [ ] Update traceability.md [Low risk]
7. [ ] Run `sdlc.cs gate <spec-dir> contract` and `sdlc.cs drift <new> --old=<baseline>` [Gate]
```

### Step 4 — Approval gate (if High-risk)

If any change is High-risk:
```
⚠️  APPROVAL REQUIRED
The following high-risk changes require explicit human approval before any edits:
- <list>
Do not proceed until approval is recorded.
```

## Stop conditions

- Do not begin any edits before producing the full change plan.
- Do not bypass the approval gate for High-risk changes.
- If the impact cannot be fully traced (traceability.md is incomplete), report the gap.

## Outputs

- A change-impact report in the conversation.
- If the change touches an approved artifact, `verify-state` will report that approval as STALE.
  That is intended: re-run the relevant gate, then the maintainer re-approves
  (`set-stage <spec-dir> <stage> in_progress` first revokes the old approval). Never hand-edit `status.yaml`.

## Handoff

After human approval (if required): execute the change plan one step at a time using the relevant skill.
