import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { ProblemDetails } from '../../core/models/account.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-copy-template-dialog',
  template: `
    <div class="dialog-overlay">
      <div class="dialog">
        <h3 class="dialog-title">Copy from Template</h3>
        <p class="dialog-subtitle">Creating a copy of account <strong>{{ sourceAccountId }}</strong></p>

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="newAccountId">New Account ID <span class="required">*</span></label>
            <input id="newAccountId" formControlName="newAccountId"
                   placeholder="e.g. 01-100-9999" class="input-field" />
            <div class="field-error"
                 *ngIf="form.get('newAccountId')?.invalid && form.get('newAccountId')?.touched">
              Account ID is required.
            </div>
          </div>

          <div class="field">
            <label for="description">Description <span class="required">*</span></label>
            <input id="description" formControlName="description"
                   placeholder="Account description" class="input-field" />
            <div class="field-error"
                 *ngIf="form.get('description')?.invalid && form.get('description')?.touched">
              Description is required.
            </div>
          </div>

          <div class="api-error" *ngIf="apiError">{{ apiError }}</div>

          <div class="dialog-actions">
            <button type="submit" class="btn-primary" [disabled]="submitting">
              {{ submitting ? 'Copying…' : 'Copy Account' }}
            </button>
            <button type="button" class="btn-secondary" (click)="cancel()">Cancel</button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .dialog-overlay { position:fixed; inset:0; background:rgba(0,0,0,.4); display:flex; align-items:center; justify-content:center; z-index:1000; }
    .dialog { background:#fff; border-radius:6px; padding:28px 32px; width:420px; max-width:95vw; }
    .dialog-title { margin:0 0 4px; font-size:18px; }
    .dialog-subtitle { color:#57606a; margin:0 0 20px; font-size:14px; }
    .field { display:flex; flex-direction:column; gap:4px; margin-bottom:16px; }
    .field label { font-size:13px; font-weight:600; color:#1f2328; }
    .required { color:#721c24; }
    .input-field { padding:8px 10px; border:1px solid #e5e7eb; border-radius:4px; font-size:14px; }
    .field-error { color:#721c24; font-size:12px; }
    .api-error { background:#f8d7da; border:1px solid #f5c6cb; padding:8px 12px; border-radius:4px; color:#721c24; font-size:13px; margin-bottom:16px; }
    .dialog-actions { display:flex; gap:8px; justify-content:flex-end; margin-top:8px; }
    .btn-primary  { background:#0060a9; color:#fff; border:none; padding:8px 18px; border-radius:4px; cursor:pointer; font-size:14px; }
    .btn-primary:disabled { opacity:.6; cursor:not-allowed; }
    .btn-secondary { background:#f7f8fa; border:1px solid #e5e7eb; padding:8px 18px; border-radius:4px; cursor:pointer; font-size:14px; }
  `]
})
export class CopyTemplateDialogComponent implements OnInit {
  @Input() sourceAccountId = '';

  form!: FormGroup;
  submitting = false;
  apiError: string | null = null;

  constructor(
    private fb: FormBuilder,
    private svc: GlAccountsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      newAccountId: ['', [Validators.required]],
      description:  ['', [Validators.required]]
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting = true;
    this.apiError   = null;

    this.svc.copyFromTemplate(this.sourceAccountId, this.form.value).subscribe({
      next: account => {
        this.router.navigate(['/gl/accounts', account.accountId]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting = false;
        const problem = err.error as ProblemDetails;
        this.apiError = problem?.detail ?? 'Copy failed.';
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/gl/accounts', this.sourceAccountId]);
  }
}
