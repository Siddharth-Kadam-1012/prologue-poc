# Quickstart: GL Account Maintenance and Inquiry

**Feature**: 001-gl-account-maintenance
**Date**: 2026-10-16

This guide shows how to build, run, and verify the feature end-to-end using the stub data layer.
It is not a full test suite — see `tasks.md` and the test project for that.

---

## Prerequisites

- .NET 10 SDK
- PowerShell 7+ (for gate scripts)

---

## Build

```powershell
dotnet restore
dotnet build --no-restore -warnaserror
```

Expected: exit 0, 0 warnings.

---

## Run the API

```powershell
dotnet run --project src/GlAccounts.Api
```

The API starts on `https://localhost:5001` (or the port in `launchSettings.json`).

---

## Validation Scenarios

All scenarios use the stub seed data defined in [`data-model.md`](data-model.md).
Use `curl`, Postman, or any HTTP client. Replace `BASE` with `https://localhost:5001`.

---

### SC-1: Create a valid account (AC-GLA-001)

```
POST BASE/api/gl/accounts
Content-Type: application/json
X-User-Id: test-user

{
  "accountId": "01-300-1000",
  "description": "New Test Account",
  "accountType": "Financial",
  "category": "Asset",
  "status": "Active",
  "balanceType": "BalanceForward",
  "normalBalance": "Debit",
  "currencyId": "USD"
}
```

**Expected**: `201 Created` with the new account in the body.

---

### SC-2: Reject duplicate account ID (AC-GLA-002)

```
POST BASE/api/gl/accounts
(same payload as SC-1, same accountId "01-300-1000")
```

**Expected**: `409 Conflict` with an error message referencing the duplicate ID.

---

### SC-3: Reject invalid segment format (AC-GLA-003)

```
POST BASE/api/gl/accounts
{ "accountId": "INVALID", ... }
```

**Expected**: `422 Unprocessable Entity` with expected format in the error body.

---

### SC-4: Retrieve an account (AC-GLA-005)

```
GET BASE/api/gl/accounts/01-100-1000
```

**Expected**: `200 OK` with full account record for "Cash Account".

---

### SC-5: Update editable fields (AC-GLA-005)

```
PUT BASE/api/gl/accounts/01-100-1000
Content-Type: application/json
X-User-Id: test-user

{ "description": "Updated Cash Account" }
```

**Expected**: `200 OK`; description changed. AccountId and AccountType unchanged.

---

### SC-6: Reject permanent-field change (AC-GLA-004)

```
PUT BASE/api/gl/accounts/01-100-1000
{ "accountType": "Statistical" }
```

**Expected**: `422 Unprocessable Entity`; permanent field change rejected.

---

### SC-7: Block deletion of account with transactions (AC-GLA-013)

```
DELETE BASE/api/gl/accounts/01-200-2000
X-User-Id: test-user
```

**Expected**: `409 Conflict` with message stating number of transactions and suggesting deactivation.

---

### SC-8: Delete a clean account (AC-GLA-014)

First create a clean account (SC-1), then:

```
DELETE BASE/api/gl/accounts/01-300-1000
X-User-Id: test-user
```

**Expected**: `204 No Content`.

---

### SC-9: View period summary (AC-GLA-011)

```
GET BASE/api/gl/accounts/01-100-1000/period-summary
```

**Expected**: `200 OK` with 4 period balance records for FY2026.

---

### SC-10: View transaction history (AC-GLA-012)

```
GET BASE/api/gl/accounts/01-200-2000/transactions
```

**Expected**: `200 OK` with 3 posted transactions.

---

### SC-11: Assign attribute (AC-GLA-008)

```
POST BASE/api/gl/accounts/01-100-1000/attributes
Content-Type: application/json
X-User-Id: test-user

{ "attributeTypeId": "REPORTING_UNIT", "attributeValueId": "NORTH_REGION" }
```

**Expected**: `201 Created`.

---

### SC-12: Audit log produced (AC-GLA-015)

After SC-1 (create):

```
GET BASE/api/gl/accounts/01-300-1000/audit
```

**Expected**: `200 OK`; at least one audit entry with `changeType: Created`, `userId: test-user`.

> **Note**: This endpoint is an optional convenience endpoint for test verification.
> Implementations may expose it under a different path.

---

### SC-13: Copy from template (AC-GLA-017)

```
POST BASE/api/gl/accounts/01-100-5000/copy
Content-Type: application/json
X-User-Id: test-user

{ "newAccountId": "01-100-9000", "description": "Copy of Template" }
```

**Expected**: `201 Created`; new account has same classification as the template; source account remains `Template` status.

---

## Run Unit Tests

```powershell
dotnet test --no-build --logger "console;verbosity=normal"
```

Expected: all tests pass, 0 failures.

---

## Gate Verification

```powershell
dotnet run scripts/sdlc/sdlc.cs -- gate specs/001-gl-account-maintenance specification
dotnet run scripts/sdlc/sdlc.cs -- gate specs/001-gl-account-maintenance contract
dotnet run scripts/sdlc/sdlc.cs -- gate specs/001-gl-account-maintenance traceability
```

All must exit 0 before the PR is raised.
