import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { AttributesPanelComponent } from './attributes-panel.component';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { of, throwError } from 'rxjs';
import { AttributeResponse } from '../../core/models/account.model';
import { HttpErrorResponse } from '@angular/common/http';

describe('AttributesPanelComponent', () => {
  let component: AttributesPanelComponent;
  let fixture: ComponentFixture<AttributesPanelComponent>;
  let svcSpy: jasmine.SpyObj<GlAccountsService>;

  const mockAttr: AttributeResponse = {
    attributeId: 'attr-001',
    accountId: '01-100-1000',
    attributeTypeId: 'REPORTING_UNIT',
    attributeTypeDescription: 'Reporting Unit',
    attributeValueId: 'NORTH_REGION',
    attributeValueDescription: 'North Region'
  };

  beforeEach(async () => {
    svcSpy = jasmine.createSpyObj('GlAccountsService', [
      'addAttribute', 'updateAttribute', 'removeAttribute'
    ]);

    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, FormsModule],
      declarations: [AttributesPanelComponent],
      providers: [{ provide: GlAccountsService, useValue: svcSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(AttributesPanelComponent);
    component = fixture.componentInstance;
    component.accountId = '01-100-1000';
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should show empty state when no attributes', () => {
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('No attributes assigned.');
  });

  it('should display populated attributes in table', () => {
    fixture.detectChanges();
    component.attributes = [mockAttr];
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Reporting Unit');
    expect(el.textContent).toContain('North Region');
  });

  it('should call addAttribute on add success and append to list', () => {
    svcSpy.addAttribute.and.returnValue(of(mockAttr));
    fixture.detectChanges();

    component.newTypeId  = 'REPORTING_UNIT';
    component.newValueId = 'NORTH_REGION';
    component.add();

    expect(svcSpy.addAttribute).toHaveBeenCalledWith('01-100-1000', {
      attributeTypeId: 'REPORTING_UNIT',
      attributeValueId: 'NORTH_REGION'
    });
    expect(component.attributes.length).toBe(1);
    expect(component.showAdd).toBeFalse();
  });

  it('should show addError on 422 from add', () => {
    const errResp = new HttpErrorResponse({
      error: { detail: 'Attribute type not found.' },
      status: 422
    });
    svcSpy.addAttribute.and.returnValue(throwError(() => errResp));
    fixture.detectChanges();

    component.newTypeId  = 'BAD_TYPE';
    component.newValueId = 'SOME_VAL';
    component.add();

    expect(component.addError).toBe('Attribute type not found.');
  });

  it('should remove attribute from list on delete', () => {
    svcSpy.removeAttribute.and.returnValue(of(undefined as unknown as void));
    component.attributes = [mockAttr];
    fixture.detectChanges();

    component.remove('attr-001');

    expect(svcSpy.removeAttribute).toHaveBeenCalledWith('01-100-1000', 'attr-001');
    expect(component.attributes.length).toBe(0);
  });
});
