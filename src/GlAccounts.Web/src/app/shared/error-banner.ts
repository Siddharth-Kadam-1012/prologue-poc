import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ApiError } from '../core/api-error';

/**
 * UX gap: ErrorBanner. Shows a failed request. `role="alert"` makes it assertive for screen readers.
 * The retry button is shown only when the parent listens for it.
 */
@Component({
  selector: 'app-error-banner',
  imports: [MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="error-banner" role="alert">
      <strong>{{ heading() }}</strong>
      <span>{{ error().message }}</span>
      @if (actionLabel()) {
        <button mat-stroked-button type="button" (click)="action.emit()">{{ actionLabel() }}</button>
      }
    </div>
  `,
  styles: `
    .error-banner {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      margin-bottom: 16px;
      border-radius: 8px;
      border: 1px solid var(--mat-sys-error);
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }
  `,
})
export class ErrorBanner {
  readonly error = input.required<ApiError>();
  readonly actionLabel = input<string>();
  readonly action = output<void>();

  heading(): string {
    switch (this.error().kind) {
      case 'notFound':
        return 'Not found.';
      case 'staleVersion':
        return 'This account was changed by someone else.';
      case 'network':
        return 'Service unavailable.';
      case 'server':
        return 'Something went wrong.';
      default:
        return 'The request was rejected.';
    }
  }
}
