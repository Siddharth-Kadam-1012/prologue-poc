# Feature Specification: GL Account Maintenance and Inquiry

**Feature Branch**: `001-gl-account-maintenance`

**Created**: 2026-05-30

**Status**: Draft

**Input**: PRD — General Ledger (GL) Module, Account Maintenance and Inquiry (v3.0 Final)

---

## PRD Analysis

| ID | Category | Statement |
|---|---|---|
| PRD-GLA-001 | Requirement | Create fully qualified GL accounts using defined segment values with validation |
| PRD-GLA-002 | Requirement | Modify editable account attributes while protecting permanent fields (ID, Type) |
| PRD-GLA-003 | Requirement | Configure account classification: Category, Type, Balance Type, Normal Balance |
| PRD-GLA-004 | Requirement | Assign and manage account attributes for reporting classification |
| PRD-GLA-005 | Requirement | Manage account status lifecycle (Active, Hold, Inactive, Template) |
| PRD-GLA-006 | Requirement | View account group membership for a selected account |
| PRD-GLA-007 | Requirement | View period summary and account balances by fiscal period |
| PRD-GLA-008 | Requirement | View account transaction history with filtering |
| PRD-GLA-009 | Requirement | Protect accounts from deletion when dependencies exist |
| PRD-GLA-010 | Requirement | Enforce Chart-of-Accounts (COA) structural validation rules on all operations |
| PRD-GLA-011 | Requirement | Control account access and maintain comprehensive audit trail |
| PRD-GLA-012 | Requirement | Create accounts from templates for rapid, consistent setup |
| PRD-GLA-013 | [RESOLVED: Q-001] | Authorization enforcement is OUT OF SCOPE for this phase; no access-control checks will be implemented; audit trail (logging) is retained |
| PRD-GLA-014 | [INFERENCE] | Segment format definitions (US-SF-001) and segment value lists (US-SV-001) are provided by upstream stubs; they are external dependencies not implemented in this feature |
| PRD-GLA-016 | [NFR] | Account creation completes within 2 seconds; account retrieval within 1 second |
| PRD-GLA-017 | Out of scope | Real database/ORM persistence, migrations, external message queues |
| PRD-GLA-018 | [RESOLVED: Q-003] | Frontend technology: Angular + TypeScript (confirmed by Jaymin Trivedi) |

---

## Actors and Personas

| Persona | Role | Key Actions |
|---|---|---|
| **Accountant** | Day-to-day GL work | Create accounts, modify attributes, assign attributes, view transactions, create from templates |
| **Finance Manager** | Financial oversight | View account groups, view period summaries, read-only inquiries |
| **GL Administrator** | COA configuration | Configure segment formats and values (upstream dependency), enforce COA rules |
| **System Administrator** | Security and compliance | Control access, maintain audit trail, protect accounts from deletion |

---

## Business Goals and Outcomes

- Establish a robust, validated GL Chart of Accounts that supports accurate financial reporting.
- Prevent data integrity violations through strict validation (segment format, duplicate IDs, deletion guards).
- Maintain a complete and auditable history of all account changes for SOX/regulatory compliance.
- Enable efficient account creation via templating for sets of similar accounts.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Create a New GL Account (Priority: P1)

An Accountant selects valid segment values to compose an account ID, fills in the required description, type, and category, and saves. The system validates the segment format and uniqueness before persisting the new account.

**Why this priority**: Core capability — all other stories depend on an account existing.

**Independent Test**: Can be fully tested by POSTing a new account payload and verifying 201 + stored record.

**Acceptance Scenarios**:

1. **Given** valid segment values and a unique account ID, **When** the Accountant saves, **Then** a new account is created and returns success.
2. **Given** an account ID that already exists, **When** the Accountant saves, **Then** the system rejects the request with a duplicate-ID error.
3. **Given** an account ID that does not match the defined segment format, **When** the Accountant saves, **Then** the system rejects the request with a format-validation error.
4. **Given** a missing required field (e.g., Description), **When** the Accountant saves, **Then** the system rejects with a field-validation error.

---

### User Story 2 — Modify Account Attributes (Priority: P2)

An Accountant retrieves an existing account, changes editable fields (Description, Category, Currency ID, Status), and saves. Permanent fields (Account ID, Account Type) are read-only.

**Why this priority**: Accounts change over their lifecycle; updates must be reliable and safe.

