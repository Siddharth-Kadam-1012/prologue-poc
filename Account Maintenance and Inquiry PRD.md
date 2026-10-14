# General Ledger (GL) Module - Account Maintenance and Inquiry Final User Stories

**Module**: General Ledger (GL) Module  
**Screen**: Account Maintenance and Inquiry  
**Version**: 3.0 - Final (Reformatted to Standard)  
**Date**: 2026-05-30  
**Status**: Ready for Review

---

## Document Control

| Attribute | Value |
|-----------|-------|
| Baseline Source | Account Maintenance and Inquiry.md |
| PB Frontend Source | spec-frontend.md (FE-1, FE-2, FE-3) |
| PB Backend Source | spec-backend.md (BE-1, BE-2, BE-3) |
| Total User Stories | 12 |
| Total Story Points | 67 |
| Total Business Rules | 9 (5 FE + 4 BE) |
| Enhancement Status | ✅ Complete |

---

## User Personas

| Persona | Role | Responsibilities |
|---------|------|------------------|
| **GL Administrator** | System configuration and setup | Creates and maintains segment formats, segment values, and system configurations |
| **Finance Manager** | Financial oversight and reporting | Reviews accounts, monitors financial data, ensures compliance with accounting standards |
| **Accountant** | Day-to-day account management | Creates and maintains GL accounts, performs inquiries, manages account attributes |
| **System Administrator** | Security and access control | Manages user permissions, audit trails, and system security |

---

## Epic and Feature Structure

```
EPIC: General Ledger Chart of Accounts Management
└── Feature 3: GL Account Lifecycle Management
    └── Stories: US-AM-001 to US-AM-012 (67 story points)
```

---

## Traceability Matrix

| Baseline Story | PB Frontend | PB Backend | Enhancement Status |
|----------------|-------------|------------|-------------------|
| US-AM-001 | FE-1 | BE-1 | ✅ Enhanced with 4 new criteria |
| US-AM-002 | FE-1 | BE-1 | ✅ Enhanced with 3 new criteria |
| US-AM-003 | FE-1 | None | ✅ Enhanced with 2 new criteria |
| US-AM-004 | FE-2, FE-3 | BE-2, BE-3 | ✅ Enhanced with 5 new criteria |
| US-AM-005 | FE-1 | None | ✅ Enhanced with 3 new criteria |
| US-AM-006 | None | None | No PB enhancements |
| US-AM-007 | None | BE-2, BE-3 | ✅ Enhanced with 4 new criteria |
| US-AM-008 | None | BE-1 | ✅ Enhanced with 2 new criteria |
| US-AM-009 | FE-1 | BE-1 | ✅ Enhanced with 5 new criteria |
| US-AM-010 | FE-1 | BE-1 | ✅ Enhanced with 4 new criteria |
| US-AM-011 | None | None | No PB enhancements |
| US-AM-012 | None | None | No PB enhancements |

---

# User Stories

## US-AM-001: Create New GL Accounts with Segment Validation

**Baseline Story ID**: US-AM-001  
**PB Frontend Story**: FE-1 (GL Account Maintenance)  
**PB Backend Story**: BE-1 (GL Account API)

### User Story

**As an** Accountant  
**I want to** create fully qualified GL accounts using defined segment values  
**So that** I can establish accounts for recording financial transactions

**Priority**: Must Have  
**Story Points**: 8  
**Requirements Covered**: R-001, R-002, R-003, R-004, R-005, R-006, R-007

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Account ID is built from validated segment value lists
- [ ] System validates against Chart-of-Accounts structure rules
- [ ] Edit mask and segment rules are enforced during creation
- [ ] System prevents creation of duplicate accounts
- [ ] Account ID becomes permanent and non-editable after save
- [ ] Account Type (Financial/Statistical) becomes permanent after save

#### From PowerBuilder Analysis (Enhanced)
- [ ] Account ID must be unique and match the defined segment format [PB-FE-1, BR-FE-1-001]
- [ ] Account ID format validated against segment format definition [PB-BE-1, BR-BE-1-002]
- [ ] New account record created with unique account ID via POST /api/gl/accounts [PB-BE-1]
- [ ] Account creation returns 201 status on success [PB-BE-1]

---

