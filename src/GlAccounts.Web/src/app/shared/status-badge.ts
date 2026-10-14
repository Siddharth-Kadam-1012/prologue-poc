import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { AccountStatus } from '../core/models';

/**
 * UX gap: StatusBadge. The status is always text, never colour alone (user-flows.md, Accessibility).
 * Only Active accounts are eligible for new transactions, and the label says so.
 */
@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class.active]="status() === 'Active'">{{ status() }}</span>`,
  styles: `
    .badge {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 12px;
      border: 1px solid var(--mat-sys-outline);
      font: var(--mat-sys-label-large);
    }
    .badge.active {
      border-color: var(--mat-sys-primary);
      color: var(--mat-sys-primary);
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<AccountStatus>();
  readonly eligible = computed(() => this.status() === 'Active');
}
