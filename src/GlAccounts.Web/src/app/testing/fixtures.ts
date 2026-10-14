import { Account, AccountGroup, AccountSummaryPage } from '../core/models';

// Mirrors the deterministic seed accounts of the API (stub data only).
export const ACTIVE_ACCOUNT: Account = {
  accountId: '1000-100-01',
  segments: [
    { name: 'Department', value: '1000' },
    { name: 'Account', value: '100' },
    { name: 'Sub-account', value: '01' },
  ],
  description: 'Cash and equivalents',
  accountType: 'Financial',
  category: 'Asset',
  currencyId: 'USD',
  status: 'Active',
  balanceType: 'BalanceForward',
  normalBalance: 'Debit',
  createdDate: '2024-01-01',
  eligibleForPosting: true,
  attributes: [
    { attributeId: 'att-1', attributeTypeId: 'dept', attributeValueId: 'finance' },
    { attributeId: 'att-2', attributeTypeId: 'region', attributeValueId: 'emea' },
  ],
};

export const HOLD_ACCOUNT: Account = {
  ...ACTIVE_ACCOUNT,
  accountId: '4000-100-01',
  status: 'Hold',
  eligibleForPosting: false,
  attributes: [],
};

export const NESTED_GROUP: AccountGroup = {
  groupId: 'grp-2',
  name: 'Operating',
  parentChain: [
    { groupId: 'grp-1', name: 'Current assets' },
    { groupId: 'grp-all', name: 'All accounts' },
  ],
};

export const ROOT_GROUP: AccountGroup = { groupId: 'grp-all', name: 'All accounts', parentChain: [] };

export function summaryPage(items: AccountSummaryPage['items'], total = items.length): AccountSummaryPage {
  return { items, total, page: 1, pageSize: 50 };
}

export const SUMMARY_ROW = {
  accountId: '1000-100-01',
  description: 'Cash and equivalents',
  accountType: 'Financial',
  category: 'Asset',
  currencyId: 'USD',
  status: 'Active',
} as const;
