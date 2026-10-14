import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FileSaver } from '../../core/file-saver';
import { JournalEntryDetail, JournalEntryPage } from '../../core/models';
import { TransactionsPage } from './transactions';

// UX-GLA-005 transaction list (Flow 7). Stub data only.
// Covers AC-GLA-033, AC-GLA-034, AC-GLA-035, AC-GLA-036, AC-GLA-037.
describe('TransactionsPage', () => {
  let http: HttpTestingController;
  const URL = '/api/gl/accounts/1000-200-01/transactions';

  const entry = (n: number) => ({
    entryId: `je-${String(n).padStart(6, '0')}`,
    batchId: `B-2026-${String(n).padStart(4, '0')}`,
    description: `Entry ${n}`,
    amount: 5000,
    postingStatus: 'Posted' as const,
    postingDate: '2026-01-05',
  });

  const page = (count: number, total: number, pageNo = 1, pageSize = 50): JournalEntryPage => ({
    items: Array.from({ length: count }, (_, i) => entry((pageNo - 1) * pageSize + i + 1)),
    total,
    page: pageNo,
    pageSize,
  });

  const DETAIL: JournalEntryDetail = {
    ...entry(1),
    fiscalYear: 2026,
    fiscalPeriod: 1,
    lines: [
      { lineNumber: 1, accountId: '1000-200-01', description: 'Cash received', debit: 5000, credit: 0 },
      { lineNumber: 2, accountId: '3000-100-01', description: 'Capital contribution', debit: 0, credit: 5000 },
    ],
  };

  async function settle(fixture: { detectChanges(): void; whenStable(): Promise<void> }) {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function render(first: JournalEntryPage | null = page(50, 10001)) {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(TransactionsPage);
    fixture.componentRef.setInput('id', '1000-200-01');
    fixture.detectChanges();
    if (first) http.expectOne((r) => r.url === URL).flush(first);
    await settle(fixture);
    return fixture;
  }

  type Fixture = Awaited<ReturnType<typeof render>>;
  const root = (f: { nativeElement: unknown }) => f.nativeElement as HTMLElement;
  const button = (f: { nativeElement: unknown }, label: string) =>
    Array.from(root(f).querySelectorAll('button')).find((b) => (b.getAttribute('aria-label') ?? b.textContent?.trim()) === label) as HTMLButtonElement;
  const form = (f: Fixture) =>
    (f.componentInstance as unknown as {
      form: { controls: Record<string, { value: unknown; enabled: boolean; setValue(v: unknown): void }> };
    }).form;
  const set = (f: Fixture, values: Record<string, unknown>) => {
    for (const [name, value] of Object.entries(values)) form(f).controls[name].setValue(value);
  };

  afterEach(() => http?.verify());

  it('shows a loading state with aria-busy, then the rows', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(TransactionsPage);
    fixture.componentRef.setInput('id', '1000-200-01');
    fixture.detectChanges();
    expect(root(fixture).textContent).toContain('Loading transactions');
    expect(root(fixture).querySelector('[aria-busy="true"]')).not.toBeNull();

    http.expectOne((r) => r.url === URL).flush(page(2, 2));
    await settle(fixture);

    expect(root(fixture).textContent).not.toContain('Loading transactions');
  });

  it('AC-GLA-033 AC-GLA-037 requests the first page of 50 and shows batch, description, amount and posting status', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(TransactionsPage);
    fixture.componentRef.setInput('id', '1000-200-01');
    fixture.detectChanges();

    const req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('50');
    req.flush(page(50, 10001));
    await settle(fixture);

    const rows = root(fixture).querySelectorAll('table[aria-label="Transactions"] tbody tr');
    expect(rows.length).toBe(50);
    expect(rows[0].textContent).toContain('B-2026-0001');
    expect(rows[0].textContent).toContain('Entry 1');
    expect(rows[0].textContent).toContain('5,000.00');
    expect(rows[0].textContent).toContain('Posted');
  });

  it('AC-GLA-037 shows the result count for a very large history and a way to reach the rest', async () => {
    const fixture = await render();

    expect(root(fixture).querySelector('.count')?.textContent).toContain('Showing 1 to 50 of 10,001 entries');
    expect(root(fixture).querySelector('mat-paginator')).not.toBeNull();
  });

  it('AC-GLA-037 pages up to a size of 200', async () => {
    const fixture = await render();

    (fixture.componentInstance as unknown as { onPage(e: unknown): void }).onPage({ pageIndex: 1, pageSize: 200, length: 10001 });
    const req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('200');
    req.flush(page(200, 10001, 2, 200));
    await settle(fixture);

    expect(root(fixture).querySelector('.count')?.textContent).toContain('Showing 201 to 400 of 10,001 entries');
  });

  it('AC-GLA-037 shows an empty state for an account with no transactions', async () => {
    const fixture = await render(page(0, 0));

    expect(root(fixture).textContent).toContain('No transactions for this account or filter.');
    expect(root(fixture).querySelector('table')).toBeNull();
  });

  it('shows a not-found state with a link to search for an unknown account', async () => {
    const fixture = await render(null);
    http.expectOne((r) => r.url === URL).flush({ detail: "Account '1000-200-01' was not found." }, { status: 404, statusText: 'Not Found' });
    await settle(fixture);

    expect(root(fixture).textContent).toContain("Account '1000-200-01' was not found.");
    expect(root(fixture).querySelector('a[href="/gl/accounts"]')).not.toBeNull();
  });

  it('shows an error banner with Try again on a server error', async () => {
    const fixture = await render(null);
    http.expectOne((r) => r.url === URL).flush({}, { status: 500, statusText: 'Server Error' });
    await settle(fixture);
    expect(root(fixture).querySelector('[role="alert"]')).not.toBeNull();

    button(fixture, 'Try again').click();
    http.expectOne((r) => r.url === URL).flush(page(1, 1));
    await settle(fixture);

    expect(root(fixture).querySelectorAll('tbody tr').length).toBe(1);
  });

  it('AC-GLA-034 filters by date range and returns to the first page', async () => {
    const fixture = await render();
    set(fixture, { fromDate: '2026-01-01', toDate: '2026-01-31' });

    button(fixture, 'Apply').click();
    const req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.get('fromDate')).toBe('2026-01-01');
    expect(req.request.params.get('toDate')).toBe('2026-01-31');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.has('fiscalYear')).toBe(false);
    req.flush(page(3, 3));
    await settle(fixture);

    expect(root(fixture).querySelectorAll('tbody tr').length).toBe(3);
  });

  it('AC-GLA-034 filters by fiscal period', async () => {
    const fixture = await render();
    set(fixture, { fiscalYear: 2026, fiscalPeriod: 1 });

    button(fixture, 'Apply').click();
    const req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.get('fiscalYear')).toBe('2026');
    expect(req.request.params.get('fiscalPeriod')).toBe('1');
    expect(req.request.params.has('fromDate')).toBe(false);
    req.flush(page(1, 1));
    await settle(fixture);
  });

  it('keeps the date range and the fiscal period mutually exclusive in the UI', async () => {
    const fixture = await render();
    expect(form(fixture).controls['fromDate'].enabled).toBe(true);
    expect(form(fixture).controls['fiscalYear'].enabled).toBe(true);

    set(fixture, { fromDate: '2026-01-01' });
    expect(form(fixture).controls['fiscalYear'].enabled).toBe(false);
    expect(form(fixture).controls['fiscalPeriod'].enabled).toBe(false);
    expect(form(fixture).controls['toDate'].enabled).toBe(true);

    set(fixture, { fromDate: '' });
    expect(form(fixture).controls['fiscalYear'].enabled).toBe(true);

    set(fixture, { fiscalYear: 2026 });
    expect(form(fixture).controls['fromDate'].enabled).toBe(false);
    expect(form(fixture).controls['toDate'].enabled).toBe(false);
  });

  it('refuses a fiscal period without a fiscal year, with an inline error and no request', async () => {
    const fixture = await render();
    set(fixture, { fiscalPeriod: 3 });

    button(fixture, 'Apply').click();
    await settle(fixture);

    http.expectNone((r) => r.url === URL);
    expect(root(fixture).querySelector('.filter-error')?.getAttribute('role')).toBe('alert');
    expect(root(fixture).querySelector('.filter-error')?.textContent).toContain('fiscal year');
  });

  it('refuses a to date earlier than the from date, with an inline error and no request', async () => {
    const fixture = await render();
    set(fixture, { fromDate: '2026-02-01', toDate: '2026-01-01' });

    button(fixture, 'Apply').click();
    await settle(fixture);

    http.expectNone((r) => r.url === URL);
    expect(root(fixture).querySelector('.filter-error')?.textContent).toContain('cannot be earlier');
  });

  it('shows a 400 from the API as an inline filter error and keeps the earlier results', async () => {
    const fixture = await render();
    set(fixture, { fiscalYear: 2026 });

    button(fixture, 'Apply').click();
    http
      .expectOne((r) => r.url === URL)
      .flush({ errors: { filter: ['Filter by either date range or fiscal period, not both.'] } }, { status: 400, statusText: 'Bad Request' });
    await settle(fixture);

    expect(root(fixture).querySelector('.filter-error')?.textContent).toContain('not both');
    expect(root(fixture).querySelector('app-error-banner')).toBeNull();
  });

  it('Clear filter removes the filter and reloads', async () => {
    const fixture = await render();
    set(fixture, { fiscalYear: 2026 });

    button(fixture, 'Clear filter').click();
    const req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.has('fiscalYear')).toBe(false);
    req.flush(page(50, 10001));
    await settle(fixture);

    expect(form(fixture).controls['fromDate'].enabled).toBe(true);
  });

  it('AC-GLA-035 opens the journal entry detail with its lines when a row is selected', async () => {
    const fixture = await render(page(2, 2));

    button(fixture, 'View journal entry je-000001').click();
    await settle(fixture);
    expect(root(fixture).textContent).toContain('Loading journal entry');
    http.expectOne(`${URL}/je-000001`).flush(DETAIL);
    await settle(fixture);

    const detail = root(fixture).querySelector('.detail')!;
    expect(detail.textContent).toContain('Journal entry je-000001');
    expect(detail.textContent).toContain('fiscal year 2026 period 1');
    const lines = detail.querySelectorAll('tbody tr');
    expect(lines.length).toBe(2);
    expect(lines[0].textContent).toContain('Cash received');
    expect(lines[1].textContent).toContain('Capital contribution');
    expect(button(fixture, 'View journal entry je-000001').getAttribute('aria-expanded')).toBe('true');
  });

  it('AC-GLA-035 shows the error and a retry when the entry cannot be loaded, and Close detail hides it', async () => {
    const fixture = await render(page(2, 2));

    button(fixture, 'View journal entry je-000002').click();
    http.expectOne(`${URL}/je-000002`).flush({ detail: 'Entry not found.' }, { status: 404, statusText: 'Not Found' });
    await settle(fixture);
    expect(root(fixture).querySelector('.detail [role="alert"]')?.textContent).toContain('Entry not found.');

    button(fixture, 'Close detail').click();
    await settle(fixture);
    expect(root(fixture).querySelector('.detail')).toBeNull();
  });

  it('AC-GLA-036 exports every entry that matches the filter, not only the visible page', async () => {
    const fixture = await render();
    const save = vi.spyOn(TestBed.inject(FileSaver), 'save').mockImplementation(() => undefined);
    set(fixture, { fiscalYear: 2026, fiscalPeriod: 1 });
    button(fixture, 'Apply').click();
    http.expectOne((r) => r.url === URL).flush(page(10, 10));
    await settle(fixture);

    button(fixture, 'Export CSV').click();
    const req = http.expectOne((r) => r.url === `${URL}/export`);
    expect(req.request.params.get('fiscalYear')).toBe('2026');
    expect(req.request.params.get('fiscalPeriod')).toBe('1');
    expect(req.request.params.has('page')).toBe(false);
    expect(req.request.params.has('pageSize')).toBe(false);
    await settle(fixture);
    expect(button(fixture, 'Export CSV').disabled).toBe(true);
    req.flush(new Blob(['csv']), { headers: { 'Content-Disposition': 'attachment; filename="transactions-1000-200-01.csv"' } });
    await settle(fixture);

    expect(save).toHaveBeenCalledTimes(1);
    expect(save.mock.calls[0][0].fileName).toBe('transactions-1000-200-01.csv');
    expect(button(fixture, 'Export CSV').disabled).toBe(false);
  });

  it('re-enables Export CSV after a failed export', async () => {
    const fixture = await render();

    button(fixture, 'Export CSV').click();
    http.expectOne((r) => r.url === `${URL}/export`).flush(new Blob(['Export failed.']), { status: 500, statusText: 'Server Error' });
    await settle(fixture);

    expect(button(fixture, 'Export CSV').disabled).toBe(false);
  });
});
