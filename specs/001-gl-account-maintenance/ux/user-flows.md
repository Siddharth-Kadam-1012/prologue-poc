# User Flows: GL Account Maintenance and Inquiry

**Feature**: 001-gl-account-maintenance  
**Date**: 2026-10-16  
**Note**: This feature is **API-only** in this phase. No frontend/React UI is in scope.
The flows below describe API call sequences (actor → endpoint → response) rather than UI navigation.

---

## UF-GLA-001: Create a GL Account

**Actor**: Accountant  
**Trigger**: Need to establish a new account in the Chart of Accounts

```
Accountant
  → POST /api/gl/accounts  {accountId, description, accountType, category, status, balanceType, normalBalance}
  ← 201 Created  (account record)
  [Error path] ← 409 Conflict  (duplicate ID)
  [Error path] ← 422 Unprocessable Entity  (invalid segment format or category/type mismatch)
  [Error path] ← 400 Bad Request  (missing required field)
```

---

## UF-GLA-002: Retrieve and Update a GL Account

**Actor**: Accountant  
**Trigger**: Need to view or change account details

```
Accountant
  → GET /api/gl/accounts/{accountId}
  ← 200 OK  (current account record)
  [Error path] ← 404 Not Found

  → PUT /api/gl/accounts/{accountId}  {editable fields only}
  ← 200 OK  (updated account record)
  [Error path] ← 422  (attempt to change permanent field)
  [Error path] ← 404 Not Found
```

---

## UF-GLA-003: Delete a GL Account

**Actor**: System Administrator  
**Trigger**: Need to remove an account with no dependencies

```
System Administrator
  → DELETE /api/gl/accounts/{accountId}
  ← 204 No Content  (account removed)
  [Blocked path] ← 409 Conflict  (has transactions / budgets / group membership)
               message suggests deactivation instead
  [Error path] ← 404 Not Found
```

---

## UF-GLA-004: Manage Account Attributes

**Actor**: Accountant  
**Trigger**: Need to classify account for reporting

```
Accountant
  → POST /api/gl/accounts/{accountId}/attributes  {attributeTypeId, attributeValueId}
  ← 201 Created  (attribute record)

  → PUT /api/gl/accounts/{accountId}/attributes/{attributeId}  {attributeValueId}
  ← 200 OK  (updated attribute)

  → DELETE /api/gl/accounts/{accountId}/attributes/{attributeId}
  ← 204 No Content
```

---

## UF-GLA-005: View Account Group Membership

**Actor**: Finance Manager  
**Trigger**: Need to understand how an account is grouped for reporting

```
Finance Manager
  → GET /api/gl/accounts/{accountId}/groups
  ← 200 OK  (list of groups containing the account)
```

---

## UF-GLA-006: View Period Summary

**Actor**: Finance Manager  
**Trigger**: Need to analyse financial performance over time

```
Finance Manager
  → GET /api/gl/accounts/{accountId}/period-summary?fiscalYear=2026&fromPeriod=1&toPeriod=4
  ← 200 OK  (period balance records)
```

---

## UF-GLA-007: View Transaction History

**Actor**: Accountant  
**Trigger**: Need to verify posting accuracy or perform reconciliation

```
Accountant
  → GET /api/gl/accounts/{accountId}/transactions?fromDate=2026-01-01&toDate=2026-12-31
  ← 200 OK  (transaction records)
```

---

## UF-GLA-008: Copy Account from Template

**Actor**: Accountant  
**Trigger**: Need to create a new account with consistent settings

```
Accountant
  → POST /api/gl/accounts/{templateAccountId}/copy  {newAccountId, description}
  ← 201 Created  (new account, status=Active, settings copied from template)
  [Error path] ← 422  (source not a Template account, or invalid new ID format)
  [Error path] ← 409  (new ID already exists)
  [Error path] ← 404  (source account not found)
```
