import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { AccountListComponent } from './account-list.component';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { environment } from '../../../environments/environment';

describe('AccountListComponent', () => {
  let component: AccountListComponent;
  let fixture: ComponentFixture<AccountListComponent>;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/gl/accounts`;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule, FormsModule],
      declarations: [AccountListComponent],
      providers: [GlAccountsService]
    }).compileComponents();

    fixture   = TestBed.createComponent(AccountListComponent);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be in loading state while HTTP is pending', () => {
    fixture.detectChanges();   // triggers ngOnInit → load() → HTTP call in flight
    expect(component.loading).toBeTrue();  // still loading until response arrives
    httpMock.expectOne(base).flush({ items: [], total: 0 });
    fixture.detectChanges();
    expect(component.loading).toBeFalse();
  });

  it('should display accounts when populated', () => {
    fixture.detectChanges();
    httpMock.expectOne(base).flush({
      items: [{ accountId: '01-100-1000', description: 'Cash', accountType: 'Financial',
                category: 'Asset', status: 'Active', balanceType: 'BalanceForward',
                normalBalance: 'Debit', currencyId: null, createdDate: '2026-01-01', createdBy: 'seed' }],
      total: 1
    });
    fixture.detectChanges();
    const rows = fixture.debugElement.queryAll(By.css('tr.clickable-row'));
    expect(rows.length).toBe(1);
    expect(rows[0].nativeElement.textContent).toContain('01-100-1000');
  });

  it('should show empty state when no accounts', () => {
    fixture.detectChanges();
    httpMock.expectOne(base).flush({ items: [], total: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No accounts found');
  });

  it('should show error state on HTTP failure', () => {
    fixture.detectChanges();
    httpMock.expectOne(base).error(new ProgressEvent('network error'));
    fixture.detectChanges();
    expect(component.error).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Failed to load accounts');
  });

  it('should filter by status when status filter changes', () => {
    component.statusFilter = 'Active';
    component.load();
    const req = httpMock.expectOne(`${base}?status=Active`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0 });
  });
});
