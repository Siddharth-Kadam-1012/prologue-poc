import { Component, Input, OnInit } from '@angular/core';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { PeriodBalanceResponse } from '../../core/models/account.model';

@Component({
  selector: 'app-period-summary',
  template: `
    <div class="period-panel">
      <div class="filters">
        <label>Fiscal Year
          <input type="number" [(ngModel)]="fiscalYear" placeholder="e.g. 2026" class="filter-input" />
        </label>
        <label>From Period
          <input type="number" [(ngModel)]="fromPeriod" placeholder="1" class="filter-input filter-small" />
        </label>
        <label>To Period
          <input type="number" [(ngModel)]="toPeriod" placeholder="12" class="filter-input filter-small" />
        </label>
        <button class="btn-small" (click)="load()">Filter</button>
        <button class="btn-small" (click)="clearFilters()">Clear</button>
      </div>

      <div *ngIf="loading" class="state-msg">Loading…</div>
      <div *ngIf="error"   class="state-msg error">{{ error }}</div>
      <div *ngIf="!loading && !error && items.length === 0" class="state-msg">No period balances found.</div>

      <table *ngIf="!loading && items.length > 0" class="period-table">
        <thead>
          <tr>
            <th>Fiscal Year</th>
            <th>Period</th>
            <th>Budget Version</th>
            <th class="amount">Balance</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let b of items">
            <td>{{ b.fiscalYear }}</td>
            <td>{{ b.fiscalPeriod }}</td>
            <td>{{ b.budgetVersion ?? '—' }}</td>
            <td class="amount">{{ b.balance | number:'1.2-2' }}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <td colspan="3" class="total-label">Total ({{ total }} record(s))</td>
            <td class="amount total-val">{{ grandTotal | number:'1.2-2' }}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  `,
  styles: [`
    .filters { display:flex; gap:12px; flex-wrap:wrap; align-items:flex-end; margin-bottom:16px; }
    .filters label { display:flex; flex-direction:column; font-size:12px; color:#57606a; gap:4px; }
    .filter-input { padding:6px 8px; border:1px solid #e5e7eb; border-radius:4px; font-size:13px; width:110px; }
    .filter-small { width:60px; }
    .btn-small { padding:6px 12px; border:1px solid #e5e7eb; border-radius:4px; cursor:pointer; font-size:13px; background:#f7f8fa; align-self:flex-end; }
    .period-table { width:100%; border-collapse:collapse; font-size:14px; }
    .period-table th, .period-table td { padding:6px 8px; border-bottom:1px solid #e5e7eb; text-align:left; }
    .amount { text-align:right; }
    .total-label { color:#57606a; font-size:13px; }
    .total-val { font-weight:600; }
    .state-msg { color:#57606a; padding:8px 0; }
    .error { color:#721c24; }
  `]
})
export class PeriodSummaryComponent implements OnInit {
  @Input() accountId = '';

  items: PeriodBalanceResponse[] = [];
  total = 0;
  loading = true;
  error: string | null = null;

  fiscalYear:  number | null = null;
  fromPeriod:  number | null = null;
  toPeriod:    number | null = null;

  get grandTotal(): number {
    return this.items.reduce((sum, b) => sum + b.balance, 0);
  }

  constructor(private svc: GlAccountsService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.error   = null;
    this.svc.getPeriodSummary(
      this.accountId,
      this.fiscalYear ?? undefined,
      this.fromPeriod ?? undefined,
      this.toPeriod   ?? undefined
    ).subscribe({
      next: r => { this.items = r.items; this.total = r.total; this.loading = false; },
      error: () => { this.error = 'Failed to load period summary.'; this.loading = false; }
    });
  }

  clearFilters(): void {
    this.fiscalYear = null;
    this.fromPeriod = null;
    this.toPeriod   = null;
    this.load();
  }
}
