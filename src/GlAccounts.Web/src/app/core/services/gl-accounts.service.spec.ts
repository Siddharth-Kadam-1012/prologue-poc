import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { GlAccountsService } from './gl-accounts.service';
import { environment } from '../../../environments/environment';
import { AccountResponse, CreateAccountRequest } from '../models/account.model';

describe('GlAccountsService', () => {
  let service: GlAccountsService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/gl/accounts`;

  const mockAccount: AccountResponse = {
    accountId: '01-100-1000', description: 'Cash Account',
    accountType: 'Financial', category: 'Asset', status: 'Active',
    balanceType: 'BalanceForward', normalBalance: 'Debit',
    currencyId: null, createdDate: '2026-01-01', createdBy: 'seed'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [GlAccountsService]
    });
    service  = TestBed.inject(GlAccountsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('listAccounts() should GET /api/gl/accounts', () => {
    const mockList = { items: [mockAccount], total: 1 };
    service.listAccounts().subscribe(r => expect(r.total).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush(mockList);
  });

  it('listAccounts() with status filter should include ?status= param', () => {
    service.listAccounts('Active').subscribe();
    const req = httpMock.expectOne(`${base}?status=Active`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0 });
  });

  it('getAccount() should GET /api/gl/accounts/{id}', () => {
    service.getAccount('01-100-1000').subscribe(r => expect(r.accountId).toBe('01-100-1000'));
    const req = httpMock.expectOne(`${base}/01-100-1000`);
    expect(req.request.method).toBe('GET');
    req.flush(mockAccount);
  });

  it('createAccount() should POST /api/gl/accounts', () => {
    const payload: CreateAccountRequest = {
      accountId: '01-300-1000', description: 'New', accountType: 'Financial',
      category: 'Asset', status: 'Active', balanceType: 'BalanceForward', normalBalance: 'Debit'
    };
    service.createAccount(payload).subscribe(r => expect(r.accountId).toBe('01-300-1000'));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    req.flush({ ...mockAccount, accountId: '01-300-1000' });
  });

  it('updateAccount() should PUT /api/gl/accounts/{id}', () => {
    service.updateAccount('01-100-1000', { description: 'Updated' }).subscribe();
    const req = httpMock.expectOne(`${base}/01-100-1000`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockAccount);
  });

  it('deleteAccount() should DELETE /api/gl/accounts/{id}', () => {
    service.deleteAccount('01-100-1000').subscribe();
    const req = httpMock.expectOne(`${base}/01-100-1000`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('addAttribute() should POST /api/gl/accounts/{id}/attributes', () => {
    service.addAttribute('01-100-1000', { attributeTypeId: 'COST-CENTRE', attributeValueId: 'CC-100' }).subscribe();
    const req = httpMock.expectOne(`${base}/01-100-1000/attributes`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('updateAttribute() should PUT /api/gl/accounts/{id}/attributes/{attrId}', () => {
    service.updateAttribute('01-100-1000', 'attr-1', { attributeValueId: 'CC-200' }).subscribe();
    const req = httpMock.expectOne(`${base}/01-100-1000/attributes/attr-1`);
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('removeAttribute() should DELETE /api/gl/accounts/{id}/attributes/{attrId}', () => {
    service.removeAttribute('01-100-1000', 'attr-1').subscribe();
    const req = httpMock.expectOne(`${base}/01-100-1000/attributes/attr-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('listGroups() should GET /api/gl/accounts/{id}/groups', () => {
    service.listGroups('01-100-1000').subscribe();
    const req = httpMock.expectOne(`${base}/01-100-1000/groups`);
    expect(req.request.method).toBe('GET');
    req.flush({ accountId: '01-100-1000', items: [], total: 0 });
  });

  it('getPeriodSummary() with filters should include query params', () => {
    service.getPeriodSummary('01-100-1000', 2026, 1, 4).subscribe();
    const req = httpMock.expectOne(`${base}/01-100-1000/period-summary?fiscalYear=2026&fromPeriod=1&toPeriod=4`);
    expect(req.request.method).toBe('GET');
    req.flush({ accountId: '01-100-1000', items: [], total: 0 });
  });

  it('getTransactions() with filters should include query params', () => {
    service.getTransactions('01-100-1000', '2026-01-01', '2026-12-31', 'Posted').subscribe();
    const req = httpMock.expectOne(
      `${base}/01-100-1000/transactions?fromDate=2026-01-01&toDate=2026-12-31&postingStatus=Posted`);
    expect(req.request.method).toBe('GET');
    req.flush({ accountId: '01-100-1000', items: [], total: 0 });
  });

  it('copyFromTemplate() should POST /api/gl/accounts/{id}/copy', () => {
    service.copyFromTemplate('01-100-5000', { newAccountId: '01-300-9000', description: 'Copy' }).subscribe();
    const req = httpMock.expectOne(`${base}/01-100-5000/copy`);
    expect(req.request.method).toBe('POST');
    req.flush(mockAccount);
  });
});
