import { ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { ApiError, toApiError } from '../../core/api-error';
import { Announcer } from '../../core/announcer';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import { AccountSummary, AccountSummaryPage, Category, FINANCIAL_CATEGORIES } from '../../core/models';
import { EmptyState } from '../../shared/empty-state';
import { ErrorBanner } from '../../shared/error-banner';
import { PageHeader } from '../../shared/page-header';
import { CreateFromTemplate } from './create-from-template';

/** UX-GLA-006: template library (Flow 8, REQ-GLA-034). Lists accounts with status Template. Stub data only. */
@Component({
  selector: 'app-template-library',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatSelectModule,
    PageHeader,
    EmptyState,
    ErrorBanner,
    CreateFromTemplate,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="Account templates" subtitle="Create a new account from a template's settings">
      <a mat-stroked-button routerLink="/gl/accounts">Accounts</a>
    </app-page-header>

    <form class="filters" [formGroup]="form" (ngSubmit)="search()" role="search" aria-label="Template search">
      <mat-form-field>
        <mat-label>Search</mat-label>
        <input matInput type="search" formControlName="q" maxlength="200" />
        <mat-hint>Account ID or description</mat-hint>
      </mat-form-field>
      <mat-form-field>
        <mat-label>Category</mat-label>
        <mat-select formControlName="category">
          <mat-option value="">All categories</mat-option>
          @for (category of categories; track category) {
            <mat-option [value]="category">{{ category }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <button mat-flat-button type="submit" [disabled]="loading()">Search</button>
    </form>

    <section [attr.aria-busy]="loading()" aria-label="Template results">
      @if (loading()) {
        <p role="status">Loading templates…</p>
      } @else if (error(); as failure) {
        <app-error-banner [error]="failure" actionLabel="Try again" (action)="fetch()" />
      } @else if (result(); as data) {
        @if (data.items.length === 0) {
          <app-empty-state message="No templates match your search.">
            <a mat-stroked-button routerLink="/gl/accounts/new">Create an account with status Template</a>
          </app-empty-state>
        } @else {
          <table aria-label="Templates">
            <thead>
              <tr>
                <th scope="col">Account ID</th>
                <th scope="col">Description</th>
                <th scope="col">Type</th>
                <th scope="col">Category</th>
                <th scope="col">Currency</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (row of data.items; track row.accountId) {
                <tr [class.selected]="row.accountId === selected()?.accountId">
                  <td><a [routerLink]="['/gl/accounts', row.accountId]">{{ row.accountId }}</a></td>
                  <td>{{ row.description }}</td>
                  <td>{{ row.accountType }}</td>
                  <td>{{ row.category }}</td>
                  <td>{{ row.currencyId }}</td>
                  <td>
                    <button
                      mat-stroked-button
                      type="button"
                      [attr.aria-label]="'Create account from template ' + row.accountId"
                      [attr.aria-expanded]="row.accountId === selected()?.accountId"
                      (click)="choose(row)"
                    >
                      Create account from template
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
          <mat-paginator
            [length]="data.total"
            [pageIndex]="data.page - 1"
            [pageSize]="data.pageSize"
            [pageSizeOptions]="[25, 50, 100, 200]"
            aria-label="Select page of templates"
            (page)="onPage($event)"
          />
        }
      }
    </section>

    @if (selected(); as template) {
      <app-create-from-template [template]="template" (closed)="close()" />
    }
  `,
  styles: `
    .filters {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
      align-items: baseline;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th,
    td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    tr.selected {
      background: var(--mat-sys-surface-container);
    }
  `,
})
export class TemplateLibrary {
  private readonly api = inject(GlAccountsApi);
  private readonly announcer = inject(Announcer);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly categories: readonly Category[] = FINANCIAL_CATEGORIES;

  protected readonly form = new FormGroup({
    q: new FormControl('', { nonNullable: true }),
    category: new FormControl<Category | ''>('', { nonNullable: true }),
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly result = signal<AccountSummaryPage | null>(null);
  protected readonly selected = signal<AccountSummary | null>(null);

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

  protected choose(row: AccountSummary): void {
    this.selected.set(row);
  }

  protected close(): void {
    const previous = this.selected();
    this.selected.set(null);
    if (previous) {
      afterNextRender(
        () => this.host.nativeElement.querySelector<HTMLElement>(`button[aria-label="Create account from template ${previous.accountId}"]`)?.focus(),
        { injector: this.injector },
      );
    }
  }

  /** AC-GLA-055: only accounts with status Template are listed, filtered by the search text and category. */
  protected fetch(): void {
    const { q, category } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set(null);
    this.api
      .searchAccounts({ q: q.trim(), status: 'Template', category: category || undefined, page: this.page, pageSize: this.pageSize })
      .subscribe({
        next: (page) => {
          this.result.set(page);
          this.loading.set(false);
          this.announcer.announce(page.items.length === 0 ? 'No templates found.' : `${page.items.length} of ${page.total} templates`);
        },
        error: (e: unknown) => {
          this.error.set(toApiError(e));
          this.loading.set(false);
          this.announcer.alert('Could not load templates.');
        },
      });
  }
}
