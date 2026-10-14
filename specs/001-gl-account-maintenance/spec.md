# Feature Specification: GL Account Maintenance and Inquiry

**Feature directory**: `specs/001-gl-account-maintenance` (no git branch created; working on `master`)
**Created**: 2026-05-30
**Status**: Draft
**Input**: `Account Maintenance and Inquiry PRD.md` (v3.0, stories US-AM-001 .. US-AM-012, 67 points)
**Domain code**: `GLA`

---

## Clarifications

### Session 2026-10-13

- Q: Which roles may create, modify, delete and inquire? -> A: None; no authorization in this phase (Q-001).
- Q: Does the client send segment values or a full Account ID? -> A: The full Account ID (Q-002).
- Q: Which categories are valid? -> A: Defined per Financial account type: Asset, Liability, Equity, Revenue, Expense (Q-003).
- Q: Which status transitions are allowed? -> A: Generic rule: Active, Hold and Inactive move freely; Template is set only at creation (Q-004).
- Q: Is a status-change justification mandatory? -> A: Yes, 1 to 500 characters (Q-005).
- Q: How are attributes saved? -> A: Both with the parent account (atomic) and one at a time (Q-006).
- Q: Normalise the PRD API paths? -> A: Yes, under /api/gl/accounts/{id}/... (Q-007).
- Q: May other-module data be stubbed? -> A: Yes, read-only synthetic seeds (Q-008).
- Q: Which export formats? -> A: .xlsx for period summary; CSV for transactions and groups (Q-009).
- Q: Transaction history page size? -> A: Default 50, maximum 200 (Q-010).
- Q: Currencies and currency changes? -> A: USD, EUR, GBP, JPY; changes always allowed (Q-011).
- Q: Is the audit trail readable here? -> A: No public read; append-only; retention deferred (Q-012).
- Q: Concurrent updates? -> A: Optimistic concurrency with a conflict response (Q-013).
- Q: PRD DDL and database-level validation? -> A: Deferred while data is stubbed (Q-014).
- Q: Is the US-AM-007 details grid a mapping error? -> A: Yes; belongs to US-AM-004 (Q-015).
- Q: What do group details and hierarchy require? -> A: ID, name and parent chain; link by group ID (Q-016).
- Q: Frontend choice? -> A: Framework-neutral UX; frontend ADR first (Q-017).
- Q: Statistical account rules? -> A: Category Statistical; no Balance Type or Normal Balance (Q-018).
- Q: How should as-you-type Account ID validation be handled? -> A: Dropped; errors are shown after submit and NFR-GLA-006 is relaxed (A-007), decided at the contract stage.

---

## PRD Analysis

Every PRD item is categorised. Labels: `[INFERENCE]`, `[ASSUMPTION: ...]`, `[NFR]`,
`[FAILURE]`, `[PERMISSION]`. Question and assumption IDs refer to the table "Open questions and assumptions".

| PRD-ID | Category | Statement |
|---|---|---|
| PRD-GLA-001 | Requirement | US-AM-001: Accountants create GL accounts by supplying a full Account ID, which is validated segment by segment. (Q-002 resolved: full Account ID.) |
| PRD-GLA-002 | Requirement | US-AM-001/010: Account ID is validated against the segment format (edit mask) and segment value lists; duplicates are rejected. |
| PRD-GLA-003 | Requirement | US-AM-001/002/010: Account ID and Account Type become permanent after the first save. |
| PRD-GLA-004 | Requirement | US-AM-002: Description, Category, Currency ID and Status are editable; validation runs before save. |
| PRD-GLA-005 | [INFERENCE] | US-AM-002 "reflected in reporting immediately" is read as: a read after a successful update returns the new values. Reporting/cache invalidation belongs to another module (A-004). |
| PRD-GLA-006 | Requirement | US-AM-003: Category is validated against Account Type; Account Type is Financial or Statistical; Balance Type is Balance Forward or Periodic; Normal Balance is Debit or Credit. Categories are defined for Financial accounts (Q-003 resolved; Statistical handling is Q-018). |
| PRD-GLA-007 | Requirement | US-AM-004: Accounts are linked to one or more attribute type/value pairs used only for classification and reporting. |
| PRD-GLA-008 | Requirement | US-AM-004: Attribute add/edit/delete is staged and persisted in the same transaction as the parent save. Attributes may be saved with the parent account (staged) or one at a time (Q-006 resolved). |
| PRD-GLA-009 | Requirement | US-AM-005: Status values are Active, Hold, Inactive, Template, each with defined availability for transactions. Generic transition rules apply (Q-004 resolved). |
| PRD-GLA-010 | [ASSUMPTION: A-005] | US-AM-005 "status enforcement at transaction entry points": transaction entry is outside this feature, so this feature exposes a posting-eligibility result derived from status. |
| PRD-GLA-011 | Requirement | US-AM-006: Users view the account groups that contain an account, including hierarchy. The PRD's "subject to security permissions" is deferred with Q-001. Each group shows its ID, name and parent chain (Q-016 resolved). |
| PRD-GLA-012 | Requirement | US-AM-006: Group membership can be exported, as CSV (Q-009 resolved). |
| PRD-GLA-013 | Requirement | US-AM-007: Period summary shows balances by fiscal period and fiscal year, plus budget versions, with filter, sort and Excel export. |
| PRD-GLA-014 | Out of scope | US-AM-007 lists a details grid with add/edit/delete and a Save button for a read-only view. Treated as a mapping error: those criteria belong to US-AM-004 (Q-015 resolved). |
| PRD-GLA-015 | Requirement | US-AM-008: Posted journal entries for an account can be listed (batch ID, description, amount, posting status), filtered by date range or fiscal period, drilled into, and exported. |
| PRD-GLA-016 | Requirement | US-AM-009: Deletion is blocked when posted or unposted transactions, budgets, allocations or group memberships exist; otherwise allowed; deactivation is suggested. |
| PRD-GLA-017 | [FAILURE] | US-AM-009: A blocked delete returns 409 Conflict with a message; a successful delete returns 204. |
| PRD-GLA-018 | Requirement | US-AM-010: All account operations are validated against chart-of-accounts rules with clear, actionable messages; enforcement also at API level. |
| PRD-GLA-019 | Deferred | US-AM-011: Only authorised users can create or modify accounts; inquiry respects user and account-level restrictions. Not enforced in this phase (Q-001 resolved: no authorization needed right now). |
| PRD-GLA-020 | Requirement | US-AM-011: Audit trail records creation (user, timestamp), modifications (before/after), status changes (with justification) and attribute updates. A justification is mandatory, 1 to 500 characters (Q-005 resolved). The audit trail is append-only and not readable through a public operation in this feature (Q-012 resolved). |
| PRD-GLA-021 | Requirement | US-AM-012: Template accounts can be created, searched and used to create new accounts with copied settings; templates cannot be used in transactions. |
| PRD-GLA-022 | [NFR] | Create <= 2 s; retrieve, update, attribute operations and dependency check <= 1 s. |
| PRD-GLA-023 | [NFR] | Segment validation occurs in real time (feedback without submitting the form). Relaxed by user decision at the contract stage: errors are shown only after the form is submitted [ASSUMPTION: A-007]. |
| PRD-GLA-024 | Requirement | Business rules BR-FE-1-001/002/003, BR-FE-2-001, BR-FE-3-001, BR-BE-1-001/002, BR-BE-2-001, BR-BE-3-001 (nine rules) apply as listed under "Business rules". |
| PRD-GLA-025 | Out of scope | PowerBuilder window/DataWindow/button/event items marked NA in the PRD (w_gl_account, dw_segments, pb_save, pb_delete, event open, AccountRepository.findById, PB DELETE, PB referential check). |
| PRD-GLA-026 | Out of scope | Database DDL and "validation enforced at database level". Stub data only in this phase; deferred (Q-014 resolved). |
| PRD-GLA-027 | Out of scope | React component names (AccountForm, SegmentGrid, DeleteBtn, AttributeGrid, DetailsForm). Frontend is undecided: UX deliverables stay framework-neutral and a frontend ADR precedes UI implementation (Q-017 resolved). |
| PRD-GLA-028 | Dependency | US-SF-001 (segment structure) and US-SV-001 (segment value lists) are inputs owned by other features; seeded as stub data here (A-009). |
| PRD-GLA-029 | Out of scope | PRD "Next Steps" (stakeholder review, Jira upload, sprint planning). |

