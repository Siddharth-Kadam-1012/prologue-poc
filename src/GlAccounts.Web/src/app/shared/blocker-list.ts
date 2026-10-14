import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DeleteBlocker, DeleteBlockerKind } from '../core/models';

const LABELS: Record<DeleteBlockerKind, string> = {
  PostedTransactions: 'posted transaction(s)',
  UnpostedTransactions: 'unposted transaction(s)',
  Budgets: 'budget(s)',
  Allocations: 'allocation(s)',
  GroupMemberships: 'account group membership(s)',
};

export function blockerLabel(kind: DeleteBlockerKind): string {
  return LABELS[kind] ?? kind;
}

/** UX gap: BlockerList. Every blocking dependency type with its count (AC-GLA-040, AC-GLA-042). */
@Component({
  selector: 'app-blocker-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="blocker-list">
      @for (blocker of blockers(); track blocker.kind) {
        <li>{{ blocker.count }} {{ label(blocker) }}</li>
      }
    </ul>
  `,
  styles: `
    .blocker-list {
      margin: 8px 0;
      padding-left: 20px;
    }
  `,
})
export class BlockerList {
  readonly blockers = input.required<DeleteBlocker[]>();

  protected label(blocker: DeleteBlocker): string {
    return blockerLabel(blocker.kind);
  }
}
