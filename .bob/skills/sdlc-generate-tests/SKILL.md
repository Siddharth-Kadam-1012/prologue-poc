---
name: sdlc-generate-tests
description: >-
  Use when the user wants to generate or expand the test suite for a feature —
  derives test cases from acceptance criteria, produces unit, integration,
  contract, Playwright, and accessibility tests, and maps coverage to
  requirement IDs.
---

# SDLC — Generate Tests from Acceptance Criteria

You are acting as the **Test Engineer** for this repository.

## Inputs

- `$ARGUMENTS` — feature directory path or feature name/number.
- `specs/<NNN>/spec.md` — source of acceptance criteria.
- `specs/<NNN>/contracts/openapi.yaml` — source of API behavior expectations.
- `specs/<NNN>/ux/user-flows.md` — source of Playwright journey steps.

## Pre-flight checks

1. Read `AGENTS.md` and `.bob/instructions/testing.md`.
2. Inspect the `tests/` directory: identify existing frameworks, fixtures, base classes, auth helpers.
3. Read `specs/<NNN>/traceability.md` to see which ACs are already covered.

## Test generation steps

### Step 1 — Test matrix

For each acceptance criterion (AC-XXX-NNN) produce a test matrix entry:

| AC ID | Scenario | Test type | Status |
|---|---|---|---|
| AC-XXX-001 | Happy path | Integration | Needed |
| AC-XXX-001 | Invalid input | Integration | Needed |
| AC-XXX-001 | Unauthorized | Integration | Needed |
| AC-XXX-002 | E2E journey | Playwright | Needed |

### Step 2 — Unit tests

For each handler / business rule:
- Test valid input → expected result.
- Test invalid / boundary input → expected exception or error result.
- Test authorization checks where the handler enforces them.
- Use the existing test framework and assertion library.

### Step 3 — Integration tests

For each API operation in the OpenAPI contract:
- Use `WebApplicationFactory<TProgram>` with the in-memory **stub data** store (see `.bob/instructions/dotnet.md` § Stub data).
  Replace/reseed the store per test from the deterministic seed data; do not add a database, Testcontainers or SQLite.
- Test: valid request → correct status and response shape.
- Test: invalid input → `400` with `HttpValidationProblemDetails`.
- Test: unauthenticated → `401`.
- Test: insufficient permission → `403`.
- Test: resource absent → `404`.
- Test: conflict condition → `409` (if applicable).

Reuse the project's existing auth test helpers. Never bypass auth middleware.

### Step 4 — Contract tests

Generate a contract test that:
1. Starts the application with `WebApplicationFactory`.
2. Calls every operation in `specs/<NNN>/contracts/openapi.yaml`.
3. Compares actual response schemas against the committed contract.

Reference: `dotnet run scripts/sdlc/sdlc.cs -- drift` for the comparison logic.

### Step 5 — Playwright E2E tests

From `specs/<NNN>/ux/user-flows.md`, for each user flow:
- Write a test that follows the exact steps defined.
- Use `getByRole`, `getByLabel`, `getByText` locators.
- Rely on the seeded stub data for test data; reset it between tests. Assert against seed values, not hard-coded literals that drift.
- Authenticate, run the flow, assert the outcome.
- Add to the existing Playwright config — do not create a second runner.

Template:
```typescript
import { test, expect } from '@playwright/test';

// AC-XXX-NNN: <acceptance criterion text>
test('<AC description>', async ({ page }) => {
  // Arrange: reset/reseed the stub data store
  // Act: navigate and interact
  // Assert: verify visible outcome and resulting stub-store state
});
```

### Step 6 — Accessibility checks

For each HTML wireframe screen, include an accessibility test:
- Use `@axe-core/playwright` or equivalent (verify it's in the project).
- Run `checkA11y` on the populated state of each screen.
- Violations must fail the test — do not suppress them.

### Step 7 — Security tests

For each operation with an authorization requirement:
- Test that an unauthenticated caller gets `401`.
- Test that a caller with insufficient role/claim gets `403`.
- Test boundary inputs (extremely long strings, special characters, SQL injection patterns).
- Test that error responses do not leak sensitive data.

### Step 8 — Update traceability

For each AC now covered:
- Ensure the AC ID appears in the test's name or a comment (coverage is derived from this).
- Add a row to the map in `specs/<NNN>/traceability.md` with the test file path.
- Run `dotnet run scripts/sdlc/sdlc.cs -- gate <spec-dir> traceability` and report the real exit code.
  Do not write "covered"/"verified" for an AC that no test references — the gate rejects it.

## Stub data limits

- Tests run against stub data, so they do **not** prove durability, transactions, concurrency across processes or migrations.
  List these as "not verified (stub data)" in `test-plan.md`; do not claim persistence coverage.

## Stop conditions

- Do not weaken assertions to get a green run.
- Do not delete tests to fix a failing build — investigate the failure.
- Do not claim coverage is sufficient without evidence.

## Outputs

- Test files in `tests/` (appropriate project).
- Updated `specs/<NNN>/traceability.md`.
- `specs/<NNN>/test-plan.md` (test matrix).

## Handoff

Next: `/sdlc-converge-feature`
