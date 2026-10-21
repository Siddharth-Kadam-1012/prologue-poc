# Tasks: GL Account Maintenance and Inquiry

**Feature**: 001-gl-account-maintenance  
**Input**: `specs/001-gl-account-maintenance/` — spec.md, plan.md, data-model.md, contracts/openapi.yaml, research.md  
**Updated**: 2026-10-16 — Angular + TypeScript frontend added (Q-003, Jaymin Trivedi)

**Prerequisites**: All design artifacts approved. OpenAPI contract gate passed.

**Tests**: Test tasks included — spec requires all 17 ACs covered (SC-006).

**Stack**: Backend = .NET 10 ASP.NET Core 10 (six-project Clean Architecture, stub data). Frontend = Angular + TypeScript SPA.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Parallelizable (different files, no shared dependencies)
- **[Story]**: Which user story this belongs to (US1–US12)

---

## Phase 1: Backend Setup (Solution Scaffold)

**Purpose**: Create the six-project Clean Architecture solution, build config, CORS, and verify the empty build passes.

- [x] T001 Create `GlAccounts.sln` and six-project structure: `src/GlAccounts.Api`, `src/GlAccounts.Application`, `src/GlAccounts.Domain`, `src/GlAccounts.Infrastructure`, `src/GlAccounts.Contracts`, `src/GlAccounts.Tests` (run `dotnet new sln` + `dotnet new webapi/classlib/xunit` per project)
- [x] T002 Create `Directory.Build.props` at repo root: `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<ImplicitUsings>enable</ImplicitUsings>`
- [x] T003 Add project references per dependency graph: Api→Application→Domain; Infrastructure→Application+Domain; Tests→all; Contracts standalone
- [x] T004 [P] Add NuGet packages: `FluentValidation.AspNetCore` to Api+Application; `xUnit` + `FluentAssertions` + `Microsoft.AspNetCore.Mvc.Testing` to Tests
- [x] T005 Configure `Program.cs` with minimal API host, service registration stubs, Swagger/OpenAPI middleware, and CORS policy allowing Angular dev origin (`http://localhost:4200`)
- [x] T006 Verify empty build: `dotnet build --no-restore -warnaserror` exits 0

**Checkpoint**: `dotnet build` exits 0. CORS configured for Angular SPA.

---

## Phase 2: Foundational (Domain + Ports + Stubs)

**Purpose**: Core domain types, port interfaces, and in-memory stub stores. MUST be complete before any endpoint or Angular service slice.

### Domain Layer (`src/GlAccounts.Domain/`)

- [x] T007 [P] Create enums in `src/GlAccounts.Domain/Enums/`: `AccountType.cs` (Financial, Statistical), `AccountStatus.cs` (Active, Hold, Inactive, Template), `BalanceType.cs` (BalanceForward, Periodic), `NormalBalance.cs` (Debit, Credit)
- [x] T008 [P] Create `src/GlAccounts.Domain/Entities/GlAccount.cs` with fields: `AccountId` (string, max 32), `Description` (string, max 200), `AccountType` (enum, permanent), `Category` (string, max 50), `Status` (AccountStatus), `BalanceType`, `NormalBalance`, `CurrencyId` (string?, max 10), `CreatedDate` (DateOnly), `CreatedBy` (string, max 100)
- [x] T009 [P] Create `src/GlAccounts.Domain/Entities/AccountAttribute.cs` with fields: `AttributeId` (string/GUID), `AccountId` (string), `AttributeTypeId` (string, max 50), `AttributeTypeDescription` (string), `AttributeValueId` (string, max 50), `AttributeValueDescription` (string)
- [x] T010 [P] Create `src/GlAccounts.Domain/Rules/ClassificationRules.cs` — static method `IsValidCategoryForType(string category, AccountType type) → bool` (Financial: Asset/Liability/Equity/Revenue/Expense; Statistical: any non-Financial category)
- [x] T011 [P] Create `src/GlAccounts.Domain/Rules/StatusRules.cs` — static method `CanBeUsedInTransaction(AccountStatus status) → bool` (Active = true; Hold/Inactive/Template = false)
- [x] T012 [P] Create `src/GlAccounts.Domain/Exceptions/DomainException.cs` — sealed exception with `string Message` property

