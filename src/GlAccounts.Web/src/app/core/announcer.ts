import { Injectable, signal } from '@angular/core';

/**
 * Text for the two live regions (user-flows.md, "Screen-reader announcements").
 * `polite` is for success and result counts, `assertive` for validation failures, 409 conflicts and 500 errors.
 */
@Injectable({ providedIn: 'root' })
export class Announcer {
  readonly polite = signal('');
  readonly assertive = signal('');

  announce(message: string): void {
    // Clear first so the same message is announced again.
    this.polite.set('');
    queueMicrotask(() => this.polite.set(message));
  }

  alert(message: string): void {
    this.assertive.set('');
    queueMicrotask(() => this.assertive.set(message));
  }
}
