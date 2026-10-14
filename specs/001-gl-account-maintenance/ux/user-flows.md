# User Flows — GL Account Maintenance and Inquiry

Framework-neutral (spec Q-017). ADR-002 now decides Angular with Angular Material (`src/GlAccounts.Web`);
components Material provides are `existing` in `component-map.yaml`, the rest remain design-system `gap`s.
Wireframes are grayscale layout sketches in `ux/wireframes/`, one per screen.

Validation errors are shown **after submit** only (spec A-007, NFR-GLA-006 relaxed). The create form may
show the expected Account ID format as a static hint (4-3-2 digits, e.g. `1000-100-01`).

## Screen index

| UX-ID | Screen | Route | Wireframe |
|---|---|---|---|
| UX-GLA-001 | Account create / edit form | `/gl/accounts/new`, `/gl/accounts/{id}/edit` | `wireframes/UX-GLA-001.html` |
| UX-GLA-002 | Account inquiry | `/gl/accounts/{id}` | `wireframes/UX-GLA-002.html` |
| UX-GLA-003 | Attribute panel (inside UX-GLA-001) | within UX-GLA-001 | `wireframes/UX-GLA-003.html` |
| UX-GLA-004 | Delete confirmation and blocked-delete message (dialog on UX-GLA-002) | dialog | `wireframes/UX-GLA-004.html` |
| UX-GLA-005 | Period summary and transaction list | `/gl/accounts/{id}/period-summary`, `/gl/accounts/{id}/transactions` | `wireframes/UX-GLA-005.html` |
| UX-GLA-006 | Template library and create-from-template | `/gl/account-templates` | `wireframes/UX-GLA-006.html` |

Account groups are shown as a panel on UX-GLA-002 (entry point plus list), not as a separate route.

## Flow 1: Create an account (REQ-GLA-001, 002, 003, 007, 008, 030)

Actor: Accountant
Entry: `/gl/accounts/new` (UX-GLA-001)
API: API-GLA-001 `createAccount`
Steps:
  1. Enter the full Account ID, description, account type, category, currency, status, and (Financial only) balance type and normal balance.
  2. Optionally add attributes in the attribute panel (UX-GLA-003); they are sent with the create call.
  3. Select Save. The button is disabled while the request is in flight.
Exit: 201, then `/gl/accounts/{id}` (UX-GLA-002) with a success confirmation.
Error paths: 400 field errors shown beside each field after submit, including the invalid-segment message; 409 duplicate shown at the Account ID field; 500 error banner, form values kept.

## Flow 2: Edit an account (REQ-GLA-004, 006, 007, 014, 035)

Actor: Accountant
Entry: Edit on UX-GLA-002, route `/gl/accounts/{id}/edit` (UX-GLA-001)
API: API-GLA-002 `getAccount` (keeps the ETag), API-GLA-003 `updateAccount`
Steps:
  1. The form loads with Account ID and Account Type read-only.
  2. Change description, category, currency or status. A changed status reveals a required justification field (1-500 chars).
  3. Optionally stage attribute changes (Flow 4).
  4. Select Save; the stored ETag is sent as `If-Match`.
Exit: 200, back to UX-GLA-002 with a success confirmation.
Error paths: 400 field errors; 404 "account no longer exists"; **409 stale version** shows "changed by someone else, reload" with a Reload action, and the user's input is not silently discarded; 500 banner.

## Flow 3: Inquire on an account (REQ-GLA-005, 013, 015)

Actor: Accountant, Finance Manager
Entry: `/gl/accounts/{id}` (UX-GLA-002)
API: API-GLA-002 `getAccount`, API-GLA-008 `listAccountGroups`
Steps:
  1. View classification, status, posting eligibility, segments and attributes.
  2. View the account groups panel (ID, name, parent chain), or export it as CSV (API-GLA-009 `exportAccountGroups`).
  3. Follow one link each to Period Summary, Transactions, Edit, or Delete (SC-005: one step each).
Exit: any linked screen.
Error paths: 404 shows a not-found state with a link to search; groups panel shows its own empty state when the account is in no group.

## Flow 4: Manage attributes (REQ-GLA-009, 010, 011)

Actor: Accountant
Entry: attribute panel on UX-GLA-001 (UX-GLA-003)
API: staged: part of API-GLA-001 / API-GLA-003 (`attributeChanges`); one at a time: API-GLA-005 `addAccountAttribute`, API-GLA-006 `updateAccountAttribute`, API-GLA-007 `removeAccountAttribute`
Steps:
  1. Add a row (attribute type, then a value that belongs to the type), edit a row, or mark a row for delete.
  2. Staged mode: changes stay in the panel until the parent Save, then apply all-or-nothing.
  3. Immediate mode (account already saved): an action on a row calls the single-attribute operation at once.
Exit: panel shows the stored attributes after Save.
Error paths: value not in type or empty field gives a row-level error; if one staged change is invalid nothing is applied and the panel keeps all staged rows; 409 on the parent Save as in Flow 2.

## Flow 5: Delete an account (REQ-GLA-024, 025, 026)

Actor: System Administrator
Entry: Delete on UX-GLA-002, opens the dialog (UX-GLA-004)
API: API-GLA-004 `deleteAccount`
Steps:
  1. Confirm the delete in the dialog.
  2. 204: the dialog closes and the user lands on search with a success confirmation.
  3. 409: the dialog lists every blocker with its count and offers "Set status to Inactive instead", which opens the edit form (Flow 2) with status Inactive preselected.
