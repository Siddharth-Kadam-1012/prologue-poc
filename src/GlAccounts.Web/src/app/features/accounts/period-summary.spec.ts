import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FileSaver } from '../../core/file-saver';
import { PeriodSummary } from '../../core/models';
import { PeriodSummaryPage } from './period-summary';

// UX-GLA-005 period summary (Flow 6). Stub data only.
// Covers AC-GLA-028, AC-GLA-029, AC-GLA-030, AC-GLA-031, AC-GLA-032.
describe('PeriodSummaryPage', () => {
  let http: HttpTestingController;
  const URL = '/api/gl/accounts/1000-100-01/period-summary';

  const SUMMARY: PeriodSummary = {
    accountId: '1000-100-01',
    periods: [
      { fiscalYear: 2025, fiscalPeriod: 1, balance: 12500 },
      { fiscalYear: 2025, fiscalPeriod: 2, balance: 13250.5 },
    ],
    years: [{ fiscalYear: 2025, balance: 13250.5 }],
    budgetVersions: [{ budgetVersion: 'Original', fiscalYear: 2025, amount: 150000 }],
  };

  async function settle(fixture: { detectChanges(): void; whenStable(): Promise<void> }) {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function render(body: PeriodSummary | null = SUMMARY) {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(PeriodSummaryPage);
    fixture.componentRef.setInput('id', '1000-100-01');
    fixture.detectChanges();
    if (body) http.expectOne(URL).flush(body);
    await settle(fixture);
    return fixture;
  }

  const root = (f: { nativeElement: unknown }) => f.nativeElement as HTMLElement;
  const header = (f: { nativeElement: unknown }, label: string) =>
    Array.from(root(f).querySelectorAll('th')).find((th) => th.textContent?.includes(label)) as HTMLElement;
  const exportButton = (f: { nativeElement: unknown }) =>
    Array.from(root(f).querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Export Excel') as HTMLButtonElement;

  afterEach(() => http?.verify());

  it('shows a loading state, then the populated state', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(PeriodSummaryPage);
    fixture.componentRef.setInput('id', '1000-100-01');
    fixture.detectChanges();
    expect(root(fixture).textContent).toContain('Loading period summary');
    expect(root(fixture).querySelector('[aria-busy="true"]')).not.toBeNull();

    http.expectOne(URL).flush(SUMMARY);
    await settle(fixture);

    expect(root(fixture).textContent).not.toContain('Loading period summary');
  });

  it('AC-GLA-028 shows one balance row per fiscal period', async () => {
    const fixture = await render();

    const rows = root(fixture).querySelectorAll('table[aria-labelledby="periods-heading"] tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('2025');
    expect(rows[0].textContent).toContain('12,500.00');
    expect(rows[1].textContent).toContain('13,250.50');
  });

  it('AC-GLA-029 shows balances by fiscal year and the budget versions', async () => {
    const fixture = await render();

    const years = root(fixture).querySelector('table[aria-labelledby="years-heading"]')!;
    expect(years.textContent).toContain('13,250.50');
    const budgets = root(fixture).querySelector('table[aria-labelledby="budgets-heading"]')!;
    expect(budgets.textContent).toContain('Original');
    expect(budgets.textContent).toContain('150,000.00');
  });

  it('shows an empty state for a budgets-less summary', async () => {
    const fixture = await render({ ...SUMMARY, budgetVersions: [] });

    expect(root(fixture).textContent).toContain('No budget versions for this account.');
    expect(root(fixture).querySelector('table[aria-labelledby="periods-heading"]')).not.toBeNull();
  });

  it('AC-GLA-032 shows "No balances for this account." for an empty 200 result', async () => {
    const fixture = await render({ accountId: '8000-100-01', periods: [], years: [], budgetVersions: [] });

    expect(root(fixture).textContent).toContain('No balances for this account.');
    expect(root(fixture).querySelector('table')).toBeNull();
  });

  it('AC-GLA-032 shows a not-found state with a link to search for an unknown account', async () => {
    const fixture = await render(null);
    http.expectOne(URL).flush({ detail: "Account '1000-100-01' was not found." }, { status: 404, statusText: 'Not Found' });
    await settle(fixture);

    expect(root(fixture).textContent).toContain("Account '1000-100-01' was not found.");
    expect(root(fixture).querySelector('a[href="/gl/accounts"]')).not.toBeNull();
  });

  it('shows an error banner with Try again on a server error', async () => {
    const fixture = await render(null);
    http.expectOne(URL).flush({}, { status: 500, statusText: 'Server Error' });
    await settle(fixture);
    expect(root(fixture).querySelector('[role="alert"]')).not.toBeNull();

    (Array.from(root(fixture).querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Try again') as HTMLButtonElement).click();
    http.expectOne(URL).flush(SUMMARY);
    await settle(fixture);

    expect(root(fixture).querySelector('table')).not.toBeNull();
  });

  it('AC-GLA-030 filters by fiscal year', async () => {
    const fixture = await render();
    (fixture.componentInstance as unknown as { form: { controls: { fiscalYear: { setValue(v: number): void } } } }).form.controls.fiscalYear.setValue(2025);

    (root(fixture).querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    const req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.get('fiscalYear')).toBe('2025');
    req.flush({ ...SUMMARY, periods: [SUMMARY.periods[0]] });
    await settle(fixture);

    expect(root(fixture).querySelectorAll('table[aria-labelledby="periods-heading"] tbody tr').length).toBe(1);
  });

  it('AC-GLA-030 sorts by a column, toggles the direction, and exposes it with aria-sort', async () => {
    const fixture = await render();
    expect(header(fixture, 'Balance').getAttribute('aria-sort')).toBe('none');

    (header(fixture, 'Balance').querySelector('button') as HTMLButtonElement).click();
    let req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.get('sortBy')).toBe('balance');
    expect(req.request.params.get('sortDirection')).toBe('asc');
    req.flush(SUMMARY);
    await settle(fixture);
    expect(header(fixture, 'Balance').getAttribute('aria-sort')).toBe('ascending');
    expect(header(fixture, 'Fiscal year').getAttribute('aria-sort')).toBe('none');

    (header(fixture, 'Balance').querySelector('button') as HTMLButtonElement).click();
    req = http.expectOne((r) => r.url === URL);
    expect(req.request.params.get('sortDirection')).toBe('desc');
    req.flush({ ...SUMMARY, periods: [...SUMMARY.periods].reverse() });
    await settle(fixture);
    expect(header(fixture, 'Balance').getAttribute('aria-sort')).toBe('descending');
  });

  it('AC-GLA-031 exports to Excel with the same filter and sort as the view', async () => {
    const fixture = await render();
    const save = vi.spyOn(TestBed.inject(FileSaver), 'save').mockImplementation(() => undefined);
    (fixture.componentInstance as unknown as { form: { controls: { fiscalYear: { setValue(v: number): void } } } }).form.controls.fiscalYear.setValue(2025);
    (header(fixture, 'Fiscal period').querySelector('button') as HTMLButtonElement).click();
    http.expectOne((r) => r.url === URL).flush(SUMMARY);
    await settle(fixture);

    exportButton(fixture).click();
    const req = http.expectOne((r) => r.url === `${URL}/export`);
    expect(req.request.params.get('fiscalYear')).toBe('2025');
    expect(req.request.params.get('sortBy')).toBe('fiscalPeriod');
    expect(req.request.params.get('sortDirection')).toBe('asc');
    await settle(fixture);
    expect(exportButton(fixture).disabled).toBe(true);
    req.flush(new Blob(['xlsx']), { headers: { 'Content-Disposition': 'attachment; filename="period-summary-1000-100-01.xlsx"' } });
    await settle(fixture);

    expect(save).toHaveBeenCalledTimes(1);
    expect(save.mock.calls[0][0].fileName).toBe('period-summary-1000-100-01.xlsx');
    expect(exportButton(fixture).disabled).toBe(false);
  });

  it('re-enables Export Excel after a failed export', async () => {
    const fixture = await render();
    exportButton(fixture).click();
    http.expectOne((r) => r.url === `${URL}/export`).flush(new Blob(['Export failed.']), { status: 500, statusText: 'Server Error' });
    await settle(fixture);

    expect(exportButton(fixture).disabled).toBe(false);
  });
});
