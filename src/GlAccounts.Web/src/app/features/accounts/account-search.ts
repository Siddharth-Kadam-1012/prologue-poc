import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { ApiError, toApiError } from '../../core/api-error';
import { Announcer } from '../../core/announcer';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import { ACCOUNT_STATUSES, AccountStatus, AccountSummaryPage } from '../../core/models';
import { EmptyState } from '../../shared/empty-state';
import { ErrorBanner } from '../../shared/error-banner';
import { PageHeader } from '../../shared/page-header';
import { StatusBadge } from '../../shared/status-badge';

/** Entry page of /gl/accounts: search by Account ID or description (API-GLA-015). Stub data only. */
@Component({
  selector: 'app-account-search',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatSelectModule,
    MatTableModule,
    PageHeader,
    EmptyState,
    ErrorBanner,
    StatusBadge,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="Accounts" subtitle="Search by Account ID or description">
      <a mat-flat-button routerLink="/gl/accounts/new">New account</a>
      <a mat-stroked-button routerLink="/gl/account-templates">Template library</a>
    </app-page-header>

    <form class="filters" [formGroup]="form" (ngSubmit)="search()" role="search" aria-label="Account search">
      <mat-form-field>
        <mat-label>Search</mat-label>
        <input matInput formControlName="q" type="search" maxlength="200" />
      </mat-form-field>
      <mat-form-field>
        <mat-label>Status</mat-label>
        <mat-select formControlName="status">
          <mat-option value="">Any status</mat-option>
          @for (status of statuses; track status) {
            <mat-option [value]="status">{{ status }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <button mat-flat-button type="submit" [disabled]="loading()">Search</button>
    </form>

    <section [attr.aria-busy]="loading()" aria-label="Search results">
      @if (loading()) {
        <p role="status">Loading accounts…</p>
      } @else if (error(); as failure) {
        <app-error-banner [error]="failure" actionLabel="Try again" (action)="search()" />
      } @else if (result(); as page) {
        @if (page.items.length === 0) {
          <app-empty-state message="No accounts match your search." />
        } @else {
          <table mat-table [dataSource]="page.items" aria-label="Accounts">
            <ng-container matColumnDef="accountId">
              <th mat-header-cell *matHeaderCellDef scope="col">Account ID</th>
              <td mat-cell *matCellDef="let row"><a [routerLink]="['/gl/accounts', row.accountId]">{{ row.accountId }}</a></td>
            </ng-container>
            <ng-container matColumnDef="description">
              <th mat-header-cell *matHeaderCellDef scope="col">Description</th>
              <td mat-cell *matCellDef="let row">{{ row.description }}</td>
            </ng-container>
            <ng-container matColumnDef="accountType">
              <th mat-header-cell *matHeaderCellDef scope="col">Type</th>
              <td mat-cell *matCellDef="let row">{{ row.accountType }}</td>
            </ng-container>
            <ng-container matColumnDef="category">
              <th mat-header-cell *matHeaderCellDef scope="col">Category</th>
              <td mat-cell *matCellDef="let row">{{ row.category }}</td>
            </ng-container>
            <ng-container matColumnDef="currencyId">
              <th mat-header-cell *matHeaderCellDef scope="col">Currency</th>
              <td mat-cell *matCellDef="let row">{{ row.currencyId }}</td>
            </ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef scope="col">Status</th>
              <td mat-cell *matCellDef="let row"><app-status-badge [status]="row.status" /></td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="columns"></tr>
            <tr mat-row *matRowDef="let row; columns: columns"></tr>
          </table>
          <mat-paginator
            [length]="page.total"
            [pageIndex]="page.page - 1"
            [pageSize]="page.pageSize"
            [pageSizeOptions]="[25, 50, 100, 200]"
            aria-label="Select page of accounts"
            (page)="onPage($event)"
          />
        }
      }
    </section>
  `,
  styles: `
    .filters {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
      align-items: baseline;
    }
  `,
})
export class AccountSearch {
  private readonly api = inject(GlAccountsApi);
  private readonly announcer = inject(Announcer);

  protected readonly columns = ['accountId', 'description', 'accountType', 'category', 'currencyId', 'status'];
  protected readonly statuses = ACCOUNT_STATUSES;

  protected readonly form = new FormGroup({
    q: new FormControl('', { nonNullable: true }),
    status: new FormControl<AccountStatus | ''>('', { nonNullable: true }),
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly result = signal<AccountSummaryPage | null>(null);

  private page = 1;
  private pageSize = 50;

  constructor() {
    this.fetch();
  }

  protected search(): void {
    this.page = 1;
    this.fetch();
  }

  protected onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.fetch();
  }

  private fetch(): void {
    const { q, status } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set(null);
    this.api.searchAccounts({ q: q.trim(), status: status || undefined, page: this.page, pageSize: this.pageSize }).subscribe({
      next: (page) => {
        this.result.set(page);
        this.loading.set(false);
        this.announcer.announce(`${page.items.length} of ${page.total} accounts`);
      },
      error: (e: unknown) => {
        this.error.set(toApiError(e));
        this.loading.set(false);
        this.announcer.alert('Could not load accounts.');
      },
    });
  }
}
