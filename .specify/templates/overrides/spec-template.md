# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`
**Created**: [DATE]
**Status**: Draft
**Input**: "$ARGUMENTS"

---

## PRD Analysis

Categorize every PRD item before writing requirements.
Use `[INFERENCE]`, `[ASSUMPTION: <rationale>]`, `[QUESTION: <text>]`, `[NFR]` labels.

| PRD-ID | Category | Statement |
|---|---|---|
| PRD-XXX-001 | Requirement | ... |

---

## Actors and personas

| Persona | Description | Primary goal |
|---|---|---|
| ... | ... | ... |

---

## Business goals and outcomes

- **Goal 1:** ...
- **Goal 2:** ...

---

## User scenarios *(mandatory)*

User stories should be prioritized as user journeys ordered by importance.
Each must be independently testable — implementing just one should deliver a
viable MVP increment.

### User Story 1 — [Brief Title] (Priority: P1)

[Describe the journey in plain language]

**Why P1:** [Value and rationale]
**Independent test:** [What to do and observe to verify this story in isolation]

**Acceptance scenarios:**

| AC-ID | Given | When | Then |
|---|---|---|---|
| AC-XXX-001 | ... | ... | ... |
| AC-XXX-002 (negative) | ... | ... | ... |

---

### User Story 2 — [Brief Title] (Priority: P2)

[Describe the journey]

**Acceptance scenarios:**

| AC-ID | Given | When | Then |
|---|---|---|---|
| AC-XXX-003 | ... | ... | ... |

---

## Functional requirements

| ID | Requirement | Priority | Source |
|---|---|---|---|
| REQ-XXX-001 | System MUST ... | P1 | PRD-XXX-001 |
| REQ-XXX-002 | System MUST ... | P2 | PRD-XXX-002 |

---

## Non-functional requirements

| ID | NFR | Target | Measurement |
|---|---|---|---|
| NFR-XXX-001 | Response time | p95 < 200 ms | Load test |
| NFR-XXX-002 | Availability | 99.9% | Uptime monitor |

---

## Business rules

- **BR-001:** ...
- **BR-002:** ...

---

## Edge cases and failure scenarios

- What happens when [boundary condition]?
- How does the system handle [error scenario]?
- What happens when [concurrent operation]?
- What does an unauthorized caller see?

---

## API operations required

| API-ID | Operation | Method + Path | Auth | Linked requirements |
|---|---|---|---|---|
| API-XXX-001 | Create X | POST /x | Bearer | REQ-XXX-001 |

---

## UX screens required

| UX-ID | Screen | Route | Linked requirements |
|---|---|---|---|
| UX-XXX-001 | ... | /... | REQ-XXX-001 |

---

## Open questions and assumptions

| ID | Type | Description | Impact | Status |
|---|---|---|---|---|
| Q-001 | [QUESTION] | ... | Blocks contract | ❌ Unresolved |
| A-001 | [ASSUMPTION] | ... | Low risk | ✅ Approved |

---

## Out of scope

- ...

---

## Dependencies and constraints

- **Dependency:** ...
- **Constraint:** ...

---

## Success criteria (measurable)

| SC-ID | Criterion | How measured |
|---|---|---|
| SC-001 | ... | ... |

---

## Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| ... | Low/Med/High | Low/Med/High | ... |
