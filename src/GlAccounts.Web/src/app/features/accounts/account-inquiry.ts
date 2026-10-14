import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { ApiError, toApiError } from '../../core/api-error';
import { Announcer } from '../../core/announcer';
import { FileSaver } from '../../core/file-saver';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import { Account, AccountGroup } from '../../core/models';
import { DescriptionItem, DescriptionList } from '../../shared/description-list';
import { EmptyState } from '../../shared/empty-state';
import { ErrorBanner } from '../../shared/error-banner';
import { PageHeader } from '../../shared/page-header';
import { StatusBadge } from '../../shared/status-badge';
import { DeleteAccountDialog, DeleteDialogData, DeleteDialogResult } from './delete-account-dialog';

/** UX-GLA-002: account inquiry (Flow 3). Stub data only. */
@Component({
  selector: 'app-account-inquiry',
  imports: [RouterLink, MatButtonModule, MatTableModule, PageHeader, DescriptionList, StatusBadge, EmptyState, ErrorBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <p role="status" aria-busy="true">Loading account…</p>
    } @else if (loadError(); as failure) {
      @if (failure.kind === 'notFound') {
        <app-page-header title="Account not found" />
        <app-empty-state [message]="failure.message">
          <a mat-stroked-button routerLink="/gl/accounts">Search accounts</a>
        </app-empty-state>
      } @else {
        <app-error-banner [error]="failure" actionLabel="Try again" (action)="load(id())" />
      }
    } @else if (account(); as acct) {
      <app-page-header [title]="acct.accountId" [subtitle]="acct.description">
        <a mat-stroked-button [routerLink]="['/gl/accounts', acct.accountId, 'edit']">Edit</a>
        <a mat-stroked-button [routerLink]="['/gl/accounts', acct.accountId, 'period-summary']">Period summary</a>
        <a mat-stroked-button [routerLink]="['/gl/accounts', acct.accountId, 'transactions']">Transactions</a>
        <button mat-flat-button type="button" (click)="openDelete(acct.accountId)">Delete</button>
      </app-page-header>

      <section aria-labelledby="details-heading">
        <h2 id="details-heading">Details</h2>
        <p>Status: <app-status-badge [status]="acct.status" /></p>
        <app-description-list [items]="details()" />
      </section>

      <section aria-labelledby="segments-heading">
        <h2 id="segments-heading">Segments</h2>
        <app-description-list [items]="segmentItems()" />
      </section>

      <section aria-labelledby="attributes-heading">
        <h2 id="attributes-heading">Attributes</h2>
        @if (acct.attributes.length === 0) {
          <app-empty-state message="This account has no attributes." />
        } @else {
          <table mat-table [dataSource]="acct.attributes" aria-label="Attributes">
            <ng-container matColumnDef="attributeTypeId">
              <th mat-header-cell *matHeaderCellDef scope="col">Type</th>
              <td mat-cell *matCellDef="let row">{{ row.attributeTypeId }}</td>
            </ng-container>
            <ng-container matColumnDef="attributeValueId">
              <th mat-header-cell *matHeaderCellDef scope="col">Value</th>
              <td mat-cell *matCellDef="let row">{{ row.attributeValueId }}</td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="attributeColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: attributeColumns"></tr>
          </table>
        }
      </section>

      <section aria-labelledby="groups-heading" [attr.aria-busy]="groups() === null && !groupsError()">
        <h2 id="groups-heading">Account groups</h2>
        @if (groupsError(); as failure) {
          <app-error-banner [error]="failure" actionLabel="Try again" (action)="loadGroups(id())" />
        } @else if (groups(); as list) {
          @if (list.length === 0) {
            <app-empty-state message="This account is in no group." />
          } @else {
            <table mat-table [dataSource]="list" aria-label="Account groups">
              <ng-container matColumnDef="groupId">
                <th mat-header-cell *matHeaderCellDef scope="col">Group ID</th>
                <td mat-cell *matCellDef="let row">{{ row.groupId }}</td>
              </ng-container>
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef scope="col">Name</th>
                <td mat-cell *matCellDef="let row">{{ row.name }}</td>
              </ng-container>
              <ng-container matColumnDef="parentChain">
                <th mat-header-cell *matHeaderCellDef scope="col">Parent groups</th>
                <td mat-cell *matCellDef="let row">{{ parentChain(row) }}</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="groupColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: groupColumns"></tr>
            </table>
            <p><button mat-stroked-button type="button" (click)="exportGroups(acct.accountId)">Export CSV</button></p>
          }
        }
      </section>
    }
  `,
})
export class AccountInquiry {
  /** Route parameter `:id` (bound by withComponentInputBinding). */
  readonly id = input.required<string>();

  private readonly api = inject(GlAccountsApi);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly announcer = inject(Announcer);
  private readonly saver = inject(FileSaver);

  protected readonly attributeColumns = ['attributeTypeId', 'attributeValueId'];
  protected readonly groupColumns = ['groupId', 'name', 'parentChain'];

  protected readonly loading = signal(true);
  protected readonly account = signal<Account | null>(null);
  protected readonly loadError = signal<ApiError | null>(null);
  protected readonly groups = signal<AccountGroup[] | null>(null);
  protected readonly groupsError = signal<ApiError | null>(null);

  /** AC-GLA-020 / AC-GLA-021: eligibility is shown as text, and the reason names the status. */
  protected readonly details = computed<DescriptionItem[]>(() => {
    const acct = this.account();
    if (!acct) return [];
    const items: DescriptionItem[] = [
      { label: 'Account ID', value: acct.accountId },
      { label: 'Description', value: acct.description },
      { label: 'Account type', value: acct.accountType },
      { label: 'Category', value: acct.category },
      { label: 'Currency', value: acct.currencyId },
    ];
    if (acct.balanceType) items.push({ label: 'Balance type', value: acct.balanceType });
    if (acct.normalBalance) items.push({ label: 'Normal balance', value: acct.normalBalance });
    items.push(
      { label: 'Created', value: acct.createdDate },
      {
        label: 'Posting eligibility',
        value: acct.eligibleForPosting
          ? 'Eligible for new transactions'
          : `Not eligible for new transactions: the account status is ${acct.status}`,
      },
    );
    return items;
  });

  protected readonly segmentItems = computed<DescriptionItem[]>(() =>
    (this.account()?.segments ?? []).map((segment) => ({ label: segment.name, value: segment.value })),
  );

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => this.load(id));
    });
  }

  protected load(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.account.set(null);
    this.api.getAccount(id).subscribe({
      next: ({ body }) => {
        this.account.set(body);
        this.loading.set(false);
        this.loadGroups(id);
      },
      error: (e: unknown) => {
        this.loadError.set(toApiError(e));
        this.loading.set(false);
      },
    });
  }

  protected loadGroups(id: string): void {
    this.groups.set(null);
    this.groupsError.set(null);
    this.api.listAccountGroups(id).subscribe({
      next: (list) => this.groups.set(list.items),
      error: (e: unknown) => this.groupsError.set(toApiError(e)),
    });
  }

  protected parentChain(group: AccountGroup): string {
    return group.parentChain.length === 0 ? '—' : group.parentChain.map((parent) => parent.name).join(' > ');
  }

  protected exportGroups(accountId: string): void {
    this.api.exportAccountGroups(accountId).subscribe({
      next: (file) => {
        this.saver.save(file);
        this.announcer.announce('Export CSV started.');
      },
      error: (e: unknown) => this.announcer.alert(toApiError(e).message),
    });
  }

  protected openDelete(accountId: string): void {
    const data: DeleteDialogData = { accountId };
    this.dialog
      .open<DeleteAccountDialog, DeleteDialogData, DeleteDialogResult>(DeleteAccountDialog, {
        data,
        role: 'alertdialog',
        ariaDescribedBy: 'delete-dialog-description',
        restoreFocus: true,
      })
      .afterClosed()
      .subscribe((result) => {
        if (result === 'deleted' || result === 'gone') {
          const message = result === 'deleted' ? `Account ${accountId} deleted.` : `Account ${accountId} no longer exists.`;
          this.snackBar.open(message, 'Dismiss', { duration: 6000, politeness: 'polite' });
          void this.router.navigate(['/gl/accounts']);
        } else if (result === 'deactivate') {
          void this.router.navigate(['/gl/accounts', accountId, 'edit'], { queryParams: { status: 'Inactive' } });
        }
      });
  }
}
