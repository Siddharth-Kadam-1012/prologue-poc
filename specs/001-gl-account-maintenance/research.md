# Research: GL Account Maintenance and Inquiry

All technical unknowns were resolved from repository policy (`AGENTS.md`, `.bob/instructions/`) and the
approved spec. Nothing here required external sources; library facts below must be confirmed by a successful
restore when the slice that uses them is built.

## R-01 Runtime and framework

- **Decision**: .NET 10, ASP.NET Core 10 minimal APIs, built-in OpenAPI support.
- **Rationale**: `AGENTS.md` states .NET 10 as the target intent; SDK 10.0.401 is installed; `.bob/instructions/api-contracts.md` points to the built-in `Microsoft.AspNetCore.OpenApi`.
- **Alternatives**: MVC controllers (heavier, no benefit for thin vertical-slice endpoints); .NET 8 (SDK 8.0.425 is also installed, but `AGENTS.md` says new projects target 10).

## R-02 Data layer

- **Decision**: in-memory, thread-safe stub stores seeded with synthetic data, behind small feature-specific interfaces.
- **Rationale**: `AGENTS.md` and `.bob/instructions/dotnet.md` forbid a database, ORM, migrations or connection strings in this phase. Spec A-001, Q-008, Q-014.
- **Alternatives**: SQLite or Testcontainers (explicitly forbidden by `testing.md` in this phase); generic `IRepository<T>` (forbidden by `vertical-slices.md`).

## R-03 Validation approach

- **Decision**: plain validator classes per slice returning field-keyed errors mapped to `HttpValidationProblemDetails`.
- **Rationale**: no validation library exists in the repository, and `dotnet.md` says to use whichever is already present; the rules (segment format, category-by-type, currency list, justification length) are few and specific. Avoids a new dependency.
- **Alternatives**: FluentValidation (new dependency, no clear gain here); Data Annotations (cannot express cross-field rules such as category-by-type or statistical constraints cleanly).

## R-04 Optimistic concurrency

- **Decision**: `ETag` returned on account reads, `If-Match` required on update, stale value returns 409.
- **Rationale**: Q-013 requires optimistic concurrency with a conflict response; `api-contracts.md` asks for the ETag/If-Match pattern. Using 409 matches the spec wording ("conflict response"); HTTP convention would use 412, which can be chosen at the contract stage if preferred.
- **Alternatives**: version field in the body (works, but hides the concurrency token from HTTP caches and tooling); last-write-wins (rejected by Q-013).

## R-05 Excel export

- **Decision**: ClosedXML for the single `.xlsx` export (period summary).
- **Rationale**: needed only for REQ-GLA-019. ClosedXML is MIT licensed and builds on the OpenXML SDK with a small API.
- **Alternatives**: EPPlus (commercial license terms since version 5, avoided); raw `DocumentFormat.OpenXml` (no extra abstraction, but more code for a simple table); CSV with an `.xls` name (misleading, rejected).
- **Open check**: confirm the package restores and its current license when slice S05 is built; if it cannot, fall back to `DocumentFormat.OpenXml` and record the change in the plan.

## R-06 CSV export

- **Decision**: hand-written CSV writer with proper quoting and a header row, no package.
- **Rationale**: two simple tables (groups, transactions). Q-009. An empty result returns headers only (spec edge cases).
- **Alternatives**: CsvHelper (new dependency for trivial output).

## R-07 OpenAPI source of truth

- **Decision**: design-time first (user choice, recorded in ADR-001): the reviewed `specs/001-gl-account-maintenance/contracts/openapi.yaml` is authoritative; implementation must conform; drift is checked against the generated document.
- **Rationale**: constitution principle II; reduces drift risk; fits the approval gates.
- **Alternatives**: code-first (contract follows code; risk of unreviewed API change).

## R-08 Test stack

- **Decision**: xUnit, `WebApplicationFactory<Program>`, fresh seeded store per test. Playwright deferred until a frontend exists.
- **Rationale**: `testing.md`. No NUnit present. No frontend yet (ADR-002 undecided).

## R-09 Audit trail

- **Decision**: append-only audit store interface with an in-memory implementation; no public read operation; verified by tests through the store interface.
- **Rationale**: Q-012; NFR-GLA-007; A-008 (unverified user identifier from `X-User-Id`).
- **Alternatives**: structured logging as the audit trail (logs are not tamper-evident or queryable by tests, and `dotnet.md` says not to log PII).

## R-10 Frontend and live validation

- **Decision**: no frontend choice is made in this plan (ADR-002 undecided; Q-017 requires an ADR before UI implementation). Live as-you-type segment validation was dropped by the user at the contract stage, so no validation-only endpoint is added and NFR-GLA-006 is relaxed (spec A-007).
- **Rationale**: avoids a spec change for a new operation and keeps the contract aligned with API-GLA-001..016.
- **Next step**: the frontend choice is raised at the UX stage (plan.md, Open decisions 1).

## Result

No remaining unknowns that block the architecture stage. Items marked "Open check" or "Open decisions" are scheduled for later stages and do not change the architecture.