**Independent Test**: Can be fully tested by GETting an account, PUTting changed editable fields, and verifying 200 + updated record, and confirming permanent fields are unchanged.

**Acceptance Scenarios**:

1. **Given** an existing account, **When** the Accountant changes the Description and saves, **Then** the account reflects the new description.
2. **Given** an existing account, **When** the Accountant attempts to change Account ID or Account Type, **Then** the system ignores or rejects those fields.

---

### User Story 3 — Configure Account Classification (Priority: P2)

An Accountant sets Account Category (validated against Account Type), Balance Type (Balance Forward / Periodic), and Normal Balance (Debit / Credit) at creation or update.

**Why this priority**: Classification drives correct posting and reporting behavior.

**Independent Test**: Create an account with all classification fields; verify they are stored and returned correctly; verify invalid Category+Type combinations are rejected.

**Acceptance Scenarios**:

1. **Given** an Account Type of Financial, **When** the Accountant selects a valid Category, **Then** the account is saved with that classification.
2. **Given** an Account Type of Financial and Account Type field on an existing account, **When** the Accountant attempts to change the type, **Then** the system rejects the change.
3. **Given** an Account Type of Statistical, **When** the Accountant selects an incompatible Category, **Then** the system rejects with a classification error.

---

### User Story 4 — Manage Account Status Lifecycle (Priority: P2)

An Accountant changes account status (Active → Hold → Inactive → Template) as the account moves through its lifecycle. The system enforces status-based restrictions on transaction entry.

**Why this priority**: Status controls account availability and is essential for lifecycle management.

**Independent Test**: Set an account to Inactive; attempt to use it in a transaction lookup; verify it is rejected.

**Acceptance Scenarios**:

1. **Given** an Active account, **When** the status is set to Hold, **Then** new transactions cannot reference the account.
2. **Given** an Active account, **When** the status is set to Inactive, **Then** the account cannot be selected in new transactions.
3. **Given** a Template account, **When** a user attempts to post a transaction to it, **Then** the system rejects it.

---

### User Story 5 — Assign Account Attributes for Reporting (Priority: P3)

An Accountant links one or more Attribute Types and Values to an account for classification in specialized reports. Attributes do not affect transaction posting.

**Why this priority**: Should-have for reporting; does not block core accounting.

**Independent Test**: POST an attribute assignment to an account, GET the account attributes, DELETE an attribute; verify staged save commits all changes.

**Acceptance Scenarios**:

1. **Given** an existing account, **When** the Accountant assigns an Attribute Type and Value, **Then** the attribute is linked to the account.
2. **Given** an account with attributes, **When** the Accountant removes an attribute and saves, **Then** the attribute is removed from the account.
3. **Given** multiple pending attribute changes, **When** the parent account is saved, **Then** all attribute changes are committed in one transaction.

---

### User Story 6 — View Account Group Membership (Priority: P3)

A Finance Manager selects an account and views all account groups that include it, respecting security permissions.

**Why this priority**: Should-have read-only inquiry.

**Independent Test**: GET account-groups for a known account and verify the list matches stub data.

**Acceptance Scenarios**:

1. **Given** an account that belongs to two groups, **When** the Finance Manager requests group membership, **Then** both groups are returned.
2. **Given** a Finance Manager without access to a private group, **When** group membership is requested, **Then** that group is not included in the response.

---

### User Story 7 — View Period Summary and Balances (Priority: P2)

A Finance Manager views period-level balances for an account across fiscal periods and years, with filter/sort and export capability.

**Why this priority**: Must-have for financial performance analysis.

**Independent Test**: GET period summary for an account; verify periods, balances, and fiscal year grouping in response.

**Acceptance Scenarios**:

1. **Given** an account with period balance data, **When** the Finance Manager requests the period summary, **Then** balances are returned grouped by fiscal period.
2. **Given** a filter for a specific fiscal year, **When** the request is made, **Then** only periods for that year are returned.

---

### User Story 8 — View Transaction History (Priority: P2)

An Accountant views posted journal entries for an account, filtered by date range or fiscal period, and can export the list.

**Why this priority**: Must-have for reconciliation and audit.

**Independent Test**: GET transactions for an account with a date-range filter; verify only matching transactions are returned.

**Acceptance Scenarios**:

1. **Given** an account with posted transactions, **When** the Accountant requests transaction history, **Then** all posted entries are returned.
2. **Given** a date range filter, **When** the request is made, **Then** only transactions within the range are returned.