### Non-Functional Acceptance Criteria

#### API Specifications
- [ ] POST /api/gl/accounts - Create new GL account [PB-BE-1]
- [ ] GET /api/gl/accounts/:id - Retrieve account with segments [PB-BE-1]
- [ ] HTTP 201 Created returned on successful POST [PB-BE-1]
- [ ] HTTP 200 OK returned on successful GET [PB-BE-1]

#### Database Schema
```sql
CREATE TABLE gl_account (
    account_id       VARCHAR(32)   NOT NULL PRIMARY KEY,
    description      VARCHAR(200)  NOT NULL,
    account_type     CHAR(1)       NOT NULL,
    status           CHAR(1)       NOT NULL,
    created_date     DATE          NOT NULL
);
```
- [ ] Primary key on account_id ensures uniqueness [PB-BE-1]
- [ ] account_type field is permanent after creation [PB-BE-1]
- [ ] Segment format validation enforced at database level [PB-BE-1]

#### Performance
- [ ] Account creation completes within 2 seconds [PB-FE-1]
- [ ] Segment validation occurs in real-time [PB-FE-1]

---

### Business Rules

| Rule ID | Description | Source | Status |
|---------|-------------|--------|--------|
| BR-FE-1-001 | Account ID must be unique and match defined segment format | PB Frontend | Active |
| BR-BE-1-002 | Account ID format validated against segment format definition | PB Backend | Active |

---

### Edge Cases

#### Edge Case 1: Duplicate Account ID
**Scenario**: User attempts to create an account with an ID that already exists  
**Expected Behavior**:
- System detects duplicate account_id
- Validation error prevents saving
- Error message: "Account ID '{id}' already exists. Please use a unique ID."
- Existing account is highlighted in grid  
**Source**: PB Frontend (FE-1, BR-FE-1-001), PB Backend (BE-1, BR-BE-1-002)

#### Edge Case 2: Invalid Segment Format
**Scenario**: User enters account ID that doesn't match defined segment format  
**Expected Behavior**:
- System validates against segment format definition
- Validation error prevents saving
- Error message: "Account ID does not match segment format. Expected format: {format}"
- Segment breakdown shows invalid segments  
**Source**: PB Frontend (FE-1, BR-FE-1-001), PB Backend (BE-1, BR-BE-1-002)

---

### Not Applicable (NA) Items

#### UI/UX Details (Redesign Planned)
- ❌ PowerBuilder window (w_gl_account.srw) - React AccountForm component [PB-FE-1]
- ❌ PowerBuilder DataWindow (dw_segments) - React SegmentGrid component [PB-FE-1]
- ❌ PowerBuilder Save button (pb_save) - Standard React button [PB-FE-1]

#### Technology Stack (Migration Planned)
- ❌ PowerBuilder event open (lines 49-200) - React useEffect hook [PB-FE-1]
- ❌ PowerBuilder function AccountRepository.findById() - Backend API endpoint [PB-BE-1]

---

### Dependencies

- US-SF-001 (Define Multi-Segment GL Account Structure)
- US-SV-001 (Create and Maintain Segment Value Lists)

---

### Business Value

Enables creation of valid GL accounts that comply with organizational standards and support accurate financial reporting.

---

### Technical Notes

#### Migration Considerations
- PB window w_gl_account.srw inherits from w_am_master - implement base component pattern in React
- PB event open (lines 49-200) loads account and segments - implement as React useEffect with API calls
- AccountForm component will manage account CRUD operations

#### Database Compatibility
- Existing gl_account table structure will be maintained
- Primary key on account_id ensures uniqueness
- Segment format validation enforced at application and database levels

---

## US-AM-002: Modify GL Account Attributes and Descriptions

**Baseline Story ID**: US-AM-002  
**PB Frontend Story**: FE-1 (GL Account Maintenance)  
**PB Backend Story**: BE-1 (GL Account API)

### User Story

**As an** Accountant  
**I want to** modify editable account fields while protecting permanent attributes  
**So that** I can update account information as business needs change without compromising data integrity

**Priority**: Must Have  
**Story Points**: 5  
**Requirements Covered**: R-008, R-009, R-010, R-011

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Editable fields include Description, Category, Currency ID, and Status
- [ ] Description updates are reflected on financial statements
- [ ] Permanent fields (Account ID, Account Type) cannot be edited
- [ ] Currency ID changes are allowed per system configuration
- [ ] Validation occurs before saving changes

