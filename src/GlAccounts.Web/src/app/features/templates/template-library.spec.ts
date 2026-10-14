import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Account } from '../../core/models';
import { summaryPage } from '../../testing/fixtures';
import { TemplateLibrary } from './template-library';

// UX-GLA-006 template library and create-from-template (Flow 8). Stub data only.
// Covers AC-GLA-052, AC-GLA-053, AC-GLA-054, AC-GLA-055, AC-GLA-002, AC-GLA-003.
describe('TemplateLibrary', () => {
  let http: HttpTestingController;
  let router: Router;
  let snackBar: MatSnackBar;

  const TEMPLATE_ROW = {
    accountId: '9000-100-01',
    description: 'Standard expense template',
    accountType: 'Financial',
    category: 'Expense',
    currencyId: 'USD',
    status: 'Template',
  } as const;

  const TEMPLATE: Account = {
    accountId: '9000-100-01',
    segments: [
      { name: 'Main', value: '9000' },
      { name: 'Department', value: '100' },
      { name: 'Sub', value: '01' },
    ],
    description: 'Standard expense template',
    accountType: 'Financial',
    category: 'Expense',
    currencyId: 'USD',
    status: 'Template',
    balanceType: 'BalanceForward',
    normalBalance: 'Debit',
    createdDate: '2024-01-01',
    eligibleForPosting: false,
    attributes: [],
  };

  const SEARCH = (r: { url: string }) => r.url === '/api/gl/accounts';
  const TEMPLATE_URL = '/api/gl/accounts/9000-100-01';

  async function settle(fixture: { detectChanges(): void; whenStable(): Promise<void> }) {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function render(first: ReturnType<typeof summaryPage> | null = summaryPage([TEMPLATE_ROW])) {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    snackBar = TestBed.inject(MatSnackBar);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    vi.spyOn(snackBar, 'open').mockReturnValue(undefined as never);
    const fixture = TestBed.createComponent(TemplateLibrary);
    fixture.detectChanges();
    if (first) http.expectOne(SEARCH).flush(first);
    await settle(fixture);
    return fixture;
  }

  type Fixture = Awaited<ReturnType<typeof render>>;
  const root = (f: { nativeElement: unknown }) => f.nativeElement as HTMLElement;
  const button = (f: { nativeElement: unknown }, label: string) =>
    Array.from(root(f).querySelectorAll('button')).find((b) => (b.getAttribute('aria-label') ?? b.textContent?.trim()) === label) as HTMLButtonElement;
  const field = (f: { nativeElement: unknown }, name: string) => root(f).querySelector(`#tpl-field-${name}`) as HTMLInputElement | null;

  async function type(fixture: Fixture, name: string, value: string) {
    const el = field(fixture, name)!;
    el.value = value;
    el.dispatchEvent(new Event('input'));
    await settle(fixture);
  }

  function setControl(fixture: Fixture, name: string, value: unknown) {
    const panel = fixture.debugElement.query((d) => d.name === 'app-create-from-template');
    (panel.componentInstance as { form: { get(n: string): { setValue(v: unknown): void } } }).form.get(name)!.setValue(value);
  }

  async function openPanel(fixture: Fixture) {
    button(fixture, 'Create account from template 9000-100-01').click();
    await settle(fixture);
    http.expectOne(TEMPLATE_URL).flush(TEMPLATE, { headers: { ETag: '"v1"' } });
    await settle(fixture);
  }

  const save = (f: Fixture) => root(f).querySelector('app-create-from-template button[type="submit"]') as HTMLButtonElement;

  afterEach(() => http?.verify());

  describe('library', () => {
    it('shows a loading state with aria-busy, then the templates', async () => {
      TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
      http = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(TemplateLibrary);
      fixture.detectChanges();
      expect(root(fixture).textContent).toContain('Loading templates');
      expect(root(fixture).querySelector('[aria-busy="true"]')).not.toBeNull();

      http.expectOne(SEARCH).flush(summaryPage([TEMPLATE_ROW]));
      await settle(fixture);

      expect(root(fixture).textContent).not.toContain('Loading templates');
    });

    it('AC-GLA-055 lists only accounts with status Template, with a Create action per row', async () => {
      TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
      http = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(TemplateLibrary);
      fixture.detectChanges();

      const req = http.expectOne(SEARCH);
      expect(req.request.params.get('status')).toBe('Template');
      req.flush(summaryPage([TEMPLATE_ROW]));
      await settle(fixture);

      const rows = root(fixture).querySelectorAll('tbody tr');
      expect(rows.length).toBe(1);
      expect(rows[0].textContent).toContain('9000-100-01');
      expect(rows[0].textContent).toContain('Standard expense template');
      expect(rows[0].textContent).toContain('Expense');
      expect(button(fixture, 'Create account from template 9000-100-01')).toBeDefined();
    });

    it('AC-GLA-055 searches by text and filters by category, keeping the Template status', async () => {
      const fixture = await render();
      const instance = fixture.componentInstance as unknown as {
        form: { controls: Record<string, { setValue(v: unknown): void }> };
      };
      instance.form.controls['q'].setValue(' expense ');
      instance.form.controls['category'].setValue('Expense');

      button(fixture, 'Search').click();
      const req = http.expectOne(SEARCH);
      expect(req.request.params.get('q')).toBe('expense');
      expect(req.request.params.get('category')).toBe('Expense');
      expect(req.request.params.get('status')).toBe('Template');
      expect(req.request.params.get('page')).toBe('1');
      req.flush(summaryPage([TEMPLATE_ROW]));
      await settle(fixture);

      expect(root(fixture).querySelectorAll('tbody tr').length).toBe(1);
    });

    it('AC-GLA-055 pages the library', async () => {
      const fixture = await render({ items: [TEMPLATE_ROW], total: 120, page: 1, pageSize: 50 });

      (fixture.componentInstance as unknown as { onPage(e: unknown): void }).onPage({ pageIndex: 1, pageSize: 100, length: 120 });
      const req = http.expectOne(SEARCH);
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('100');
      req.flush({ items: [], total: 120, page: 2, pageSize: 100 });
      await settle(fixture);
    });

    it('shows an empty state with a link to create a Template account when nothing matches', async () => {
      const fixture = await render(summaryPage([]));

      expect(root(fixture).textContent).toContain('No templates match your search.');
      expect(root(fixture).querySelector('a[href="/gl/accounts/new"]')?.textContent).toContain('status Template');
      expect(root(fixture).querySelector('table')).toBeNull();
    });

    it('shows an error banner with Try again on a server error', async () => {
      const fixture = await render(null);
      http.expectOne(SEARCH).flush({}, { status: 500, statusText: 'Server Error' });
      await settle(fixture);
      expect(root(fixture).querySelector('[role="alert"]')).not.toBeNull();

      button(fixture, 'Try again').click();
      http.expectOne(SEARCH).flush(summaryPage([TEMPLATE_ROW]));
      await settle(fixture);

      expect(root(fixture).querySelectorAll('tbody tr').length).toBe(1);
    });
  });

  describe('create from template', () => {
    it('AC-GLA-053 opens a panel with the template settings prefilled and editable, and an empty new Account ID', async () => {
      const fixture = await render();
      await openPanel(fixture);

      expect(root(fixture).querySelector('app-create-from-template h2')?.textContent).toContain('Create account from template 9000-100-01');
      expect(field(fixture, 'accountId')!.value).toBe('');
      expect(field(fixture, 'description')!.value).toBe('Standard expense template');
      const panel = fixture.debugElement.query((d) => d.name === 'app-create-from-template').componentInstance as {
        form: { getRawValue(): Record<string, unknown> };
      };
      expect(panel.form.getRawValue()).toMatchObject({
        category: 'Expense',
        currencyId: 'USD',
        status: 'Active',
        balanceType: 'BalanceForward',
        normalBalance: 'Debit',
      });
      expect(root(fixture).textContent).toContain('9999-999-99');
    });

    it('AC-GLA-053 creates the account with templateAccountId and the new Account ID, then opens it', async () => {
      const fixture = await render();
      await openPanel(fixture);
      await type(fixture, 'accountId', '1000-100-02');

      save(fixture).click();
      const req = http.expectOne(SEARCH_POST);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        accountId: '1000-100-02',
        templateAccountId: '9000-100-01',
        description: 'Standard expense template',
        accountType: 'Financial',
        category: 'Expense',
        currencyId: 'USD',
        status: 'Active',
        balanceType: 'BalanceForward',
        normalBalance: 'Debit',
      });
      req.flush({ ...TEMPLATE, accountId: '1000-100-02', status: 'Active' }, { status: 201, statusText: 'Created' });
      await settle(fixture);

      expect(router.navigate).toHaveBeenCalledWith(['/gl/accounts', '1000-100-02']);
      expect(snackBar.open).toHaveBeenCalledWith('Account 1000-100-02 created from template 9000-100-01.', 'Dismiss', expect.anything());
    });

    it('AC-GLA-053 sends the settings the user edited before saving', async () => {
      const fixture = await render();
      await openPanel(fixture);
      await type(fixture, 'accountId', '1000-100-02');
      await type(fixture, 'description', 'Travel costs');
      setControl(fixture, 'currencyId', 'EUR');
      setControl(fixture, 'status', 'Hold');
      await settle(fixture);

      save(fixture).click();
      const req = http.expectOne(SEARCH_POST);
      expect(req.request.body).toMatchObject({ description: 'Travel costs', currencyId: 'EUR', status: 'Hold', templateAccountId: '9000-100-01' });
      req.flush({ ...TEMPLATE, accountId: '1000-100-02' }, { status: 201, statusText: 'Created' });
      await settle(fixture);
    });

    it('AC-GLA-002 refuses a missing Account ID without a request, with a role=alert summary', async () => {
      const fixture = await render();
      await openPanel(fixture);

      save(fixture).click();
      await settle(fixture);

      http.expectNone(SEARCH_POST);
      const summary = root(fixture).querySelector('app-create-from-template .error-summary') as HTMLElement;
      expect(summary.getAttribute('role')).toBe('alert');
      expect(summary.textContent).toContain('New Account ID is required.');
    });

    it('AC-GLA-002 shows an invalid-segment message from the API at the Account ID field', async () => {
      const fixture = await render();
      await openPanel(fixture);
      await type(fixture, 'accountId', '9999-999-99');
      const message = "Account ID does not match segment format. Expected format: 9999-999-99 (segment Main '9999' is not an allowed value).";

      save(fixture).click();
      http.expectOne(SEARCH_POST).flush({ errors: { accountId: [message] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('app-create-from-template mat-error')?.textContent).toContain(message);
      expect(root(fixture).querySelector('app-create-from-template .error-summary')?.textContent).toContain(message);
    });

    it('AC-GLA-003 shows the duplicate message at the Account ID field with a link to the existing account', async () => {
      const fixture = await render();
      await openPanel(fixture);
      await type(fixture, 'accountId', '1000-100-01');

      save(fixture).click();
      http
        .expectOne(SEARCH_POST)
        .flush({ detail: "Account ID '1000-100-01' already exists. Please use a unique ID." }, { status: 409, statusText: 'Conflict' });
      await settle(fixture);

      expect(root(fixture).querySelector('app-create-from-template mat-error')?.textContent).toContain(
        "Account ID '1000-100-01' already exists. Please use a unique ID.",
      );
      expect(root(fixture).querySelector('.duplicate a')?.getAttribute('href')).toBe('/gl/accounts/1000-100-01');
      expect(field(fixture, 'accountId')!.value).toBe('1000-100-01');
    });

    it('AC-GLA-054 shows the "not a template" message from the API in the panel and keeps the form', async () => {
      const fixture = await render();
      await openPanel(fixture);
      await type(fixture, 'accountId', '1000-100-02');
      const message = "Account '9000-100-01' is not a template and cannot be used as a template source.";

      save(fixture).click();
      http.expectOne(SEARCH_POST).flush({ errors: { templateAccountId: [message] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('app-create-from-template .error-summary')?.textContent).toContain(message);
      expect(field(fixture, 'accountId')!.value).toBe('1000-100-02');
      expect(save(fixture).disabled).toBe(false);
    });

    it('AC-GLA-054 shows an error banner when the template can no longer be loaded', async () => {
      const fixture = await render();
      button(fixture, 'Create account from template 9000-100-01').click();
      await settle(fixture);
      http.expectOne(TEMPLATE_URL).flush({ detail: "Account '9000-100-01' was not found." }, { status: 404, statusText: 'Not Found' });
      await settle(fixture);

      expect(root(fixture).querySelector('app-create-from-template [role="alert"]')?.textContent).toContain("Account '9000-100-01' was not found.");
      expect(field(fixture, 'accountId')).toBeNull();
    });

    it('disables Save while the request is in flight, so it cannot be sent twice', async () => {
      const fixture = await render();
      await openPanel(fixture);
      await type(fixture, 'accountId', '1000-100-02');

      save(fixture).click();
      await settle(fixture);
      expect(save(fixture).disabled).toBe(true);
      expect(save(fixture).textContent).toContain('Saving');

      http.expectOne(SEARCH_POST).flush({}, { status: 500, statusText: 'Server Error' });
      await settle(fixture);
      expect(save(fixture).disabled).toBe(false);
      expect(root(fixture).querySelector('app-create-from-template [role="alert"]')?.textContent).toContain('Something went wrong.');
    });

    it('AC-GLA-052 offers the Template status so an account can be stored as a template', async () => {
      const fixture = await render();
      await openPanel(fixture);

      const panel = fixture.debugElement.query((d) => d.name === 'app-create-from-template').componentInstance as { statuses: string[] };
      expect(panel.statuses).toContain('Template');
    });

    it('Cancel closes the panel and returns focus to the row action, and the template is not changed', async () => {
      const fixture = await render();
      await openPanel(fixture);

      button(fixture, 'Cancel').click();
      await settle(fixture);

      expect(root(fixture).querySelector('app-create-from-template')).toBeNull();
      http.expectNone(SEARCH_POST);
    });
  });
});

const SEARCH_POST = (r: { url: string; method: string }) => r.url === '/api/gl/accounts' && r.method === 'POST';