---

### User Story 9 — Protect Account from Deletion (Priority: P1)

A System Administrator attempts to delete an account. The system checks for dependencies (posted/unposted transactions, budgets, allocations, group membership) and blocks deletion if any exist, suggesting deactivation instead.

**Why this priority**: Must-have data integrity guardrail.

**Independent Test**: Attempt DELETE on an account with stub transactions; verify 409 response with dependency count. Attempt DELETE on a clean account; verify 204.

**Acceptance Scenarios**:

1. **Given** an account with posted transactions, **When** DELETE is called, **Then** 409 Conflict is returned with an explanation.
2. **Given** an account with no dependencies, **When** DELETE is called, **Then** 204 No Content is returned and the account is removed.
3. **Given** a blocked deletion, **Then** the error message suggests deactivation as an alternative.

---

### User Story 10 — Enforce COA Validation Rules (Priority: P1)

The system enforces Chart-of-Accounts structural rules on every create and update: segment format, segment value lists, required fields, permanent-field protection, and security rights.

**Why this priority**: Cross-cutting Must-have; prevents invalid data entering the GL.

**Independent Test**: Submit creates and updates with various invalid inputs; verify each is rejected with a clear error message.

**Acceptance Scenarios**:

1. **Given** an account ID that doesn't match the segment format, **When** created or updated, **Then** a format-validation error is returned.
2. **Given** a request to change Account ID after creation, **When** PUT is called, **Then** the change is rejected.
3. **Given** a user without appropriate security rights, **When** a create or modify is attempted, **Then** access is denied.

---

### User Story 11 — Audit Trail (Priority: P2)

Any caller can perform operations in this phase (authorization enforcement is out of scope). All account create, modify, status-change, and attribute-update events are logged with a caller-supplied user ID, timestamp, and before/after values.

**Why this priority**: Must-have for SOX compliance audit-trail requirement; access control deferred to a future phase.

**Independent Test**: Perform account CRUD operations; verify audit log entries contain the correct user ID, timestamp, and before/after field values.

**Acceptance Scenarios**:

1. **Given** any caller creates an account, **Then** an audit entry is logged with the caller's user ID, timestamp, and the new field values.
2. **Given** any caller modifies an account field, **Then** an audit entry is logged with the before and after values of the changed fields.

---

### User Story 12 — Create Account from Template (Priority: P4)

An Accountant selects an existing Template-status account, copies all settings to a new account (with a new ID), and saves. Template accounts cannot be used in transactions.

**Why this priority**: Could-have efficiency feature; not blocking for core GL operations.

**Independent Test**: Copy a template account; verify new account has same classification and attributes as the source; verify template account is still status=Template.

**Acceptance Scenarios**:

1. **Given** a Template account, **When** an Accountant copies it with a new account ID, **Then** a new account is created with copied settings.
2. **Given** a Template account, **When** a transaction reference is attempted, **Then** the system rejects it with an appropriate error.

---

### Edge Cases

- Duplicate Account ID on creation → 409 Conflict with exact conflicting ID in error body.
- Account ID that does not match segment format → 422 Unprocessable Entity with expected format in error body.
- Deleting an account with 10,000+ posted transactions → 409 with transaction count in response, deactivation suggested.
- Modifying a non-existent account → 404 Not Found.
- Setting Balance Type or Normal Balance inconsistent with Account Type → 422 with explanation.
- Account with zero transactions but still in an account group → deletion blocked; group membership must be removed first.
- Concurrent updates to the same account → last-write-wins; no conflict detection or optimistic locking in this phase.

---

## Functional Requirements

