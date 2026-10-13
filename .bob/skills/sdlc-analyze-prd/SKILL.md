---
name: sdlc-analyze-prd
description: >-
  Use when the user wants to analyze a PRD or feature description and produce a
  structured, reviewable requirement set with stable IDs, acceptance criteria,
  assumptions, open questions, and a SpecKit-ready spec.md.
---

# SDLC — Analyze PRD

You are acting as the **PRD Analyst** for this repository.

## Inputs

Required (provided via `$ARGUMENTS` or the current conversation):
- Path to the PRD file **or** an inline feature description.

## Pre-flight checks

1. Read `AGENTS.md` and `.bob/instructions/` to understand repository conventions.
2. Check `git status` to avoid overwriting unrelated changes.
3. Identify the next available feature number: run `.specify/scripts/powershell/create-new-feature.ps1 -DryRun "<feature description>"` to determine the feature directory path.

## Analysis steps

### Step 1 — Create the feature directory

Run:
```powershell
.specify/scripts/powershell/create-new-feature.ps1 "<feature description>"
```
Record the feature directory path as `$FEATURE_DIR`.

### Step 2 — Classify every requirement

For each item in the PRD, determine:

| Category | Label |
|---|---|
| Explicitly stated | (none — write as a plain requirement) |
| Reasonable inference | `[INFERENCE]` |
| Requires approval | `[ASSUMPTION: <rationale>]` |
| Requires clarification | `[QUESTION: <question text>]` |
| Out of scope | Record in an "Out of scope" section |
| NFR | Tag with `[NFR]` |
| Failure / recovery / permission | Tag with `[FAILURE]`, `[RECOVERY]`, `[PERMISSION]` |

### Step 3 — Assign stable IDs

Use the pattern `REQ-<DOMAIN>-<NNN>` for requirements and `AC-<DOMAIN>-<NNN>` for acceptance criteria.
`<DOMAIN>` is a 2-4 letter abbreviation of the feature's domain (e.g., `ORD` for orders).
IDs must never be reused for a different requirement.

### Step 4 — Write spec.md

Extend the SpecKit spec template (`.specify/templates/spec-template.md`) with the following additional sections:

```markdown
## PRD Analysis

| ID | Category | Statement |
|---|---|---|
| PRD-XXX-001 | Requirement | ... |

## Actors and personas
...

## Business goals and outcomes
...

## Functional requirements
| ID | Requirement | Priority |
|---|---|---|
| REQ-XXX-001 | ... | P1 |

## Non-functional requirements
| ID | NFR | Target |
|---|---|---|

## Business rules
...

## Acceptance criteria
| ID | Linked Req | Given | When | Then |
|---|---|---|---|---|
| AC-XXX-001 | REQ-XXX-001 | ... | ... | ... |

## Open questions and assumptions
| ID | Type | Description | Impact |
|---|---|---|---|
| Q-001 | QUESTION | ... | Blocks contract design |
| A-001 | ASSUMPTION | ... | Low risk |

## Out of scope
...

## Dependencies and constraints
...

## Risk register
| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
```

### Step 5 — Specification gate check

Evaluate whether **any** of the following block progression:
- Critical ambiguity in security, data integrity, authorization, business rules, or public API behavior.
- A required actor or key business rule is completely undefined.

If a blocker is found: output a **GATE BLOCKED** notice listing the specific questions that must be answered before proceeding.

Otherwise: output **SPECIFICATION GATE: PASSED** and list the next recommended step.

## Stop conditions

- Do not proceed to plan or implementation if critical questions remain unresolved.
- Do not invent business policy to fill gaps — record them as `[QUESTION]`.

## Outputs

- `$FEATURE_DIR/spec.md` — structured specification.
- Gate status reported in the conversation.

## Handoff

Next step: `/speckit-clarify` (if questions remain) **or** `/sdlc-contracts` + `/sdlc-ux` (if the spec is approved).
