// ── Enums ─────────────────────────────────────────────────────────────────────
export type AccountType    = 'Financial' | 'Statistical';
export type AccountStatus  = 'Active' | 'Hold' | 'Inactive' | 'Template';
export type BalanceType    = 'BalanceForward' | 'Periodic';
export type NormalBalance  = 'Debit' | 'Credit';
export type PostingStatus  = 'Posted' | 'Unposted';

// ── Account ───────────────────────────────────────────────────────────────────
export interface AccountResponse {
  accountId:     string;
  description:   string;
  accountType:   AccountType;
  category:      string;
  status:        AccountStatus;
  balanceType:   BalanceType;
  normalBalance: NormalBalance;
  currencyId:    string | null;
  createdDate:   string;   // ISO date string (YYYY-MM-DD)
  createdBy:     string;
}

export interface AccountListResponse {
  items: AccountResponse[];
  total: number;
}

export interface CreateAccountRequest {
  accountId:     string;
  description:   string;
  accountType:   AccountType;
  category:      string;
  status:        AccountStatus;
  balanceType:   BalanceType;
  normalBalance: NormalBalance;
  currencyId?:   string;
}

export interface UpdateAccountRequest {
  description?:   string;
  category?:      string;
  status?:        AccountStatus;
  balanceType?:   BalanceType;
  normalBalance?: NormalBalance;
  currencyId?:    string;
}

// ── Attributes ────────────────────────────────────────────────────────────────
export interface AttributeResponse {
  attributeId:               string;
  accountId:                 string;
  attributeTypeId:           string;
  attributeTypeDescription:  string;
  attributeValueId:          string;
  attributeValueDescription: string;
}

export interface AddAttributeRequest {
  attributeTypeId:  string;
  attributeValueId: string;
}

export interface UpdateAttributeRequest {
  attributeValueId: string;
}

// ── Account Groups ────────────────────────────────────────────────────────────
export interface AccountGroupResponse {
  groupId:      string;
  groupName:    string;
  description:  string | null;
}

export interface AccountGroupListResponse {
  accountId: string;
  items:     AccountGroupResponse[];
  total:     number;
}

// ── Period Summary ────────────────────────────────────────────────────────────
export interface PeriodBalanceResponse {
  balanceId:     string;
  accountId:     string;
  fiscalYear:    number;
  fiscalPeriod:  number;
  budgetVersion: string | null;
  balance:       number;
}

export interface PeriodSummaryResponse {
  accountId: string;
  items:     PeriodBalanceResponse[];
  total:     number;
}

// ── Transactions ──────────────────────────────────────────────────────────────
export interface TransactionResponse {
  transactionId: string;
  accountId:     string;
  batchId:       string;
  description:   string;
  amount:        number;
  postingDate:   string;
  postingStatus: PostingStatus;
}

export interface TransactionListResponse {
  accountId: string;
  items:     TransactionResponse[];
  total:     number;
}

// ── Template Copy ─────────────────────────────────────────────────────────────
export interface CopyFromTemplateRequest {
  newAccountId: string;
  description:  string;
}

// ── Error ─────────────────────────────────────────────────────────────────────
export interface ProblemDetails {
  type:     string;
  title:    string;
  status:   number;
  detail?:  string;
  errors?:  Record<string, string[]>;
}
