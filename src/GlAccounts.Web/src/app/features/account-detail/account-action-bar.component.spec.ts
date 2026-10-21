import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router } from '@angular/router';
import { AccountActionBarComponent } from './account-action-bar.component';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('AccountActionBarComponent', () => {
  let component: AccountActionBarComponent;
  let fixture: ComponentFixture<AccountActionBarComponent>;
  let svcSpy: jasmine.SpyObj<GlAccountsService>;
  let router: Router;

  beforeEach(async () => {
    svcSpy = jasmine.createSpyObj('GlAccountsService', ['deleteAccount']);

    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
      declarations: [AccountActionBarComponent],
      providers: [{ provide: GlAccountsService, useValue: svcSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(AccountActionBarComponent);
    component = fixture.componentInstance;
    component.accountId = '01-100-1000';
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show Edit, Copy, and Delete buttons', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Edit');
    expect(el.textContent).toContain('Copy from Template');
    expect(el.textContent).toContain('Delete');
  });

  it('should navigate to edit route on Edit click', () => {
    const spy = spyOn(router, 'navigate');
    component.onEdit();
    expect(spy).toHaveBeenCalledWith(['/gl/accounts', '01-100-1000', 'edit']);
  });

  it('should navigate to copy route on Copy click', () => {
    const spy = spyOn(router, 'navigate');
    component.onCopy();
    expect(spy).toHaveBeenCalledWith(['/gl/accounts', '01-100-1000', 'copy']);
  });

  it('should call deleteAccount and navigate to list on 204 success', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    svcSpy.deleteAccount.and.returnValue(of(undefined as unknown as void));
    const navSpy = spyOn(router, 'navigate');

    component.onDelete();

    expect(svcSpy.deleteAccount).toHaveBeenCalledWith('01-100-1000');
    expect(navSpy).toHaveBeenCalledWith(['/gl/accounts']);
  });

  it('should show dependency message on 409 blocked delete', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    const errResp = new HttpErrorResponse({
      error: { detail: 'Cannot delete: 3 posted transaction(s). Consider setting the account to Inactive.' },
      status: 409
    });
    svcSpy.deleteAccount.and.returnValue(throwError(() => errResp));

    component.onDelete();
    fixture.detectChanges();

    expect(component.deleteError).toBe('Cannot delete: 3 posted transaction(s). Consider setting the account to Inactive.');
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Cannot delete: 3 posted transaction(s)');
  });

  it('should not delete if confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.onDelete();
    expect(svcSpy.deleteAccount).not.toHaveBeenCalled();
  });
});