### Application Ports (`src/GlAccounts.Application/Abstractions/Persistence/`)

- [x] T013 [P] Create `IAccountStore.cs`: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsAsync`
- [x] T014 [P] Create `IAccountAttributeStore.cs`: `GetByAccountIdAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- [x] T015 [P] Create `IAccountGroupStore.cs`: `GetGroupsByAccountIdAsync`
- [x] T016 [P] Create `IPeriodBalanceStore.cs`: `GetByAccountIdAsync` (with optional fiscalYear/period range filters)
- [x] T017 [P] Create `ITransactionStore.cs`: `GetByAccountIdAsync` (with optional date range/status filters); `HasTransactionsForAccountAsync`
- [x] T018 [P] Create `IAuditStore.cs`: `AppendAsync(AuditEntry entry)` — append-only; also create `AuditEntry` record type in `src/GlAccounts.Application/Abstractions/`
- [x] T019 [P] Create `ISegmentFormatStore.cs`: `GetActiveFormatAsync()` returning segment definitions

### Infrastructure Stubs (`src/GlAccounts.Infrastructure/Persistence/`)

- [x] T020 [P] Create `InMemoryAccountStore.cs` implementing `IAccountStore` — seed 7 accounts (fixed IDs: 01-100-1000 Cash, 01-100-2000 AR, 01-200-1000 AP, 01-100-3000 Inactive, 01-100-4000 Hold, 01-100-5000 Template, 01-200-2000 AccountWithTransactions)
- [x] T021 [P] Create `InMemoryAccountAttributeStore.cs` implementing `IAccountAttributeStore` — empty seed
- [x] T022 [P] Create `InMemoryAccountGroupStore.cs` implementing `IAccountGroupStore` — seed 1 group (GRP-001 "Current Assets" containing 01-100-1000 and 01-100-2000)
- [x] T023 [P] Create `InMemoryPeriodBalanceStore.cs` implementing `IPeriodBalanceStore` — seed 4 period balance records for 01-100-1000 (FY2026 periods 1–4, fixed UUIDs)
- [x] T024 [P] Create `InMemoryTransactionStore.cs` implementing `ITransactionStore` — seed 3 posted transactions for 01-200-2000 (TXN-001, TXN-002, TXN-003)
- [x] T025 [P] Create `InMemoryAuditStore.cs` implementing `IAuditStore` — append-only `List<AuditEntry>` with thread-safe lock
- [x] T026 [P] Create `InMemorySegmentFormatStore.cs` implementing `ISegmentFormatStore` — returns 3-segment format; validates accountId via regex `^\d{2}-\d{3}-\d{4}$`
- [x] T027 [P] Create `IAttributeCatalogStore.cs` port + `InMemoryAttributeCatalogStore.cs` stub seeded with type REPORTING_UNIT (values: NORTH_REGION, SOUTH_REGION)
- [x] T028 Create `InfrastructureServiceCollectionExtensions.cs` — registers all stub stores as singletons; wire into `Program.cs`
- [x] T029 [P] Create domain unit tests: `src/GlAccounts.Tests/Unit/Domain/ClassificationRulesTests.cs` and `StatusRulesTests.cs`
- [x] T030 Verify build + passing tests: `dotnet build -warnaserror` exits 0; `dotnet test` exits 0

**Checkpoint**: Domain compiles, all ports defined, all stubs registered, domain unit tests pass.

---

## Phase 3: User Story 1 — Create GL Account API (Priority: P1)

**Goal**: `POST /api/gl/accounts` creates a valid account, rejects duplicates and invalid segment formats.  
**AC**: AC-GLA-001, AC-GLA-002, AC-GLA-003  
**Independent Test**: POST valid payload → 201; duplicate → 409; bad format → 422.