Exit: search page, or the edit form.
Error paths: 404 account already gone; 500 banner inside the dialog.

## Flow 6: Review balances (REQ-GLA-017, 018, 019)

Actor: Finance Manager
Entry: Period Summary link on UX-GLA-002 (UX-GLA-005)
API: API-GLA-010 `getPeriodSummary`, API-GLA-011 `exportPeriodSummary`
Steps:
  1. View balances by fiscal period, by fiscal year, and the budget versions.
  2. Filter by fiscal year, sort by a column.
  3. Export to Excel with the current filter and sort.
Exit: stays on screen; a file downloads.
Error paths: empty result shows the empty state (200 with empty lists); 404; 500.

## Flow 7: Review transactions (REQ-GLA-020, 021, 022, 023)

Actor: Accountant, Finance Manager
Entry: Transactions link on UX-GLA-002 (UX-GLA-005)
API: API-GLA-012 `listAccountTransactions`, API-GLA-013 `getAccountTransaction`, API-GLA-014 `exportAccountTransactions`
Steps:
  1. View the first page (50 rows). Move between pages; page size is selectable up to 200.
  2. Filter by date range **or** fiscal period (the two are mutually exclusive in the UI, matching the 400 rule).
  3. Select a row to open the journal entry detail with its lines.
  4. Export the filtered set as CSV (all matching rows, not only the page).
Exit: stays on screen; a file downloads.
Error paths: invalid filter combination shows an inline error; empty state; 404; 500.

## Flow 8: Use the template library (REQ-GLA-032, 033, 034)

Actor: Accountant
Entry: `/gl/account-templates` (UX-GLA-006)
API: API-GLA-015 `searchAccounts` (status = Template), API-GLA-001/016 `createAccount` (with `templateAccountId`)
Steps:
  1. Search or filter the template list.
  2. Select "Create account from template" on a row and enter a new full Account ID (other settings are prefilled and editable).
  3. Save.
Exit: 201, then UX-GLA-002 of the new account.
Error paths: duplicate Account ID (409) and invalid segments (400) as in Flow 1; the source no longer a template shows the 400 message; empty library shows the empty state.

## Stub data scenarios per state

Screens render from the deterministic seed set (plan "Seed data states"). The foundation task must seed the fixed
IDs below; the contract examples already use them.

| State | How it is reproduced from stub data |
|---|---|
| loading | Test or demo hook that delays the store response; not a seed record |
| empty | Account in no group (groups panel); account without balances (period summary); account without transactions (transactions); a search with no match; empty template filter |
| populated | `1000-100-01` Active Financial account with attributes; account with two nested groups; account with balances over several periods and years |
| error | A test fault-injection store that throws; not reachable from seed data |
| forbidden | **Reserved and unreachable in this phase.** No authentication or authorization exists (Q-001), so no API response can produce this state. The state is declared only because the UX gate requires it; each wireframe shows a placeholder for it. |
| paging / large | `1000-200-01` with 10,001 posted transactions |
| delete blocked | `1000-200-01` (posted transactions); accounts with only unposted transactions, with a budget, an allocation, a group; an account with several blocker types |
| delete allowed | Active Financial account with no dependencies |
| template | `9000-100-01` Template account |
| stale version (409) | Test performs two updates using the same ETag |

## Accessibility (WCAG 2.1 AA)

**Keyboard paths**
- Create/Edit form: Tab order follows visual order (Account ID, description, type, category, currency, status, justification, balance type, normal balance, attribute panel, Save, Cancel). Enter in a field does not submit unless focus is on Save. Escape cancels an open dialog.
- Inquiry: links and buttons in the order panels appear; Delete opens a dialog that traps focus and returns it to the Delete button on close.
- Lists and tables: filter controls, sortable column headers (buttons), rows (drill-down by Enter or Space), pagination controls, Export button.
- Attribute panel: Add/Edit/Remove are buttons inside each row; arrow keys are not required.

**Roles and ARIA (only where semantics are insufficient)**
- Tables use real `<table>`; sortable headers expose `aria-sort`.
- Delete dialog uses `role="alertdialog"` with `aria-labelledby` and `aria-describedby`.
- Field errors are linked with `aria-describedby`; the form error summary has `role="alert"`.
- Loading regions use `aria-busy="true"`.
- Status and posting eligibility are text, never colour alone.

**Contrast and visuals**
- Text contrast at least 4.5:1, large text and UI component boundaries at least 3:1. Error and success states carry an icon or text label as well as colour.

**Screen-reader announcements**
- Save success, delete success, and export started are announced in a polite live region (`role="status"`).
- Validation failures, 409 conflicts, and 500 errors are announced in an assertive live region (`role="alert"`).
- After filter, sort or page change, the result count is announced ("50 of 10,001 entries").
- Export buttons announce the format ("Export CSV", "Export Excel").

## Design-system gaps and open UX decisions

- ADR-002 is **Accepted** (Angular + Angular Material). Components without a Material equivalent stay `gap`s and need human review.
- The group panel is not a route; group maintenance and navigation to a group page are out of scope (Q-016).
