import { Routes } from '@angular/router';

// T016: search and inquiry. T017: `new` and `:id/edit` (`new` must stay before `:id`).
// T018: `:id/period-summary` and `:id/transactions`.
export const ACCOUNT_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./account-search').then((m) => m.AccountSearch), title: 'Accounts' },
  { path: 'new', loadComponent: () => import('./account-form').then((m) => m.AccountForm), title: 'New account' },
  { path: ':id/edit', loadComponent: () => import('./account-form').then((m) => m.AccountForm), title: 'Edit account' },
  {
    path: ':id/period-summary',
    loadComponent: () => import('./period-summary').then((m) => m.PeriodSummaryPage),
    title: 'Period summary',
  },
  { path: ':id/transactions', loadComponent: () => import('./transactions').then((m) => m.TransactionsPage), title: 'Transactions' },
  { path: ':id', loadComponent: () => import('./account-inquiry').then((m) => m.AccountInquiry), title: 'Account' },
];