---

## Actors and personas

| Persona | Description | Primary goal |
|---|---|---|
| Accountant | Day-to-day account management | Create, modify, classify and inquire on GL accounts; manage attributes and templates |
| Finance Manager | Financial oversight | Review group membership, period summaries and transaction history |
| System Administrator | Security and access control | Control deletion, access and audit trail |
| GL Administrator | Chart-of-accounts configuration | Owns segment formats and segment value lists (inputs to this feature, not a user of its screens) |

---

## Business goals and outcomes

- **Goal 1:** Only valid, unique, correctly classified GL accounts exist, so financial reporting is accurate.
- **Goal 2:** Permanent attributes (Account ID, Account Type) and historical data are protected from accidental change or deletion.
- **Goal 3:** Finance users can inspect balances, groups and transactions of an account without leaving account inquiry.
- **Goal 4:** Every change to an account is attributable and auditable for compliance.

---

## User scenarios *(mandatory)*

Priorities map from the PRD: Must Have = P1, Should Have = P2, Could Have = P3. Error status codes not
stated in the PRD are left to the contract stage and described here as "rejected with a validation error".

### User Story 1 — Create a GL account with segment validation (US-AM-001, Priority: P1)

An Accountant enters a full Account ID for a new account, supplies description, type, category and status,
and saves. The system splits the ID into segments, validates them against the format and value lists, and rejects duplicates.

**Why P1:** Every other capability depends on accounts existing.
**Independent test:** Create one account with valid segments and read it back; repeat with an invalid segment and a duplicate.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-001 | REQ-GLA-001, REQ-GLA-002 | An Accountant and a full Account ID whose segments are all in their value lists and match the format | The Accountant creates the account | The account is created, the response is 201 Created and contains the Account ID, its parsed segments and permanent Account Type |
| AC-GLA-002 (negative) | REQ-GLA-002 | A full Account ID with a segment not in its value list, or a wrong segment count, length or separator | The Accountant creates the account | Nothing is created; the error reads "Account ID does not match segment format. Expected format: {format}" and identifies each invalid segment |
| AC-GLA-003 (negative) | REQ-GLA-003 | An account with ID X already exists | An Accountant creates another account with the same full Account ID X | Nothing is created; the error reads "Account ID 'X' already exists. Please use a unique ID." and the existing account is identifiable |
| AC-GLA-004 | REQ-GLA-005 | An existing account | Its details are requested | 200 OK with segments, description, type, category, currency, status, balance type, normal balance and attributes |
| AC-GLA-005 (negative) | REQ-GLA-005 | No account with the requested ID | Its details are requested | A not-found response is returned and no data is leaked |

---

### User Story 2 — Modify editable account fields (US-AM-002, Priority: P1)

An Accountant changes description, category, currency or status while permanent fields stay protected.