| ID | Requirement | Priority | Source |
|---|---|---|---|
| REQ-GLA-001 | System MUST allow creation of a GL account with a validated, unique account ID built from segment values | P1 | US-AM-001 |
| REQ-GLA-002 | System MUST validate Account ID against the active segment format definition before saving | P1 | US-AM-001, US-AM-010 |
| REQ-GLA-003 | System MUST prevent creation of duplicate Account IDs | P1 | US-AM-001 |
| REQ-GLA-004 | System MUST make Account ID and Account Type permanent (non-editable) after first save | P1 | US-AM-001, US-AM-002, US-AM-010 |
| REQ-GLA-005 | System MUST allow modification of editable fields: Description, Category, Currency ID, Status | P1 | US-AM-002 |
| REQ-GLA-006 | System MUST validate Account Category against Account Type | P2 | US-AM-003 |
| REQ-GLA-007 | System MUST support Balance Type options: Balance Forward, Periodic | P2 | US-AM-003 |
| REQ-GLA-008 | System MUST support Normal Balance options: Debit, Credit | P2 | US-AM-003 |
| REQ-GLA-009 | System MUST allow assignment, update, and removal of account attributes for reporting classification | P2 | US-AM-004 |
| REQ-GLA-010 | System MUST commit attribute changes in the same transaction as the parent account save | P2 | US-AM-004 |
| REQ-GLA-011 | System MUST support account status values: Active, Hold, Inactive, Template | P1 | US-AM-005 |
| REQ-GLA-012 | System MUST prevent Hold and Inactive accounts from being used in new transactions | P1 | US-AM-005 |
| REQ-GLA-013 | System MUST prevent Template accounts from being used in transactions | P2 | US-AM-005, US-AM-012 |
| REQ-GLA-014 | System MUST return all account groups that contain a given account | P3 | US-AM-006 |
| REQ-GLA-015 | System MUST return period-level balance data for an account, filterable by fiscal period and year | P2 | US-AM-007 |
| REQ-GLA-016 | System MUST return posted transaction history for an account, filterable by date range or fiscal period | P2 | US-AM-008 |
| REQ-GLA-017 | System MUST block deletion of accounts with posted transactions, unposted transactions, budgets, allocations, or group membership | P1 | US-AM-009 |
| REQ-GLA-018 | System MUST return 409 Conflict with dependency detail when deletion is blocked | P1 | US-AM-009 |
| REQ-GLA-019 | System MUST suggest deactivation as an alternative when deletion is blocked | P2 | US-AM-009 |
| REQ-GLA-020 | System MUST enforce COA segment format and segment value rules on all create and update operations | P1 | US-AM-010 |
| REQ-GLA-021 | ~~Authorization enforcement is OUT OF SCOPE for this phase~~ — removed per Q-001 resolution | — | US-AM-011 |
| REQ-GLA-022 | System MUST log all account changes with caller-supplied user ID, timestamp, and before/after values | P2 | US-AM-011 |
| REQ-GLA-023 | System MUST allow creation of a new account by copying all settings from a Template-status account | P4 | US-AM-012 |
| REQ-GLA-024 | System MUST provide clear, actionable error messages for all validation failures | P1 | US-AM-010 |

### Non-Functional Requirements

| ID | NFR | Target | Source |
|---|---|---|---|
| NFR-GLA-001 | Account creation response time | ≤ 2 seconds | US-AM-001 |
| NFR-GLA-002 | Account retrieval and update response time | ≤ 1 second | US-AM-002, US-AM-004 |
| NFR-GLA-003 | Deletion dependency check response time | ≤ 1 second | US-AM-009 |
| NFR-GLA-004 | Attribute operations response time | ≤ 1 second | US-AM-004 |
| NFR-GLA-005 | Data layer | Stub data (in-memory) only; no real database in this phase | AGENTS.md |

### Key Entities

- **GLAccount**: The primary entity. Fields: AccountId (permanent), Description, AccountType (Financial/Statistical, permanent), Category, Status (Active/Hold/Inactive/Template), BalanceType, NormalBalance, CurrencyId, CreatedDate, CreatedBy.
- **AccountAttribute**: Links an account to an AttributeType and AttributeValue for reporting classification. Many-per-account.
- **AccountGroup**: A named group containing one or more accounts. Read-only from this feature; managed by a separate feature.
- **PeriodBalance**: Period-level balance snapshot for an account (AccountId, FiscalYear, FiscalPeriod, Balance, BudgetVersion).
- **GLTransaction**: Posted journal entry linked to an account (TransactionId, AccountId, BatchId, Description, Amount, PostingDate, PostingStatus).
- **AuditEntry**: Immutable record of a change event (EntityType, EntityId, UserId, Timestamp, ChangeType, BeforeValues, AfterValues).
- **SegmentFormatDefinition**: External dependency (upstream feature US-SF-001) — describes the pattern an Account ID must match.
- **SegmentValueList**: External dependency (upstream feature US-SV-001) — provides valid values per segment position.

---

## Business Rules

