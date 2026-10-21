import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AccountGroupsComponent } from './account-groups.component';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { of, throwError } from 'rxjs';
import { AccountGroupListResponse } from '../../core/models/account.model';

describe('AccountGroupsComponent', () => {
  let component: AccountGroupsComponent;
  let fixture: ComponentFixture<AccountGroupsComponent>;
  let svcSpy: jasmine.SpyObj<GlAccountsService>;

  const mockResponse: AccountGroupListResponse = {
    accountId: '01-100-1000',
    items: [{ groupId: 'GRP-001', groupName: 'Current Assets', description: null }],
    total: 1
  };

  beforeEach(async () => {
    svcSpy = jasmine.createSpyObj('GlAccountsService', ['listGroups']);

    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      declarations: [AccountGroupsComponent],
      providers: [{ provide: GlAccountsService, useValue: svcSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(AccountGroupsComponent);
    component = fixture.componentInstance;
    component.accountId = '01-100-1000';
  });

  it('should create', () => {
    svcSpy.listGroups.and.returnValue(of(mockResponse));
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should show empty state when no groups', () => {
    svcSpy.listGroups.and.returnValue(of({ accountId: '01-100-1000', items: [], total: 0 }));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('not a member of any account groups');
  });

  it('should display groups in table when populated', () => {
    svcSpy.listGroups.and.returnValue(of(mockResponse));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('GRP-001');
    expect(el.textContent).toContain('Current Assets');
  });

  it('should show error state on failure', () => {
    svcSpy.listGroups.and.returnValue(throwError(() => new Error('Network error')));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Failed to load groups');
  });
});