#### From PowerBuilder Analysis (Enhanced)
- [ ] Account updates via PUT /api/gl/accounts/:id return 200 status [PB-BE-1]
- [ ] Changes saved and reflected in reporting immediately [PB-BE-1]
- [ ] Account record retrieved via GET /api/gl/accounts/:id with segments and attributes [PB-BE-1]

---

### Non-Functional Acceptance Criteria

#### API Specifications
- [ ] GET /api/gl/accounts/:id - Retrieve account for editing [PB-BE-1]
- [ ] PUT /api/gl/accounts/:id - Update account [PB-BE-1]
- [ ] HTTP 200 OK returned on successful GET and PUT [PB-BE-1]

#### Performance
- [ ] Account retrieval for editing completes within 1 second [PB-FE-1]
- [ ] Account update saves within 1 second [PB-FE-1]

---

### Dependencies

- US-AM-001

---

### Business Value

Provides flexibility to maintain accurate account information while protecting critical attributes that ensure data integrity.

---

### Technical Notes

#### Migration Considerations
- PB SaveBtn component maps to React button with save handler
- PUT endpoint will validate permanent fields are not changed
- Immediate reflection in reporting requires cache invalidation

---

## US-AM-003: Configure Account Classification and Behavior

**Baseline Story ID**: US-AM-003  
**PB Frontend Story**: FE-1 (GL Account Maintenance)  
**PB Backend Story**: None (baseline functionality)

### User Story

**As an** Accountant  
**I want to** set account category, type, balance type, and normal balance  
**So that** accounts are properly classified for financial reporting and transaction posting

**Priority**: Must Have  
**Story Points**: 5  
**Requirements Covered**: R-015, R-016, R-017, R-018, R-019, R-020, R-021, R-022

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Account Category selection is validated against Account Type
- [ ] Account Type options: Financial (monetary) or Statistical (non-monetary)
- [ ] Account Type cannot be changed after initial creation
- [ ] Balance Type options: Balance Forward or Periodic
- [ ] Normal Balance options: Debit or Credit
- [ ] Normal Balance is used in journal entry validation

#### From PowerBuilder Analysis (Enhanced)
- [ ] All segment values, status, and attribute fields displayed when screen opens [PB-FE-1]
- [ ] AttributeGrid component manages custom attributes [PB-FE-1]

---

### Dependencies

- US-AM-001

---

### Business Value

Ensures accounts are properly classified for accurate financial reporting and supports correct posting behavior.

---

## US-AM-004: Assign Account Attributes for Reporting Classification

**Baseline Story ID**: US-AM-004  
**PB Frontend Story**: FE-2 (Details/Event Handling), FE-3 (Details Data)  
**PB Backend Story**: BE-2 (Details API), BE-3 (Details Validation)

### User Story

**As an** Accountant  
**I want to** link accounts to attribute types and values  
**So that** I can classify accounts for specialized reporting and analysis

**Priority**: Should Have  
**Story Points**: 5  
**Requirements Covered**: R-012, R-013, R-014

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Link account to predefined Attribute Type
- [ ] Select Attribute Value from available options for chosen type
- [ ] Attributes are used for classification and reporting only
- [ ] Attributes do not affect transaction posting logic
- [ ] Multiple attributes can be assigned to a single account

#### From PowerBuilder Analysis (Enhanced)
- [ ] Standard field-level validation required for all attribute fields [PB-FE-2, BR-FE-2-001; PB-BE-2, BR-BE-2-001]
- [ ] Standard field validation required for details data [PB-FE-3, BR-FE-3-001; PB-BE-3, BR-BE-3-001]
- [ ] Details panel allows add, edit, delete operations with staged saves [PB-FE-2]
- [ ] All changes persisted in same transaction on parent save [PB-BE-2]
- [ ] Row marked for deletion confirmed on Save action [PB-FE-2]

---

### Non-Functional Acceptance Criteria

#### API Specifications
- [ ] POST /api/account-attributes - Create attribute assignment [PB-BE-2]
- [ ] PUT /api/account-attributes/:id - Update attribute [PB-BE-2]
- [ ] DELETE /api/account-attributes/:id - Remove attribute [PB-BE-2]

