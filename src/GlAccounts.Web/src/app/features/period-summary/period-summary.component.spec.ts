import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { PeriodSummaryComponent } from './period-summary.component';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { of, throwError } from 'rxjs';
import { PeriodSummaryResponse } from '../../core/models/account.model';

describe('PeriodSummaryComponent', () => {
  let component: PeriodSummaryComponent;
  let fixture: ComponentFixture<PeriodSummaryComponent>;
  let svcSpy: jasmine.SpyObj<GlAccountsService>;

  const mockResponse: PeriodSummaryResponse = {
    accountId: '01-100-1000',
    items: [
      { balanceId: 'b1', accountId: '01-100-1000', fiscalYear: 2026, fiscalPeriod: 1, budgetVersion: null, balance: 1000.00 },
      { balanceId: 'b2', accountId: '01-100-1000', fiscalYear: 2026, fiscalPeriod: 2, budgetVersion: null, balance: 2500.50 }
    ],
    total: 2
  };

  beforeEach(async () => {
    svcSpy = jasmine.createSpyObj('GlAccountsService', ['getPeriodSummary']);

    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, FormsModule],
      declarations: [PeriodSummaryComponent],
      providers: [{ provide: GlAccountsService, useValue: svcSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(PeriodSummaryComponent);
    component = fixture.componentInstance;
    component.accountId = '01-100-1000';
  });

  it('should create', () => {
    svcSpy.getPeriodSummary.and.returnValue(of(mockResponse));
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should show empty state when no balances', () => {
    svcSpy.getPeriodSummary.and.returnValue(of({ accountId: '01-100-1000', items: [], total: 0 }));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('No period balances found');
  });

  it('should display period balances in table when populated', () => {
    svcSpy.getPeriodSummary.and.returnValue(of(mockResponse));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('2026');
    expect(el.textContent).toContain('1,000.00');
    expect(el.textContent).toContain('2,500.50');
  });

  it('should call getPeriodSummary with filters when load is called', () => {
    svcSpy.getPeriodSummary.and.returnValue(of(mockResponse));
    fixture.detectChanges();

    component.fiscalYear = 2026;
    component.fromPeriod = 1;
    component.toPeriod   = 6;
    component.load();

    expect(svcSpy.getPeriodSummary).toHaveBeenCalledWith('01-100-1000', 2026, 1, 6);
  });

  it('should show error state on failure', () => {
    svcSpy.getPeriodSummary.and.returnValue(throwError(() => new Error('Network error')));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Failed to load period summary');
  });
});