**Why P1:** Accounts must be kept accurate as the business changes.
**Independent test:** Update the description of a seeded account and read it back; attempt to change the Account ID.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-006 | REQ-GLA-006 | An existing account | An Accountant changes the description and saves | 200 OK; a subsequent read returns the new description |
| AC-GLA-007 | REQ-GLA-006 | An existing account | An Accountant changes category and currency to valid values | 200 OK; both values are persisted and returned |
| AC-GLA-008 (negative) | REQ-GLA-004, REQ-GLA-006 | An existing account | An update attempts to change Account ID or Account Type | The update is rejected, no field is changed, and the message names the permanent field |
| AC-GLA-009 (negative) | REQ-GLA-007 | An existing account | An update contains an invalid category or currency | Nothing is saved; field-level errors explain what is wrong |
| AC-GLA-010 (negative) | REQ-GLA-006 | No account with the requested ID | An update is submitted | A not-found response is returned |
| AC-GLA-056 (negative) | REQ-GLA-035 | Two users have read the same account | Both save a change and the second save is based on the older version | The second save is rejected with a conflict response and the first user's change is kept |

---

### User Story 3 — Configure classification and behaviour (US-AM-003, Priority: P1)

An Accountant sets category, account type, balance type and normal balance.

**Why P1:** Classification drives reporting and posting validation.
**Independent test:** Create accounts of each type and check classification fields are accepted or rejected.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-011 | REQ-GLA-008 | A Financial account | Balance Type and Normal Balance are set to allowed values | They are saved and returned |
| AC-GLA-012 (negative) | REQ-GLA-008 | A Financial account with a category outside Asset, Liability, Equity, Revenue, Expense | The account is saved | The save is rejected with a message naming the invalid category and the allowed list |
| AC-GLA-013 (negative) | REQ-GLA-008 | A Balance Type or Normal Balance outside the allowed values | The account is saved | The save is rejected with a field-level error |
| AC-GLA-057 (negative) | REQ-GLA-008 | A Statistical account | It is saved with a category other than Statistical, or with a Balance Type or Normal Balance | The save is rejected with a field-level error |

---

### User Story 4 — Assign reporting attributes (US-AM-004, Priority: P2)

An Accountant links an account to attribute types and values, adds several, edits and removes them.

**Why P2:** Useful for reporting; does not affect posting.
**Independent test:** Add two attributes to a seeded account, edit one, delete one.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-014 | REQ-GLA-009 | An existing account and a valid attribute type with a value belonging to it | The attribute is added | It is stored and returned with the account |
| AC-GLA-015 | REQ-GLA-009 | An account with one attribute | A second attribute of another type is added | Both are returned |
| AC-GLA-016 (negative) | REQ-GLA-010 | An attribute value that does not belong to the chosen attribute type, or an empty required field | The attribute is saved | The save is rejected with a field-level error |
| AC-GLA-017 | REQ-GLA-011 | Staged attribute additions, edits and deletions for one account | The parent account is saved | All staged changes are applied together |
| AC-GLA-018 (negative) | REQ-GLA-011 | Staged changes where one is invalid | The parent account is saved | No staged change and no account change is applied |
| AC-GLA-019 | REQ-GLA-012 | An account whose attributes change | Posting eligibility is evaluated | The result is the same as before the attribute change |

---

### User Story 5 — Manage account status (US-AM-005, Priority: P1)

An Accountant sets and changes the status, which controls availability for transactions.

**Why P1:** Prevents use of inactive or template accounts.
**Independent test:** Read posting eligibility for one account in each status; change a status with a justification.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-020 | REQ-GLA-013 | An account with status Active | Posting eligibility is requested | The account is eligible for new transactions |
| AC-GLA-021 (negative) | REQ-GLA-013 | An account with status Hold, Inactive or Template | Posting eligibility is requested | The account is not eligible for new transactions and the reason names the status |
| AC-GLA-022 | REQ-GLA-014 | An account and an allowed status change | The change is saved with a justification | The new status is stored and the justification is recorded |
| AC-GLA-023 (negative) | REQ-GLA-014 | A status change without a justification, or a change to or from Template | The change is saved | It is rejected and the status is unchanged |

---

### User Story 6 — View account group membership (US-AM-006, Priority: P2)

A Finance Manager sees the groups that contain an account, with hierarchy, and exports them.

**Why P2:** Transparency of reporting structure.
**Independent test:** Open groups for a seeded account that belongs to two groups, then one that belongs to none.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-024 | REQ-GLA-015 | An account that belongs to two groups, one nested in another | Group membership is requested | Both groups are returned with their hierarchy |
| AC-GLA-025 | REQ-GLA-015 | An account that belongs to no group | Group membership is requested | An empty list is returned, not an error |
| AC-GLA-027 | REQ-GLA-016 | An account with group memberships | Export of group membership is requested | A downloadable file lists every group for the account |

Note: criterion number 026 (group visibility by user permission) is retired with Q-001 and is not reused.

---

### User Story 7 — View period summary and balances (US-AM-007, Priority: P1)

A Finance Manager analyses balances by fiscal period, fiscal year and budget version.

**Why P1:** Core inquiry for financial analysis.
**Independent test:** Request the period summary for a seeded account with known balances.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-028 | REQ-GLA-017 | An account with balances in several fiscal periods | The period summary is requested | 200 OK with one balance per fiscal period |
| AC-GLA-029 | REQ-GLA-017 | An account with balances across fiscal years and budget versions | The period summary is requested for annual analysis | Balances by fiscal year and the available budget versions are returned |
| AC-GLA-030 | REQ-GLA-018 | A period summary result | The user filters by fiscal year or sorts by a column | The result contains only matching rows in the requested order |
| AC-GLA-031 | REQ-GLA-019 | A period summary result | Export to Excel is requested | A spreadsheet with the same rows as the (filtered) view is returned |
| AC-GLA-032 (negative) | REQ-GLA-017 | An existing account without balances / an unknown account | The period summary is requested | Empty result with 200 OK / a not-found response respectively |

---

### User Story 8 — View account transaction history (US-AM-008, Priority: P1)

An Accountant reviews posted journal entries for an account with filters, drill-down and export.

