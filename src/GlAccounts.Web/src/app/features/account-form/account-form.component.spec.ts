import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { AccountFormComponent } from './account-form.component';
import { ValidationSummaryComponent } from '../../shared/validation-summary.component';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { environment } from '../../../environments/environment';

describe('AccountFormComponent', () => {
  let component: AccountFormComponent;
  let fixture: ComponentFixture<AccountFormComponent>;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/gl/accounts`;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule, ReactiveFormsModule],
      declarations: [AccountFormComponent, ValidationSummaryComponent],
      providers: [GlAccountsService]
    }).compileComponents();

    fixture   = TestBed.createComponent(AccountFormComponent);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);
    fixture.detectChanges(); // triggers ngOnInit (create mode — no HTTP)
  });

  afterEach(() => httpMock.verify());

  it('should create component in create mode', () => {
    expect(component.isEdit).toBeFalse();
    expect(component.form).toBeTruthy();
  });

  it('form should be invalid when empty required fields', () => {
    component.form.get('accountId')!.setValue('');
    component.form.get('description')!.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form should be valid when all required fields filled', () => {
    component.form.patchValue({
      accountId: '01-300-1000', description: 'Test', accountType: 'Financial',
      category: 'Asset', status: 'Active', balanceType: 'BalanceForward', normalBalance: 'Debit'
    });
    expect(component.form.valid).toBeTrue();
  });

  it('should show validation errors on submit with empty form', () => {
    component.submit();
    fixture.detectChanges();
    expect(component.form.touched).toBeTrue();
  });

  it('should POST on valid create submit', () => {
    component.form.patchValue({
      accountId: '01-300-1000', description: 'Test', accountType: 'Financial',
      category: 'Asset', status: 'Active', balanceType: 'BalanceForward', normalBalance: 'Debit'
    });
    component.submit();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    req.flush({ accountId: '01-300-1000', description: 'Test', accountType: 'Financial',
                category: 'Asset', status: 'Active', balanceType: 'BalanceForward',
                normalBalance: 'Debit', currencyId: null, createdDate: '2026-01-01', createdBy: 'test' });
  });

  it('should show server error on 422 response', () => {
    component.form.patchValue({
      accountId: '01-300-1000', description: 'Test', accountType: 'Financial',
      category: 'Asset', status: 'Active', balanceType: 'BalanceForward', normalBalance: 'Debit'
    });
    component.submit();
    const req = httpMock.expectOne(base);
    req.flush(
      { type: '', title: 'Unprocessable Entity', status: 422, errors: { accountId: ['Invalid format'] } },
      { status: 422, statusText: 'Unprocessable Entity' }
    );
    fixture.detectChanges();
    expect(component.serverError).toBeTruthy();
    expect(component.serverError!.title).toBe('Unprocessable Entity');
  });
});
