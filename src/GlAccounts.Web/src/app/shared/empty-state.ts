import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** UX gap: EmptyState. Plain text so it reads the same to every user. */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty-state">
      <p class="message">{{ message() }}</p>
      <ng-content />
    </div>
  `,
  styles: `
    .empty-state {
      padding: 24px;
      text-align: center;
      border: 1px dashed var(--mat-sys-outline);
      border-radius: 8px;
      color: var(--mat-sys-on-surface-variant);
    }
    .message {
      margin: 0 0 8px;
    }
  `,
})
export class EmptyState {
  readonly message = input.required<string>();
}