**Why P1:** Needed for reconciliation and audit.
**Independent test:** Request transactions of a seeded account with a date range; drill into one entry.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-033 | REQ-GLA-020 | An account with posted journal entries | Transactions are requested | 200 OK with batch ID, description, amount and posting status per entry |
| AC-GLA-034 | REQ-GLA-021 | Entries in several periods | A date range or fiscal period filter is applied | Only entries inside the filter are returned |
| AC-GLA-035 | REQ-GLA-022 | A listed entry | The user drills down | Detailed journal entry information is returned |
| AC-GLA-036 | REQ-GLA-023 | A filtered transaction list | Export is requested | A file with exactly the filtered entries is returned |
| AC-GLA-037 (negative) | REQ-GLA-020 | An account with no transactions, or a very large history (10,000+ entries) | Transactions are requested | An empty list, or a page of at most 200 results (default 50) with a way to reach the rest; never a failure |

---

### User Story 9 — Protect accounts from inappropriate deletion (US-AM-009, Priority: P1)

A System Administrator deletes only accounts that have no dependencies and is steered to deactivation otherwise.

**Why P1:** Preserves historical financial data.
**Independent test:** Delete a seeded dependency-free account (204) and a seeded account with posted transactions (409).

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-038 (negative) | REQ-GLA-024, REQ-GLA-026 | An account with N posted transactions | Deletion is requested | 409 Conflict; message "Cannot delete account '{id}'. It has {N} posted transaction(s)."; deactivation is offered as the alternative; the account still exists |
| AC-GLA-039 (negative) | REQ-GLA-024 | An account with unposted transactions only | Deletion is requested | 409 Conflict naming unposted transactions with their count |
| AC-GLA-040 (negative) | REQ-GLA-024 | An account referenced by a budget, an allocation, or an account group | Deletion is requested | 409 Conflict naming each blocking dependency type and count |
| AC-GLA-041 | REQ-GLA-025 | An account with no dependencies | Deletion is requested by an authorised user | 204 No Content; a later read returns not found |
| AC-GLA-042 (negative) | REQ-GLA-024 | An account with several dependency types | Deletion is requested | One 409 response lists all blocking dependency types |

---

### User Story 10 — Enforce chart-of-accounts validation (US-AM-010, Priority: P1)

All create and update operations are validated server-side against COA rules with actionable messages.

**Why P1:** Data integrity of the chart of accounts.
**Independent test:** Call the operations directly (without any UI) with invalid input.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-043 (negative) | REQ-GLA-007 | A create or update request missing required fields | It is submitted | Nothing is saved; each missing field has its own actionable message |
| AC-GLA-044 (negative) | REQ-GLA-027 | A request that bypasses any client-side checks and contains an invalid segment | It is submitted directly to the service | It is rejected exactly as in AC-GLA-002 |
| AC-GLA-045 (negative) | REQ-GLA-004, REQ-GLA-027 | A previously saved account | A later request changes Account ID or Account Type | It is rejected exactly as in AC-GLA-008 |

---

### User Story 11 — Control access and keep an audit trail (US-AM-011, Priority: P1)

Access control is deferred (Q-001: no authorization needed right now). Every change is still recorded in an audit trail.

**Why P1:** Compliance (SOX) audit trail. The access-control half of US-AM-011 is deferred.
**Independent test:** Perform each kind of change and inspect the audit entries.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-049 | REQ-GLA-030 | A caller who supplies a user identifier | An account is created | An audit entry records that identifier and a timestamp |
| AC-GLA-050 | REQ-GLA-031 | A caller who supplies a user identifier | An account field is modified | An audit entry records the field with its before and after values |
| AC-GLA-051 | REQ-GLA-031 | A caller who supplies a user identifier | A status change is saved or an attribute is added, edited or deleted | An audit entry records the change (and the justification for status changes) |

Note: criterion numbers 046, 047 and 048 (forbidden, unauthenticated, account-level restriction) are retired with Q-001 and are not reused.

---

### User Story 12 — Create accounts from templates (US-AM-012, Priority: P3)

An Accountant keeps template accounts and creates new accounts that copy their settings.

**Why P3:** Efficiency only (Could Have).
**Independent test:** Create a template, search the library, create an account from it.

| AC-ID | Linked REQ | Given | When | Then |
|---|---|---|---|---|
| AC-GLA-052 | REQ-GLA-032 | An Accountant | An account is created with status Template | It is stored as a template and is not eligible for transactions (see AC-GLA-021) |
| AC-GLA-053 | REQ-GLA-033 | An existing template and a valid new full Account ID | An account is created from the template | The new account copies the template's settings, gets its own unique permanent Account ID, and the template is unchanged |
| AC-GLA-054 (negative) | REQ-GLA-033 | An account that is not a template | It is used as a template source | The request is rejected with a message saying the source is not a template |
| AC-GLA-055 | REQ-GLA-034 | Several templates with different descriptions | The library is searched or filtered | Only matching templates are returned |

---

## Functional requirements