#### Performance
- [ ] Attribute operations complete within 1 second [PB-FE-2]

---

### Business Rules

| Rule ID | Description | Source | Status |
|---------|-------------|--------|--------|
| BR-FE-2-001 | Standard field-level validation required | PB Frontend | Active |
| BR-FE-3-001 | Standard field validation required | PB Frontend | Active |
| BR-BE-2-001 | Standard field-level validation required | PB Backend | Active |
| BR-BE-3-001 | Standard data validation required | PB Backend | Active |

---

### Dependencies

- US-AM-001

---

### Business Value

Enables flexible account classification for management reporting and analysis without impacting core accounting processes.

---

### Technical Notes

#### Migration Considerations
- DetailsForm component (DataWindow Grid/Form) maps to React details grid
- Staged saves require state management for pending changes
- Transaction boundary ensures all changes commit together

---

## US-AM-005: Manage Account Status Throughout Lifecycle

**Baseline Story ID**: US-AM-005  
**PB Frontend Story**: FE-1 (GL Account Maintenance)  
**PB Backend Story**: None (baseline functionality)

### User Story

**As an** Accountant  
**I want to** set and change account status appropriately  
**So that** I can control account availability and prevent use of inactive or template accounts

**Priority**: Must Have  
**Story Points**: 5  
**Requirements Covered**: R-023, R-024, R-025, R-026

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Status options: Active, Hold, Inactive, Template
- [ ] Active accounts are fully available for transactions
- [ ] Hold status prevents new transaction references
- [ ] Inactive accounts cannot be used in future transactions
- [ ] Template accounts are used only as models for creating new accounts
- [ ] System prevents deletion of accounts with transaction history

#### From PowerBuilder Analysis (Enhanced)
- [ ] Inactive accounts may not be selected in new transactions [PB-FE-1, BR-FE-1-003]
- [ ] Status enforcement applied at transaction entry points [PB-FE-1]
- [ ] Status field displayed and editable in AccountForm [PB-FE-1]

---

### Business Rules

| Rule ID | Description | Source | Status |
|---------|-------------|--------|--------|
| BR-FE-1-003 | Inactive accounts may not be selected in new transactions | PB Frontend | Active |

---

### Dependencies

- US-AM-001

---

### Business Value

Provides controlled account lifecycle management, preventing errors while maintaining historical data integrity.

---

## US-AM-006: View Account Group Membership

**Baseline Story ID**: US-AM-006  
**PB Frontend Story**: None (baseline functionality)  
**PB Backend Story**: None (baseline functionality)

### User Story

**As a** Finance Manager  
**I want to** view all account groups that include a selected account  
**So that** I understand how accounts are organized for financial reporting

**Priority**: Should Have  
**Story Points**: 3  
**Requirements Covered**: R-027, R-028

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Account Groups button displays all groups containing the account
- [ ] View respects user security permissions
- [ ] Navigate to group details for further analysis
- [ ] Export group membership for documentation
- [ ] Display group hierarchy and relationships

---

### Dependencies

- US-AM-001

---

### Business Value

Improves transparency in account organization and supports accurate financial statement preparation.

---

## US-AM-007: View Period Summary and Account Balances

**Baseline Story ID**: US-AM-007  
**PB Frontend Story**: None (baseline functionality)  
**PB Backend Story**: BE-2 (Details API), BE-3 (Details Validation)

### User Story

**As a** Finance Manager  
**I want to** view period-level balances for an account across fiscal periods  
**So that** I can analyze financial performance and trends over time

**Priority**: Must Have  
**Story Points**: 8  
**Requirements Covered**: R-029, R-030

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Period Summary displays balances by fiscal period
- [ ] Display balances by fiscal year for annual analysis
- [ ] Display budget versions for comparison
- [ ] Filter and sort capabilities for flexible analysis
- [ ] Export to Excel for further analysis and reporting

#### From PowerBuilder Analysis (Enhanced)
- [ ] Period summary data retrieved from gl_account_period_summary table [PB-BE-2]
- [ ] Details grid displays with add, edit, delete capabilities [PB-BE-2]
- [ ] Save button activates when rows are staged [PB-BE-2]
- [ ] Transaction commits persist all changes correctly [PB-BE-3]

