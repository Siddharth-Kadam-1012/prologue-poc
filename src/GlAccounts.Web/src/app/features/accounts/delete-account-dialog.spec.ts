import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DeleteAccountDialog, DeleteDialogResult } from './delete-account-dialog';

// UX-GLA-004 delete confirmation and blocked-delete message. Stub data only.
// Covers AC-GLA-038, AC-GLA-039, AC-GLA-040, AC-GLA-041, AC-GLA-042.
describe('DeleteAccountDialog', () => {
  let http: HttpTestingController;
  let closed: (DeleteDialogResult | undefined)[];

  async function render(accountId = '1000-200-01') {
    closed = [];
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MAT_DIALOG_DATA, useValue: { accountId } },
        { provide: MatDialogRef, useValue: { close: (r?: DeleteDialogResult) => closed.push(r) } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(DeleteAccountDialog);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  const root = (f: { nativeElement: unknown }) => f.nativeElement as HTMLElement;
  const button = (f: { nativeElement: unknown }, label: string) =>
    Array.from(root(f).querySelectorAll('button')).find((b) => b.textContent?.trim() === label) as HTMLButtonElement | undefined;

  afterEach(() => http.verify());

  it('asks for confirmation and names the account', async () => {
    const fixture = await render('1000-100-03');

    expect(root(fixture).textContent).toContain('Delete account 1000-100-03?');
    expect(button(fixture, 'Delete')).toBeDefined();
    expect(button(fixture, 'Cancel')).toBeDefined();
  });

  it('Cancel closes without calling the API', async () => {
    const fixture = await render();

    button(fixture, 'Cancel')!.click();

    expect(closed).toEqual([undefined]);
  });

  it('AC-GLA-041 closes with "deleted" on 204', async () => {
    const fixture = await render('1000-100-03');

    button(fixture, 'Delete')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/gl/accounts/1000-100-03' }).flush(null, { status: 204, statusText: 'No Content' });

    expect(closed).toEqual(['deleted']);
  });

  it('disables Delete while the request is in flight (no double submit)', async () => {
    const fixture = await render('1000-100-03');

    button(fixture, 'Delete')!.click();
    fixture.detectChanges();

    expect(button(fixture, 'Delete')!.disabled).toBe(true);
    http.expectOne('/api/gl/accounts/1000-100-03').flush(null, { status: 204, statusText: 'No Content' });
  });

  it('AC-GLA-038 shows the posted-transaction count, the message and the deactivation offer on 409', async () => {
    const fixture = await render();

    button(fixture, 'Delete')!.click();
    http.expectOne('/api/gl/accounts/1000-200-01').flush(
      {
        status: 409,
        detail: "Cannot delete account '1000-200-01'. It has 10001 posted transaction(s).",
        blockers: [{ kind: 'PostedTransactions', count: 10001 }],
        suggestion: 'Set the account status to Inactive instead.',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const text = root(fixture).textContent ?? '';
    expect(text).toContain("Cannot delete account '1000-200-01'. It has 10001 posted transaction(s).");
    expect(text).toContain('10001 posted transaction(s)');
    expect(text).toContain('Set the account status to Inactive instead.');
    expect(button(fixture, 'Set status to Inactive instead')).toBeDefined();
    expect(closed).toEqual([]);
  });

  it('AC-GLA-039 / AC-GLA-040 / AC-GLA-042 lists every blocker type with its count in one response', async () => {
    const fixture = await render('3000-100-01');

    button(fixture, 'Delete')!.click();
    http.expectOne('/api/gl/accounts/3000-100-01').flush(
      {
        status: 409,
        detail: 'blocked',
        blockers: [
          { kind: 'UnpostedTransactions', count: 5 },
          { kind: 'Budgets', count: 3 },
          { kind: 'Allocations', count: 1 },
          { kind: 'GroupMemberships', count: 1 },
        ],
        suggestion: 'Deactivate instead.',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const items = Array.from(root(fixture).querySelectorAll('li')).map((li) => li.textContent?.trim());
    expect(items).toEqual([
      '5 unposted transaction(s)',
      '3 budget(s)',
      '1 allocation(s)',
      '1 account group membership(s)',
    ]);
  });

  it('closes with "deactivate" when the user picks the alternative', async () => {
    const fixture = await render();
    button(fixture, 'Delete')!.click();
    http.expectOne('/api/gl/accounts/1000-200-01').flush(
      { status: 409, detail: 'blocked', blockers: [{ kind: 'PostedTransactions', count: 2 }], suggestion: 's' },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    button(fixture, 'Set status to Inactive instead')!.click();

    expect(closed).toEqual(['deactivate']);
  });

  it('on 404 says the account is gone and closes with "gone"', async () => {
    const fixture = await render('1000-100-03');
    button(fixture, 'Delete')!.click();
    http.expectOne('/api/gl/accounts/1000-100-03').flush({ status: 404, detail: "Account '1000-100-03' was not found." }, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root(fixture).textContent).toContain("Account '1000-100-03' was not found.");
    button(fixture, 'Close')!.click();
    expect(closed).toEqual(['gone']);
  });

  it('shows an error banner inside the dialog on 500 and lets the user retry', async () => {
    const fixture = await render('1000-100-03');
    button(fixture, 'Delete')!.click();
    http.expectOne('/api/gl/accounts/1000-100-03').flush({ status: 500, title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root(fixture).querySelector('[role="alert"]')).not.toBeNull();
    expect(button(fixture, 'Delete')!.disabled).toBe(false);
  });
});