| ID | Requirement | Priority | Source |
|---|---|---|---|
| REQ-GLA-001 | System MUST let a user create a GL account from a full Account ID plus description, account type, category, currency, status, balance type and normal balance. | P1 | PRD-GLA-001 |
| REQ-GLA-002 | System MUST split the supplied Account ID into segments and validate them against the segment format (edit mask) and segment value lists before creating the account, and report which segments are invalid. | P1 | PRD-GLA-002 |
| REQ-GLA-003 | System MUST reject creation of an account whose Account ID already exists. | P1 | PRD-GLA-002 |
| REQ-GLA-004 | System MUST make Account ID and Account Type immutable after the first save and reject any attempt to change them. | P1 | PRD-GLA-003 |
| REQ-GLA-005 | System MUST return one account by ID with its segments, classification, status and attributes, or not found. | P1 | PRD-GLA-001, PRD-GLA-005 |
| REQ-GLA-006 | System MUST allow updating Description, Category, Currency ID and Status only, and make the change visible to subsequent reads. | P1 | PRD-GLA-004, PRD-GLA-005 |
| REQ-GLA-007 | System MUST validate all required fields and field values before saving a create or update and return one actionable message per problem. Currency ID must be one of USD, EUR, GBP, JPY, and a currency change is allowed at any time. | P1 | PRD-GLA-004, PRD-GLA-018 |
| REQ-GLA-008 | System MUST validate Category against Account Type (Financial categories: Asset, Liability, Equity, Revenue, Expense) and restrict Account Type, Balance Type and Normal Balance to their allowed values. Statistical accounts have category Statistical and must not carry a Balance Type or Normal Balance; Financial accounts require both. | P1 | PRD-GLA-006 |
| REQ-GLA-009 | System MUST let a user add, edit and remove attribute assignments (attribute type + value) on an account; an account may have several. | P2 | PRD-GLA-007 |
| REQ-GLA-010 | System MUST validate attribute fields, including that the value belongs to the chosen attribute type. | P2 | PRD-GLA-007, PRD-GLA-024 |
| REQ-GLA-011 | System MUST apply staged attribute changes together with the parent account save as one all-or-nothing change; attributes can also be added, edited or removed one at a time. | P2 | PRD-GLA-008 |
| REQ-GLA-012 | System MUST NOT let attribute assignments affect an account's posting eligibility. | P2 | PRD-GLA-007 |
| REQ-GLA-013 | System MUST support statuses Active, Hold, Inactive and Template and report posting eligibility: only Active is eligible for new transactions. | P1 | PRD-GLA-009, PRD-GLA-010 |
| REQ-GLA-014 | System MUST require a justification of 1 to 500 characters for every status change; Active, Hold and Inactive may move freely among each other, and Template is set only at creation and never changes to or from another status. | P1 | PRD-GLA-009, PRD-GLA-020 |
| REQ-GLA-015 | System MUST list the account groups that contain an account, with hierarchy (each group's ID, name and parent chain). | P2 | PRD-GLA-011 |
| REQ-GLA-016 | System MUST export the group membership of an account as CSV. | P2 | PRD-GLA-012 |
| REQ-GLA-017 | System MUST return an account's balances by fiscal period, by fiscal year, and by budget version. | P1 | PRD-GLA-013 |
| REQ-GLA-018 | System MUST support filtering and sorting of period summary results. | P1 | PRD-GLA-013 |
| REQ-GLA-019 | System MUST export the (filtered) period summary to Excel (.xlsx). | P1 | PRD-GLA-013 |
| REQ-GLA-020 | System MUST list posted journal entries of an account with batch ID, description, amount and posting status, in server-side pages (default 50, maximum 200 per page). | P1 | PRD-GLA-015 |
| REQ-GLA-021 | System MUST filter transaction history by date range or fiscal period. | P1 | PRD-GLA-015 |
| REQ-GLA-022 | System MUST return detailed journal entry information for a listed entry. | P1 | PRD-GLA-015 |
| REQ-GLA-023 | System MUST export the (filtered) transaction list as CSV. | P1 | PRD-GLA-015 |
| REQ-GLA-024 | System MUST block deletion, with a conflict response, when posted transactions, unposted transactions, budgets, allocations or group memberships reference the account, naming each blocker and its count. | P1 | PRD-GLA-016, PRD-GLA-017 |
| REQ-GLA-025 | System MUST delete an account with no dependencies and confirm without a body. | P1 | PRD-GLA-016, PRD-GLA-017 |
| REQ-GLA-026 | System MUST suggest deactivation as the alternative whenever a deletion is blocked. | P1 | PRD-GLA-016 |
| REQ-GLA-027 | System MUST enforce all COA validation rules on the service side for every operation, independent of any client. | P1 | PRD-GLA-018 |
| REQ-GLA-028 | DEFERRED (Q-001): create, modify and delete restricted to authorised, identified users. Not enforced in this phase; to be specified in a later feature with its own ADR. | Deferred | PRD-GLA-019 |
| REQ-GLA-029 | DEFERRED (Q-001): user and account-level security restrictions on inquiries. Not enforced in this phase; to be specified in a later feature with its own ADR. | Deferred | PRD-GLA-019 |
| REQ-GLA-030 | System MUST record an audit entry for every account creation with the caller-supplied user identifier (unverified, A-008) and a timestamp. | P1 | PRD-GLA-020 |
| REQ-GLA-031 | System MUST record audit entries for modifications (before/after values), status changes (with justification) and attribute updates. | P1 | PRD-GLA-020 |
| REQ-GLA-032 | System MUST allow creating accounts with status Template. | P3 | PRD-GLA-021 |
| REQ-GLA-033 | System MUST create a new account from a template, copying the template's settings but not its Account ID. | P3 | PRD-GLA-021 |
| REQ-GLA-034 | System MUST let users search and filter the template library. | P3 | PRD-GLA-021 |
| REQ-GLA-035 | System MUST reject an update that is based on a stale version of the account with a conflict response and leave the stored account unchanged (optimistic concurrency). | P1 | PRD-GLA-004 |

---

## Non-functional requirements

Targets are taken from the PRD. In this phase they are measured against stub data only; behaviour under a durable store is **not verified (stub data)**.

| ID | NFR | Target | Measurement |
|---|---|---|---|
| NFR-GLA-001 | Account creation | Completes within 2 s | Automated timing test on create |
| NFR-GLA-002 | Account retrieval for editing | Completes within 1 s | Automated timing test on read |
| NFR-GLA-003 | Account update | Completes within 1 s | Automated timing test on update |
| NFR-GLA-004 | Attribute operations | Complete within 1 s | Automated timing test |
| NFR-GLA-005 | Deletion dependency check | Completes within 1 s, including accounts with 10,000+ transactions | Automated timing test with large seed account |
| NFR-GLA-006 | Segment validation feedback | Relaxed (A-007): validation errors are returned and shown after the form is submitted; no as-you-type validation and no separate validation operation | Create-account tests (AC-GLA-002, AC-GLA-044) |
| NFR-GLA-007 | Audit trail integrity | Audit entries are append-only; none can be changed or removed through this feature | Test attempting modification |
| NFR-GLA-008 | Seed data | Synthetic only; no real names, emails or identifiers | Review of seed files |

---

## Business rules

- **BR-FE-1-001:** Account ID must be unique and match the defined segment format.
- **BR-BE-1-002:** Account ID format is validated against the segment format definition (also on the service side).
- **BR-FE-1-002:** Accounts with posted transactions cannot be deleted.
- **BR-BE-1-001:** Deletion is blocked if transaction history exists.
- **BR-FE-1-003:** Inactive accounts may not be selected in new transactions.
- **BR-FE-2-001 / BR-BE-2-001:** Standard field-level validation applies to attribute fields.
- **BR-FE-3-001 / BR-BE-3-001:** Standard field/data validation applies to details data.
- **BR-GLA-010 [INFERENCE]:** Hold, Inactive and Template accounts are not eligible for new transactions; existing history is preserved.
- **BR-GLA-011 [INFERENCE]:** A blocked deletion lists every blocking dependency, not only the first one found.

---

## Edge cases and failure scenarios

- Duplicate Account ID (AC-GLA-003); invalid segment format (AC-GLA-002).
- Deleting an account with 10,000+ posted transactions: blocked quickly, message shows the count, deactivation offered (AC-GLA-038, NFR-GLA-005).
- Transaction history of 10,000+ entries is returned in bounded pages (AC-GLA-037).
- Two users update the same account at the same time: the second, stale update is rejected with a conflict response (REQ-GLA-035, AC-GLA-056).
- An account that is both inactive and still referenced in groups or budgets: inquiry still works, deletion still blocked.
- A template account is edited or deleted after accounts were created from it: created accounts are unaffected.
- Unauthenticated or unauthorised caller: no authentication or authorization is enforced in this phase (Q-001); see the risk register.
- Unknown account ID on any operation: not found, no data leaked.
- Export requested for an empty result: an empty file with headers, not an error.

---

## API operations required

Paths are **proposed**; the PRD paths are inconsistent (`/api/gl/accounts`, `/api/account/:id/...`, `/api/account-attributes`). See Q-007. No authentication or authorization in this phase (Q-001 resolved), so the Auth column is "None (deferred)".

| API-ID | Operation | Method + Path | Auth | Linked requirements |
|---|---|---|---|---|
| API-GLA-001 | Create account | POST /api/gl/accounts | None (deferred) | REQ-GLA-001, REQ-GLA-002, REQ-GLA-003, REQ-GLA-007, REQ-GLA-008 |
| API-GLA-002 | Get account | GET /api/gl/accounts/{id} | None (deferred) | REQ-GLA-005, REQ-GLA-013 |
| API-GLA-003 | Update account | PUT /api/gl/accounts/{id} | None (deferred) | REQ-GLA-004, REQ-GLA-006, REQ-GLA-007, REQ-GLA-011, REQ-GLA-014 |
| API-GLA-004 | Delete account | DELETE /api/gl/accounts/{id} | None (deferred) | REQ-GLA-024, REQ-GLA-025, REQ-GLA-026 |
| API-GLA-005 | Add attribute | POST /api/gl/accounts/{id}/attributes | None (deferred) | REQ-GLA-009, REQ-GLA-010 |
| API-GLA-006 | Update attribute | PUT /api/gl/accounts/{id}/attributes/{attributeId} | None (deferred) | REQ-GLA-009, REQ-GLA-010 |
| API-GLA-007 | Remove attribute | DELETE /api/gl/accounts/{id}/attributes/{attributeId} | None (deferred) | REQ-GLA-009 |
| API-GLA-008 | List account groups | GET /api/gl/accounts/{id}/groups | None (deferred) | REQ-GLA-015 |
| API-GLA-009 | Export account groups | GET /api/gl/accounts/{id}/groups/export | None (deferred) | REQ-GLA-016 |
| API-GLA-010 | Period summary | GET /api/gl/accounts/{id}/period-summary | None (deferred) | REQ-GLA-017, REQ-GLA-018 |
| API-GLA-011 | Export period summary | GET /api/gl/accounts/{id}/period-summary/export | None (deferred) | REQ-GLA-019 |
| API-GLA-012 | List transactions | GET /api/gl/accounts/{id}/transactions | None (deferred) | REQ-GLA-020, REQ-GLA-021 |
| API-GLA-013 | Journal entry detail | GET /api/gl/accounts/{id}/transactions/{entryId} | None (deferred) | REQ-GLA-022 |
| API-GLA-014 | Export transactions | GET /api/gl/accounts/{id}/transactions/export | None (deferred) | REQ-GLA-023 |
| API-GLA-015 | Search accounts / template library | GET /api/gl/accounts | None (deferred) | REQ-GLA-034 |
| API-GLA-016 | Create account from template | POST /api/gl/accounts (with template reference) | None (deferred) | REQ-GLA-032, REQ-GLA-033 |

---

## UX screens required

Wireframes are produced framework-neutral (Q-017).

| UX-ID | Screen | Route | Linked requirements |
|---|---|---|---|
| UX-GLA-001 | Account create / edit form with full Account ID entry; validation errors shown after submit | /gl/accounts/new, /gl/accounts/{id}/edit | REQ-GLA-001, REQ-GLA-002, REQ-GLA-003, REQ-GLA-006, REQ-GLA-007, REQ-GLA-008, REQ-GLA-013, REQ-GLA-014 |
| UX-GLA-002 | Account inquiry with entry points to Account Groups, Period Summary and Transactions | /gl/accounts/{id} | REQ-GLA-005, REQ-GLA-015, REQ-GLA-017, REQ-GLA-020 |
| UX-GLA-003 | Attribute panel (add, edit, mark for delete, staged save) | within UX-GLA-001 | REQ-GLA-009, REQ-GLA-010, REQ-GLA-011 |
| UX-GLA-004 | Delete confirmation and blocked-delete message with "deactivate instead" | dialog on UX-GLA-002 | REQ-GLA-024, REQ-GLA-025, REQ-GLA-026 |
| UX-GLA-005 | Period summary and transaction list views with filter, sort, paging, export, drill-down | /gl/accounts/{id}/period-summary, /gl/accounts/{id}/transactions | REQ-GLA-017 .. REQ-GLA-023 |
| UX-GLA-006 | Template library and create-from-template | /gl/account-templates | REQ-GLA-032, REQ-GLA-033, REQ-GLA-034 |

---

## Open questions and assumptions

Q-001 to Q-004 were answered by the user. Q-005 to Q-018 were resolved when the user accepted the suggested default for each in the clarification step. No open questions remain.

| ID | Type | Description | Impact | Status |
|---|---|---|---|---|
| Q-001 | RESOLVED | Roles and authorization. Answer (user): no authorization is needed right now. Authentication and authorization are not enforced in this phase. Effect: requirements REQ-GLA-028 and REQ-GLA-029 and the access-control acceptance criteria numbered 046 to 048 and 026 are deferred (removed from the active set; numbers are retired, not reused); audit "user" is a caller-supplied identifier (A-008). Real access control needs its own feature, ADR and human approval before any production use. | Closed; residual risk recorded in risk register | Resolved |
| Q-002 | RESOLVED | Account ID input. Answer (user): the client sends the full Account ID. The system splits it into segments using the segment format. The segment format and value lists remain seeded stub data (A-009). | Closed | Resolved |
| Q-003 | RESOLVED | Categories. Answer (user): valid categories are defined per Financial account type. Recorded as Asset, Liability, Equity, Revenue, Expense (the earlier suggested list, accepted). Statistical accounts are covered by the new Q-018. | Closed for Financial accounts | Resolved |
| Q-004 | RESOLVED | Status transitions. Answer (user): use the generic one. Recorded as: Active, Hold and Inactive move freely among each other; Template is set only at creation and never changes to or from another status. | Closed | Resolved |
| Q-005 | RESOLVED | Status change justification. Answer (user, accepted default): mandatory for every status change, 1 to 500 characters. See REQ-GLA-014. | Closed | Resolved |
| Q-006 | RESOLVED | Attribute saving. Answer (accepted default): support both. The account update accepts a set of attribute changes applied atomically (AC-GLA-017, AC-GLA-018); the standalone attribute endpoints act on one attribute at a time. See REQ-GLA-011. | Closed | Resolved |
| Q-007 | RESOLVED | API paths. Answer (accepted default): normalise all paths under /api/gl/accounts/{id}/... as listed in "API operations required". | Closed | Resolved |
| Q-008 | RESOLVED | Other-module data. Answer (accepted default): transactions, budgets, allocations, groups, period balances, attribute types/values and currencies are read-only synthetic seeds behind store interfaces, covering empty, populated and blocking states. | Closed | Resolved |
| Q-009 | RESOLVED | Export formats. Answer (accepted default): .xlsx for the period summary; CSV for transactions and group membership. See REQ-GLA-016, REQ-GLA-019, REQ-GLA-023. | Closed | Resolved |
| Q-010 | RESOLVED | Paging. Answer (accepted default): server-side paging for transaction history, default 50, maximum 200 per page. See REQ-GLA-020. | Closed | Resolved |
| Q-011 | RESOLVED | Currency. Answer (accepted default): seeded list USD, EUR, GBP, JPY; a currency change is always allowed. See REQ-GLA-007. | Closed | Resolved |
| Q-012 | RESOLVED | Audit readability and retention. Answer (accepted default): not readable through a public operation in this feature; stored append-only behind a store interface and verified by tests; retention deferred. See NFR-GLA-007. | Closed | Resolved |
| Q-013 | RESOLVED | Concurrency. Answer (accepted default): optimistic concurrency; a stale update is rejected with a conflict response. See REQ-GLA-035, AC-GLA-056. | Closed | Resolved |
| Q-014 | RESOLVED | PRD DDL and database-level validation. Answer (accepted default): deferred while data is stubbed; the model is derived from requirements; recorded as "not verified (stub data)". | Closed | Resolved |
| Q-015 | RESOLVED | US-AM-007 details grid. Answer (accepted default): mapping error; the period summary is read-only and those criteria belong to US-AM-004. | Closed | Resolved |
| Q-016 | RESOLVED | Group details and hierarchy. Answer (accepted default): show each group's ID, name and parent chain; navigation is a link by group ID only; group maintenance is out of scope. See REQ-GLA-015. | Closed | Resolved |
| Q-017 | RESOLVED | Frontend. Answer (accepted default): UX deliverables stay framework-neutral; a frontend ADR is created before UI implementation. | Closed | Resolved |
| Q-018 | RESOLVED | Statistical accounts. Answer (accepted default): category is Statistical; Balance Type and Normal Balance are not allowed. See REQ-GLA-008, AC-GLA-057. | Closed | Resolved |
| A-001 | [ASSUMPTION: Data layer is deterministic in-memory stub data behind feature-specific store interfaces; no database, ORM or migrations.] | Per AGENTS.md; low risk | Approved by repository policy |
| A-002 | [ASSUMPTION: Error messages in the PRD (duplicate ID, invalid format, blocked delete) are used verbatim, with placeholders filled in.] | Low risk | Pending approval |
| A-003 | [ASSUMPTION: Validation failures use a standard problem-details error body; exact status codes (400 vs 422) and duplicate = 409 are fixed in the contract stage.] | Low risk | Pending approval |
| A-004 | [ASSUMPTION: "Reflected in reporting immediately" means read-after-write consistency for this feature's own reads; reporting and cache invalidation are out of scope.] | Low risk | Pending approval |
| A-005 | [ASSUMPTION: Status enforcement at transaction entry points is delivered as a posting-eligibility result on the account; entering transactions is another feature.] | Low risk | Pending approval |
| A-006 | [ASSUMPTION: PRD priorities map Must Have = P1, Should Have = P2, Could Have = P3.] | Low risk | Pending approval |
| A-007 | [ASSUMPTION: The PRD's "real-time" segment validation is dropped by user decision (contract stage): errors are shown only after the form is submitted, and no validation-only operation is added. The UI may still show the expected format as a hint.] | Low risk; relaxes NFR-GLA-006 | Pending approval |
| A-008 | [ASSUMPTION: Because no authorization is enforced (Q-001), the audit "user" is an identifier supplied by the caller (for example a request header) and is not verified. If it is absent, the audit entry records "unknown".] | Medium: audit value is weak until real authentication exists | Pending approval |
| A-009 | [ASSUMPTION: The full Account ID uses seeded stub segment structure: three numeric segments joined by hyphens (4-3-2 digits, total at most 32 characters), with a small seeded value list per segment. The real structure comes from US-SF-001 and US-SV-001 later.] | Medium: replaceable seed | Pending approval |

---

## Out of scope

- Definition and maintenance of segment structures and segment value lists (US-SF-001, US-SV-001), attribute type/value maintenance, account group maintenance, transaction entry, posting and budgeting.
- Real database, DDL, migrations, database-level validation, durable transactions.
- PowerBuilder-specific items and React component naming (see PRD-GLA-025, PRD-GLA-027).
- Authentication and authorization of any kind (Q-001), including role checks, user and account-level restrictions, and forbidden/unauthenticated responses. Requirements REQ-GLA-028 and REQ-GLA-029 are deferred.
- Choice of frontend framework (separate ADR).
- Reporting and cache invalidation.
- The details grid, staged Save button and add/edit/delete criteria listed under US-AM-007: treated as a mapping error and covered by US-AM-004 (Q-015).
- PRD "Next Steps" (stakeholder review, Jira upload, sprint planning).

---

## Dependencies and constraints

- **Dependency:** US-SF-001 and US-SV-001 supply the segment structure and value lists; stubbed here (Q-002).
- **Dependency:** Other modules own transactions, budgets, allocations, account groups and period balances; read-only stubs here (Q-008).
- **Constraint:** Data layer is stub data only in this phase (no database/ORM). Durability, transactions and migrations are recorded as deferred and "not verified (stub data)".
- **Constraint:** No authentication or authorization is implemented in this phase (Q-001). Adding either later is a high-risk change that requires an ADR and explicit human approval (AGENTS.md). The frontend choice also requires an ADR.
- **Constraint:** Target runtime intent is .NET 10 / ASP.NET Core 10; no project exists yet.

---

## Success criteria (measurable)

| SC-ID | Criterion | How measured |
|---|---|---|
| SC-001 | An Accountant can create a valid account in under 2 seconds from submit to confirmation | NFR-GLA-001 timing test |
| SC-002 | 100% of attempts to create an account with an invalid or duplicate Account ID are refused, with 0 such accounts stored | AC-GLA-002, AC-GLA-003, AC-GLA-044 tests |
| SC-003 | 100% of attempts to change Account ID or Account Type after creation are refused | AC-GLA-008, AC-GLA-045 tests |
| SC-004 | 100% of deletion attempts on accounts with any dependency are refused and list all blockers; dependency-free accounts delete successfully | AC-GLA-038 .. AC-GLA-042 tests |
| SC-005 | Users find the account's groups, balances and transactions from the inquiry screen in no more than one step each | UX component map review |
| SC-006 | Every account creation, modification, status change and attribute update produces exactly one audit entry with user and time | AC-GLA-049 .. AC-GLA-051 tests |
| SC-007 | Every active acceptance criterion (AC-GLA-001 .. AC-GLA-057 except the retired 026, 046, 047, 048) has at least one automated test mapped to it | Traceability gate |
| SC-008 | Retired: access-control success criterion deferred with Q-001; the number is not reused | Not applicable in this phase |

---

## Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| No authentication or authorization in this phase (Q-001): anyone who can reach the service can create, modify or delete accounts, and the audit user is unverified | High | High | Deferred by user decision; use only in local or non-production environments; a separate feature, ADR and human approval are required before any production use |
| Segment structure and value lists are seeded stub values (A-009), not the real chart-of-accounts structure | Medium | High | Keep seeds replaceable behind a store interface; replace when US-SF-001 and US-SV-001 are delivered |
| PRD paths and mapped criteria are inconsistent (Q-007, Q-015) | Medium | Medium | Paths normalised in the contract (Q-007); the US-AM-007 grid is treated as out of scope (Q-015) |
| Dependency data lives in other modules, so delete and inquiry behaviour is only simulated | High | Medium | Read-only stubs; record "not verified (stub data)" |
| Stub performance numbers mislead about production behaviour | Medium | Medium | Label NFR evidence as stub-only |
| Spreadsheet export adds a dependency | Medium | Low | .xlsx for the period summary only (Q-009); choose and record the library in the plan |
| Scope is large (12 stories, 67 points) | Medium | Medium | Deliver in vertical slices ordered P1, then P2, then P3 |