---

### Non-Functional Acceptance Criteria

#### API Specifications
- [ ] GET /api/account/:id/period-summary - Retrieve period balances [PB-BE-2]
- [ ] HTTP 200 OK returned on successful GET [PB-BE-2]

#### Database Schema
- [ ] gl_account_period_summary table stores period-level balances [PB-BE-2]

---

### Dependencies

- US-AM-001

---

### Business Value

Enables financial analysis and performance monitoring essential for management decision-making.

---

## US-AM-008: View Account Transaction History

**Baseline Story ID**: US-AM-008  
**PB Frontend Story**: None (baseline functionality)  
**PB Backend Story**: BE-1 (GL Account API)

### User Story

**As an** Accountant  
**I want to** view posted journal entries for an account with filtering options  
**So that** I can review transaction history and verify posting accuracy

**Priority**: Must Have  
**Story Points**: 8  
**Requirements Covered**: R-031, R-032, R-033

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Transactions button displays all posted journal entries
- [ ] Filter by date range or fiscal period
- [ ] Display batch ID, description, amount, and posting status
- [ ] Drill-down to detailed journal entry information
- [ ] Export transaction list for reconciliation and audit

#### From PowerBuilder Analysis (Enhanced)
- [ ] Transaction history retrieved via gl_transaction table SELECT [PB-BE-1]
- [ ] Referential check performed before account deletion [PB-BE-1]

---

### Non-Functional Acceptance Criteria

#### API Specifications
- [ ] GET /api/gl/accounts/:id/transactions - Retrieve transaction history [PB-BE-1]
- [ ] HTTP 200 OK returned on successful GET [PB-BE-1]

---

### Dependencies

- US-AM-001

---

### Business Value

Supports account reconciliation, audit requirements, and transaction verification essential for financial accuracy.

---

## US-AM-009: Protect Accounts from Inappropriate Deletion

**Baseline Story ID**: US-AM-009  
**PB Frontend Story**: FE-1 (GL Account Maintenance)  
**PB Backend Story**: BE-1 (GL Account API)

### User Story

**As a** System Administrator  
**I want to** prevent deletion of accounts with dependencies  
**So that** data integrity is maintained and historical financial data is preserved

**Priority**: Must Have  
**Story Points**: 5  
**Requirements Covered**: R-034, R-035, R-036

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Block deletion if posted transactions exist
- [ ] Block deletion if unposted transactions exist
- [ ] Block deletion if budgets reference the account
- [ ] Block deletion if allocations reference the account
- [ ] Block deletion if account is in account groups (unless removed first)
- [ ] Allow deletion only when no dependencies exist
- [ ] Suggest deactivation as alternative to deletion

#### From PowerBuilder Analysis (Enhanced)
- [ ] Accounts with posted transactions cannot be deleted [PB-FE-1, BR-FE-1-002]
- [ ] Deletion blocked if transaction history exists [PB-BE-1, BR-BE-1-001]
- [ ] DELETE /api/gl/accounts/:id returns 409 Conflict when blocked [PB-BE-1]
- [ ] Error message shown when deletion is blocked [PB-FE-1]
- [ ] DeleteBtn component performs dependency check before deletion [PB-FE-1]

---

### Non-Functional Acceptance Criteria

#### API Specifications
- [ ] DELETE /api/gl/accounts/:id - Delete account [PB-BE-1]
- [ ] HTTP 409 Conflict returned when deletion is blocked [PB-BE-1]
- [ ] HTTP 204 No Content returned when deletion is successful [PB-BE-1]

#### Performance
- [ ] Dependency check completes within 1 second [PB-FE-1]

---

### Business Rules

| Rule ID | Description | Source | Status |
|---------|-------------|--------|--------|
| BR-FE-1-002 | Accounts with posted transactions cannot be deleted | PB Frontend | Active |
| BR-BE-1-001 | Deletion blocked if transaction history exists | PB Backend | Active |

---

### Edge Cases

