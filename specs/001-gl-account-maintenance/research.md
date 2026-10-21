# Research: GL Account Maintenance and Inquiry

**Feature**: 001-gl-account-maintenance
**Date**: 2026-10-16
**Phase**: 0 — Pre-design research

---

## Stack Decisions

### Language / Runtime

| Decision | .NET 10 / C# 13, ASP.NET Core 10 |
|---|---|
| Rationale | AGENTS.md target runtime; repo policy |
| Alternatives considered | .NET 8 LTS — rejected (AGENTS.md specifies .NET 10) |

### API Style

| Decision | ASP.NET Core 10 Minimal API (endpoint-per-feature) |
|---|---|
| Rationale | Matches the existing `src/GlAccounts.Api` pattern before the reset; aligns with vertical-slice convention |
| Alternatives considered | Controller-based MVC — rejected (heavier, less slice-friendly) |

### Validation

| Decision | FluentValidation |
|---|---|
| Rationale | Already used in the prior implementation (`CreateAccountValidator.cs`); `.bob/instructions/dotnet.md` says use whatever is already in project |
| Alternatives considered | Data Annotations — less expressive for cross-field rules (Category vs. Type); rejected |

### Data Layer

| Decision | In-memory stub stores — interfaces in `Application`, implementations in `Infrastructure` |
|---|---|
| Rationale | AGENTS.md mandates stub data for this phase; Clean Architecture port/adapter pattern |
| Alternatives considered | EF Core InMemory — rejected (over-engineering for stub phase; introduces ORM dependency) |

### Project Structure

| Decision | Six-project Clean Architecture: `GlAccounts.Api`, `GlAccounts.Application`, `GlAccounts.Domain`, `GlAccounts.Infrastructure`, `GlAccounts.Contracts`, `GlAccounts.Tests` |
|---|---|
| Rationale | Mandated by `AGENTS.md` and `.bob/instructions/dotnet.md` |
| Alternatives considered | Single-project — rejected (ADR required; not authorised) |

### Testing

| Decision | xUnit + FluentAssertions for unit and integration tests |
|---|---|
| Rationale | AGENTS.md preferred test runner; standard .NET ecosystem choice |
| Alternatives considered | NUnit — only if already present; it is not |

### Audit Trail (US-AM-011)

| Decision | In-memory `AuditStore` implementing `IAuditStore` port; appends immutable `AuditEntry` records |
|---|---|
| Rationale | Authorization is out of scope (Q-001); only the logging side of US-AM-011 is implemented; simple append-only list in Infrastructure |
| Alternatives considered | Event sourcing — far too heavy for stub phase; rejected |

### Concurrent Update Handling (Q-002)

| Decision | Last-write-wins; no ETag / If-Match |
|---|---|
| Rationale | Confirmed by Jaymin Trivedi; simplest model for stub data; ETag deferred |
| Alternatives considered | Optimistic locking (ETag + 412) — deferred to future phase per spec clarification |

### Segment Format Validation

| Decision | `ISegmentFormatStore` port in Application; `InMemorySegmentFormatStore` stub seeded with a 3-segment format (`CC-DEPT-NATURAL`, e.g. `01-100-1000`) |
|---|---|
| Rationale | External dependency US-SF-001 not in scope; pre-seeded stub required; stub validates Account ID via regex derived from segment format |
| Alternatives considered | Skip validation in stub — rejected; BR-GLA-001/002 are P1 requirements |

---

## Key Findings for Data Model

- `GLAccount`: 10 fields; AccountId and AccountType are immutable after creation.
- `AccountAttribute`: FK to AccountId + AttributeTypeId + AttributeValueId; many-per-account.
- `PeriodBalance`: FK to AccountId; keyed by FiscalYear + FiscalPeriod + BudgetVersion.
- `GLTransaction`: FK to AccountId; read-only from this feature (transactions come from journal module).
- `AuditEntry`: append-only; never modified.
- `AccountGroup`: read-only from this feature; managed by a separate feature.

---

## Key Findings for API Contract

Endpoints identified from PRD non-functional ACs and requirements:

| Method | Path | Purpose |
|---|---|---|
| POST | /api/gl/accounts | Create account (US-AM-001) |
| GET | /api/gl/accounts/{id} | Retrieve account (US-AM-001, US-AM-002) |
| PUT | /api/gl/accounts/{id} | Update editable fields (US-AM-002, US-AM-003, US-AM-005) |
| DELETE | /api/gl/accounts/{id} | Delete account with dependency check (US-AM-009) |
| GET | /api/gl/accounts/{id}/groups | Account group membership (US-AM-006) |
| GET | /api/gl/accounts/{id}/period-summary | Period balances (US-AM-007) |
| GET | /api/gl/accounts/{id}/transactions | Transaction history (US-AM-008) |
| POST | /api/gl/accounts/{id}/attributes | Assign attribute (US-AM-004) |
| PUT | /api/gl/accounts/{id}/attributes/{attrId} | Update attribute (US-AM-004) |
| DELETE | /api/gl/accounts/{id}/attributes/{attrId} | Remove attribute (US-AM-004) |
| POST | /api/gl/accounts/{id}/copy | Copy from template (US-AM-012) |

---

## Resolved NEEDS CLARIFICATION Items

All items resolved — none remain. See `spec.md` Clarifications section.