- [x] T031 [P] [US1] Create `src/GlAccounts.Application/Features/GlAccounts/CreateAccount/CreateAccountCommand.cs` and `CreateAccountValidator.cs` — FluentValidation: AccountId required+max32; Description required+max200; AccountType valid enum; Category required+max50 + `ClassificationRules.IsValidCategoryForType` rule; Status/BalanceType/NormalBalance valid enums; CurrencyId optional max10
- [x] T032 [US1] Create `src/GlAccounts.Application/Features/GlAccounts/CreateAccount/CreateAccountHandler.cs`: validate segment format → check duplicate → create `GlAccount` → persist → append audit entry (Created)
- [x] T033 [US1] Create `src/GlAccounts.Api/Features/GlAccounts/CreateAccount/CreateAccountEndpoint.cs` — `POST /api/gl/accounts`; reads `X-User-Id` header; 201/400/409/422/500; uses `ProblemDetails` helper
- [x] T034 [P] [US1] Create `src/GlAccounts.Tests/Integration/CreateAccountEndpointTests.cs` — AC-GLA-001 (valid → 201), AC-GLA-002 (duplicate → 409), AC-GLA-003 (bad format → 422)
- [x] T035 [US1] Run `dotnet test` — all tests pass

**Checkpoint**: POST /api/gl/accounts working end-to-end. 3 ACs covered.

---

## Phase 4: User Story 2 — Retrieve and Update Account API (Priority: P2)

**Goal**: `GET` and `PUT /api/gl/accounts/{id}` — retrieve and update editable fields; permanent fields protected.  
**AC**: AC-GLA-004, AC-GLA-005

- [x] T036 [P] [US2] Create `src/GlAccounts.Application/Features/GlAccounts/GetAccount/GetAccountQuery.cs` + `GetAccountHandler.cs` — 404 if not found
- [x] T037 [P] [US2] Create `src/GlAccounts.Api/Features/GlAccounts/GetAccount/GetAccountEndpoint.cs` — `GET /api/gl/accounts/{accountId}`; 200/404/500
- [x] T038 [P] [US2] Create `UpdateAccountCommand.cs` + `UpdateAccountValidator.cs` — validates editable fields; rejects attempt to change AccountId or AccountType (422)
- [x] T039 [US2] Create `UpdateAccountHandler.cs`: load → reject permanent-field changes → validate Category/Type → update → persist → audit entry (Updated, with before/after)
- [x] T040 [US2] Create `src/GlAccounts.Api/Features/GlAccounts/UpdateAccount/UpdateAccountEndpoint.cs` — `PUT /api/gl/accounts/{accountId}`; 200/400/404/422/500
- [x] T041 [P] [US2] Create `src/GlAccounts.Tests/Integration/GetAndUpdateAccountEndpointTests.cs` — AC-GLA-005 (GET → 200; PUT description → 200), AC-GLA-004 (PUT accountType → 422)
- [x] T042 [US2] Run `dotnet test` — all tests pass

**Checkpoint**: GET + PUT working. 2 ACs covered.

---

## Phase 5: User Story 3 — Classification Validation (Priority: P2)

**Goal**: Category validated against AccountType on create and update.  
**AC**: AC-GLA-006

- [x] T043 [P] [US3] Create `src/GlAccounts.Tests/Integration/AccountClassificationTests.cs` — AC-GLA-006 (Statistical + Asset category → 422); valid combos → 201/200
- [x] T044 [US3] Verify `ClassificationRules` covers Financial (Asset/Liability/Equity/Revenue/Expense) and rejects non-Financial categories for Statistical; update validators if needed
- [x] T045 [US3] Run `dotnet test` — all tests pass

**Checkpoint**: Category/Type rules enforced. 1 AC covered.

---

## Phase 6: User Story 4 — Account Status Lifecycle (Priority: P2)

**Goal**: Status (Active/Hold/Inactive/Template) changes persist; `StatusRules` enforced.  
**AC**: AC-GLA-007

- [x] T046 [P] [US4] Create `src/GlAccounts.Tests/Integration/AccountStatusTests.cs` — AC-GLA-007 (Inactive account status = Inactive in GET; StatusRules.CanBeUsedInTransaction returns false); PUT status changes → 200
- [x] T047 [US4] Verify `StatusRules.CanBeUsedInTransaction` is reachable from `UpdateAccountHandler`; document that journal module will call it
- [x] T048 [US4] Run `dotnet test` — all tests pass

**Checkpoint**: Status lifecycle works. 1 AC covered.

---

## Phase 7: User Story 5 — Manage Account Attributes API (Priority: P2)

