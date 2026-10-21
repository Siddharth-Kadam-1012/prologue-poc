import { Component, Input, OnInit } from '@angular/core';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { AccountGroupResponse } from '../../core/models/account.model';

@Component({
  selector: 'app-account-groups',
  template: `
    <div class="groups-panel">
      <div *ngIf="loading" class="state-msg">Loading…</div>
      <div *ngIf="error"   class="state-msg error">{{ error }}</div>
      <div *ngIf="!loading && !error && groups.length === 0" class="state-msg">
        This account is not a member of any account groups.
      </div>
      <table *ngIf="!loading && groups.length > 0" class="groups-table">
        <thead><tr><th>Group ID</th><th>Group Name</th><th>Description</th></tr></thead>
        <tbody>
          <tr *ngFor="let g of groups">
            <td>{{ g.groupId }}</td>
            <td>{{ g.groupName }}</td>
            <td>{{ g.description ?? '—' }}</td>
          </tr>
        </tbody>
      </table>
      <p class="total" *ngIf="total > 0">{{ total }} group(s)</p>
    </div>
  `,
  styles: [`
    .groups-table { width:100%; border-collapse:collapse; font-size:14px; }
    .groups-table th, .groups-table td { padding:6px 8px; border-bottom:1px solid #e5e7eb; text-align:left; }
    .state-msg { color:#57606a; padding:8px 0; }
    .error { color:#721c24; }
    .total { color:#57606a; font-size:13px; margin-top:8px; }
  `]
})
export class AccountGroupsComponent implements OnInit {
  @Input() accountId = '';
  groups: AccountGroupResponse[] = [];
  total  = 0;
  loading = true;
  error: string | null = null;

  constructor(private svc: GlAccountsService) {}

  ngOnInit(): void {
    this.svc.listGroups(this.accountId).subscribe({
      next: r => { this.groups = r.items; this.total = r.total; this.loading = false; },
      error: () => { this.error = 'Failed to load groups.'; this.loading = false; }
    });
  }
}