| Rule ID | Description | Source |
|---|---|---|
| BR-GLA-001 | Account ID must be unique across all GL accounts | BR-FE-1-001, BR-BE-1-002 |
| BR-GLA-002 | Account ID format must match the active segment format definition | BR-FE-1-001, BR-BE-1-002 |
| BR-GLA-003 | Account ID and Account Type are permanent after first save; changes are rejected | US-AM-001, US-AM-003 |
| BR-GLA-004 | Accounts with posted transactions cannot be deleted | BR-FE-1-002, BR-BE-1-001 |
| BR-GLA-005 | Inactive and Hold accounts may not be selected in new transactions | BR-FE-1-003 |
| BR-GLA-006 | Template accounts may not be used in transactions | US-AM-005 |
| BR-GLA-007 | Account Category must be valid for the selected Account Type | US-AM-003 |
| BR-GLA-008 | Normal Balance (Debit/Credit) is used in journal entry validation (enforced by journal module) | US-AM-003 |
| BR-GLA-009 | All attribute changes are committed with the parent account save in one atomic operation | BR-BE-2-001 |
| BR-GLA-010 | Deletion is also blocked if unposted transactions, budgets, allocations, or group membership exist | US-AM-009 |

---

## Acceptance Criteria

| ID | Linked Req | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-001 | REQ-GLA-001 | Valid segment values and unique ID provided | Accountant saves new account | Account created, 201 returned |
| AC-GLA-002 | REQ-GLA-003 | Account ID already exists | Accountant saves duplicate | 409 Conflict with duplicate-ID message |
| AC-GLA-003 | REQ-GLA-002 | Account ID does not match segment format | Accountant saves | 422 with expected format in error body |
| AC-GLA-004 | REQ-GLA-004 | Existing account, Accountant sends changed Account ID or Account Type | PUT request | Field change rejected; 422 returned |
| AC-GLA-005 | REQ-GLA-005 | Existing account | Accountant PUTs updated Description | 200 returned; Description updated |
| AC-GLA-006 | REQ-GLA-006 | Account Type = Statistical, invalid Category selected | PUT request | 422 with classification-error message |
| AC-GLA-007 | REQ-GLA-011 | Account Status = Inactive | Transaction lookup includes this account | Account excluded / lookup rejects with status error |
| AC-GLA-008 | REQ-GLA-009 | Existing account | Accountant POSTs attribute assignment | 201 returned; attribute linked |
| AC-GLA-009 | REQ-GLA-010 | Multiple attribute changes pending | Parent account saved | All attribute changes committed atomically |
| AC-GLA-010 | REQ-GLA-014 | Account belongs to two groups | GET account-groups | Both groups returned |
| AC-GLA-011 | REQ-GLA-015 | Account has period balance data | GET period-summary | Balances returned by fiscal period |
| AC-GLA-012 | REQ-GLA-016 | Account has posted transactions | GET transactions with date filter | Matching transactions returned |
| AC-GLA-013 | REQ-GLA-017 | Account has posted transactions | DELETE account | 409 Conflict with transaction count |
| AC-GLA-014 | REQ-GLA-017 | Account has no dependencies | DELETE account | 204 No Content; account removed |
| AC-GLA-015 | REQ-GLA-022 | Any caller creates an account | Account saved successfully | Audit entry logged with caller user ID, timestamp, new values |
| AC-GLA-016 | REQ-GLA-022 | Any caller modifies a field | Account saved successfully | Audit entry logged with before and after field values |
| AC-GLA-017 | REQ-GLA-023 | Template-status account exists | Accountant copies it with new ID | New account created with copied settings |

---

## Open Questions and Assumptions

