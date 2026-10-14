import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input, signal, untracked } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ApiError, toApiError } from '../../core/api-error';
import { Announcer } from '../../core/announcer';
import { FileSaver } from '../../core/file-saver';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import { PeriodSummary, PeriodSummaryQuery } from '../../core/models';
import { EmptyState } from '../../shared/empty-state';
import { ErrorBanner } from '../../shared/error-banner';
import { PageHeader } from '../../shared/page-header';

type SortColumn = NonNullable<PeriodSummaryQuery['sortBy']>;
type SortDirection = NonNullable<PeriodSummaryQuery['sortDirection']>;

/** UX-GLA-005 (left half): balances by fiscal period and year, plus budget versions (Flow 6). Stub data only. */
@Component({
  selector: 'app-period-summary',
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    PageHeader,
    EmptyState,
    ErrorBanner,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'Period summary for ' + id()" subtitle="Balances by fiscal period and year, and budget versions">
      <a mat-stroked-button [routerLink]="['/gl/accounts', id()]">Back to account</a>
      <a mat-stroked-button [routerLink]="['/gl/accounts', id(), 'transactions']">Transactions</a>
    </app-page-header>

    <form class="filters" [formGroup]="form" (ngSubmit)="apply()" role="search" aria-label="Period summary filter">
      <mat-form-field>
        <mat-label>Fiscal year</mat-label>
        <input matInput type="number" formControlName="fiscalYear" min="1900" max="2999" />
        <mat-hint>Leave empty for all years</mat-hint>
      </mat-form-field>
      <button mat-flat-button type="submit" [disabled]="loading()">Apply</button>
      <button mat-stroked-button type="button" [disabled]="loading() || exporting()" (click)="exportExcel()">Export Excel</button>
    </form>

    <section [attr.aria-busy]="loading()" aria-label="Period summary results">
      @if (loading()) {
        <p role="status">Loading period summary…</p>
      } @else if (error(); as failure) {
        @if (failure.kind === 'notFound') {
          <app-empty-state [message]="failure.message">
            <a mat-stroked-button routerLink="/gl/accounts">Search accounts</a>
          </app-empty-state>
        } @else {
          <app-error-banner [error]="failure" actionLabel="Try again" (action)="load()" />
        }
      } @else if (summary(); as data) {
        @if (isEmpty(data)) {
          <app-empty-state message="No balances for this account." />
        } @else {
          <h2 id="periods-heading">Balances by fiscal period</h2>
          <table aria-labelledby="periods-heading">
            <thead>
              <tr>
                @for (column of columns; track column.key) {
                  <th scope="col" [attr.aria-sort]="ariaSort(column.key)" [class.numeric]="column.key === 'balance'">
                    <button type="button" class="sort" (click)="sortBy(column.key)">
                      {{ column.label }}{{ sortMark(column.key) }}
                    </button>
                  </th>
                }
              </tr>
            </thead>
            <tbody>
              @for (row of data.periods; track row.fiscalYear + '-' + row.fiscalPeriod) {
                <tr>
                  <td>{{ row.fiscalYear }}</td>
                  <td>{{ row.fiscalPeriod }}</td>
                  <td class="numeric">{{ row.balance | number: '1.2-2' }}</td>
                </tr>
              }
            </tbody>
          </table>

          <h2 id="years-heading">Balances by fiscal year</h2>
          <table aria-labelledby="years-heading">
            <thead>
              <tr>
                <th scope="col">Fiscal year</th>
                <th scope="col" class="numeric">Balance</th>
              </tr>
            </thead>
            <tbody>
              @for (row of data.years; track row.fiscalYear) {
                <tr>
                  <td>{{ row.fiscalYear }}</td>
                  <td class="numeric">{{ row.balance | number: '1.2-2' }}</td>
                </tr>
              }
            </tbody>
          </table>

          <h2 id="budgets-heading">Budget versions</h2>
          @if (data.budgetVersions.length === 0) {
            <app-empty-state message="No budget versions for this account." />
          } @else {
            <table aria-labelledby="budgets-heading">
              <thead>
                <tr>
                  <th scope="col">Version</th>
                  <th scope="col">Fiscal year</th>
                  <th scope="col" class="numeric">Amount</th>
                </tr>
              </thead>
              <tbody>
                @for (row of data.budgetVersions; track row.budgetVersion + '-' + row.fiscalYear) {
                  <tr>
                    <td>{{ row.budgetVersion }}</td>
                    <td>{{ row.fiscalYear }}</td>
                    <td class="numeric">{{ row.amount | number: '1.2-2' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
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
    table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 24px;
    }
    th,
    td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .numeric {
      text-align: right;
    }
    .sort {
      border: 0;
      background: none;
      padding: 0;
      font: inherit;
      font-weight: 600;
      cursor: pointer;
      color: inherit;
    }
  `,
})
export class PeriodSummaryPage {
  /** Route parameter `:id`. */
  readonly id = input.required<string>();

  private readonly api = inject(GlAccountsApi);
  private readonly announcer = inject(Announcer);
  private readonly saver = inject(FileSaver);

  protected readonly columns: { key: SortColumn; label: string }[] = [
    { key: 'fiscalYear', label: 'Fiscal year' },
    { key: 'fiscalPeriod', label: 'Fiscal period' },
    { key: 'balance', label: 'Balance' },
  ];

  protected readonly form = new FormGroup({ fiscalYear: new FormControl<number | null>(null) });

  protected readonly loading = signal(true);
  protected readonly exporting = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly summary = signal<PeriodSummary | null>(null);
  protected readonly sort = signal<{ column: SortColumn; direction: SortDirection } | null>(null);

  constructor() {
    effect(() => {
      this.id();
      untracked(() => this.apply());
    });
  }

  protected apply(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getPeriodSummary(this.id(), this.query()).subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
        this.announcer.announce(`${data.periods.length} fiscal periods shown.`);
      },
      error: (e: unknown) => {
        this.error.set(toApiError(e));
        this.loading.set(false);
        this.announcer.alert('Could not load the period summary.');
      },
    });
  }

  protected sortBy(column: SortColumn): void {
    const current = this.sort();
    const direction: SortDirection = current?.column === column && current.direction === 'asc' ? 'desc' : 'asc';
    this.sort.set({ column, direction });
    this.load();
  }

  protected ariaSort(column: SortColumn): 'ascending' | 'descending' | 'none' {
    const current = this.sort();
    if (current?.column !== column) return 'none';
    return current.direction === 'asc' ? 'ascending' : 'descending';
  }

  protected sortMark(column: SortColumn): string {
    const state = this.ariaSort(column);
    return state === 'ascending' ? ' ▲' : state === 'descending' ? ' ▼' : '';
  }

  protected isEmpty(data: PeriodSummary): boolean {
    return data.periods.length === 0 && data.years.length === 0 && data.budgetVersions.length === 0;
  }

  /** AC-GLA-031: the export uses the same filter and sort as the view. */
  protected exportExcel(): void {
    this.exporting.set(true);
    this.api.exportPeriodSummary(this.id(), this.query()).subscribe({
      next: (file) => {
        this.exporting.set(false);
        this.saver.save(file);
        this.announcer.announce('Export Excel started.');
      },
      error: (e: unknown) => {
        this.exporting.set(false);
        this.announcer.alert(toApiError(e).message);
      },
    });
  }

  private query(): PeriodSummaryQuery {
    const year = this.form.controls.fiscalYear.value;
    const sort = this.sort();
    return {
      fiscalYear: year ?? undefined,
      sortBy: sort?.column,
      sortDirection: sort?.direction,
    };
  }
}
