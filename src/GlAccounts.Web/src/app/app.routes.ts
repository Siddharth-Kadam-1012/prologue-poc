import { Routes } from '@angular/router';

// Feature routes are lazy-loaded (frontend.md). Each feature file is filled in by its own task:
// accounts (T016, T017, T018) and templates (T019).
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'gl/accounts' },
  { path: 'gl/accounts', loadChildren: () => import('./features/accounts/accounts.routes').then((m) => m.ACCOUNT_ROUTES) },
  { path: 'gl/account-templates', loadChildren: () => import('./features/templates/templates.routes').then((m) => m.TEMPLATE_ROUTES) },
  { path: '**', loadComponent: () => import('./shared/not-found').then((m) => m.NotFound) },
];
