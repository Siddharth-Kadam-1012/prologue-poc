# Quickstart: validating GL Account Maintenance and Inquiry

A run and validation guide. It describes what to do once the project exists; no application code exists yet
(repository baseline). Details of shapes live in `contracts/openapi.yaml` (Step 4) and `data-model.md`.

## Prerequisites

- .NET SDK 10.0.401 or later (`dotnet --version`).
- This feature built through its tasks (`tasks.md`, Step 6). Solution layout: `plan.md`, "Source layout".
- No database, secrets or external service are needed. Run on a local or non-production machine only, because no authentication or authorization is implemented (Q-001).

## Build and test

```powershell
dotnet restore
dotnet build --no-restore -warnaserror
dotnet test --no-build
dotnet run scripts/sdlc/sdlc.cs -- gate specs/001-gl-account-maintenance traceability
```

Expected: build with zero warnings, all tests pass, traceability lists every active AC as referenced by a test.

## Run the service

```powershell
dotnet run --project src/GlAccounts.Api
```

The service starts with deterministic seed data. Restarting it resets all data.

## Validation scenarios

Each scenario names the acceptance criteria it proves. Use `X-User-Id: demo-user` for audit attribution.
Seed account IDs are fixed and listed in `StubData/` once built.

| # | Scenario | Expected | AC |
|---|---|---|---|
| 1 | Create an account with a valid full Account ID and Financial classification | 201, body contains segments and Account Type; audit entry exists | AC-GLA-001, 049 |
| 2 | Create with an unknown segment value, or wrong separator | 400, message names the invalid segment and expected format | AC-GLA-002, 044 |
| 3 | Create with an Account ID that already exists | 409 with the duplicate message | AC-GLA-003 |
| 4 | Read an existing account, then an unknown one | 200 with `ETag`; then 404 | AC-GLA-004, 005 |
| 5 | Update description and currency using the `ETag` as `If-Match` | 200; a later read shows new values; audit has before/after | AC-GLA-006, 007, 050 |
| 6 | Update with a stale `If-Match`, then with Account ID or Account Type changed | 409 with stored account unchanged; 400 naming the permanent field | AC-GLA-056, 008, 045 |
| 7 | Change status Active to Hold with a justification, then without one, then try to change a Template | 200; 400; 400 | AC-GLA-022, 023 |
| 8 | Read posting eligibility for Active, Hold, Inactive, Template accounts | Eligible only for Active | AC-GLA-020, 021 |
| 9 | Save a Financial account with a category outside the list; a Statistical account with a Balance Type | 400 field errors | AC-GLA-012, 013, 057 |
| 10 | Delete a dependency-free account; delete the account with 10,001 posted transactions; delete accounts blocked by budget, allocation, group, or several blockers | 204; 409 with count and deactivation hint; 409 naming each blocker | AC-GLA-038..042 |
| 11 | Request the period summary for an account with balances and for one without | Per-period and per-year balances with budget versions; empty list with 200 | AC-GLA-028, 029, 032 |
| 12 | Filter and sort the period summary, then export `.xlsx` | Only matching rows in order; file has the same rows | AC-GLA-030, 031 |
| 13 | Request transactions with default paging, a date range, then a drill-down and a CSV export | 50 items by default, filtered rows, entry detail, CSV matches filter | AC-GLA-033..036 |
| 14 | Request transactions with `pageSize` above 200 | 400 | AC-GLA-037 |
| 15 | Add two attributes, edit one, remove one; try a value that does not belong to the type | Stored and returned; 400 on the invalid value | AC-GLA-014..016 |
| 16 | Save an account update carrying attribute changes where one is invalid | Nothing applied | AC-GLA-017, 018 |
| 17 | List groups for an account in nested groups, one in no group, then export CSV | Hierarchy returned; empty list; CSV lists every group | AC-GLA-024, 025, 027 |
| 18 | Search templates, create an account from a template, use a non-template as source | Matches only; new account copies settings with its own ID; 400 for non-template | AC-GLA-053..055 |

## Not covered (stub data)

Durability across restarts, real transactions, concurrency against a real database, migrations, and production
performance are **not verified (stub data)**. Timing results describe the in-memory stub only.