**Goal**: `POST/PUT/DELETE /api/gl/accounts/{id}/attributes` — CRUD on attributes; atomic with parent save.  
**AC**: AC-GLA-008, AC-GLA-009

- [x] T049 [P] [US5] Create `AddAttributeCommand.cs` + `AddAttributeHandler.cs` — validates type/value exist in catalog → add → audit entry
- [x] T050 [P] [US5] Create `UpdateAttributeCommand.cs` + `UpdateAttributeHandler.cs` — validates new value → update → audit entry
- [x] T051 [P] [US5] Create `RemoveAttributeCommand.cs` + `RemoveAttributeHandler.cs` — delete → audit entry
- [x] T052 [US5] Create `src/GlAccounts.Api/Features/GlAccounts/ManageAttributes/ManageAttributesEndpoint.cs` — POST (201), PUT (200), DELETE (204); 400/404/422/500
- [x] T053 [P] [US5] Create `src/GlAccounts.Tests/Integration/ManageAttributesEndpointTests.cs` — AC-GLA-008 (POST → 201), AC-GLA-009 (add+update+delete sequence commits atomically)
- [x] T054 [US5] Run `dotnet test` — all tests pass

**Checkpoint**: Attribute CRUD working. 2 ACs covered.

---

## Phase 8: User Story 9 — Delete Account with Protection (Priority: P1)

**Goal**: `DELETE /api/gl/accounts/{id}` — blocked if dependencies; 204 when clean.  
**AC**: AC-GLA-013, AC-GLA-014

- [x] T055 [P] [US9] Create `DeleteAccountCommand.cs` + `DeleteAccountHandler.cs`: load → check transactions → check groups → if blocked 409 with count + deactivation suggestion → else delete → audit entry (Deleted)
- [x] T056 [US9] Create `src/GlAccounts.Api/Features/GlAccounts/DeleteAccount/DeleteAccountEndpoint.cs` — `DELETE /api/gl/accounts/{accountId}`; 204/400/404/409/500
- [x] T057 [P] [US9] Create `src/GlAccounts.Tests/Integration/DeleteAccountEndpointTests.cs` — AC-GLA-013 (01-200-2000 → 409 with "3 posted transaction(s)" + deactivation message), AC-GLA-014 (clean account → 204)
- [x] T058 [US9] Run `dotnet test` — all tests pass

**Checkpoint**: Deletion protection enforced. 2 ACs covered.

---

## Phase 9: User Story 6 — Account Groups API (Priority: P3)

**Goal**: `GET /api/gl/accounts/{id}/groups` returns all groups containing the account.  
**AC**: AC-GLA-010

- [x] T059 [P] [US6] Create `ListAccountGroupsQuery.cs` + `ListAccountGroupsHandler.cs`
- [x] T060 [US6] Create `src/GlAccounts.Api/Features/GlAccounts/ListAccountGroups/ListAccountGroupsEndpoint.cs` — `GET /api/gl/accounts/{accountId}/groups`; 200/400/404/500
- [x] T061 [P] [US6] Create `src/GlAccounts.Tests/Integration/ListAccountGroupsEndpointTests.cs` — AC-GLA-010 (seed account → GRP-001 returned; account not in group → empty list)
- [x] T062 [US6] Run `dotnet test` — all tests pass

**Checkpoint**: Account group inquiry working. 1 AC covered.

---

## Phase 10: User Story 7 — Period Summary API (Priority: P2)

**Goal**: `GET /api/gl/accounts/{id}/period-summary` returns period balances, filterable.  
**AC**: AC-GLA-011

- [x] T063 [P] [US7] Create `PeriodSummaryQuery.cs` + `PeriodSummaryHandler.cs` — delegates to `IPeriodBalanceStore` with optional filters
- [x] T064 [US7] Create `src/GlAccounts.Api/Features/GlAccounts/PeriodSummary/PeriodSummaryEndpoint.cs` — `GET /api/gl/accounts/{accountId}/period-summary`; optional `?fiscalYear`, `?fromPeriod`, `?toPeriod`; 200/400/404/500
- [x] T065 [P] [US7] Create `src/GlAccounts.Tests/Integration/PeriodSummaryEndpointTests.cs` — AC-GLA-011 (seed account → 4 records; fiscal-year filter; period-range filter; empty → empty list)
- [x] T066 [US7] Run `dotnet test` — all tests pass

