# Data Model: GL Account Maintenance and Inquiry

**Feature**: 001-gl-account-maintenance
**Date**: 2026-10-16

---

## Entities

### GLAccount

The core entity representing a General Ledger account.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| AccountId | string | Required; unique; max 32 chars; matches segment format | Permanent after creation (BR-GLA-001, BR-GLA-003) |
| Description | string | Required; max 200 chars | Editable |
| AccountType | enum: Financial \| Statistical | Required | Permanent after creation (BR-GLA-003) |
| Category | string | Required; must be valid for AccountType (BR-GLA-007) | Editable |
| Status | enum: Active \| Hold \| Inactive \| Template | Required; default = Active | Editable |
| BalanceType | enum: BalanceForward \| Periodic | Required | Editable |
| NormalBalance | enum: Debit \| Credit | Required | Editable; used by journal module |
| CurrencyId | string | Optional; max 10 chars; stored as string, no validation | Editable |
| CreatedDate | DateOnly | Required; set at creation | Immutable |
| CreatedBy | string | Required; caller-supplied; max 100 chars | Immutable; used for audit |

**Uniqueness**: `AccountId` is the primary key; no two accounts may share an `AccountId`.

**State Transitions**:

```
Active ──► Hold ──► Inactive
Active ──► Inactive
Active ──► Template
Hold   ──► Active
Hold   ──► Inactive
Inactive ──► Active   (re-activation allowed)
Template ──► Active   (when used as a copy source, the copy becomes Active)
```

Restrictions:
- Hold and Inactive accounts: cannot be referenced in new transactions.
- Template accounts: cannot be referenced in any transactions.

---

### AccountAttribute

Links an account to a reporting classification (Attribute Type + Value).

| Field | Type | Constraints | Notes |
|---|---|---|---|
| AttributeId | string (GUID) | Required; unique | System-generated |
| AccountId | string (FK → GLAccount) | Required | |
| AttributeTypeId | string | Required | Must exist in AttributeCatalog |
| AttributeTypeDescription | string | Denormalised for read performance | |
| AttributeValueId | string | Required | Must be valid value for the chosen type |
| AttributeValueDescription | string | Denormalised for read performance | |

**Rules**:
- Multiple attributes of different types allowed per account.
- Multiple attributes of the same type allowed (no uniqueness constraint on type per account, per PRD).
- Attribute changes are committed atomically with the parent account save (BR-GLA-009).
- Attributes are used for reporting classification only; they do not affect transaction posting.

---

### AccountGroup (read model — managed by separate feature)

| Field | Type | Notes |
|---|---|---|
| GroupId | string | |
| GroupName | string | |
| Description | string | |
| AccountIds | IReadOnlyList\<string\> | Members of this group |

This feature only reads account group data via `IAccountGroupStore`. It never modifies it.

---

### PeriodBalance

Period-level balance snapshot for an account.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| BalanceId | string (GUID) | Required; unique | System-generated |
| AccountId | string (FK → GLAccount) | Required | |
| FiscalYear | int | Required | e.g., 2026 |
| FiscalPeriod | int | Required; 1–13 | Fiscal period within the year |
| BudgetVersion | string | Optional | e.g., "ORIGINAL", "REVISED" |
| Balance | decimal | Required | Period ending balance |

**Filtering**: Supported by FiscalYear, FiscalPeriod range.

---

### GLTransaction (read model — managed by journal module)

| Field | Type | Notes |
|---|---|---|
| TransactionId | string | |
| AccountId | string (FK → GLAccount) | |
| BatchId | string | Journal batch identifier |
| Description | string | |
| Amount | decimal | |
| PostingDate | DateOnly | |
| PostingStatus | enum: Posted \| Unposted | |

This feature only reads transaction data. It never writes to this entity.
It is used for two purposes: dependency check before deletion, and transaction history inquiry.

---

### AuditEntry (append-only)

| Field | Type | Constraints | Notes |
|---|---|---|---|
| EntryId | string (GUID) | Required; unique | System-generated |
| EntityType | string | Required | e.g., "GLAccount", "AccountAttribute" |
| EntityId | string | Required | The AccountId or AttributeId |
| UserId | string | Required | Caller-supplied user ID |
| Timestamp | DateTimeOffset (UTC) | Required | Set by the application |
| ChangeType | enum: Created \| Updated \| Deleted | Required | |
| BeforeValues | Dictionary\<string, string?\> | Optional | Null for Created events |
| AfterValues | Dictionary\<string, string?\> | Optional | Null for Deleted events |

**Rules**:
- Entries are immutable; never modified after creation.
- Stored in `IAuditStore` (in-memory append-only list in this phase).

---

### SegmentFormatDefinition (external stub — from US-SF-001)

| Field | Type | Notes |
|---|---|---|
| FormatId | string | |
| Segments | List\<SegmentDefinition\> | |

**SegmentDefinition**:

| Field | Type | Notes |
|---|---|---|
| SegmentName | string | e.g., "Company", "Department", "Natural" |
| Length | int | Fixed width of segment |
| ValidValues | List\<string\> | Allowed values for this segment position |
| Separator | char | Separator character (e.g., '-') |

**Stub seed data** (`CC-DEPT-NATURAL` format):
```
Company:    2 digits (01–99)
Department: 3 digits (100–999)
Natural:    4 digits (1000–9999)
Separator:  '-'
Example:    01-100-1000
```

---

### AttributeCatalog (external stub)

| Field | Type | Notes |
|---|---|---|
| AttributeTypeId | string | |
| AttributeTypeName | string | |
| Values | List\<AttributeValue\> | |

---

## Validation Rules Summary

| Rule | Entity | Enforcement point |
|---|---|---|
| AccountId unique | GLAccount | Application layer before CREATE |
| AccountId matches segment format | GLAccount | Domain / Application before CREATE |
| AccountId and AccountType immutable | GLAccount | Application layer on UPDATE (reject changed fields) |
| Category valid for AccountType | GLAccount | Domain (ClassificationRules) |
| Status-based transaction restriction | GLAccount | Referenced in transaction lookup logic |
| Deletion blocked if dependencies exist | GLAccount | Application layer before DELETE |
| Attribute type/value must exist in catalog | AccountAttribute | Application layer before CREATE/UPDATE |
| Attribute changes atomic | AccountAttribute | Infrastructure layer (all-or-nothing on stub store) |

---

## Stub Seed Data

All IDs are fixed so tests are deterministic.

### GLAccounts (seed)

| AccountId | Description | AccountType | Status | BalanceType | NormalBalance |
|---|---|---|---|---|---|
| 01-100-1000 | Cash Account | Financial | Active | BalanceForward | Debit |
| 01-100-2000 | Accounts Receivable | Financial | Active | BalanceForward | Debit |
| 01-200-1000 | Accounts Payable | Financial | Active | BalanceForward | Credit |
| 01-100-3000 | Inactive Account | Financial | Inactive | BalanceForward | Debit |
| 01-100-4000 | Hold Account | Financial | Hold | BalanceForward | Debit |
| 01-100-5000 | Template Account | Financial | Template | BalanceForward | Debit |
| 01-200-2000 | Account with Transactions | Financial | Active | BalanceForward | Debit |

### GLTransactions (seed — linked to 01-200-2000)

3 posted transactions with fixed TransactionIds so deletion-protection tests are deterministic.

### AccountGroups (seed)

1 group containing `01-100-1000` and `01-100-2000`.

### PeriodBalances (seed — linked to 01-100-1000)

4 records covering FY2026 periods 1–4.
