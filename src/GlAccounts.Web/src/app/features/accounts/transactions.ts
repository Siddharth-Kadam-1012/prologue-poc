import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { ApiError, flattenFieldErrors, toApiError } from '../../core/api-error';
import { Announcer } from '../../core/announcer';
import { FileSaver } from '../../core/file-saver';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import { JournalEntryDetail, JournalEntryPage, TransactionsQuery } from '../../core/models';
import { EmptyState } from '../../shared/empty-state';
import { ErrorBanner } from '../../shared/error-banner';
import { PageHeader } from '../../shared/page-header';

/** UX-GLA-005 (right half): transaction list with filters, paging, drill-down and CSV export (Flow 7). Stub data only. */
@Component({
  selector: 'app-transactions',
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    PageHeader,
    EmptyState,
    ErrorBanner,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="'Transactions for ' + id()" subtitle="Journal entries posted to this account">
      <a mat-stroked-button [routerLink]="['/gl/accounts', id()]">Back to account</a>
      <a mat-stroked-button [routerLink]="['/gl/accounts', id(), 'period-summary']">Period summary</a>
    </app-page-header>

    <form class="filters" [formGroup]="form" (ngSubmit)="apply()" role="search" aria-label="Transaction filter">
      <mat-form-field>
        <mat-label>From date</mat-label>
        <input matInput type="date" formControlName="fromDate" />
      </mat-form-field>
      <mat-form-field>
        <mat-label>To date</mat-label>
        <input matInput type="date" formControlName="toDate" />
      </mat-form-field>
      <span class="or">or</span>
      <mat-form-field>
        <mat-label>Fiscal year</mat-label>
        <input matInput type="number" formControlName="fiscalYear" min="1900" max="2999" />
      </mat-form-field>
      <mat-form-field>
        <mat-label>Fiscal period</mat-label>
        <input matInput type="number" formControlName="fiscalPeriod" min="1" max="13" />
      </mat-form-field>
      <button mat-flat-button type="submit" [disabled]="loading()">Apply</button>
      <button mat-stroked-button type="button" (click)="clear()">Clear filter</button>
      <button mat-stroked-button type="button" [disabled]="loading() || exporting()" (click)="exportCsv()">Export CSV</button>
    </form>
    <p class="hint" id="filter-hint">Date range and fiscal period filters cannot be combined: filling one disables the other.</p>
    @if (filterError(); as message) {
      <p class="filter-error" role="alert">{{ message }}</p>
    }

    <section [attr.aria-busy]="loading()" aria-label="Transaction results">
      @if (loading()) {
        <p role="status">Loading transactions…</p>
      } @else if (error(); as failure) {
        @if (failure.kind === 'notFound') {
          <app-empty-state [message]="failure.message">
            <a mat-stroked-button routerLink="/gl/accounts">Search accounts</a>
          </app-empty-state>
        } @else {
          <app-error-banner [error]="failure" actionLabel="Try again" (action)="load()" />
        }
      } @else if (result(); as data) {
        @if (data.total === 0) {
          <app-empty-state message="No transactions for this account or filter." />
        } @else {
          <p class="count">{{ countText(data) }}</p>
          <table aria-label="Transactions">
            <thead>
              <tr>
                <th scope="col">Batch ID</th>
                <th scope="col">Description</th>
                <th scope="col" class="numeric">Amount</th>
                <th scope="col">Posting status</th>
                <th scope="col">Posting date</th>
                <th scope="col">Detail</th>
              </tr>
            </thead>
            <tbody>
              @for (row of data.items; track row.entryId) {
                <tr [class.selected]="row.entryId === selectedId()">
                  <td>{{ row.batchId }}</td>
                  <td>{{ row.description }}</td>
                  <td class="numeric">{{ row.amount | number: '1.2-2' }}</td>
                  <td>{{ row.postingStatus }}</td>
                  <td>{{ row.postingDate }}</td>
                  <td>
                    <button
                      mat-stroked-button
                      type="button"
                      [attr.aria-label]="'View journal entry ' + row.entryId"
                      [attr.aria-expanded]="row.entryId === selectedId()"
                      (click)="openEntry(row.entryId)"
                    >
                      View
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
            [pageSizeOptions]="[50, 100, 200]"
            aria-label="Select page of transactions"
            (page)="onPage($event)"
          />
        }
      }
    </section>

    @if (selectedId()) {
      <section class="detail" aria-labelledby="entry-heading" [attr.aria-busy]="detailLoading()">
        <h2 id="entry-heading" tabindex="-1" #entryHeading>Journal entry {{ selectedId() }}</h2>
        @if (detailLoading()) {
          <p role="status">Loading journal entry…</p>
        } @else if (detailError(); as failure) {
          <app-error-banner [error]="failure" actionLabel="Try again" (action)="openEntry(selectedId()!)" />
        } @else if (detail(); as entry) {
          <p>
            Batch {{ entry.batchId }}, fiscal year {{ entry.fiscalYear }} period {{ entry.fiscalPeriod }},
            {{ entry.postingStatus }} on {{ entry.postingDate }}
          </p>
          <p>{{ entry.description }}</p>
          <table aria-label="Journal entry lines">
            <thead>
              <tr>
                <th scope="col">Line</th>
                <th scope="col">Account</th>
                <th scope="col">Description</th>
                <th scope="col" class="numeric">Debit</th>
                <th scope="col" class="numeric">Credit</th>
              </tr>
            </thead>
            <tbody>
              @for (line of entry.lines; track line.lineNumber) {
                <tr>
                  <td>{{ line.lineNumber }}</td>
                  <td>{{ line.accountId }}</td>
                  <td>{{ line.description ?? '' }}</td>
                  <td class="numeric">{{ line.debit | number: '1.2-2' }}</td>
                  <td class="numeric">{{ line.credit | number: '1.2-2' }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
        <button mat-stroked-button type="button" (click)="closeEntry()">Close detail</button>
      </section>
    }
  `,
  styles: `
    .filters {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
      align-items: baseline;
    }
    .or,
    .hint {
      color: var(--mat-sys-on-surface-variant);
    }
    .filter-error {
      color: var(--mat-sys-error);
    }
    table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 16px;
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
    tr.selected {
      background: var(--mat-sys-surface-container);
    }
    .detail {
      margin-top: 24px;
      padding: 16px;
      border: 2px solid var(--mat-sys-outline);
      border-radius: 8px;
    }
  `,
})
export class TransactionsPage {
  /** Route parameter `:id`. */
  readonly id = input.required<string>();

  private readonly api = inject(GlAccountsApi);
  private readonly announcer = inject(Announcer);
  private readonly saver = inject(FileSaver);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly form = new FormGroup({
    fromDate: new FormControl('', { nonNullable: true }),
    toDate: new FormControl('', { nonNullable: true }),
    fiscalYear: new FormControl<number | null>(null),
    fiscalPeriod: new FormControl<number | null>(null),
  });

  protected readonly loading = signal(true);
  protected readonly exporting = signal(false);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly filterError = signal<string | null>(null);
  protected readonly result = signal<JournalEntryPage | null>(null);

  protected readonly selectedId = signal<string | null>(null);
  protected readonly detail = signal<JournalEntryDetail | null>(null);
  protected readonly detailLoading = signal(false);
  protected readonly detailError = signal<ApiError | null>(null);

  /** The default page is 50 rows (AC-GLA-037). */
  private page = 1;
  private pageSize = 50;

  constructor() {
    const controls = this.form.controls;
    // The two filters are mutually exclusive in the UI, matching the API's 400 rule.
    const sync = () => {
      const dates = !!controls.fromDate.value || !!controls.toDate.value;
      const fiscal = controls.fiscalYear.value !== null || controls.fiscalPeriod.value !== null;
      this.toggle([controls.fromDate, controls.toDate], !fiscal);
      this.toggle([controls.fiscalYear, controls.fiscalPeriod], !dates);
    };
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(sync);

    effect(() => {
      this.id();
      untracked(() => {
        this.closeEntry();
        this.page = 1;
        this.load();
      });
    });
  }

  private toggle(list: { enabled: boolean; enable(o: object): void; disable(o: object): void }[], enabled: boolean): void {
    for (const control of list) {
      if (control.enabled === enabled) continue;
      if (enabled) control.enable({ emitEvent: false });
      else control.disable({ emitEvent: false });
    }
  }

  protected apply(): void {
    const raw = this.form.getRawValue();
    if (raw.fiscalPeriod !== null && raw.fiscalYear === null) {
      this.filterError.set('Enter a fiscal year together with the fiscal period.');
      return;
    }
    if (raw.fromDate && raw.toDate && raw.toDate < raw.fromDate) {
      this.filterError.set('The to date cannot be earlier than the from date.');
      return;
    }
    this.page = 1;
    this.load();
  }

  protected clear(): void {
    this.form.reset({ fromDate: '', toDate: '', fiscalYear: null, fiscalPeriod: null });
    this.page = 1;
    this.load();
  }

  protected onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  protected load(): void {
    this.filterError.set(null);
    this.loading.set(true);
    this.error.set(null);
    this.api.listAccountTransactions(this.id(), { ...this.filter(), page: this.page, pageSize: this.pageSize }).subscribe({
      next: (data) => {
        this.result.set(data);
        this.loading.set(false);
        this.announcer.announce(data.total === 0 ? 'No transactions found.' : this.countText(data));
      },
      error: (e: unknown) => {
        const failure = toApiError(e);
        this.loading.set(false);
        if (failure.kind === 'validation') {
          // An invalid filter combination is an inline error, not a page failure.
          const fields = flattenFieldErrors(failure);
          this.filterError.set(fields.length > 0 ? fields.map((f) => f.message).join(' ') : failure.message);
          this.announcer.alert(this.filterError()!);
          return;
        }
        this.error.set(failure);
        this.announcer.alert('Could not load transactions.');
      },
    });
  }

  /** "Showing 1 to 50 of 10,001 entries" style count (AC-GLA-037). */
  protected countText(data: JournalEntryPage): string {
    const first = (data.page - 1) * data.pageSize + 1;
    const last = Math.min(first + data.items.length - 1, data.total);
    return `Showing ${first.toLocaleString('en-US')} to ${last.toLocaleString('en-US')} of ${data.total.toLocaleString('en-US')} entries`;
  }

  protected openEntry(entryId: string): void {
    this.selectedId.set(entryId);
    this.detail.set(null);
    this.detailError.set(null);
    this.detailLoading.set(true);
    this.api.getAccountTransaction(this.id(), entryId).subscribe({
      next: (entry) => {
        this.detail.set(entry);
        this.detailLoading.set(false);
        this.announcer.announce(`Journal entry ${entryId} loaded, ${entry.lines.length} lines.`);
        this.focusDetail();
      },
      error: (e: unknown) => {
        this.detailError.set(toApiError(e));
        this.detailLoading.set(false);
        this.focusDetail();
      },
    });
  }

  protected closeEntry(): void {
    this.selectedId.set(null);
    this.detail.set(null);
    this.detailError.set(null);
  }

  /** AC-GLA-036: the export covers every entry matching the filter, not only the visible page. */
  protected exportCsv(): void {
    this.exporting.set(true);
    this.api.exportAccountTransactions(this.id(), this.filter()).subscribe({
      next: (file) => {
        this.exporting.set(false);
        this.saver.save(file);
        this.announcer.announce('Export CSV started.');
      },
      error: (e: unknown) => {
        this.exporting.set(false);
        this.announcer.alert(toApiError(e).message);
      },
    });
  }

  private filter(): Omit<TransactionsQuery, 'page' | 'pageSize'> {
    const raw = this.form.getRawValue();
    return {
      fromDate: raw.fromDate || undefined,
      toDate: raw.toDate || undefined,
      fiscalYear: raw.fiscalYear ?? undefined,
      fiscalPeriod: raw.fiscalPeriod ?? undefined,
    };
  }

  private focusDetail(): void {
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLElement>('#entry-heading')?.focus(), { injector: this.injector });
  }
}
