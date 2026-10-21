import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AccountDetailComponent } from './account-detail.component';
import {
  AttributesPanelStubComponent,
  AccountGroupsStubComponent,
  PeriodSummaryStubComponent,
  TransactionListStubComponent
} from './account-detail-stubs';
import { Component, Input } from '@angular/core';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { environment } from '../../../environments/environment';

// Stub for AccountActionBarComponent to isolate AccountDetailComponent
@Component({ selector: 'app-account-action-bar', template: '' })
class AccountActionBarStubComponent { @Input() accountId = ''; }

describe('AccountDetailComponent', () => {
  let component: AccountDetailComponent;
  let fixture: ComponentFixture<AccountDetailComponent>;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/gl/accounts`;

  const mockAccount = {
    accountId: '01-100-1000', description: 'Cash Account', accountType: 'Financial',
    category: 'Asset', status: 'Active', balanceType: 'BalanceForward',
    normalBalance: 'Debit', currencyId: null, createdDate: '2026-01-01', createdBy: 'seed'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
      declarations: [
        AccountDetailComponent,
        AccountActionBarStubComponent,
        AttributesPanelStubComponent,
        AccountGroupsStubComponent,
        PeriodSummaryStubComponent,
        TransactionListStubComponent
      ],
      providers: [GlAccountsService]
    }).compileComponents();

    fixture   = TestBed.createComponent(AccountDetailComponent);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should show loading state initially', () => {
    // Before detectChanges, ngOnInit has not run yet
    expect(component.loading).toBeTrue();
    fixture.detectChanges();
    httpMock.expectOne(`${base}/`).flush(mockAccount);
  });

  it('should display account fields when loaded', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${base}/`).flush(mockAccount);
    fixture.detectChanges();
    expect(component.account).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('01-100-1000');
    expect(fixture.nativeElement.textContent).toContain('Cash Account');
  });

  it('should show error state on 404', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${base}/`).error(new ProgressEvent('error'));
    fixture.detectChanges();
    expect(component.error).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('not found');
  });
});