| ID | Type | Description | Impact |
|---|---|---|---|
| Q-001 | RESOLVED | Authorization enforcement is out of scope for this phase; no role-based access control will be implemented; audit trail retained | Resolved 2026-10-16 |
| A-001 | ASSUMPTION | Segment format definition and segment value lists are provided by pre-seeded in-memory stubs (upstream US-SF-001, US-SV-001 not in scope) | Low risk; stubs will reflect a representative format |
| A-002 | ASSUMPTION | No authentication or authorization is enforced in this phase; all callers are trusted; caller identity is caller-supplied for audit purposes only | Explicitly confirmed by Jaymin Trivedi |
| A-003 | ASSUMPTION | "Normal Balance is used in journal entry validation" means it is stored and returned; the journal module (not this feature) uses it | Narrows scope; confirmed by PRD |
| A-004 | ASSUMPTION | Export to Excel (US-AM-006, US-AM-007, US-AM-008) is out of scope for this API-only phase | Low risk; file export requires frontend integration |
| A-005 | ASSUMPTION | Period budget versions (US-AM-007) are stored in stub data; no budget management feature in scope | Low risk |
| A-006 | ASSUMPTION | Currency ID field is stored and returned as a string; no currency validation against a currency master in this phase | Low risk |
| Q-003 | RESOLVED | Frontend technology: Angular + TypeScript confirmed by Jaymin Trivedi | Resolved 2026-10-16 |
| A-007 | ASSUMPTION | Angular app runs as a standalone SPA (no SSR); communicates with backend via REST API; no shared auth provider in this phase | Low risk |

---

## Out of Scope

- Real database / ORM (stub data only in this phase — see AGENTS.md)
- Authorization / access-control enforcement (deferred to a future phase — confirmed Q-001)
- Segment format definition management (upstream US-SF-001)
- Segment value list management (upstream US-SV-001)
- Journal entry posting (uses `NormalBalance`; posting logic is in a separate module)
- Budget management
- Export to Excel / CSV (deferred — no file-download endpoint in this phase)
- External integrations or message queues

---

## Dependencies and Constraints

- **Stub data only**: No database, ORM, or external service. All stores are in-memory, implemented in the `Infrastructure` layer.
- **US-SF-001** (Segment Format): Segment format definition must be available via an in-memory stub at startup.
- **US-SV-001** (Segment Value Lists): Segment value lists must be available via an in-memory stub at startup.
- **Auth context**: User identity and role are read from a stub context (no real authentication provider).
- **Backend architecture**: Must follow the Clean Architecture six-project structure per `AGENTS.md` and `docs/architecture/Microservices_Target_Solution_Structure.md`.
- **Frontend**: Angular + TypeScript (confirmed Q-003). Angular app communicates with the backend via the REST API defined in `contracts/openapi.yaml`. No SSR in this phase.

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Authorization deferred — no access control in this phase | N/A | Resolved: out of scope | Confirmed by Jaymin Trivedi; deferred to future phase |
| Segment format stub diverges from real COA | Low | Medium | Document stub format clearly; flag as migration consideration |
| Attribute transactional boundary in stub data | Low | Low | In-memory store can implement pseudo-transaction; document limitation |
| SOX audit trail stored only in-memory — lost on restart | Medium | Medium | Document "not durable in stub phase"; no data loss risk in production (real DB will be added per ADR) |

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 12 user stories have at least one passing automated test derived from their acceptance criteria.
- **SC-002**: Account creation completes in under 2 seconds under normal load.
- **SC-003**: Account retrieval, update, and attribute operations complete in under 1 second.
- **SC-004**: All deletion attempts on accounts with dependencies are blocked — 0% unguarded deletions.
- **SC-005**: Every create, modify, status-change, and attribute-update event produces an audit log entry — 100% audit coverage.
- **SC-006**: All 17 acceptance criteria (AC-GLA-001 through AC-GLA-017) are covered by tests in `traceability.md`.

---

## Assumptions

- Stub data layer (in-memory) is the only persistence mechanism in this phase; durability is not required.
- Segment format and segment value list stubs will be pre-seeded with a representative multi-segment format (e.g., `CC-DEPT-NATURAL` with 3 segments).
- Authorization enforcement is not implemented in this phase; all callers are trusted. Caller identity (user ID) is accepted as a request header/parameter and used for audit logging only.
- The feature directory follows sequential numbering: `specs/001-gl-account-maintenance`.
- No external services, message queues, or databases will be introduced without an ADR and human approval.

---

## Clarifications

### Session 2026-10-16

- Q: Which roles should have permission to create, modify, or delete GL accounts, and which should be read-only? → A: None; no authorization in this phase — authorization enforcement is out of scope; all callers are trusted; caller identity is accepted for audit logging only.
- Q: How should the system behave when two callers attempt to update the same account at the same time? → A: Last-write-wins — no conflict detection or optimistic locking in this phase; simplest model for stub data; ETag/If-Match deferred to a future phase.
- Q: Which frontend technology should be used for the GL Account Maintenance UI? → A: Angular + TypeScript — confirmed by Jaymin Trivedi; React references in PRD are superseded by this decision.