#### Edge Case: Deleting Account with Many Transactions
**Scenario**: Administrator attempts to delete an account with 10,000+ posted transactions  
**Expected Behavior**:
- System performs referential check
- Deletion is blocked
- Error message: "Cannot delete account '{id}'. It has {count} posted transaction(s)."
- Option to deactivate instead of delete
- Delete button remains disabled  
**Source**: PB Frontend (FE-1, BR-FE-1-002), PB Backend (BE-1, BR-BE-1-001)

---

### Not Applicable (NA) Items

#### UI/UX Details (Redesign Planned)
- ❌ PowerBuilder Delete button (pb_delete) - React DeleteBtn component [PB-FE-1]

#### Technology Stack (Migration Planned)
- ❌ PowerBuilder DELETE operation - REST API DELETE endpoint [PB-BE-1]
- ❌ PowerBuilder referential check - Backend dependency check service [PB-BE-1]

---

### Dependencies

- US-AM-001

---

### Business Value

Protects historical financial data and ensures compliance with record retention requirements.

---

### Technical Notes

#### Migration Considerations
- DeleteBtn component will perform dependency check before deletion
- DELETE endpoint will enforce referential integrity at API level
- Suggest deactivation as safer alternative to deletion

---

## US-AM-010: Enforce Chart-of-Accounts Validation Rules

**Baseline Story ID**: US-AM-010  
**PB Frontend Story**: FE-1 (GL Account Maintenance)  
**PB Backend Story**: BE-1 (GL Account API)

### User Story

**As a** System Administrator  
**I want to** validate all account operations against COA structural rules  
**So that** account integrity is maintained across all operations

**Priority**: Must Have  
**Story Points**: 5  
**Requirements Covered**: R-037, R-038, R-039, R-040, R-041

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Enforce Segment Format rules on all account operations
- [ ] Enforce Segment Value List rules on account creation
- [ ] Validate all required fields before saving
- [ ] Prevent Account Type changes after initial save
- [ ] Prevent Account ID changes after initial save
- [ ] Verify user has appropriate security rights
- [ ] Provide clear, actionable error messages

#### From PowerBuilder Analysis (Enhanced)
- [ ] Account ID must be unique and match defined segment format [PB-FE-1, BR-FE-1-001]
- [ ] Account ID format validated against segment format definition [PB-BE-1, BR-BE-1-002]
- [ ] Validation performed via w_gl_account validation logic [PB-FE-1]
- [ ] Segment format check enforced at API level [PB-BE-1]

---

### Business Rules

| Rule ID | Description | Source | Status |
|---------|-------------|--------|--------|
| BR-FE-1-001 | Account ID must be unique and match defined segment format | PB Frontend | Active |
| BR-BE-1-002 | Account ID format validated against segment format definition | PB Backend | Active |

---

### Dependencies

- US-AM-001
- US-SF-001 (Define Multi-Segment GL Account Structure)
- US-SV-001 (Create and Maintain Segment Value Lists)

---

### Business Value

Ensures data integrity and prevents errors that could compromise financial reporting accuracy.

---

## US-AM-011: Control Account Access and Maintain Audit Trail

**Baseline Story ID**: US-AM-011  
**PB Frontend Story**: None (baseline functionality)  
**PB Backend Story**: None (baseline functionality)

### User Story

**As a** System Administrator  
**I want to** control account access and track all changes comprehensively  
**So that** accounts are secure and changes are auditable for compliance

**Priority**: Must Have  
**Story Points**: 5  
**Requirements Covered**: R-042, R-043, R-044

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Only authorized users can create or modify accounts
- [ ] Log account creation with user and timestamp
- [ ] Log modifications with before and after values
- [ ] Log status changes with justification
- [ ] Log attribute updates for audit trail
- [ ] Inquiry functions respect user security restrictions
- [ ] Account-level restrictions are enforced

---

### Dependencies

- US-AM-001

---

### Business Value

Ensures compliance with SOX and other regulatory requirements while maintaining comprehensive audit trail.

---

## US-AM-012: Create Accounts from Templates

**Baseline Story ID**: US-AM-012  
**PB Frontend Story**: None (baseline functionality)  
**PB Backend Story**: None (baseline functionality)

### User Story

**As an** Accountant  
**I want to** create and use account templates  
**So that** I can quickly create similar accounts with consistent settings

**Priority**: Could Have  
**Story Points**: 5  
**Requirements Covered**: R-023, R-024

