import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { ApiError, toApiError } from '../../core/api-error';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import { BlockerList } from '../../shared/blocker-list';
import { ErrorBanner } from '../../shared/error-banner';

export interface DeleteDialogData {
  accountId: string;
}

/** deleted: 204; gone: 404 (already removed); deactivate: user chose "Set status to Inactive instead". */
export type DeleteDialogResult = 'deleted' | 'gone' | 'deactivate';

/** UX-GLA-004: delete confirmation and blocked-delete message (REQ-GLA-024, 025, 026). */
@Component({
  selector: 'app-delete-account-dialog',
  imports: [MatDialogModule, MatButtonModule, BlockerList, ErrorBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Delete account {{ data.accountId }}?</h2>
    <mat-dialog-content id="delete-dialog-description" [attr.aria-busy]="busy()">
      @let failure = error();
      @if (!failure) {
        <p>This removes the account. It cannot be undone.</p>
      } @else if (failure.kind === 'deleteBlocked') {
        <p>{{ failure.message }}</p>
        <app-blocker-list [blockers]="failure.blockers" />
        @if (failure.suggestion) {
          <p>{{ failure.suggestion }}</p>
        }
      } @else if (failure.kind === 'notFound') {
        <p>{{ failure.message }}</p>
      } @else {
        <app-error-banner [error]="failure" />
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      @let current = error();
      @if (current?.kind === 'deleteBlocked') {
        <button mat-button type="button" (click)="close()">Close</button>
        <button mat-flat-button type="button" (click)="deactivate()">Set status to Inactive instead</button>
      } @else if (current?.kind === 'notFound') {
        <button mat-flat-button type="button" (click)="gone()">Close</button>
      } @else {
        <button mat-button type="button" (click)="close()">Cancel</button>
        <button mat-flat-button type="button" [disabled]="busy()" (click)="confirm()">Delete</button>
      }
    </mat-dialog-actions>
  `,
})
export class DeleteAccountDialog {
  protected readonly data = inject<DeleteDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject<MatDialogRef<DeleteAccountDialog, DeleteDialogResult>>(MatDialogRef);
  private readonly api = inject(GlAccountsApi);

  protected readonly busy = signal(false);
  protected readonly error = signal<ApiError | null>(null);

  protected confirm(): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.api.deleteAccount(this.data.accountId).subscribe({
      next: () => this.ref.close('deleted'),
      error: (e: unknown) => {
        this.busy.set(false);
        this.error.set(toApiError(e));
      },
    });
  }

  protected close(): void {
    this.ref.close();
  }

  protected gone(): void {
    this.ref.close('gone');
  }

  protected deactivate(): void {
    this.ref.close('deactivate');
  }
}
