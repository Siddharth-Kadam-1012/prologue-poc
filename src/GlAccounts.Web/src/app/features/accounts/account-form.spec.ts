import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AccountForm } from './account-form';
import { ACTIVE_ACCOUNT } from '../../testing/fixtures';

// UX-GLA-001 account create / edit form. Stub data only.
// Covers AC-GLA-001, AC-GLA-002, AC-GLA-003, AC-GLA-006, AC-GLA-007, AC-GLA-008, AC-GLA-009,
// AC-GLA-011, AC-GLA-012, AC-GLA-013, AC-GLA-017, AC-GLA-018, AC-GLA-022, AC-GLA-023,
// AC-GLA-043, AC-GLA-044, AC-GLA-045, AC-GLA-056, AC-GLA-057.
describe('AccountForm', () => {
  let http: HttpTestingController;
  let router: Router;
  let snackBar: MatSnackBar;

  async function settle(fixture: { detectChanges(): void; whenStable(): Promise<void> }) {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function render(inputs: { id?: string; status?: string } = {}) {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    snackBar = TestBed.inject(MatSnackBar);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    vi.spyOn(snackBar, 'open').mockReturnValue(undefined as never);
    const fixture = TestBed.createComponent(AccountForm);
    if (inputs.id) fixture.componentRef.setInput('id', inputs.id);
    if (inputs.status) fixture.componentRef.setInput('status', inputs.status);
    fixture.detectChanges();
    if (inputs.id) {
      http
        .expectOne(`/api/gl/accounts/${inputs.id}`)
        .flush(ACTIVE_ACCOUNT, { headers: { ETag: '"v1"' } });
    }
    await settle(fixture);
    return fixture;
  }

  const root = (f: { nativeElement: unknown }) => f.nativeElement as HTMLElement;
  const input = (f: { nativeElement: unknown }, id: string) => root(f).querySelector(`#field-${id}`) as HTMLInputElement | null;
  const submitButton = (f: { nativeElement: unknown }) => root(f).querySelector('button[type="submit"]') as HTMLButtonElement;
  const button = (f: { nativeElement: unknown }, label: string) =>
    Array.from(root(f).querySelectorAll('button')).find((b) => b.textContent?.trim() === label) as HTMLButtonElement;
  // Attribute changes are applied at once by default on a saved account; these tests exercise the staged mode.
  const stagedMode = (f: { componentInstance: AccountForm }) =>
    (f.componentInstance as unknown as { immediate: { set(v: boolean): void } }).immediate.set(false);

  async function type(fixture: { detectChanges(): void; whenStable(): Promise<void>; nativeElement: unknown }, id: string, value: string) {
    const el = input(fixture, id)!;
    el.value = value;
    el.dispatchEvent(new Event('input'));
    await settle(fixture);
  }

  // The form's controls are driven through the component so Material selects need no overlay in the test.
  function setControl(fixture: { componentInstance: AccountForm }, name: string, value: unknown) {
    (fixture.componentInstance as unknown as { form: { get(n: string): { setValue(v: unknown): void } } }).form
      .get(name)!
      .setValue(value);
  }

  async function fillValidCreate(fixture: Awaited<ReturnType<typeof render>>) {
    await type(fixture, 'accountId', '1000-100-02');
    await type(fixture, 'description', 'Petty cash');
    setControl(fixture, 'category', 'Asset');
    setControl(fixture, 'balanceType', 'Periodic');
    setControl(fixture, 'normalBalance', 'Debit');
    await settle(fixture);
  }

  afterEach(() => http?.verify());

  describe('create', () => {
    it('shows the static 4-3-2 Account ID format hint', async () => {
      const fixture = await render();

      expect(root(fixture).querySelector('h1')?.textContent).toContain('New account');
      expect(root(fixture).textContent).toContain('9999-999-99');
      expect(root(fixture).querySelector('#field-balanceType')).not.toBeNull();
    });

    it('Enter in a text input does not submit the form (user-flows keyboard path); Enter in a textarea is left alone', async () => {
      const fixture = await render();
      await type(fixture, 'accountId', '1000-100-01');

      const textInput = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
      input(fixture, 'accountId')!.dispatchEvent(textInput);
      expect(textInput.defaultPrevented).toBe(true);

      const textarea = document.createElement('textarea');
      root(fixture).querySelector('form')!.appendChild(textarea);
      const area = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
      textarea.dispatchEvent(area);
      expect(area.defaultPrevented).toBe(false);
      http.expectNone('/api/gl/accounts');
    });

    it('AC-GLA-043 refuses an empty form without calling the API, with a role=alert summary and a message per field', async () => {
      const fixture = await render();

      submitButton(fixture).click();
      await settle(fixture);

      http.expectNone('/api/gl/accounts');
      const summary = root(fixture).querySelector('.error-summary') as HTMLElement;
      expect(summary.getAttribute('role')).toBe('alert');
      const text = summary.textContent!;
      expect(text).toContain('Account ID is required.');
      expect(text).toContain('Description is required.');
      expect(text).toContain('Category is required.');
      expect(text).toContain('Balance type is required.');
      expect(text).toContain('Normal balance is required.');
    });

    it('links each field error to its field with aria-describedby', async () => {
      const fixture = await render();

      submitButton(fixture).click();
      await settle(fixture);

      const field = input(fixture, 'accountId')!;
      const describedBy = field.getAttribute('aria-describedby')!;
      expect(describedBy.length).toBeGreaterThan(0);
      const referenced = describedBy.split(' ').map((id) => root(fixture).querySelector(`#${id}`)?.textContent ?? '');
      expect(referenced.join(' ')).toContain('Account ID is required.');
    });

    it('AC-GLA-001 creates a Financial account and goes to its inquiry page', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);

      submitButton(fixture).click();
      const req = http.expectOne('/api/gl/accounts');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        accountId: '1000-100-02',
        description: 'Petty cash',
        accountType: 'Financial',
        category: 'Asset',
        currencyId: 'USD',
        status: 'Active',
        balanceType: 'Periodic',
        normalBalance: 'Debit',
      });
      req.flush({ ...ACTIVE_ACCOUNT, accountId: '1000-100-02' }, { status: 201, statusText: 'Created' });
      await settle(fixture);

      expect(router.navigate).toHaveBeenCalledWith(['/gl/accounts', '1000-100-02']);
      expect(snackBar.open).toHaveBeenCalledWith('Account 1000-100-02 created.', 'Dismiss', expect.anything());
    });

    it('disables Save while the request is in flight, so it cannot be sent twice', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);

      submitButton(fixture).click();
      await settle(fixture);

      expect(submitButton(fixture).disabled).toBe(true);
      expect(submitButton(fixture).textContent).toContain('Saving');
      submitButton(fixture).click();
      http.expectOne('/api/gl/accounts').flush({ errors: {} }, { status: 500, statusText: 'Server Error' });
      await settle(fixture);
      expect(submitButton(fixture).disabled).toBe(false);
    });

    it('AC-GLA-002 AC-GLA-044 shows the invalid-segment message at the Account ID field and in the summary', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);
      await type(fixture, 'accountId', '9999-999-99');
      const message = 'Account ID does not match segment format. Expected format: 9999-999-99 (segment Main \'9999\' is not an allowed value).';

      submitButton(fixture).click();
      http.expectOne('/api/gl/accounts').flush({ errors: { accountId: [message] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('mat-error')?.textContent).toContain(message);
      expect(root(fixture).querySelector('.error-summary')?.textContent).toContain(message);
      expect(input(fixture, 'accountId')!.getAttribute('aria-invalid')).toBe('true');
    });

    it('AC-GLA-003 shows the duplicate message at the Account ID field with a link to the existing account', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);
      await type(fixture, 'accountId', '1000-100-01');

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts')
        .flush({ detail: "Account ID '1000-100-01' already exists. Please use a unique ID." }, { status: 409, statusText: 'Conflict' });
      await settle(fixture);

      expect(root(fixture).querySelector('mat-error')?.textContent).toContain("Account ID '1000-100-01' already exists. Please use a unique ID.");
      const link = root(fixture).querySelector('.duplicate a') as HTMLAnchorElement;
      expect(link.getAttribute('href')).toBe('/gl/accounts/1000-100-01');
      expect(root(fixture).querySelector('.error-summary')).not.toBeNull();
    });

    it('AC-GLA-011 shows balance type and normal balance for Financial accounts only', async () => {
      const fixture = await render();
      expect(root(fixture).querySelector('#field-balanceType')).not.toBeNull();
      expect(root(fixture).querySelector('#field-normalBalance')).not.toBeNull();

      setControl(fixture, 'accountType', 'Statistical');
      await settle(fixture);

      expect(root(fixture).querySelector('#field-balanceType')).toBeNull();
      expect(root(fixture).querySelector('#field-normalBalance')).toBeNull();
    });

    it('AC-GLA-012 AC-GLA-057 offers only the allowed categories and omits balances for a Statistical account', async () => {
      const fixture = await render();
      const options = () => (fixture.componentInstance as unknown as { categoryOptions(): string[] }).categoryOptions();
      expect(options()).toEqual(['Asset', 'Liability', 'Equity', 'Revenue', 'Expense']);

      setControl(fixture, 'accountType', 'Statistical');
      await settle(fixture);
      expect(options()).toEqual(['Statistical']);
      await type(fixture, 'accountId', '7000-100-02');
      await type(fixture, 'description', 'Headcount');

      submitButton(fixture).click();
      const req = http.expectOne('/api/gl/accounts');
      expect(req.request.body.category).toBe('Statistical');
      expect(req.request.body.balanceType).toBeUndefined();
      expect(req.request.body.normalBalance).toBeUndefined();
      req.flush({ ...ACTIVE_ACCOUNT, accountId: '7000-100-02' }, { status: 201, statusText: 'Created' });
      await settle(fixture);
    });

    it('AC-GLA-013 shows a server field error for a balance type outside the allowed values', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts')
        .flush({ errors: { balanceType: ["Balance type 'X' is not allowed."] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('.error-summary')?.textContent).toContain("Balance type 'X' is not allowed.");
    });

    it('AC-GLA-009 shows the field-level errors for an invalid currency', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts')
        .flush({ errors: { currencyId: ["Currency ID 'XYZ' is invalid. Allowed currencies: USD, EUR, GBP, JPY."] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('mat-error')?.textContent).toContain('Allowed currencies: USD, EUR, GBP, JPY.');
    });

    it('AC-GLA-017 sends attributes added in the panel with the create call', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);
      const panel = fixture.debugElement.nativeElement.querySelector('app-attribute-panel') as HTMLElement;
      (Array.from(panel.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Add attribute') as HTMLButtonElement).click();
      await settle(fixture);
      const selects = panel.querySelectorAll('select');
      (selects[0] as HTMLSelectElement).value = 'region';
      selects[0].dispatchEvent(new Event('change'));
      await settle(fixture);
      const value = panel.querySelectorAll('select')[1] as HTMLSelectElement;
      value.value = 'emea';
      value.dispatchEvent(new Event('change'));
      await settle(fixture);

      submitButton(fixture).click();
      const req = http.expectOne('/api/gl/accounts');
      expect(req.request.body.attributes).toEqual([{ attributeTypeId: 'region', attributeValueId: 'emea' }]);
      req.flush({ ...ACTIVE_ACCOUNT, accountId: '1000-100-02' }, { status: 201, statusText: 'Created' });
      await settle(fixture);
    });

    it('AC-GLA-018 keeps every staged row and shows the row error when the create is rejected', async () => {
      const fixture = await render();
      await fillValidCreate(fixture);
      const panel = fixture.debugElement.nativeElement.querySelector('app-attribute-panel') as HTMLElement;
      (Array.from(panel.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Add attribute') as HTMLButtonElement).click();
      await settle(fixture);
      const type0 = panel.querySelectorAll('select')[0] as HTMLSelectElement;
      type0.value = 'dept';
      type0.dispatchEvent(new Event('change'));
      await settle(fixture);
      const value0 = panel.querySelectorAll('select')[1] as HTMLSelectElement;
      value0.value = 'sales';
      value0.dispatchEvent(new Event('change'));
      await settle(fixture);

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts')
        .flush(
          { errors: { 'attributes[0].attributeValueId': ['Attribute type and value combination is not valid in the attribute catalog.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await settle(fixture);

      expect(panel.querySelector('.row-error')?.textContent).toContain('not valid in the attribute catalog');
      expect(panel.querySelectorAll('tbody tr').length).toBe(1);
    });
  });

  describe('edit', () => {
    it('loads the account with Account ID and Account Type read-only', async () => {
      const fixture = await render({ id: '1000-100-01' });

      expect(root(fixture).querySelector('h1')?.textContent).toContain('Edit account 1000-100-01');
      expect(input(fixture, 'accountId')).toBeNull();
      expect(input(fixture, 'accountType')).toBeNull();
      const readOnly = root(fixture).textContent!;
      expect(readOnly).toContain('1000-100-01');
      expect(readOnly).toContain('Financial');
      expect(input(fixture, 'description')!.value).toBe('Cash and equivalents');
    });

    it('shows loading, then a not-found state with a link to search for a 404', async () => {
      TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
      http = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(AccountForm);
      fixture.componentRef.setInput('id', '0000-000-00');
      fixture.detectChanges();
      expect(root(fixture).textContent).toContain('Loading account');

      http.expectOne('/api/gl/accounts/0000-000-00').flush({ detail: 'Account not found.' }, { status: 404, statusText: 'Not Found' });
      await settle(fixture);

      expect(root(fixture).textContent).toContain('Account not found');
      expect(root(fixture).querySelector('a[href="/gl/accounts"]')).not.toBeNull();
    });

    it('shows an error banner with Try again when the account cannot be loaded', async () => {
      TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
      http = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(AccountForm);
      fixture.componentRef.setInput('id', '1000-100-01');
      fixture.detectChanges();

      http.expectOne('/api/gl/accounts/1000-100-01').flush({}, { status: 500, statusText: 'Server Error' });
      await settle(fixture);

      expect(root(fixture).querySelector('[role="alert"]')).not.toBeNull();
      button(fixture, 'Try again').click();
      http.expectOne('/api/gl/accounts/1000-100-01').flush(ACTIVE_ACCOUNT, { headers: { ETag: '"v1"' } });
      await settle(fixture);
      expect(input(fixture, 'description')).not.toBeNull();
    });

    it('AC-GLA-006 AC-GLA-007 saves a changed description, category and currency with the stored ETag as If-Match', async () => {
      const fixture = await render({ id: '1000-100-01' });
      await type(fixture, 'description', 'Operating cash');
      setControl(fixture, 'category', 'Liability');
      setControl(fixture, 'currencyId', 'EUR');
      await settle(fixture);

      submitButton(fixture).click();
      const req = http.expectOne('/api/gl/accounts/1000-100-01');
      expect(req.request.method).toBe('PUT');
      expect(req.request.headers.get('If-Match')).toBe('"v1"');
      expect(req.request.body).toEqual({
        accountId: '1000-100-01',
        accountType: 'Financial',
        description: 'Operating cash',
        category: 'Liability',
        currencyId: 'EUR',
        status: 'Active',
      });
      req.flush({ ...ACTIVE_ACCOUNT, description: 'Operating cash' }, { headers: { ETag: '"v2"' } });
      await settle(fixture);

      expect(router.navigate).toHaveBeenCalledWith(['/gl/accounts', '1000-100-01']);
      expect(snackBar.open).toHaveBeenCalledWith('Account 1000-100-01 updated.', 'Dismiss', expect.anything());
    });

    it('AC-GLA-008 AC-GLA-045 echoes the stored Account ID and Account Type, never an edited one', async () => {
      const fixture = await render({ id: '1000-100-01' });
      const form = (fixture.componentInstance as unknown as { form: { getRawValue(): Record<string, unknown>; controls: Record<string, { disabled: boolean }> } }).form;

      expect(form.controls['accountId'].disabled).toBe(true);
      expect(form.controls['accountType'].disabled).toBe(true);
      expect(form.getRawValue()['accountId']).toBe('1000-100-01');
      expect(form.getRawValue()['accountType']).toBe('Financial');
    });

    it('AC-GLA-008 shows the permanent-field message from the API', async () => {
      const fixture = await render({ id: '1000-100-01' });

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts/1000-100-01')
        .flush({ errors: { accountType: ['Account Type is permanent and cannot be modified.'] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('[role="alert"]')?.textContent).toContain('Account Type is permanent and cannot be modified.');
    });

    it('does not offer Template, and a changed status reveals a required justification (AC-GLA-022, AC-GLA-023)', async () => {
      const fixture = await render({ id: '1000-100-01' });
      const options = (fixture.componentInstance as unknown as { statusOptions(): string[] }).statusOptions();
      expect(options).toEqual(['Active', 'Hold', 'Inactive']);
      expect(input(fixture, 'statusJustification')).toBeNull();

      setControl(fixture, 'status', 'Hold');
      await settle(fixture);
      expect(input(fixture, 'statusJustification')).not.toBeNull();

      submitButton(fixture).click();
      await settle(fixture);
      http.expectNone('/api/gl/accounts/1000-100-01');
      expect(root(fixture).querySelector('.error-summary')?.textContent).toContain('Justification is required.');
    });

    it('AC-GLA-022 sends the status and its justification', async () => {
      const fixture = await render({ id: '1000-100-01' });
      setControl(fixture, 'status', 'Hold');
      await settle(fixture);
      await type(fixture, 'statusJustification', 'Audit review');

      submitButton(fixture).click();
      const req = http.expectOne('/api/gl/accounts/1000-100-01');
      expect(req.request.body.status).toBe('Hold');
      expect(req.request.body.statusJustification).toBe('Audit review');
      req.flush({ ...ACTIVE_ACCOUNT, status: 'Hold' }, { headers: { ETag: '"v2"' } });
      await settle(fixture);
    });

    it('AC-GLA-023 shows the API message when the status change is refused', async () => {
      const fixture = await render({ id: '1000-100-01' });
      setControl(fixture, 'status', 'Hold');
      await settle(fixture);
      await type(fixture, 'statusJustification', 'x');

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts/1000-100-01')
        .flush({ errors: { status: ['Template status cannot be changed to or from another status.'] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('.error-summary')?.textContent).toContain('Template status cannot be changed');
    });

    it('preselects the status from the delete dialog (?status=Inactive) and asks for a justification', async () => {
      const fixture = await render({ id: '1000-100-01', status: 'Inactive' });

      expect((fixture.componentInstance as unknown as { form: { value: { status: string } } }).form.value.status).toBe('Inactive');
      expect(input(fixture, 'statusJustification')).not.toBeNull();
    });

    it('AC-GLA-056 shows a stale-version conflict with Reload, keeps the user input, and then saves with the new ETag', async () => {
      const fixture = await render({ id: '1000-100-01' });
      await type(fixture, 'description', 'My change');

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts/1000-100-01')
        .flush({ detail: 'The account was changed by someone else. Reload it and try again.' }, { status: 409, statusText: 'Conflict' });
      await settle(fixture);

      expect(root(fixture).textContent).toContain('This account was changed by someone else.');
      expect(input(fixture, 'description')!.value).toBe('My change');
      expect(submitButton(fixture).disabled).toBe(false);

      button(fixture, 'Reload latest version').click();
      http
        .expectOne('/api/gl/accounts/1000-100-01')
        .flush({ ...ACTIVE_ACCOUNT, description: 'Someone else wrote this' }, { headers: { ETag: '"v9"' } });
      await settle(fixture);

      expect(input(fixture, 'description')!.value).toBe('My change');
      expect(root(fixture).textContent).toContain('Your changes are kept');

      submitButton(fixture).click();
      const retry = http.expectOne('/api/gl/accounts/1000-100-01');
      expect(retry.request.headers.get('If-Match')).toBe('"v9"');
      expect(retry.request.body.description).toBe('My change');
      retry.flush({ ...ACTIVE_ACCOUNT, description: 'My change' }, { headers: { ETag: '"v10"' } });
      await settle(fixture);
    });

    it('shows the error banner and keeps the form values on a server error', async () => {
      const fixture = await render({ id: '1000-100-01' });
      await type(fixture, 'description', 'Kept');

      submitButton(fixture).click();
      http.expectOne('/api/gl/accounts/1000-100-01').flush({}, { status: 500, statusText: 'Server Error' });
      await settle(fixture);

      expect(root(fixture).textContent).toContain('Something went wrong.');
      expect(input(fixture, 'description')!.value).toBe('Kept');
    });

    it('AC-GLA-017 sends staged attribute changes as attributeChanges with the account update', async () => {
      const fixture = await render({ id: '1000-100-01' });
      stagedMode(fixture);
      await settle(fixture);
      const panel = root(fixture).querySelector('app-attribute-panel') as HTMLElement;
      (panel.querySelector('button[aria-label="Mark Region attribute for delete"]') as HTMLButtonElement).click();
      await settle(fixture);
      expect(panel.textContent).toContain('Marked for delete');

      submitButton(fixture).click();
      const req = http.expectOne('/api/gl/accounts/1000-100-01');
      expect(req.request.body.attributeChanges).toEqual([{ action: 'Remove', attributeId: 'att-2' }]);
      req.flush(ACTIVE_ACCOUNT, { headers: { ETag: '"v2"' } });
      await settle(fixture);
    });

    it('AC-GLA-018 keeps all staged rows when one staged change is rejected', async () => {
      const fixture = await render({ id: '1000-100-01' });
      stagedMode(fixture);
      await settle(fixture);
      const panel = root(fixture).querySelector('app-attribute-panel') as HTMLElement;
      (panel.querySelector('button[aria-label="Mark Region attribute for delete"]') as HTMLButtonElement).click();
      await settle(fixture);

      submitButton(fixture).click();
      http
        .expectOne('/api/gl/accounts/1000-100-01')
        .flush({ errors: { 'attributeChanges[0].attributeId': ["Attribute 'att-2' was not found on this account."] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(panel.querySelector('.row-error')?.textContent).toContain('was not found on this account');
      expect(panel.textContent).toContain('Marked for delete');
      expect(panel.querySelectorAll('tbody tr').length).toBe(2);
    });

    it('uses the ETag returned by an immediate attribute change for the next account save', async () => {
      const fixture = await render({ id: '1000-100-01' });
      const panel = root(fixture).querySelector('app-attribute-panel') as HTMLElement;
      (panel.querySelector('button[aria-label="Delete Region attribute"]') as HTMLButtonElement).click();
      http.expectOne('/api/gl/accounts/1000-100-01/attributes/att-2').flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"v5"' } });
      await settle(fixture);

      submitButton(fixture).click();
      const req = http.expectOne('/api/gl/accounts/1000-100-01');
      expect(req.request.headers.get('If-Match')).toBe('"v5"');
      req.flush(ACTIVE_ACCOUNT, { headers: { ETag: '"v6"' } });
      await settle(fixture);
    });
  });
});
