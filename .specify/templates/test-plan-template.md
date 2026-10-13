---
description: "Test plan template — maps acceptance criteria to test coverage"
---

# Test Plan: [FEATURE NAME]

**Feature:** [NNN]-[feature-name]
**Created:** [DATE]
**Status:** Draft

---

## Test matrix

Derive all rows from `specs/<NNN>/spec.md` acceptance criteria.

| AC-ID | Scenario | Test type | Framework | File | Status |
|---|---|---|---|---|---|
| AC-XXX-001 | Valid request → 201 | Integration | xUnit + WebApplicationFactory | tests/.../EndpointTests.cs | ⬜ Needed |
| AC-XXX-001 | Missing required field → 400 | Integration | xUnit + WebApplicationFactory | tests/.../EndpointTests.cs | ⬜ Needed |
| AC-XXX-001 | Unauthenticated → 401 | Integration | xUnit + WebApplicationFactory | tests/.../EndpointTests.cs | ⬜ Needed |
| AC-XXX-001 | Insufficient role → 403 | Integration | xUnit + WebApplicationFactory | tests/.../EndpointTests.cs | ⬜ Needed |
| AC-XXX-002 | Business rule enforced | Unit | xUnit | tests/.../HandlerTests.cs | ⬜ Needed |
| AC-XXX-003 | User journey end-to-end | Playwright | Playwright | tests/e2e/...spec.ts | ⬜ Needed |
| AC-XXX-004 | Accessibility (WCAG AA) | Playwright + axe | @axe-core/playwright | tests/e2e/...spec.ts | ⬜ Needed |

---

## Contract tests

| API-ID | Operation | Check | Status |
|---|---|---|---|
| API-XXX-001 | POST /x | Response schema matches openapi.yaml | ⬜ Needed |
| API-XXX-002 | GET /x | Pagination envelope present | ⬜ Needed |

---

## Security tests

| Threat | Test | Status |
|---|---|---|
| Unauthenticated access | Call without token → 401 | ⬜ Needed |
| Unauthorized access | Call with wrong role → 403 | ⬜ Needed |
| SQL injection | Pass payload `' OR 1=1--` | ⬜ Needed |
| XSS (if frontend) | Render `<script>alert(1)</script>` in field | ⬜ Needed |
| Sensitive data in error | 500 response body inspection | ⬜ Needed |

---

## Test data requirements

- **Users:** at least one per role required by the feature.
- **Seed data:** minimal data set to exercise all states (empty, populated, conflict).
- **Cleanup:** all created records must be deleted/rolled back after each test run.
- **Isolation:** tests must not depend on execution order.

---

## Skipped tests

Document any test that is explicitly skipped, with the reason.

| AC-ID | Test | Skip reason | Target date |
|---|---|---|---|

---

## Coverage status

Run `scripts/validate-traceability.ps1 -SpecDir specs/<NNN>` to regenerate coverage metrics.
