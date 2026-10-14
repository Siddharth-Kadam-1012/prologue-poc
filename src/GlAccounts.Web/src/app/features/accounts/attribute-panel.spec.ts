import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AttributePanel } from './attribute-panel';
import { ACTIVE_ACCOUNT } from '../../testing/fixtures';

// UX-GLA-003 attribute panel. Stub data only.
// Covers AC-GLA-014, AC-GLA-015, AC-GLA-016, AC-GLA-017, AC-GLA-018 (staged and immediate modes).
describe('AttributePanel', () => {
  let http: HttpTestingController;

  async function render(inputs: { stored?: unknown; accountId?: string | null; immediate?: boolean } = {}) {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(AttributePanel);
    fixture.componentRef.setInput('stored', inputs.stored ?? []);
    if (inputs.accountId !== undefined) fixture.componentRef.setInput('accountId', inputs.accountId);
    if (inputs.immediate !== undefined) fixture.componentRef.setInput('immediate', inputs.immediate);
    await settle(fixture);
    return fixture;
  }

  async function settle(fixture: { detectChanges(): void; whenStable(): Promise<void> }) {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const root = (f: { nativeElement: unknown }) => f.nativeElement as HTMLElement;
  const button = (f: { nativeElement: unknown }, label: string) =>
    Array.from(root(f).querySelectorAll('button')).find((b) => (b.getAttribute('aria-label') ?? b.textContent?.trim()) === label) as HTMLButtonElement;
  const select = (f: { nativeElement: unknown }, label: string) =>
    root(f).querySelector(`select[aria-label="${label}"]`) as HTMLSelectElement;
  function choose(el: HTMLSelectElement, value: string) {
    el.value = value;
    el.dispatchEvent(new Event('change'));
  }

  afterEach(() => http?.verify());

  it('shows an empty state with an Add attribute button when there are no attributes', async () => {
    const fixture = await render();

    expect(root(fixture).textContent).toContain('No attributes yet.');
    expect(button(fixture, 'Add attribute')).toBeDefined();
  });

  it('lists stored attributes with readable type and value labels', async () => {
    const fixture = await render({ stored: ACTIVE_ACCOUNT.attributes });

    const text = root(fixture).textContent!;
    expect(text).toContain('Department');
    expect(text).toContain('Finance');
    expect(text).toContain('Region');
    expect(text).toContain('EMEA');
    expect(fixture.componentInstance.hasPending()).toBe(false);
  });

  it('AC-GLA-014 stages an added attribute and offers only values that belong to the chosen type', async () => {
    const fixture = await render();
    button(fixture, 'Add attribute').click();
    await settle(fixture);

    choose(select(fixture, 'Attribute type'), 'region');
    await settle(fixture);
    const options = Array.from(select(fixture, 'Attribute value').options).map((o) => o.value);
    expect(options).toEqual(['', 'emea', 'na', 'apac']);

    choose(select(fixture, 'Attribute value'), 'emea');
    await settle(fixture);

    expect(fixture.componentInstance.changes()).toEqual([{ action: 'Add', attributeTypeId: 'region', attributeValueId: 'emea' }]);
    expect(fixture.componentInstance.attributeInputs()).toEqual([{ attributeTypeId: 'region', attributeValueId: 'emea' }]);
    expect(root(fixture).textContent).toContain('New (staged)');
  });

  it('AC-GLA-015 keeps two staged attributes of different types', async () => {
    const fixture = await render({ stored: [ACTIVE_ACCOUNT.attributes[0]] });
    button(fixture, 'Add attribute').click();
    await settle(fixture);
    choose(select(fixture, 'Attribute type'), 'region');
    await settle(fixture);
    choose(select(fixture, 'Attribute value'), 'na');
    await settle(fixture);

    expect(fixture.componentInstance.changes()).toEqual([{ action: 'Add', attributeTypeId: 'region', attributeValueId: 'na' }]);
    expect(root(fixture).querySelectorAll('tbody tr').length).toBe(2);
  });

  it('clears the value when the type changes, because the value must belong to the type', async () => {
    const fixture = await render();
    button(fixture, 'Add attribute').click();
    await settle(fixture);
    choose(select(fixture, 'Attribute type'), 'region');
    await settle(fixture);
    choose(select(fixture, 'Attribute value'), 'emea');
    await settle(fixture);

    choose(select(fixture, 'Attribute type'), 'dept');
    await settle(fixture);

    expect(select(fixture, 'Attribute value').value).toBe('');
  });

  it('AC-GLA-017 stages an edit and a delete together, and Undo takes the delete back', async () => {
    const fixture = await render({ stored: ACTIVE_ACCOUNT.attributes });

    button(fixture, 'Mark Region attribute for delete').click();
    await settle(fixture);
    expect(fixture.componentInstance.changes()).toEqual([{ action: 'Remove', attributeId: 'att-2' }]);
    expect(root(fixture).textContent).toContain('Marked for delete');

    button(fixture, 'Edit Department attribute').click();
    await settle(fixture);
    choose(select(fixture, 'Attribute value'), 'sales');
    await settle(fixture);
    expect(fixture.componentInstance.changes()).toEqual([
      { action: 'Edit', attributeId: 'att-1', attributeTypeId: 'dept', attributeValueId: 'sales' },
      { action: 'Remove', attributeId: 'att-2' },
    ]);

    button(fixture, 'Undo delete of Region').click();
    await settle(fixture);
    expect(fixture.componentInstance.changes()).toEqual([
      { action: 'Edit', attributeId: 'att-1', attributeTypeId: 'dept', attributeValueId: 'sales' },
    ]);
  });

  it('Cancel edit restores the stored value and drops the staged edit', async () => {
    const fixture = await render({ stored: ACTIVE_ACCOUNT.attributes });
    button(fixture, 'Edit Department attribute').click();
    await settle(fixture);
    choose(select(fixture, 'Attribute value'), 'sales');
    await settle(fixture);

    button(fixture, 'Cancel edit').click();
    await settle(fixture);

    expect(fixture.componentInstance.hasPending()).toBe(false);
    expect(root(fixture).textContent).toContain('Finance');
  });

  it('AC-GLA-016 refuses an empty type or value and links the row message with aria-describedby', async () => {
    const fixture = await render();
    button(fixture, 'Add attribute').click();
    await settle(fixture);

    expect(fixture.componentInstance.validate()).toBe(false);
    await settle(fixture);

    const value = select(fixture, 'Attribute value');
    const message = root(fixture).querySelector('.row-error') as HTMLElement;
    expect(message.textContent).toContain('Choose an attribute type and a value.');
    expect(value.getAttribute('aria-describedby')).toBe(message.id);
    expect(value.getAttribute('aria-invalid')).toBe('true');
  });

  it('AC-GLA-018 puts the API error of one staged change on its row and keeps every staged row', async () => {
    const fixture = await render({ stored: ACTIVE_ACCOUNT.attributes });
    button(fixture, 'Mark Region attribute for delete').click();
    await settle(fixture);
    button(fixture, 'Add attribute').click();
    await settle(fixture);
    choose(select(fixture, 'Attribute type'), 'dept');
    await settle(fixture);
    choose(select(fixture, 'Attribute value'), 'sales');
    await settle(fixture);

    // changes()[1] is the Add row (the Remove is index 0).
    const used = fixture.componentInstance.applyServerErrors({
      'attributeChanges[1].attributeValueId': ["Attribute type 'dept' and value 'sales' combination is not valid in the attribute catalog."],
    });
    await settle(fixture);

    expect(used.length).toBe(1);
    expect(root(fixture).querySelector('.row-error')?.textContent).toContain('combination is not valid');
    expect(fixture.componentInstance.changes().length).toBe(2);
    expect(root(fixture).querySelectorAll('tbody tr').length).toBe(3);
  });

  describe('immediate mode', () => {
    it('AC-GLA-014 adds one attribute at once and reports the account ETag from the response', async () => {
      const fixture = await render({ accountId: '1000-100-01', immediate: true });
      const etags: string[] = [];
      fixture.componentInstance.etagChange.subscribe((e) => etags.push(e));
      button(fixture, 'Add attribute').click();
      await settle(fixture);
      choose(select(fixture, 'Attribute type'), 'project');
      await settle(fixture);
      choose(select(fixture, 'Attribute value'), 'proj-alpha');
      await settle(fixture);

      button(fixture, 'Save row').click();
      const req = http.expectOne('/api/gl/accounts/1000-100-01/attributes');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ attributeTypeId: 'project', attributeValueId: 'proj-alpha' });
      req.flush({ attributeId: 'att-9', attributeTypeId: 'project', attributeValueId: 'proj-alpha' }, { headers: { ETag: '"v2"' } });
      await settle(fixture);

      expect(etags).toEqual(['"v2"']);
      expect(root(fixture).textContent).toContain('Stored');
      expect(button(fixture, 'Edit Project attribute')).toBeDefined();
    });

    it('AC-GLA-016 shows the API field error on the row when the value does not belong to the type', async () => {
      const fixture = await render({ accountId: '1000-100-01', immediate: true });
      button(fixture, 'Add attribute').click();
      await settle(fixture);
      choose(select(fixture, 'Attribute type'), 'dept');
      await settle(fixture);
      choose(select(fixture, 'Attribute value'), 'sales');
      await settle(fixture);

      button(fixture, 'Save row').click();
      http
        .expectOne('/api/gl/accounts/1000-100-01/attributes')
        .flush({ errors: { attributeValueId: ['The value does not belong to the chosen attribute type.'] } }, { status: 400, statusText: 'Bad Request' });
      await settle(fixture);

      expect(root(fixture).querySelector('.row-error')?.textContent).toContain('does not belong to the chosen attribute type');
      expect(button(fixture, 'Save row').disabled).toBe(false);
    });

    it('AC-GLA-015 updates one attribute at once', async () => {
      const fixture = await render({ stored: ACTIVE_ACCOUNT.attributes, accountId: '1000-100-01', immediate: true });
      button(fixture, 'Edit Department attribute').click();
      await settle(fixture);
      choose(select(fixture, 'Attribute value'), 'sales');
      await settle(fixture);

      button(fixture, 'Save row').click();
      const req = http.expectOne('/api/gl/accounts/1000-100-01/attributes/att-1');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ attributeTypeId: 'dept', attributeValueId: 'sales' });
      req.flush({ attributeId: 'att-1', attributeTypeId: 'dept', attributeValueId: 'sales' }, { headers: { ETag: '"v3"' } });
      await settle(fixture);

      expect(fixture.componentInstance.hasPending()).toBe(false);
      expect(root(fixture).textContent).toContain('Sales');
    });

    it('AC-GLA-017 removes one attribute at once', async () => {
      const fixture = await render({ stored: ACTIVE_ACCOUNT.attributes, accountId: '1000-100-01', immediate: true });
      const etags: string[] = [];
      fixture.componentInstance.etagChange.subscribe((e) => etags.push(e));

      button(fixture, 'Delete Region attribute').click();
      const req = http.expectOne('/api/gl/accounts/1000-100-01/attributes/att-2');
      expect(req.request.method).toBe('DELETE');
      req.flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"v4"' } });
      await settle(fixture);

      expect(etags).toEqual(['"v4"']);
      expect(root(fixture).textContent).not.toContain('EMEA');
    });
  });
});
