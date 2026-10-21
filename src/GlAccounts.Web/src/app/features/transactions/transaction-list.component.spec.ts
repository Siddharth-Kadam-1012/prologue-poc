import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { TransactionListComponent } from './transaction-list.component';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { of, throwError } from 'rxjs';
import { TransactionListResponse } from '../../core/models/account.model';

describe('TransactionListComponent', () => {
  let component: TransactionListComponent;
  let fixture: ComponentFixture<TransactionListComponent>;
  let svcSpy: jasmine.SpyObj<GlAccountsService>;

  const mockResponse: TransactionListResponse = {
    accountId: '01-200-2000',
    items: [
      { transactionId: 'TXN-001', accountId: '01-200-2000', batchId: 'BATCH-001', description: 'Invoice 1001', amount: 1500.00, postingDate: '2026-01-15', postingStatus: 'Posted' },
      { transactionId: 'TXN-002', accountId: '01-200-2000', batchId: 'BATCH-001', description: 'Invoice 1002', amount: 2300.75, postingDate: '2026-02-20', postingStatus: 'Posted' }
    ],
    total: 2
  };

  beforeEach(async () => {
    svcSpy = jasmine.createSpyObj('GlAccountsService', ['getTransactions']);

    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, FormsModule],
      declarations: [TransactionListComponent],
      providers: [{ provide: GlAccountsService, useValue: svcSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionListComponent);
    component = fixture.componentInstance;
    component.accountId = '01-200-2000';
  });

  it('should create', () => {
    svcSpy.getTransactions.and.returnValue(of(mockResponse));
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should show empty state when no transactions', () => {
    svcSpy.getTransactions.and.returnValue(of({ accountId: '01-200-2000', items: [], total: 0 }));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('No transactions found');
  });

  it('should display transactions in table when populated', () => {
    svcSpy.getTransactions.and.returnValue(of(mockResponse));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('TXN-001');
    expect(el.textContent).toContain('Invoice 1001');
    expect(el.textContent).toContain('1,500.00');
  });

  it('should call getTransactions with filters when load is called', () => {
    svcSpy.getTransactions.and.returnValue(of(mockResponse));
    fixture.detectChanges();

    component.fromDate      = '2026-01-01';
    component.toDate        = '2026-06-30';
    component.postingStatus = 'Posted';
    component.load();

    expect(svcSpy.getTransactions).toHaveBeenCalledWith('01-200-2000', '2026-01-01', '2026-06-30', 'Posted');
  });

  it('should show error state on failure', () => {
    svcSpy.getTransactions.and.returnValue(throwError(() => new Error('Network error')));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Failed to load transactions');
  });
});