**Checkpoint**: Period summary working. 1 AC covered.

---

## Phase 11: User Story 8 — Transaction History API (Priority: P2)

**Goal**: `GET /api/gl/accounts/{id}/transactions` returns transactions, filterable by date range.  
**AC**: AC-GLA-012

- [x] T067 [P] [US8] Create `TransactionsQuery.cs` + `TransactionsHandler.cs`
- [x] T068 [US8] Create `src/GlAccounts.Api/Features/GlAccounts/Transactions/TransactionsEndpoint.cs` — `GET /api/gl/accounts/{accountId}/transactions`; `?fromDate`, `?toDate`, `?postingStatus`; 200/400/404/500
- [x] T069 [P] [US8] Create `src/GlAccounts.Tests/Integration/TransactionsEndpointTests.cs` — AC-GLA-012 (seed 01-200-2000 → 3 records; date filter → correct subset; empty account → empty list)
- [x] T070 [US8] Run `dotnet test` — all tests pass

**Checkpoint**: Transaction history working. 1 AC covered.

---

## Phase 12: User Story 11 — Audit Trail (Priority: P2)

**Goal**: Every write operation produces an audit entry with caller user ID, timestamp, before/after values.  
**AC**: AC-GLA-015, AC-GLA-016

- [x] T071 [P] [US11] Create `src/GlAccounts.Api/Common/UserIdReader.cs` — reads `X-User-Id` header; returns "anonymous" if absent
- [x] T072 [US11] Verify all write handlers (Create/Update/Delete account; Add/Update/Remove attribute; CopyFromTemplate) receive and use `userId` from `UserIdReader` in their audit entries — add `userId` parameter to commands if needed
- [x] T073 [P] [US11] Create `src/GlAccounts.Tests/Integration/AuditTrailTests.cs` — AC-GLA-015 (POST account → audit entry: ChangeType=Created, UserId=test-user, AfterValues contains accountId+description); AC-GLA-016 (PUT account → audit entry: ChangeType=Updated, BeforeValues.description=old, AfterValues.description=new)
- [x] T074 [US11] Run `dotnet test` — all tests pass

**Checkpoint**: Audit trail wired into all write handlers. 2 ACs covered.

---

## Phase 13: User Story 12 — Copy from Template API (Priority: P4)

**Goal**: `POST /api/gl/accounts/{id}/copy` creates a new account from a Template source.  
**AC**: AC-GLA-017

- [x] T075 [P] [US12] Create `CopyFromTemplateCommand.cs` + `CopyFromTemplateHandler.cs`: load source → validate source.Status=Template (422 if not) → validate newAccountId format → check uniqueness → create copied account (Status=Active) → persist → audit entry
- [x] T076 [US12] Create `src/GlAccounts.Api/Features/GlAccounts/Templates/CopyFromTemplateEndpoint.cs` — `POST /api/gl/accounts/{accountId}/copy`; 201/400/404/409/422/500
- [x] T077 [P] [US12] Create `src/GlAccounts.Tests/Integration/CopyFromTemplateEndpointTests.cs` — AC-GLA-017 (copy 01-100-5000 → 201; copied fields correct; source still Template); non-template → 422; duplicate new ID → 409
- [x] T078 [US12] Run `dotnet test` — all tests pass

**Checkpoint**: Template copy working. 1 AC covered.

---

## Phase 14: Angular Foundation

**Purpose**: Bootstrap the Angular SPA, configure routing, create the API service and TypeScript models. Must be complete before any Angular feature slice.

