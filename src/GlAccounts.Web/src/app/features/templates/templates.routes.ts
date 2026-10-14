import { Routes } from '@angular/router';

// T019: the template library at /gl/account-templates.
export const TEMPLATE_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./template-library').then((m) => m.TemplateLibrary),
    title: 'Account templates',
  },
];
