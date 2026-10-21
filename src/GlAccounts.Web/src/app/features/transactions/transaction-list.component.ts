import { Component, Input, OnInit } from '@angular/core';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { TransactionResponse, PostingStatus } from '../../core/models/account.model';

@Component({
  selector: 'app-transaction-list',
  template: `
    <div class="txn-panel">
      <div class="filters">
        <label>From Date
          <input type="date" [(ngModel)]="fromDate" class="filter-input" />
        </label>
        <label>To Date
          <input type="date" [(ngModel)]="toDate" class="filter-input" />
        </label>
        <label>Status
          <select [(ngModel)]="postingStatus" class="filter-select">
            <option value="">All</option>
            <option value="Posted">Posted</option>
            <option value="Unposted">Unposted</option>
          </select>
        </label>
        <button class="btn-small" (click)="load()">Filter</button>
        <button class="btn-small" (click)="clearFilters()">Clear</button>
      </div>

      <div *ngIf="loading" class="state-msg">Loading…</div>
      <div *ngIf="error"   class="state-msg error">{{ error }}</div>
      <div *ngIf="!loading && !error && items.length === 0" class="state-msg">No transactions found.</div>

      <table *ngIf="!loading && items.length > 0" class="txn-table">
        <thead>
          <tr>
            <th>Transaction ID</th>
            <th>Description</th>
            <th>Batch</th>
            <th>Posting Date</th>
            <th>Status</th>
            <th class="amount">Amount</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let t of items">
            <td>{{ t.transactionId }}</td>
            <td>{{ t.description }}</td>
            <td>{{ t.batchId }}</td>
            <td>{{ t.postingDate }}</td>
            <td>
              <span [class]="'badge badge-' + t.postingStatus.toLowerCase()">{{ t.postingStatus }}</span>
            </td>
            <td class="amount">{{ t.amount | number:'1.2-2' }}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <td colspan="5" class="total-label">Total ({{ total }} transaction(s))</td>
            <td class="amount total-val">{{ grandTotal | number:'1.2-2' }}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  `,
  styles: [`
    .filters { display:flex; gap:12px; flex-wrap:wrap; align-items:flex-end; margin-bottom:16px; }
    .filters label { display:flex; flex-direction:column; font-size:12px; color:#57606a; gap:4px; }
    .filter-input  { padding:6px 8px; border:1px solid #e5e7eb; border-radius:4px; font-size:13px; width:130px; }
    .filter-select { padding:6px 8px; border:1px solid #e5e7eb; border-radius:4px; font-size:13px; }
    .btn-small { padding:6px 12px; border:1px solid #e5e7eb; border-radius:4px; cursor:pointer; font-size:13px; background:#f7f8fa; align-self:flex-end; }
    .txn-table { width:100%; border-collapse:collapse; font-size:14px; }
    .txn-table th, .txn-table td { padding:6px 8px; border-bottom:1px solid #e5e7eb; text-align:left; }
    .amount { text-align:right; }
    .total-label { color:#57606a; font-size:13px; }
    .total-val { font-weight:600; }
    .badge { display:inline-block; padding:2px 8px; border-radius:12px; font-size:12px; }
    .badge-posted   { background:#d4edda; color:#155724; }
    .badge-unposted { background:#fff3cd; color:#856404; }
    .state-msg { color:#57606a; padding:8px 0; }
    .error { color:#721c24; }
  `]
})
export class TransactionListComponent implements OnInit {
  @Input() accountId = '';

  items: TransactionResponse[] = [];
  total = 0;
  loading = true;
  error: string | null = null;

  fromDate:      string = '';
  toDate:        string = '';
  postingStatus: string = '';

  get grandTotal(): number {
    return this.items.reduce((sum, t) => sum + t.amount, 0);
  }

  constructor(private svc: GlAccountsService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.error   = null;
    this.svc.getTransactions(
      this.accountId,
      this.fromDate      || undefined,
      this.toDate        || undefined,
      this.postingStatus || undefined
    ).subscribe({
      next: r => { this.items = r.items; this.total = r.total; this.loading = false; },
      error: () => { this.error = 'Failed to load transactions.'; this.loading = false; }
    });
  }

  clearFilters(): void {
    this.fromDate      = '';
    this.toDate        = '';
    this.postingStatus = '';
    this.load();
  }
}
