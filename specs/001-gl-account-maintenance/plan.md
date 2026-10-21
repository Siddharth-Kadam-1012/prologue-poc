# Implementation Plan: GL Account Maintenance and Inquiry

**Branch**: `001-gl-account-maintenance` | **Date**: 2026-10-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-gl-account-maintenance/spec.md`

---

## Summary

Implement the GL Account Maintenance and Inquiry feature as a full-stack application:
- **Backend**: ASP.NET Core 10 Minimal API, six-project Clean Architecture, stub (in-memory) data.
- **Frontend**: Angular + TypeScript SPA communicating with the backend REST API.

The feature covers the full GL account lifecycle: create, read, update, delete, account attributes,
period summary, transaction history, account group membership, audit trail, and template copy.
No database or ORM is used in this phase — all backend stores are in-memory stubs.

---

## Technical Context

**Backend Language/Version**: .NET 10 / C# 13, ASP.NET Core 10

**Frontend Language/Version**: Angular (latest stable) + TypeScript 5

**Backend Dependencies**: FluentValidation, xUnit + FluentAssertions, `Microsoft.AspNetCore.OpenApi`

**Frontend Dependencies**: Angular CLI, Angular HttpClient (API calls), Angular Reactive Forms (form handling), Angular Router

**Storage**: In-memory stub stores only (no database, no EF Core, no migrations)

**Backend Testing**: xUnit + FluentAssertions; Microsoft.AspNetCore.Mvc.Testing

**Frontend Testing**: Jasmine + Karma (unit); Angular TestBed; Playwright (E2E, optional)

**Target Platform**: Linux / Windows server (backend); browser SPA (frontend)

**Project Type**: Full-stack web application (REST API + Angular SPA)

**Performance Goals**: Account creation ≤ 2s; retrieval/update ≤ 1s (from spec NFRs)

**Constraints**: Stub data only; last-write-wins; no ETag; no auth enforcement; Angular calls backend via HTTP; CORS configured on backend

**Scale/Scope**: Single-service PoC; ~11 API endpoints; 7 stub entities; Angular SPA with ~8 views

---

## Constitution Check

*Gate: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Specification First | ✅ PASS | spec.md approved |
| II. Contracts Before Implementation | ✅ PASS | OpenAPI contract in Step 4 before implementation |
| III. Vertical Slices | ✅ PASS | tasks.md will decompose by user story |
| IV. Tests as Evidence | ✅ PASS | xUnit tests derived from ACs |
| V. Deterministic Enforcement | ✅ PASS | Gate commands record real exit codes |
| VI. Human Approval at Risk Boundaries | ✅ PASS | No auth/migration/breaking changes |
| VII. Repository Context is Authoritative | ✅ PASS | Six-project structure per `dotnet.md` |
| VIII. Incremental Adoption | ✅ PASS | No new libraries introduced beyond what's needed |
| IX. Resumability | ✅ PASS | status.yaml tracks state |
| X. No Secrets, No PII | ✅ PASS | Stub seed data uses synthetic IDs |

**Post-Phase 1 re-check**: All principles still satisfied. OpenAPI contract (Step 4) will be validated before implementation begins.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-gl-account-maintenance/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/           ← Step 4 output (sdlc-contracts)
├── checklists/
│   └── requirements.md
├── status.yaml
├── traceability.md
└── tasks.md             ← Step 6 output (speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── GlAccounts.Api/
│   ├── Features/GlAccounts/
│   │   ├── CreateAccount/
│   │   │   ├── CreateAccountEndpoint.cs
│   │   │   ├── CreateAccountHandler.cs   ← lives in Application
│   │   │   └── CreateAccountRequest.cs   ← lives in Application
│   │   ├── GetAccount/
│   │   ├── UpdateAccount/
│   │   ├── DeleteAccount/
│   │   ├── ListAccountGroups/
│   │   ├── PeriodSummary/
│   │   ├── Transactions/
│   │   ├── ManageAttributes/
│   │   └── Templates/
│   ├── Common/
│   │   ├── ProblemDetails.cs
│   │   └── UserIdReader.cs
│   ├── Program.cs
│   └── GlAccounts.Api.csproj
│
├── GlAccounts.Application/
│   ├── Abstractions/Persistence/
│   │   ├── IAccountStore.cs
│   │   ├── IAccountGroupStore.cs
│   │   ├── IAccountAttributeStore.cs
│   │   ├── IPeriodBalanceStore.cs
│   │   ├── ITransactionStore.cs
│   │   ├── IAuditStore.cs
│   │   └── ISegmentFormatStore.cs
│   ├── Features/GlAccounts/
│   │   ├── CreateAccount/
│   │   │   ├── CreateAccountCommand.cs
│   │   │   ├── CreateAccountHandler.cs
│   │   │   └── CreateAccountValidator.cs
│   │   ├── GetAccount/
│   │   ├── UpdateAccount/
│   │   ├── DeleteAccount/
│   │   ├── ListAccountGroups/
│   │   ├── PeriodSummary/
│   │   ├── Transactions/
│   │   ├── ManageAttributes/
│   │   └── CopyFromTemplate/
│   └── GlAccounts.Application.csproj
│
├── GlAccounts.Domain/
│   ├── Entities/
│   │   ├── GlAccount.cs
│   │   └── AccountAttribute.cs
│   ├── Enums/
│   │   ├── AccountType.cs
│   │   ├── AccountStatus.cs
│   │   ├── BalanceType.cs
│   │   └── NormalBalance.cs
│   ├── Rules/
│   │   ├── ClassificationRules.cs
│   │   └── StatusRules.cs
│   ├── Exceptions/
│   │   └── DomainException.cs
│   └── GlAccounts.Domain.csproj
│
├── GlAccounts.Infrastructure/
│   ├── Persistence/
│   │   ├── InMemoryAccountStore.cs
│   │   ├── InMemoryAccountGroupStore.cs
│   │   ├── InMemoryAccountAttributeStore.cs
│   │   ├── InMemoryPeriodBalanceStore.cs
│   │   ├── InMemoryTransactionStore.cs
│   │   ├── InMemoryAuditStore.cs
│   │   └── InMemorySegmentFormatStore.cs
│   ├── Seed/
│   │   └── StubDataSeeder.cs
│   ├── InfrastructureServiceCollectionExtensions.cs
│   └── GlAccounts.Infrastructure.csproj
│
├── GlAccounts.Contracts/
│   └── GlAccounts.Contracts.csproj   ← versioned integration event schemas (empty for now)
│
└── GlAccounts.Tests/
    ├── Unit/Domain/
    │   ├── ClassificationRulesTests.cs
    │   └── StatusRulesTests.cs
    ├── Unit/Application/
    │   ├── CreateAccountHandlerTests.cs
    │   ├── UpdateAccountHandlerTests.cs
    │   ├── DeleteAccountHandlerTests.cs
    │   ├── ManageAttributesHandlerTests.cs
    │   └── ...
    ├── Integration/
    │   ├── CreateAccountEndpointTests.cs
    │   ├── UpdateAccountEndpointTests.cs
    │   ├── DeleteAccountEndpointTests.cs
    │   └── ...
    └── GlAccounts.Tests.csproj

GlAccounts.sln
Directory.Build.props          ← Nullable=enable, TreatWarningsAsErrors=true, net10.0

src/
└── GlAccounts.Web/            ← Angular SPA (standalone, no SSR)
    ├── src/
    │   ├── app/
    │   │   ├── core/
    │   │   │   ├── services/
    │   │   │   │   └── gl-accounts.service.ts   ← HttpClient wrapper for API
    │   │   │   └── models/
    │   │   │       └── account.model.ts         ← TypeScript interfaces matching openapi.yaml schemas
    │   │   ├── features/
    │   │   │   ├── account-list/
    │   │   │   ├── account-form/               ← create + edit
    │   │   │   ├── account-detail/             ← view + sub-panels
    │   │   │   ├── period-summary/
    │   │   │   ├── transactions/
    │   │   │   ├── account-groups/
    │   │   │   └── attributes/
    │   │   └── app.routes.ts
    │   ├── environments/
    │   │   ├── environment.ts          ← apiBaseUrl for development
    │   │   └── environment.prod.ts
    │   └── main.ts
    ├── angular.json
    ├── package.json
    └── tsconfig.json
```

