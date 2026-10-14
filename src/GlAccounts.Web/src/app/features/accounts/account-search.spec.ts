import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Announcer } from '../../core/announcer';
import { SUMMARY_ROW, summaryPage } from '../../testing/fixtures';
import { AccountSearch } from './account-search';

// Account search entry page (API-GLA-015). Stub data only.
describe('AccountSearch', () => {
  let http: HttpTestingController;

  async function render() {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(AccountSearch);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  const root = (f: { nativeElement: unknown }) => f.nativeElement as HTMLElement;
  const firstRequest = () => http.expectOne((r) => r.url === '/api/gl/accounts');

  afterEach(() => http.verify());

  it('loads the first page on open and links each Account ID to its inquiry page', async () => {
    const fixture = await render();
    expect(root(fixture).textContent).toContain('Loading accounts');

    const request = firstRequest();
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe('50');
    request.flush(summaryPage([SUMMARY_ROW]));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root(fixture).querySelector('a[href="/gl/accounts/1000-100-01"]')?.textContent).toBe('1000-100-01');
    expect(root(fixture).textContent).toContain('Active');
  });

  it('announces the result count', async () => {
    const fixture = await render();
    firstRequest().flush(summaryPage([SUMMARY_ROW], 120));
    await fixture.whenStable();
    await Promise.resolve();

    expect(TestBed.inject(Announcer).polite()).toBe('1 of 120 accounts');
  });

  it('shows an empty state when nothing matches', async () => {
    const fixture = await render();
    firstRequest().flush(summaryPage([]));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root(fixture).textContent).toContain('No accounts match your search.');
  });

  it('shows a retryable banner on failure and does not hide the form', async () => {
    const fixture = await render();
    firstRequest().flush({ status: 500, title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root(fixture).querySelector('[role="alert"]')).not.toBeNull();
    expect(root(fixture).querySelector('form[role="search"]')).not.toBeNull();
  });

  it('sends the search text trimmed and goes back to page 1', async () => {
    const fixture = await render();
    firstRequest().flush(summaryPage([SUMMARY_ROW]));
    await fixture.whenStable();
    fixture.detectChanges();

    const input = root(fixture).querySelector('input[type="search"]') as HTMLInputElement;
    input.value = '  cash  ';
    input.dispatchEvent(new Event('input'));
    (root(fixture).querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));

    const request = firstRequest();
    expect(request.request.params.get('q')).toBe('cash');
    expect(request.request.params.get('page')).toBe('1');
    request.flush(summaryPage([]));
  });

  it('offers a link to create an account and to the template library', async () => {
    const fixture = await render();
    firstRequest().flush(summaryPage([]));
    await fixture.whenStable();
    fixture.detectChanges();

    const hrefs = Array.from(root(fixture).querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/gl/accounts/new');
    expect(hrefs).toContain('/gl/account-templates');
  });
});
