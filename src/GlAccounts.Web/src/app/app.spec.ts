import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('App shell', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient()],
    }).compileComponents();
  });

  it('has a skip link, main navigation and a main landmark', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('a.skip-link')?.getAttribute('href')).toBe('#main');
    expect(root.querySelector('nav[aria-label="Main"]')).not.toBeNull();
    expect(root.querySelector('main#main')).not.toBeNull();
  });

  it('links to the accounts and template routes', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const hrefs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('nav a')).map((a) => a.getAttribute('href'));

    expect(hrefs).toEqual(['/gl/accounts', '/gl/account-templates']);
  });

  it('has a polite status region and an assertive alert region', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[role="status"][aria-live="polite"]')).not.toBeNull();
    expect(root.querySelector('[role="alert"][aria-live="assertive"]')).not.toBeNull();
  });
});