**Structure Decision**: Six-project Clean Architecture for backend (mandated by AGENTS.md). Angular SPA in `src/GlAccounts.Web/` — separate from the .NET solution; communicates via REST API. ADR not required (frontend technology confirmed by Jaymin Trivedi, Q-003).

---

## Layer Responsibilities (this feature)

| Layer | Responsibility |
|---|---|
| `GlAccounts.Domain` | `GlAccount` entity, `AccountAttribute`, enums (AccountType, AccountStatus, BalanceType, NormalBalance), `ClassificationRules` (Category ↔ Type validation), `StatusRules` (transaction eligibility), `DomainException` |
| `GlAccounts.Application` | Port interfaces (`IAccountStore`, `IAuditStore`, etc.), command/query handlers, FluentValidation validators (segment format, required fields, permanent-field protection) |
| `GlAccounts.Infrastructure` | In-memory store implementations, `StubDataSeeder` (fixed seed IDs), `InfrastructureServiceCollectionExtensions` |
| `GlAccounts.Api` | Minimal API endpoints, `Program.cs`, `UserIdReader` (reads `X-User-Id` header), `ProblemDetails` helpers, CORS policy |
| `GlAccounts.Contracts` | Empty for now; reserved for integration events |
| `GlAccounts.Tests` | xUnit tests covering all 17 ACs; unit tests for Domain rules and Application handlers; integration tests for API endpoints |
| `GlAccounts.Web` (Angular) | Angular SPA — `GlAccountsService` (HttpClient), TypeScript models, feature modules (AccountList, AccountForm, AccountDetail, PeriodSummary, Transactions, AccountGroups, Attributes) |

