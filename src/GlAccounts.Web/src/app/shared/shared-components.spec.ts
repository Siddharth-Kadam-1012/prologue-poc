import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ApiError } from '../core/api-error';
import { DescriptionList } from './description-list';
import { EmptyState } from './empty-state';
import { ErrorBanner } from './error-banner';
import { PageHeader } from './page-header';
import { StatusBadge } from './status-badge';

const apiError = (overrides: Partial<ApiError>): ApiError => ({
  kind: 'server',
  status: 500,
  message: 'Boom',
  fieldErrors: {},
  blockers: [],
  ...overrides,
});

@Component({
  imports: [PageHeader, EmptyState, ErrorBanner, StatusBadge, DescriptionList],
  template: `
    <app-page-header title="Accounts" subtitle="Stub data only" />
    <app-empty-state message="No groups." />
    <app-error-banner [error]="error" actionLabel="Reload" (action)="reloaded = true" />
    <app-status-badge status="Hold" />
    <app-description-list [items]="items" />
  `,
})
class Host {
  error = apiError({ kind: 'staleVersion', status: 409, message: 'changed by someone else' });
  items = [{ label: 'Currency', value: 'USD' }];
  reloaded = false;
}

describe('shared components', () => {
  async function render() {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    return fixture;
  }

  it('PageHeader renders one h1 and the subtitle', async () => {
    const root = (await render()).nativeElement as HTMLElement;

    expect(root.querySelectorAll('h1').length).toBe(1);
    expect(root.querySelector('h1')?.textContent).toBe('Accounts');
    expect(root.querySelector('.subtitle')?.textContent).toBe('Stub data only');
  });

  it('EmptyState shows its message as text', async () => {
    const root = (await render()).nativeElement as HTMLElement;

    expect(root.querySelector('app-empty-state')?.textContent).toContain('No groups.');
  });

  it('ErrorBanner is an alert with the server message and an action (AC-GLA-056)', async () => {
    const fixture = await render();
    const root = fixture.nativeElement as HTMLElement;

    const banner = root.querySelector('[role="alert"]');
    expect(banner?.textContent).toContain('changed by someone else');
    expect(banner?.textContent).toContain('This account was changed by someone else.');

    (banner?.querySelector('button') as HTMLButtonElement).click();
    expect(fixture.componentInstance.reloaded).toBe(true);
  });

  it('StatusBadge shows the status as text (AC-GLA-021: not colour alone)', async () => {
    const root = (await render()).nativeElement as HTMLElement;

    expect(root.querySelector('app-status-badge')?.textContent?.trim()).toBe('Hold');
  });

  it('DescriptionList uses a real dl with term and description', async () => {
    const root = (await render()).nativeElement as HTMLElement;

    expect(root.querySelector('dl dt')?.textContent).toBe('Currency');
    expect(root.querySelector('dl dd')?.textContent).toBe('USD');
  });
});

describe('ErrorBanner headings', () => {
  it('has no action button unless an action label is given', async () => {
    @Component({ imports: [ErrorBanner], template: `<app-error-banner [error]="error" />` })
    class NoAction {
      error = apiError({ kind: 'network', status: 0, message: 'down' });
    }
    const fixture = TestBed.createComponent(NoAction);
    await fixture.whenStable();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('button')).toBeNull();
    expect(root.textContent).toContain('Service unavailable.');
  });
});
