import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  { path: '',           redirectTo: 'gl/accounts', pathMatch: 'full' },
  { path: 'gl/accounts',          loadChildren: () => import('./features/account-list/account-list.module').then(m => m.AccountListModule) },
  { path: 'gl/accounts/new',      loadChildren: () => import('./features/account-form/account-form.module').then(m => m.AccountFormModule) },
  { path: 'gl/accounts/:id',      loadChildren: () => import('./features/account-detail/account-detail.module').then(m => m.AccountDetailModule) },
  { path: 'gl/accounts/:id/edit', loadChildren: () => import('./features/account-form/account-form.module').then(m => m.AccountFormModule) },
  { path: 'gl/accounts/:id/copy', loadChildren: () => import('./features/account-form/account-form.module').then(m => m.AccountFormModule) },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
