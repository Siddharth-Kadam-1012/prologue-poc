import { Component, Input } from '@angular/core';
import { ProblemDetails } from '../core/models/account.model';

@Component({
  selector: 'app-validation-summary',
  template: `
    <div class="validation-summary" *ngIf="problem">
      <p class="error-title">{{ problem.title }}</p>
      <p *ngIf="problem.detail" class="error-detail">{{ problem.detail }}</p>
      <ul *ngIf="problem.errors">
        <li *ngFor="let field of errorFields">
          <strong>{{ field }}:</strong>
          <span *ngFor="let msg of problem.errors![field]"> {{ msg }}</span>
        </li>
      </ul>
    </div>
  `,
  styles: [`
    .validation-summary { background: #fff3f3; border: 1px solid #f5c6cb; padding: 12px; border-radius: 4px; margin-bottom: 16px; }
    .error-title { font-weight: 600; color: #721c24; margin: 0 0 4px; }
    .error-detail { color: #721c24; margin: 0 0 8px; }
    ul { margin: 0; padding-left: 20px; }
    li { color: #721c24; }
  `]
})
export class ValidationSummaryComponent {
  @Input() problem: ProblemDetails | null = null;

  get errorFields(): string[] {
    return this.problem?.errors ? Object.keys(this.problem.errors) : [];
  }
}