---

## Dependency Graph

```
GlAccounts.Api
  └─► GlAccounts.Application
        └─► GlAccounts.Domain

GlAccounts.Infrastructure
  ├─► GlAccounts.Application
  └─► GlAccounts.Domain

GlAccounts.Tests
  ├─► GlAccounts.Api (WebApplicationFactory)
  ├─► GlAccounts.Application
  ├─► GlAccounts.Domain
  └─► GlAccounts.Infrastructure (test DI setup)
```

---

## API Surface (preliminary — refined in Step 4 contracts)

| # | Method | Path | Handler | AC coverage |
|---|---|---|---|---|
| 1 | POST | /api/gl/accounts | CreateAccountHandler | AC-GLA-001, 002, 003 |
| 2 | GET | /api/gl/accounts/{id} | GetAccountHandler | AC-GLA-005 |
| 3 | PUT | /api/gl/accounts/{id} | UpdateAccountHandler | AC-GLA-004, 005, 006, 007 |
| 4 | DELETE | /api/gl/accounts/{id} | DeleteAccountHandler | AC-GLA-013, 014 |
| 5 | GET | /api/gl/accounts/{id}/groups | ListAccountGroupsHandler | AC-GLA-010 |
| 6 | GET | /api/gl/accounts/{id}/period-summary | PeriodSummaryHandler | AC-GLA-011 |
| 7 | GET | /api/gl/accounts/{id}/transactions | TransactionsHandler | AC-GLA-012 |
| 8 | POST | /api/gl/accounts/{id}/attributes | AddAttributeHandler | AC-GLA-008 |
| 9 | PUT | /api/gl/accounts/{id}/attributes/{attrId} | UpdateAttributeHandler | AC-GLA-009 |
| 10 | DELETE | /api/gl/accounts/{id}/attributes/{attrId} | RemoveAttributeHandler | AC-GLA-009 |
| 11 | POST | /api/gl/accounts/{id}/copy | CopyFromTemplateHandler | AC-GLA-017 |

Audit log (AC-GLA-015, 016) is produced by handlers; no separate endpoint required in this phase.

---

## Implementation Slices (for tasks.md)

Ordered by dependency:

1. **Backend Foundation** — solution scaffold, six projects, Directory.Build.props, Program.cs, CORS
2. **Domain layer** — enums, entities, ClassificationRules, StatusRules, DomainException
3. **Application ports** — all `I*Store` interfaces
4. **Infrastructure stubs** — all `InMemory*Store` implementations + StubDataSeeder
5. **Create Account** (US-AM-001) — POST /api/gl/accounts + API tests
6. **Get Account** (US-AM-001, US-AM-002) — GET /api/gl/accounts/{id} + API tests
7. **Update Account** (US-AM-002, US-AM-003, US-AM-005) — PUT /api/gl/accounts/{id} + API tests
8. **Delete Account** (US-AM-009) — DELETE /api/gl/accounts/{id} + API tests
9. **Manage Attributes** (US-AM-004) — POST/PUT/DELETE attributes + API tests
10. **Account Groups** (US-AM-006) — GET groups + API tests
11. **Period Summary** (US-AM-007) — GET period-summary + API tests
12. **Transaction History** (US-AM-008) — GET transactions + API tests
13. **Audit Trail** (US-AM-011) — wired into all write handlers + API tests
14. **Copy from Template** (US-AM-012) — POST copy + API tests
15. **Angular Foundation** — `ng new GlAccounts.Web`, routing, `GlAccountsService`, TypeScript models
16. **Angular Account Form** (US-AM-001, US-AM-002) — create + edit form with validation
17. **Angular Account Detail** (US-AM-003, US-AM-004, US-AM-005) — classification, attributes, status
18. **Angular Inquiry Views** (US-AM-006, US-AM-007, US-AM-008) — groups, period summary, transactions
19. **Angular Delete + Template** (US-AM-009, US-AM-012) — delete with confirmation, template copy

---

## Complexity Tracking

No constitution violations. No extra complexity. All deviations documented in spec clarifications.