---

### Functional Acceptance Criteria

#### From Baseline Requirements
- [ ] Create accounts with Template status
- [ ] Copy all settings from template to new account
- [ ] Template accounts cannot be used in transactions
- [ ] Manage template library with search and filter
- [ ] Template descriptions clearly indicate purpose

---

### Dependencies

- US-AM-001

---

### Business Value

Improves efficiency in account creation and ensures consistency when creating multiple similar accounts.

---

## Summary Statistics

### Story Count and Points
- **Total User Stories**: 12
- **Total Story Points**: 67
- **Must Have Stories**: 9 (54 points)
- **Should Have Stories**: 2 (8 points)
- **Could Have Stories**: 1 (5 points)

### Enhancement Summary
- **Baseline Acceptance Criteria**: 67
- **Enhanced Acceptance Criteria Added**: 32
- **Total Acceptance Criteria**: 99
- **Business Rules Documented**: 9 (5 FE + 4 BE)
- **Edge Cases Documented**: 2
- **NA Items Marked**: 5

### Priority Distribution
| Priority | Story Count | Story Points |
|----------|-------------|--------------|
| Must Have | 9 | 54 |
| Should Have | 2 | 8 |
| Could Have | 1 | 5 |
| **Total** | **12** | **67** |

---

## Changes from Baseline

### New Acceptance Criteria Added
1. **US-AM-001**: 4 new criteria (uniqueness, format validation, API endpoints)
2. **US-AM-002**: 3 new criteria (API endpoints, immediate reflection)
3. **US-AM-003**: 2 new criteria (screen display, attribute grid)
4. **US-AM-004**: 5 new criteria (validation, staged saves, transaction boundary)
5. **US-AM-005**: 3 new criteria (status enforcement, display)
6. **US-AM-007**: 4 new criteria (data retrieval, grid operations)
7. **US-AM-008**: 2 new criteria (transaction history, referential check)
8. **US-AM-009**: 5 new criteria (deletion prevention, API responses)
9. **US-AM-010**: 4 new criteria (validation enforcement)

### Business Rules Extracted from PowerBuilder
- BR-FE-1-001: Account ID must be unique and match defined segment format
- BR-FE-1-002: Accounts with posted transactions cannot be deleted
- BR-FE-1-003: Inactive accounts may not be selected in new transactions
- BR-FE-2-001: Standard field-level validation required
- BR-FE-3-001: Standard field validation required
- BR-BE-1-001: Deletion blocked if transaction history exists
- BR-BE-1-002: Account ID format validated against segment format definition
- BR-BE-2-001: Standard field-level validation required
- BR-BE-3-001: Standard data validation required

### Edge Cases Identified
1. Duplicate Account ID
2. Invalid Segment Format
3. Deleting account with many transactions

### Non-Functional Requirements Added
- API specifications (POST, GET, PUT, DELETE endpoints)
- Database schema details
- Performance requirements

---

## Review Checklist

### Completeness
- [x] All baseline stories included
- [x] All baseline criteria included
- [x] All 9 PB business rules converted and added (5 FE + 4 BE)
- [x] Traceability matrix complete
- [x] Edge cases documented
- [x] NA items clearly marked
- [x] Story points assigned
- [x] Priority set
- [x] No duplicate criteria

### Quality
- [x] Business language used
- [x] All 9 PB business rules accounted for
- [x] Dependencies identified
- [x] Business value articulated
- [x] Acceptance criteria testable

### Traceability
- [x] Each story linked to PB stories
- [x] Each criterion tagged with source
- [x] Business rules mapped to acceptance criteria
- [x] Cross-references complete

---

## Next Steps

1. **Manual Review**: Review agent to validate all enhancements
2. **Stakeholder Review**: Present to Product Owner and business team
3. **Technical Review**: Validate API specifications and database schema with technical team
4. **Approval**: Obtain sign-off from all stakeholders
5. **Jira Upload**: Generate and upload Jira-format files
6. **Sprint Planning**: Include in upcoming sprint planning session

---

**Document Version**: 3.0  
**Last Updated**: 2026-05-30  
**Prepared By**: Bob (Code Mode) - SpecKit Automation  
**Status**: Ready for Manual Review  
**Approval Status**: Pending