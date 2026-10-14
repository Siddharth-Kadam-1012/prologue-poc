import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { Announcer } from '../../core/announcer';
import { FileSaver } from '../../core/file-saver';
import { HOLD_ACCOUNT, ACTIVE_ACCOUNT, NESTED_GROUP, ROOT_GROUP } from '../../testing/fixtures';
import { AccountInquiry } from './account-inquiry';
import { DeleteAccountDialog } from './delete-account-dialog';

// UX-GLA-002 account inquiry. Stub data only: HTTP is faked, nothing is persisted.
// Covers AC-GLA-004, AC-GLA-005, AC-GLA-020, AC-GLA-021, AC-GLA-024, AC-GLA-025, AC-GLA-027.
describe('AccountInquiry', () => {
  let http: HttpTestingController;

  async function render(id: string) {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(AccountInquiry);
    fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  const text = (fixture: { nativeElement: unknown }) => (fixture.nativeElement as HTMLElement).textContent ?? '';

  async function flushAccount(fixture: Awaited<ReturnType<typeof render>>, account = ACTIVE_ACCOUNT, groups: unknown[] = [NESTED_GROUP]) {
    http.expectOne(`/api/gl/accounts/${account.accountId}`).flush(account, { headers: { ETag: '"v1"' } });
    await fixture.whenStable();
    http.expectOne(`/api/gl/accounts/${account.accountId}/groups`).flush({ items: groups });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => http.verify());

  it('AC-GLA-004 shows classification, segments and attributes', async () => {
    const fixture = await render('1000-100-01');

    expect(text(fixture)).toContain('Loading account');
    await flushAccount(fixture);

    const content = text(fixture);
    expect(content).toContain('1000-100-01');
    expect(content).toContain('Cash and equivalents');
    expect(content).toContain('Financial');
    expect(content).toContain('BalanceForward');
    expect(content).toContain('Department');
    expect(content).toContain('finance');
    expect(content).toContain('emea');
  });

  it('AC-GLA-005 shows a not-found state with a link to search for an unknown account', async () => {
    const fixture = await render('9999-999-99');
    http.expectOne('/api/gl/accounts/9999-999-99').flush({ status: 404, detail: "Account '9999-999-99' was not found." }, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(text(fixture)).toContain("Account '9999-999-99' was not found.");
    expect((fixture.nativeElement as HTMLElement).querySelector('a[href="/gl/accounts"]')).not.toBeNull();
  });

  it('shows a retryable error banner when the service fails', async () => {
    const fixture = await render('1000-100-01');
    http.expectOne('/api/gl/accounts/1000-100-01').flush({ status: 500, title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Something went wrong.');
    expect(root.querySelector('button')?.textContent).toContain('Try again');
  });

  it('AC-GLA-020 says an Active account is eligible for new transactions, in text', async () => {
    const fixture = await render('1000-100-01');
    await flushAccount(fixture);

    expect(text(fixture)).toContain('Eligible for new transactions');
  });

  it('AC-GLA-021 says a Hold account is not eligible and names the status', async () => {
    const fixture = await render('4000-100-01');
    await flushAccount(fixture, HOLD_ACCOUNT, []);

    expect(text(fixture)).toContain('Not eligible for new transactions: the account status is Hold');
  });

  it('AC-GLA-024 lists each group with its parent chain, nearest parent first', async () => {
    const fixture = await render('1000-100-01');
    await flushAccount(fixture, ACTIVE_ACCOUNT, [NESTED_GROUP, ROOT_GROUP]);

    const content = text(fixture);
    expect(content).toContain('Operating');
    expect(content).toContain('Current assets > All accounts');
    expect(content).toContain('All accounts');
  });

  it('AC-GLA-025 shows an empty state, not an error, for an account in no group', async () => {
    const fixture = await render('1000-100-01');
    await flushAccount(fixture, ACTIVE_ACCOUNT, []);

    expect(text(fixture)).toContain('This account is in no group.');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
  });

  it('shows an empty state for an account with no attributes', async () => {
    const fixture = await render('4000-100-01');
    await flushAccount(fixture, HOLD_ACCOUNT, []);

    expect(text(fixture)).toContain('This account has no attributes.');
  });

  it('AC-GLA-027 exports the groups as CSV and announces it', async () => {
    const fixture = await render('1000-100-01');
    await flushAccount(fixture);
    const saved: string[] = [];
    vi.spyOn(TestBed.inject(FileSaver), 'save').mockImplementation((file) => void saved.push(file.fileName));

    const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find((b) => b.textContent?.includes('Export CSV'))!;
    button.click();
    http.expectOne('/api/gl/accounts/1000-100-01/groups/export').flush(new Blob(['groupId,name,parentChain']), {
      headers: { 'Content-Disposition': 'attachment; filename="groups-1000-100-01.csv"' },
    });
    await Promise.resolve();

    expect(saved).toEqual(['groups-1000-100-01.csv']);
    expect(TestBed.inject(Announcer).polite()).toBe('Export CSV started.');
  });

  it('links to edit, period summary, transactions and delete (SC-005: one step each)', async () => {
    const fixture = await render('1000-100-01');
    await flushAccount(fixture);

    const hrefs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/gl/accounts/1000-100-01/edit');
    expect(hrefs).toContain('/gl/accounts/1000-100-01/period-summary');
    expect(hrefs).toContain('/gl/accounts/1000-100-01/transactions');
    const labels = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(labels).toContain('Delete');
  });

  it('AC-GLA-041 goes to the search page after a successful delete', async () => {
    const fixture = await render('1000-100-01');
    await flushAccount(fixture);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    vi.spyOn(TestBed.inject(MatDialog), 'open').mockReturnValue({ afterClosed: () => of('deleted') } as never);

    const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Delete')!;
    button.click();

    expect(navigate).toHaveBeenCalledWith(['/gl/accounts']);
  });

  it('opens the edit form with status Inactive when the user chooses to deactivate instead', async () => {
    const fixture = await render('1000-100-01');
    await flushAccount(fixture);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const open = vi.spyOn(TestBed.inject(MatDialog), 'open').mockReturnValue({ afterClosed: () => of('deactivate') } as never);

    const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Delete')!;
    button.click();

    expect(open.mock.calls[0][0]).toBe(DeleteAccountDialog);
    expect(open.mock.calls[0][1]).toMatchObject({ role: 'alertdialog', restoreFocus: true });
    expect(navigate).toHaveBeenCalledWith(['/gl/accounts', '1000-100-01', 'edit'], { queryParams: { status: 'Inactive' } });
  });
});
