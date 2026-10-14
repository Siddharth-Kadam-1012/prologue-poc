import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GlAccountsApi, fileNameFrom, toParams } from './gl-accounts-api';
import { Account } from './models';

// Stub data only: these tests check the requests the client sends and how it reads responses;
// they do not talk to the API. The operation IDs are from contracts/openapi.yaml.
describe('GlAccountsApi', () => {
  let api: GlAccountsApi;
  let http: HttpTestingController;

  const account: Account = {
    accountId: '1000-100-01',
    segments: [],
    description: 'Cash',
    accountType: 'Financial',
    category: 'Asset',
    currencyId: 'USD',
    status: 'Active',
    createdDate: '2024-01-01',
    eligibleForPosting: true,
    attributes: [],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(GlAccountsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('API-GLA-015 searchAccounts sends only the filters that are set', () => {
    api.searchAccounts({ status: 'Template', q: '', page: 2 }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/gl/accounts');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('status')).toBe('Template');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.has('q')).toBe(false);
    request.flush({ items: [], total: 0, page: 2, pageSize: 50 });
  });

  it('API-GLA-001 createAccount posts the body and returns the ETag', () => {
    let etag: string | null = null;
    api.createAccount({ accountId: '1000-100-02', templateAccountId: '9000-100-01' }).subscribe((r) => (etag = r.etag));

    const request = http.expectOne('/api/gl/accounts');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ accountId: '1000-100-02', templateAccountId: '9000-100-01' });
    request.flush(account, { headers: { ETag: '"v1"' } });
    expect(etag).toBe('"v1"');
  });

  it('API-GLA-002 getAccount returns the body with its ETag', () => {
    let result: { body: Account; etag: string | null } | undefined;
    api.getAccount('1000-100-01').subscribe((r) => (result = r));

    http.expectOne('/api/gl/accounts/1000-100-01').flush(account, { headers: { ETag: '"v7"' } });

    expect(result?.body.accountId).toBe('1000-100-01');
    expect(result?.etag).toBe('"v7"');
  });

  it('API-GLA-003 updateAccount sends the ETag as If-Match (AC-GLA-056)', () => {
    api.updateAccount('1000-100-01', { description: 'x', category: 'Asset', currencyId: 'USD', status: 'Active' }, '"v7"').subscribe();

    const request = http.expectOne('/api/gl/accounts/1000-100-01');
    expect(request.request.method).toBe('PUT');
    expect(request.request.headers.get('If-Match')).toBe('"v7"');
    request.flush(account, { headers: { ETag: '"v8"' } });
  });

  it('API-GLA-004 deleteAccount sends DELETE', () => {
    api.deleteAccount('1000-100-03').subscribe();

    const request = http.expectOne('/api/gl/accounts/1000-100-03');
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('API-GLA-005/006/007 attribute operations use the attribute routes and return the new ETag', () => {
    const etags: (string | null)[] = [];
    api.addAccountAttribute('1000-100-01', { attributeTypeId: 'dept', attributeValueId: 'sales' }).subscribe((r) => etags.push(r.etag));
    http.expectOne({ method: 'POST', url: '/api/gl/accounts/1000-100-01/attributes' }).flush({ attributeId: 'att-3', attributeTypeId: 'dept', attributeValueId: 'sales' }, { headers: { ETag: '"a"' } });

    api.updateAccountAttribute('1000-100-01', 'att-3', { attributeTypeId: 'dept', attributeValueId: 'finance' }).subscribe((r) => etags.push(r.etag));
    http.expectOne({ method: 'PUT', url: '/api/gl/accounts/1000-100-01/attributes/att-3' }).flush({ attributeId: 'att-3', attributeTypeId: 'dept', attributeValueId: 'finance' }, { headers: { ETag: '"b"' } });

    api.removeAccountAttribute('1000-100-01', 'att-3').subscribe((etag) => etags.push(etag));
    http.expectOne({ method: 'DELETE', url: '/api/gl/accounts/1000-100-01/attributes/att-3' }).flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"c"' } });

    expect(etags).toEqual(['"a"', '"b"', '"c"']);
  });

  it('API-GLA-008 listAccountGroups reads the items', () => {
    let count = -1;
    api.listAccountGroups('1000-100-01').subscribe((r) => (count = r.items.length));

    http.expectOne('/api/gl/accounts/1000-100-01/groups').flush({ items: [{ groupId: 'g1', name: 'A', parentChain: [] }] });

    expect(count).toBe(1);
  });

  it('API-GLA-009 exportAccountGroups takes the file name from Content-Disposition', () => {
    let name = '';
    api.exportAccountGroups('1000-100-01').subscribe((f) => (name = f.fileName));

    http.expectOne('/api/gl/accounts/1000-100-01/groups/export').flush(new Blob(['x']), { headers: { 'Content-Disposition': 'attachment; filename="groups-1000-100-01.csv"' } });

    expect(name).toBe('groups-1000-100-01.csv');
  });

  it('API-GLA-010/011 period summary passes filter and sort to the view and the export (AC-GLA-030, 031)', () => {
    const query = { fiscalYear: 2024, sortBy: 'balance', sortDirection: 'desc' } as const;
    api.getPeriodSummary('1000-100-01', query).subscribe();
    api.exportPeriodSummary('1000-100-01', query).subscribe();

    for (const url of ['/api/gl/accounts/1000-100-01/period-summary', '/api/gl/accounts/1000-100-01/period-summary/export']) {
      const request = http.expectOne((r) => r.url === url);
      expect(request.request.params.get('fiscalYear')).toBe('2024');
      expect(request.request.params.get('sortBy')).toBe('balance');
      expect(request.request.params.get('sortDirection')).toBe('desc');
      request.flush(url.endsWith('export') ? new Blob(['x']) : { accountId: '1000-100-01', periods: [], years: [], budgetVersions: [] });
    }
  });

  it('API-GLA-012/013/014 transactions: list, detail and export (AC-GLA-033..036)', () => {
    api.listAccountTransactions('1000-200-01', { page: 1, pageSize: 50, fromDate: '2024-01-01' }).subscribe();
    const list = http.expectOne((r) => r.url === '/api/gl/accounts/1000-200-01/transactions');
    expect(list.request.params.get('pageSize')).toBe('50');
    expect(list.request.params.get('fromDate')).toBe('2024-01-01');
    list.flush({ items: [], total: 0, page: 1, pageSize: 50 });

    api.getAccountTransaction('1000-200-01', 'je 1').subscribe();
    http.expectOne('/api/gl/accounts/1000-200-01/transactions/je%201').flush({});

    api.exportAccountTransactions('1000-200-01', { fromDate: '2024-01-01' }).subscribe();
    const exported = http.expectOne((r) => r.url === '/api/gl/accounts/1000-200-01/transactions/export');
    expect(exported.request.params.has('page')).toBe(false);
    exported.flush(new Blob(['x']));
  });

  it('encodes the account id in the path', () => {
    api.getAccount('a/b').subscribe();

    http.expectOne('/api/gl/accounts/a%2Fb').flush(account);
  });
});

describe('toParams', () => {
  it('skips undefined, null and empty values', () => {
    const params = toParams({ a: 1, b: undefined, c: null, d: '', e: 'x' });

    expect(params.keys().sort()).toEqual(['a', 'e']);
  });
});

describe('fileNameFrom', () => {
  it('reads quoted, unquoted and RFC 5987 names, else the fallback', () => {
    expect(fileNameFrom('attachment; filename="a.csv"', 'f')).toBe('a.csv');
    expect(fileNameFrom('attachment; filename=b.xlsx', 'f')).toBe('b.xlsx');
    expect(fileNameFrom("attachment; filename*=UTF-8''c%20d.csv", 'f')).toBe('c d.csv');
    expect(fileNameFrom(null, 'fallback.csv')).toBe('fallback.csv');
  });
});