- [x] T079 Scaffold Angular project: run `ng new GlAccounts.Web --routing --style scss --standalone false` in `src/`; verify `ng build` exits 0
- [x] T080 [P] Create TypeScript interface models in `src/GlAccounts.Web/src/app/core/models/account.model.ts` matching `openapi.yaml` schemas: `AccountResponse`, `CreateAccountRequest`, `UpdateAccountRequest`, `AttributeResponse`, `AddAttributeRequest`, `AccountGroupResponse`, `PeriodBalanceResponse`, `TransactionResponse`, `ProblemDetails`
- [x] T081 [P] Create `src/GlAccounts.Web/src/app/core/services/gl-accounts.service.ts` using `HttpClient`: methods for all 11 API operations (createAccount, getAccount, updateAccount, deleteAccount, addAttribute, updateAttribute, removeAttribute, listGroups, getPeriodSummary, getTransactions, copyFromTemplate); base URL from `environment.apiBaseUrl`
- [x] T082 [P] Configure `src/GlAccounts.Web/src/environments/environment.ts`: `apiBaseUrl: 'http://localhost:5001'`
- [x] T083 Configure `app.routes.ts` with routes for: `/gl/accounts` (list), `/gl/accounts/new` (create), `/gl/accounts/:id` (detail), `/gl/accounts/:id/edit` (edit)
- [x] T084 [P] Create `src/GlAccounts.Web/src/app/core/services/gl-accounts.service.spec.ts` — unit tests for service method signatures and HTTP calls using `HttpClientTestingModule`
- [x] T085 Verify: `ng build` exits 0; `ng test --watch=false` exits 0

**Checkpoint**: Angular project builds. Service and models in place. Routing configured.

---

## Phase 15: Angular Account List + Create Form (US1, US2)

**Goal**: Account list view shows seed accounts; Create form submits valid data and shows validation errors.  
**Story**: US1 (create), US2 (list + edit)

- [x] T086 [P] [US1] Create `src/GlAccounts.Web/src/app/features/account-list/account-list.component.ts` — calls `GlAccountsService.getAll()` (add `GET /api/gl/accounts` to service); displays accounts in table; loading/empty/error states; navigate to detail and create
- [x] T087 [P] [US1] Create `src/GlAccounts.Web/src/app/features/account-form/account-form.component.ts` — Angular Reactive Form; fields: AccountId (create-only), Description, AccountType (create-only), Category, Status, BalanceType, NormalBalance, CurrencyId; client-side required validation; on submit calls `createAccount` or `updateAccount`; shows `ValidationSummaryComponent` on 400/422 errors
- [x] T088 [P] [US1] Create shared `ValidationSummaryComponent` in `src/GlAccounts.Web/src/app/shared/` — displays ProblemDetails errors list
- [x] T089 [P] [US1] Create `src/GlAccounts.Web/src/app/features/account-list/account-list.component.spec.ts` and `account-form.component.spec.ts` — unit tests with `HttpClientTestingModule`; test loading state, empty state, populated state, error state
- [x] T090 [US1] Verify: `ng build` exits 0; `ng test --watch=false` exits 0

**Checkpoint**: Account list and create form work against the running backend.

---

## Phase 16: Angular Account Detail + Edit (US2, US3, US4)

**Goal**: Account detail view shows all fields; Edit form protects permanent fields; status/classification change works.  
**Story**: US2 (edit), US3 (classification), US4 (status)

- [x] T091 [P] [US2] Create `src/GlAccounts.Web/src/app/features/account-detail/account-detail.component.ts` — loads account by ID; shows all fields; action bar with Edit/Delete/Copy buttons; tabs/panels for Attributes, Groups, Period Summary, Transactions
- [x] T092 [P] [US2] Update `account-form.component.ts` for edit mode — AccountId and AccountType fields are `readonly`; show `PermanentFieldReadonlyComponent` badge; submit calls `updateAccount`; navigate back to detail on success
- [x] T093 [P] [US3] Add `AccountClassificationPanelComponent` to detail view — displays Category, BalanceType, NormalBalance; client-side validation for Category vs AccountType
- [x] T094 [P] [US4] Add `StatusSelectComponent` to form — dropdown for Active/Hold/Inactive/Template; shows tooltip when status restricts transactions
- [x] T095 [P] [US2] Create component specs: `account-detail.component.spec.ts` — tests for loading, populated (all fields shown), error (404 handled gracefully)
- [x] T096 [US2] Verify: `ng build` exits 0; `ng test --watch=false` exits 0

**Checkpoint**: Detail + edit views complete. Permanent-field protection shown in UI.

---

## Phase 17: Angular Attributes Panel (US5)

**Goal**: Inline attributes panel on the account detail view — add, edit, remove attributes.  
**Story**: US5

