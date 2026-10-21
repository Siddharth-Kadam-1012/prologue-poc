import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { AccountResponse } from '../../core/models/account.model';

@Component({
  selector: 'app-account-detail',
  template: `
    <div class="detail-page">
      <div *ngIf="loading" class="state-loading">Loading…</div>
      <div *ngIf="error" class="state-error">{{ error }}</div>

      <ng-container *ngIf="!loading && account">
        <div class="detail-header">
          <div>
            <h2>{{ account.accountId }}</h2>
            <p class="description">{{ account.description }}</p>
          </div>
          <app-account-action-bar [accountId]="account.accountId"></app-account-action-bar>
        </div>

        <div class="detail-grid">
          <div class="field-row"><span class="label">Account Type</span><span>{{ account.accountType }}</span></div>
          <div class="field-row"><span class="label">Category</span><span>{{ account.category }}</span></div>
          <div class="field-row"><span class="label">Status</span>
            <span [class]="'badge badge-' + account.status.toLowerCase()">{{ account.status }}</span>
          </div>
          <div class="field-row"><span class="label">Balance Type</span><span>{{ account.balanceType }}</span></div>
          <div class="field-row"><span class="label">Normal Balance</span><span>{{ account.normalBalance }}</span></div>
          <div class="field-row"><span class="label">Currency</span><span>{{ account.currencyId || '—' }}</span></div>
          <div class="field-row"><span class="label">Created Date</span><span>{{ account.createdDate }}</span></div>
          <div class="field-row"><span class="label">Created By</span><span>{{ account.createdBy }}</span></div>
        </div>

        <div class="tabs">
          <button [class.active]="tab==='attributes'"    (click)="tab='attributes'">Attributes</button>
          <button [class.active]="tab==='groups'"        (click)="tab='groups'">Groups</button>
          <button [class.active]="tab==='period'"        (click)="tab='period'">Period Summary</button>
          <button [class.active]="tab==='transactions'"  (click)="tab='transactions'">Transactions</button>
        </div>

        <div class="tab-content" [ngSwitch]="tab">
          <div *ngSwitchCase="'attributes'">
            <app-attributes-panel [accountId]="account.accountId"></app-attributes-panel>
          </div>
          <div *ngSwitchCase="'groups'">
            <app-account-groups [accountId]="account.accountId"></app-account-groups>
          </div>
          <div *ngSwitchCase="'period'">
            <app-period-summary [accountId]="account.accountId"></app-period-summary>
          </div>
          <div *ngSwitchCase="'transactions'">
            <app-transaction-list [accountId]="account.accountId"></app-transaction-list>
          </div>
        </div>
      </ng-container>
    </div>
  `,
  styles: [`
    .detail-header { display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:16px; }
    .description { color:#57606a; margin:4px 0 0; }
    .detail-grid { display:grid; grid-template-columns:1fr 1fr; gap:8px 24px; margin-bottom:24px; }
    .field-row { display:flex; flex-direction:column; }
    .label { font-size:12px; color:#57606a; margin-bottom:2px; }
    .badge { display:inline-block; padding:2px 8px; border-radius:12px; font-size:12px; }
    .badge-active   { background:#d4edda; color:#155724; }
    .badge-hold     { background:#fff3cd; color:#856404; }
    .badge-inactive { background:#f8d7da; color:#721c24; }
    .badge-template { background:#d1ecf1; color:#0c5460; }
    .tabs { display:flex; gap:0; border-bottom:2px solid #e5e7eb; margin-bottom:16px; }
    .tabs button { padding:8px 16px; background:none; border:none; cursor:pointer; color:#57606a; }
    .tabs button.active { border-bottom:2px solid #0060a9; color:#0060a9; font-weight:600; margin-bottom:-2px; }
    .state-loading, .state-error { padding:24px; text-align:center; color:#57606a; }
    .state-error { color:#721c24; }
    .delete-error { background:#f8d7da; border:1px solid #f5c6cb; padding:8px 12px; border-radius:4px; margin-bottom:12px; color:#721c24; }
  `]
})
export class AccountDetailComponent implements OnInit {
  account: AccountResponse | null = null;
  loading = true;
  error: string | null = null;
  tab = 'attributes';

  constructor(private svc: GlAccountsService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.svc.getAccount(id).subscribe({
      next: a => { this.account = a; this.loading = false; },
      error: () => { this.error = 'Account not found.'; this.loading = false; }
    });
  }
}
