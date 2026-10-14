// Types mirror specs/001-gl-account-maintenance/contracts/openapi.yaml (the contract is authoritative, ADR-001).

export type AccountType = 'Financial' | 'Statistical';
export type Category = 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense' | 'Statistical';
export type CurrencyId = 'USD' | 'EUR' | 'GBP' | 'JPY';
export type AccountStatus = 'Active' | 'Hold' | 'Inactive' | 'Template';
export type BalanceType = 'BalanceForward' | 'Periodic';
export type NormalBalance = 'Debit' | 'Credit';
export type PostingStatus = 'Posted' | 'Unposted';

export const ACCOUNT_TYPES: readonly AccountType[] = ['Financial', 'Statistical'];
export const FINANCIAL_CATEGORIES: readonly Category[] = ['Asset', 'Liability', 'Equity', 'Revenue', 'Expense'];
export const CURRENCIES: readonly CurrencyId[] = ['USD', 'EUR', 'GBP', 'JPY'];
export const ACCOUNT_STATUSES: readonly AccountStatus[] = ['Active', 'Hold', 'Inactive', 'Template'];
export const BALANCE_TYPES: readonly BalanceType[] = ['BalanceForward', 'Periodic'];
export const NORMAL_BALANCES: readonly NormalBalance[] = ['Debit', 'Credit'];

export interface Segment {
  name: string;
  value: string;
}

export interface AccountAttribute {
  attributeId: string;
  attributeTypeId: string;
  attributeValueId: string;
}

export interface AttributeInput {
  attributeTypeId: string;
  attributeValueId: string;
}

export interface AttributeChange {
  action: 'Add' | 'Edit' | 'Remove';
  attributeId?: string;
  attributeTypeId?: string;
  attributeValueId?: string;
}

export interface Account {
  accountId: string;
  segments: Segment[];
  description: string;
  accountType: AccountType;
  category: Category;
  currencyId: CurrencyId;
  status: AccountStatus;
  balanceType?: BalanceType;
  normalBalance?: NormalBalance;
  createdDate: string;
  eligibleForPosting: boolean;
  attributes: AccountAttribute[];
}

export interface AccountSummary {
  accountId: string;
  description: string;
  accountType: AccountType;
  category: Category;
  currencyId: CurrencyId;
  status: AccountStatus;
}

export interface Page<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type AccountSummaryPage = Page<AccountSummary>;

export interface CreateAccountRequest {
  accountId: string;
  templateAccountId?: string;
  description?: string;
  accountType?: AccountType;
  category?: Category;
  currencyId?: CurrencyId;
  status?: AccountStatus;
  balanceType?: BalanceType;
  normalBalance?: NormalBalance;
  attributes?: AttributeInput[];
}

export interface UpdateAccountRequest {
  accountId?: string;
  accountType?: AccountType;
  description: string;
  category: Category;
  currencyId: CurrencyId;
  status: AccountStatus;
  statusJustification?: string;
  attributeChanges?: AttributeChange[];
}

export interface SearchAccountsQuery {
  q?: string;
  status?: AccountStatus;
  accountType?: AccountType;
  category?: Category;
  page?: number;
  pageSize?: number;
}

export interface GroupRef {
  groupId: string;
  name: string;
}

export interface AccountGroup {
  groupId: string;
  name: string;
  parentChain: GroupRef[];
}

export interface AccountGroupList {
  items: AccountGroup[];
}

export interface PeriodBalance {
  fiscalYear: number;
  fiscalPeriod: number;
  balance: number;
}

export interface YearBalance {
  fiscalYear: number;
  balance: number;
}

export interface BudgetVersionAmount {
  budgetVersion: string;
  fiscalYear: number;
  amount: number;
}

export interface PeriodSummary {
  accountId: string;
  periods: PeriodBalance[];
  years: YearBalance[];
  budgetVersions: BudgetVersionAmount[];
}

export interface PeriodSummaryQuery {
  fiscalYear?: number;
  sortBy?: 'fiscalYear' | 'fiscalPeriod' | 'balance';
  sortDirection?: 'asc' | 'desc';
}

export interface JournalEntrySummary {
  entryId: string;
  batchId: string;
  description: string;
  amount: number;
  postingStatus: PostingStatus;
  postingDate: string;
}

export type JournalEntryPage = Page<JournalEntrySummary>;

export interface JournalLine {
  lineNumber: number;
  accountId: string;
  description?: string;
  debit: number;
  credit: number;
}

export interface JournalEntryDetail extends JournalEntrySummary {
  fiscalYear: number;
  fiscalPeriod: number;
  lines: JournalLine[];
}

export interface TransactionsQuery {
  fromDate?: string;
  toDate?: string;
  fiscalYear?: number;
  fiscalPeriod?: number;
  page?: number;
  pageSize?: number;
}

export type DeleteBlockerKind =
  | 'PostedTransactions'
  | 'UnpostedTransactions'
  | 'Budgets'
  | 'Allocations'
  | 'GroupMemberships';

export interface DeleteBlocker {
  kind: DeleteBlockerKind;
  count: number;
}

/** A response body plus the ETag the server sent with it (used as If-Match on the next update). */
export interface WithETag<T> {
  body: T;
  etag: string | null;
}

export interface DownloadedFile {
  blob: Blob;
  fileName: string;
}