- [x] T097 [P] [US5] Create `src/GlAccounts.Web/src/app/features/attributes/attributes-panel.component.ts` — lists account attributes; Add button opens `AddAttributeFormComponent`; inline edit; delete with confirmation; all changes call API immediately (not staged — staging is an API-layer concern already tested)
- [x] T098 [P] [US5] Create `add-attribute-form.component.ts` — dropdowns for AttributeTypeId and AttributeValueId; submits to `addAccountAttribute`; shows errors on 422
- [x] T099 [P] [US5] Create `attributes-panel.component.spec.ts` — tests for empty, populated, add success, add error, delete
- [x] T100 [US5] Verify: `ng build` exits 0; `ng test --watch=false` exits 0

**Checkpoint**: Attributes panel works. Users can manage attributes from the UI.

---

## Phase 18: Angular Inquiry Views (US6, US7, US8)

**Goal**: Account groups, period summary, and transaction history views accessible from account detail tabs.  
**Story**: US6, US7, US8

- [x] T101 [P] [US6] Create `src/GlAccounts.Web/src/app/features/account-groups/account-groups.component.ts` — displays group list for account; empty state "Not in any groups"; loading/error states
- [x] T102 [P] [US7] Create `src/GlAccounts.Web/src/app/features/period-summary/period-summary.component.ts` — table of period balances; `FiscalYearFilterComponent` and `PeriodRangeFilterComponent` inputs; reactive filter updates table
- [x] T103 [P] [US8] Create `src/GlAccounts.Web/src/app/features/transactions/transaction-list.component.ts` — table of transactions; `DateRangeFilterComponent` and `PostingStatusFilterComponent`; loading/error/empty states
- [x] T104 [P] [US6] Create component specs for all three: empty/populated/error states tested
- [x] T105 [US6] Verify: `ng build` exits 0; `ng test --watch=false` exits 0

**Checkpoint**: All three inquiry views working. 3 user stories have UI.

---

## Phase 19: Angular Delete Confirmation + Template Copy (US9, US12)

**Goal**: Delete button shows confirmation with dependency message; template copy dialog works.  
**Story**: US9, US12

- [x] T106 [P] [US9] Add delete confirmation to `AccountActionBarComponent` — calls `deleteAccount`; on 409 shows dependency message from `ProblemDetails.detail` (includes transaction count and deactivation suggestion); on 204 navigates back to list
- [x] T107 [P] [US12] Create `src/GlAccounts.Web/src/app/features/account-form/copy-template-dialog.component.ts` — modal form; inputs: newAccountId + description; submits to `copyAccountFromTemplate`; navigates to new account detail on 201; shows 422 error inline
- [x] T108 [P] [US9] Create `account-action-bar.component.spec.ts` — tests: delete blocked (409 message shown), delete success (204 navigates), copy dialog opens/submits/errors
- [x] T109 [US9] Verify: `ng build` exits 0; `ng test --watch=false` exits 0

**Checkpoint**: Delete protection and template copy fully usable from UI.

---

## Phase 20: Polish and Cross-Cutting Concerns

**Purpose**: Shared helpers, error response consistency, CORS, final gate runs.

- [x] T110 [P] Create `src/GlAccounts.Api/Common/ProblemDetails.cs` helper — centralises `HttpValidationProblemDetails` and `ProblemDetails` response construction; 400/409/422/500 use consistent shape matching `openapi.yaml`
- [x] T111 [P] Create `ApplicationServiceCollectionExtensions` in `src/GlAccounts.Application/` registering FluentValidation validators; call from `Program.cs`
- [x] T112 [P] Add `GET /api/gl/accounts` list endpoint (`SearchAccountsEndpoint.cs`) — supports optional `?status` filter; needed by Angular `AccountListComponent`
- [x] T113 Run full gate suite: `dotnet run scripts/sdlc/sdlc.cs -- gate specs/001-gl-account-maintenance specification` → 0; `gate contract` → 0; `gate ux` → 0; `gate traceability` → 0
- [x] T114 Run and record: `dotnet run scripts/sdlc/sdlc.cs -- evidence specs/001-gl-account-maintenance build dotnet build --no-restore -warnaserror` — exits 0
- [x] T115 Run and record: `dotnet run scripts/sdlc/sdlc.cs -- evidence specs/001-gl-account-maintenance unit_tests dotnet test --no-build` — exits 0

