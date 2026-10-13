# Testing — Rules

## Test project layout
Inspect the repository before assuming layout. Common patterns:
- `tests/<ProjectName>.UnitTests/`
- `tests/<ProjectName>.IntegrationTests/`
- `tests/<ProjectName>.ContractTests/`
- `tests/<ProjectName>.E2ETests/` (Playwright)

Use the existing project structure; do not create a second test project that duplicates an existing one.

## Test framework
- Prefer **xUnit** unless NUnit is already in use.
- Use **FluentAssertions** for readability if already present; do not add it solely as a new dependency without a reason.
- Use **Bogus** or equivalent for test data generation if already present.

## Test type boundaries

| Type | What it tests | Isolation |
|---|---|---|
| Unit | Business rules, validators, domain logic, mapping | In-process, no I/O |
| Integration | HTTP pipeline, DI, real DB (test container or in-memory), auth | `WebApplicationFactory` + real infrastructure |
| Contract | OpenAPI schema validity and API conformance | `WebApplicationFactory` + live response comparison |
| Component/UI | Frontend component behavior and state | Framework test runner (e.g., Vitest + Testing Library) |
| Browser E2E | High-value user journeys | Playwright against the running app |
| Security | Auth matrix, boundary inputs, data exposure | Integration tests + scanner |

## Deriving test cases
- Start from the **acceptance criteria** in `specs/<NNN>/spec.md`, not from the implementation.
- Cover: valid path, invalid input, unauthorized, forbidden, resource not found, conflict, server error.
- Explicitly record acceptance criteria that do not yet have a test: mark them in `specs/<NNN>/traceability.md`.

## Integration tests
- Use `WebApplicationFactory<TProgram>` with the real DI container.
- Use a real test database (Testcontainers preferred) or `SQLite` in-memory if approved and sufficient.
- Authenticate requests using the app's actual auth middleware — not by bypassing it.
- Clean up data after each test. Use a unique test-database or per-test transaction rollback.

## Contract tests
- Run `scripts/validate-openapi.ps1` to check schema validity.
- Compare the running app's generated OpenAPI against the committed contract using `scripts/check-contract-drift.ps1`.
- Fail the build if drift is detected.

## Playwright
- Use accessible locators: `getByRole`, `getByLabel`, `getByText` — not CSS selectors or XPath.
- Each test must: set up its own data, authenticate if required, clean up after itself.
- Use `page.waitForSelector` or Playwright's auto-wait — never `page.waitForTimeout`.
- Capture traces, screenshots, and videos on failure (`playwright.config.ts` `use.trace: 'on-first-retry'`).
- Reuse existing Playwright fixtures for authentication.

## Test data
- Generate test data programmatically; do not rely on production or manually seeded data.
- Use a test-data builder or factory pattern.
- Tests must not depend on execution order.

## Coverage
- Coverage thresholds are one signal, not proof. Use them as a floor, not a ceiling.
- Mutant testing (Stryker.NET) is recommended for critical business rules.
- Report untested acceptance criteria in `specs/<NNN>/traceability.md`.

## Prohibited patterns
- No `Thread.Sleep` or `Task.Delay` in tests — use proper waits.
- No test-order dependencies.
- No broad `catch` that swallows assertion failures.
- No `Assert.True(result != null)` — use specific assertions.
- No weakening of assertions to force a green run.
- No deleting tests to fix a failing build.
