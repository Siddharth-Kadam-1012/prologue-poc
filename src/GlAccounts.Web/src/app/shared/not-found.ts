import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from './page-header';

@Component({
  selector: 'app-not-found',
  imports: [PageHeader, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="Page not found" />
    <p>There is nothing at this address. <a routerLink="/gl/accounts">Go to accounts</a>.</p>
  `,
})
export class NotFound {}