**Checkpoint**: All backend gates pass. Build clean. Tests green. Angular build passes.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1** (Backend Setup): No dependencies — start immediately
- **Phase 2** (Foundational): Depends on Phase 1 — BLOCKS Phases 3–14
- **Phases 3–13** (API slices): All depend on Phase 2; can proceed in priority order or parallel
- **Phase 14** (Angular Foundation): Depends on Phase 1 (CORS configured); can run **in parallel with Phases 3–13**
- **Phases 15–19** (Angular features): Depend on Phase 14 AND the corresponding API phase
- **Phase 20** (Polish): Depends on all phases complete

### Backend vs Frontend Parallelism

```
Phase 1 (Backend Setup)
├── Phase 2 (Domain + Stubs)  ──► Phases 3–13 (API slices, sequential by priority)
└── Phase 14 (Angular Foundation) ──► Phases 15–19 (Angular features)
```

A second developer can start Angular Foundation (Phase 14) while the first builds Phases 2–3 backend.

### User Story to Phase Mapping

| Story | Backend Phase | Angular Phase | ACs |
|---|---|---|---|
| US1 Create (P1) | 3 | 15 | AC-001,002,003 |
| US2 Get/Update (P2) | 4 | 15, 16 | AC-004,005 |
| US3 Classification (P2) | 5 | 16 | AC-006 |
| US4 Status (P2) | 6 | 16 | AC-007 |
| US5 Attributes (P2) | 7 | 17 | AC-008,009 |
| US9 Delete (P1) | 8 | 19 | AC-013,014 |
| US6 Groups (P3) | 9 | 18 | AC-010 |
| US7 Period Summary (P2) | 10 | 18 | AC-011 |
| US8 Transactions (P2) | 11 | 18 | AC-012 |
| US11 Audit Trail (P2) | 12 | — (backend only) | AC-015,016 |
| US12 Template Copy (P4) | 13 | 19 | AC-017 |

---

## Implementation Strategy

### Backend MVP (P1 stories — Phases 1–3, 8)

1. Phase 1 → Phase 2 → Phase 3 (Create Account) → Phase 8 (Delete Protection)
2. **Verify API**: POST/DELETE endpoints working; 5 ACs covered

### Full Backend (Phases 1–13)

All 17 ACs covered by backend tests before Angular work begins

### Angular MVP (Phases 14–15 after backend MVP)

Angular Foundation + Account List/Create Form — users can create and browse accounts

### Full Stack (Phases 14–20)

All views complete; end-to-end tested via `ng test` + `dotnet test`

---

## Traceability (AC → Task)

| AC | Backend task | Angular task |
|---|---|---|
| AC-GLA-001 | T034 | T087 |
| AC-GLA-002 | T034 | T087 |
| AC-GLA-003 | T034 | T087 |
| AC-GLA-004 | T041 | T092 |
| AC-GLA-005 | T041 | T087/T092 |
| AC-GLA-006 | T043 | T093 |
| AC-GLA-007 | T046 | T094 |
| AC-GLA-008 | T053 | T098 |
| AC-GLA-009 | T053 | T097 |
| AC-GLA-010 | T061 | T101 |
| AC-GLA-011 | T065 | T102 |
| AC-GLA-012 | T069 | T103 |
| AC-GLA-013 | T057 | T106 |
| AC-GLA-014 | T057 | T106 |
| AC-GLA-015 | T073 | — |
| AC-GLA-016 | T073 | — |
| AC-GLA-017 | T077 | T107 |

---

## Notes

- `[P]` = parallelizable (different files, no shared dependency)
- Authorization enforcement is NOT implemented (Q-001); `X-User-Id` header for audit only
- Last-write-wins on concurrent updates (Q-002); no ETag
- Angular SPA communicates with backend via REST API; CORS configured in Phase 1
- Durability is NOT verified in stub phase — record as "not verified (stub data)" in validation report
- `ng add @angular/material` may be used for UI components — this is an additive dependency (no ADR required)
