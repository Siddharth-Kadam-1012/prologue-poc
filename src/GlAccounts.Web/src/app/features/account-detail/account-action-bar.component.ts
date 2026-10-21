import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { ProblemDetails } from '../../core/models/account.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-account-action-bar',
  template: `
    <div class="action-bar">
      <button class="btn-secondary" (click)="onEdit()">Edit</button>
      <button class="btn-secondary" (click)="onCopy()">Copy from Template</button>
      <button class="btn-danger"    (click)="onDelete()">Delete</button>
    </div>
    <div *ngIf="deleteError" class="delete-error">{{ deleteError }}</div>
  `,
  styles: [`
    .action-bar { display:flex; gap:8px; }
    .btn-secondary { background:#f7f8fa; border:1px solid #e5e7eb; padding:6px 14px; border-radius:4px; cursor:pointer; }
    .btn-danger    { background:#f8d7da; border:1px solid #f5c6cb; padding:6px 14px; border-radius:4px; cursor:pointer; color:#721c24; }
    .delete-error  { background:#f8d7da; border:1px solid #f5c6cb; padding:8px 12px; border-radius:4px; margin-top:8px; color:#721c24; font-size:14px; }
  `]
})
export class AccountActionBarComponent {
  @Input() accountId = '';
  @Output() deleted = new EventEmitter<void>();

  deleteError: string | null = null;

  constructor(private svc: GlAccountsService, private router: Router) {}

  onEdit(): void {
    this.router.navigate(['/gl/accounts', this.accountId, 'edit']);
  }

  onCopy(): void {
    this.router.navigate(['/gl/accounts', this.accountId, 'copy']);
  }

  onDelete(): void {
    if (!confirm(`Delete account ${this.accountId}? This cannot be undone.`)) return;
    this.deleteError = null;
    this.svc.deleteAccount(this.accountId).subscribe({
      next: () => {
        this.deleted.emit();
        this.router.navigate(['/gl/accounts']);
      },
      error: (err: HttpErrorResponse) => {
        const problem = err.error as ProblemDetails;
        this.deleteError = problem?.detail ?? 'Delete failed.';
      }
    });
  }
}
