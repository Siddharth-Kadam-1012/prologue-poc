---
description: "Validation report template for the convergence / quality review stage"
---

# Validation Report: [FEATURE NAME]

**Feature:** [NNN]-[feature-name]
**Reviewed by:** Quality Reviewer agent
**Date:** [DATE]

---

## Gate results

| Gate | Status | Evidence | Notes |
|---|---|---|---|
| Specification | ⬜ Pending | — | — |
| Contract | ⬜ Pending | — | — |
| Architecture | ⬜ Pending | — | — |
| Build | ⬜ Pending | — | — |
| Tests | ⬜ Pending | — | — |
| Contract drift | ⬜ Pending | — | — |
| Traceability | ⬜ Pending | — | — |
| Security | ⬜ Pending | — | — |
| Scope | ⬜ Pending | — | — |
| Review | ⬜ Pending | — | — |

Gate status values: `✅ PASSED` · `❌ FAILED` · `⚠️ CONDITIONAL` · `⬜ Pending` · `⏭️ Skipped`

---

## Commands executed

| Command | Exit code | Run at | Output artifact |
|---|---|---|---|
| `dotnet restore` | — | — | — |
| `dotnet build --no-restore -warnaserror` | — | — | — |
| `dotnet format --verify-no-changes` | — | — | — |
| `dotnet test --no-build --logger trx` | — | — | TestResults.trx |
| `scripts/validate-openapi.ps1` | — | — | — |
| `scripts/check-contract-drift.ps1` | — | — | — |
| `scripts/validate-traceability.ps1` | — | — | — |
| `scripts/validate-workflow-state.ps1` | — | — | — |

---

## Findings (by severity)

| Severity | Gate | Finding | File / location | Resolution |
|---|---|---|---|---|
| Critical | — | — | — | — |
| High | — | — | — | — |
| Medium | — | — | — | — |
| Low | — | — | — | — |

Severity: `Critical` (blocks PR) · `High` (blocks PR unless justified) · `Medium` (follow-up task) · `Low` (informational)

---

## Missing coverage

| AC-ID | Description | Status |
|---|---|---|
| — | — | — |

---

## Architecture violations

| Rule | Violation | File | Disposition |
|---|---|---|---|
| — | — | — | — |

---

## Security observations

| Severity | Finding | Disposition |
|---|---|---|
| — | — | — |

---

## Out-of-scope changes detected

| File | Change | Justification |
|---|---|---|
| — | — | — |

---

## High-risk decisions and approvals

| Decision | Approved by | Date | Evidence |
|---|---|---|---|
| — | — | — | — |

---

## Recommendation

> **[ ] PASS** — All gates passed, no blocking findings.
>
> **[ ] CONDITIONAL PASS** — Conditions must be resolved before merge:
> - [ ] Condition 1
>
> **[ ] FAIL** — Blocking findings must be fixed before re-review:
> - Blocker 1

---

## Next steps

- [ ] [Action]
