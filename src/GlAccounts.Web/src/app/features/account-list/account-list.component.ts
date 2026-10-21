import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { AccountResponse, AccountStatus } from '../../core/models/account.model';

@Component({
  selector: 'app-account-list',
  template: `
    <div class="account-list-page">
      <div class="page-header">
        <h1>GL Accounts</h1>
        <button class="btn-primary" (click)="create()">+ New Account</button>
      </div>

      <div class="search-bar">
        <select [(ngModel)]="statusFilter" (change)="load()">
          <option value="">All Statuses</option>
          <option *ngFor="let s of statuses" [value]="s">{{ s }}</option>
        </select>
      </div>

      <div *ngIf="loading" class="state-loading">Loading accounts…</div>
      <div *ngIf="error" class="state-error">{{ error }}</div>

      <table *ngIf="!loading && !error && accounts.length > 0" class="account-table">
        <thead>
          <tr>
            <th>Account ID</th><th>Description</th><th>Type</th><th>Category</th><th>Status</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let a of accounts" (click)="view(a.accountId)" class="clickable-row">
            <td>{{ a.accountId }}</td>
            <td>{{ a.description }}</td>
            <td>{{ a.accountType }}</td>
            <td>{{ a.category }}</td>
            <td><span [class]="'badge badge-' + a.status.toLowerCase()">{{ a.status }}</span></td>
          </tr>
        </tbody>
      </table>

      <div *ngIf="!loading && !error && accounts.length === 0" class="state-empty">
        No accounts found.
      </div>
    </div>
  `,
  styles: [`
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:16px; }
    .btn-primary { background:#0060a9; color:#fff; border:none; padding:8px 16px; border-radius:4px; cursor:pointer; }
    .search-bar { margin-bottom:12px; }
    .account-table { width:100%; border-collapse:collapse; }
    .account-table th, .account-table td { padding:8px 12px; border-bottom:1px solid #e5e7eb; text-align:left; }
    .clickable-row { cursor:pointer; }
    .clickable-row:hover { background:#f7f8fa; }
    .badge { padding:2px 8px; border-radius:12px; font-size:12px; }
    .badge-active   { background:#d4edda; color:#155724; }
    .badge-hold     { background:#fff3cd; color:#856404; }
    .badge-inactive { background:#f8d7da; color:#721c24; }
    .badge-template { background:#d1ecf1; color:#0c5460; }
    .state-loading, .state-empty, .state-error { padding:24px; text-align:center; color:#57606a; }
    .state-error { color:#721c24; }
  `]
})
export class AccountListComponent implements OnInit {
  accounts: AccountResponse[] = [];
  loading = true;
  error: string | null = null;
  statusFilter = '';
  statuses: AccountStatus[] = ['Active', 'Hold', 'Inactive', 'Template'];

  constructor(private svc: GlAccountsService, private router: Router) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.error = null;
    this.svc.listAccounts(this.statusFilter || undefined).subscribe({
      next: r => { this.accounts = r.items; this.loading = false; },
      error: () => { this.error = 'Failed to load accounts.'; this.loading = false; }
    });
  }

  view(id: string): void { this.router.navigate(['/gl/accounts', id]); }
  create(): void { this.router.navigate(['/gl/accounts/new']); }
}
