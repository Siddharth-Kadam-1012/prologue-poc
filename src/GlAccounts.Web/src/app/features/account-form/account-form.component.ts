import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { AccountResponse, AccountType, AccountStatus, BalanceType, NormalBalance, ProblemDetails } from '../../core/models/account.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-account-form',
  template: `
    <div class="form-page">
      <h2>{{ isEdit ? 'Edit Account' : 'Create Account' }}</h2>

      <app-validation-summary [problem]="serverError"></app-validation-summary>

      <form [formGroup]="form" (ngSubmit)="submit()">

        <div class="field">
          <label>Account ID</label>
          <input formControlName="accountId" [readonly]="isEdit" placeholder="e.g. 01-300-1000" />
          <div *ngIf="isEdit" class="readonly-note">Permanent after creation — cannot be changed.</div>
          <div class="error" *ngIf="f['accountId'].touched && f['accountId'].invalid">Required, max 32 chars, format NN-NNN-NNNN</div>
        </div>

        <div class="field">
          <label>Description</label>
          <input formControlName="description" maxlength="200" />
          <div class="error" *ngIf="f['description'].touched && f['description'].invalid">Required</div>
        </div>

        <div class="field">
          <label>Account Type</label>
          <select formControlName="accountType" [attr.disabled]="isEdit ? true : null">
            <option value="">Select…</option>
            <option *ngFor="let t of accountTypes" [value]="t">{{ t }}</option>
          </select>
          <div *ngIf="isEdit" class="readonly-note">Permanent after creation — cannot be changed.</div>
          <div class="error" *ngIf="f['accountType'].touched && f['accountType'].invalid">Required</div>
        </div>

        <div class="field">
          <label>Category</label>
          <input formControlName="category" maxlength="50" />
          <div class="error" *ngIf="f['category'].touched && f['category'].invalid">Required</div>
        </div>

        <div class="field">
          <label>Status</label>
          <select formControlName="status">
            <option *ngFor="let s of statuses" [value]="s">{{ s }}</option>
          </select>
        </div>

        <div class="field">
          <label>Balance Type</label>
          <select formControlName="balanceType">
            <option *ngFor="let b of balanceTypes" [value]="b">{{ b }}</option>
          </select>
        </div>

        <div class="field">
          <label>Normal Balance</label>
          <select formControlName="normalBalance">
            <option *ngFor="let n of normalBalances" [value]="n">{{ n }}</option>
          </select>
        </div>

        <div class="field">
          <label>Currency ID (optional)</label>
          <input formControlName="currencyId" maxlength="10" placeholder="e.g. USD" />
        </div>

        <div class="form-actions">
          <button type="submit" class="btn-primary" [disabled]="submitting">
            {{ submitting ? 'Saving…' : (isEdit ? 'Save Changes' : 'Create Account') }}
          </button>
          <button type="button" class="btn-secondary" (click)="cancel()">Cancel</button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .form-page { max-width:560px; }
    h2 { margin-bottom:16px; }
    .field { margin-bottom:14px; display:flex; flex-direction:column; gap:4px; }
    label { font-weight:600; font-size:14px; }
    input, select { padding:8px; border:1px solid #e5e7eb; border-radius:4px; font-size:14px; }
    .error { color:#721c24; font-size:12px; }
    .readonly-note { color:#57606a; font-size:12px; font-style:italic; }
    .form-actions { display:flex; gap:8px; margin-top:16px; }
    .btn-primary { background:#0060a9; color:#fff; border:none; padding:8px 20px; border-radius:4px; cursor:pointer; }
    .btn-primary:disabled { opacity:0.6; cursor:not-allowed; }
    .btn-secondary { background:#f7f8fa; border:1px solid #e5e7eb; padding:8px 20px; border-radius:4px; cursor:pointer; }
  `]
})
export class AccountFormComponent implements OnInit {
  form!: FormGroup;
  isEdit = false;
  accountId = '';
  submitting = false;
  serverError: ProblemDetails | null = null;

  accountTypes: AccountType[] = ['Financial', 'Statistical'];
  statuses: AccountStatus[]   = ['Active', 'Hold', 'Inactive', 'Template'];
  balanceTypes: BalanceType[] = ['BalanceForward', 'Periodic'];
  normalBalances: NormalBalance[] = ['Debit', 'Credit'];

  constructor(
    private fb: FormBuilder,
    private svc: GlAccountsService,
    private router: Router,
    private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.accountId = this.route.snapshot.paramMap.get('id') ?? '';
    this.isEdit = !!this.accountId;

    this.form = this.fb.group({
      accountId:     [{ value: '', disabled: this.isEdit }, [Validators.required, Validators.maxLength(32), Validators.pattern(/^\d{2}-\d{3}-\d{4}$/)]],
      description:   ['', [Validators.required, Validators.maxLength(200)]],
      accountType:   [{ value: 'Financial', disabled: this.isEdit }, Validators.required],
      category:      ['', [Validators.required, Validators.maxLength(50)]],
      status:        ['Active', Validators.required],
      balanceType:   ['BalanceForward', Validators.required],
      normalBalance: ['Debit', Validators.required],
      currencyId:    ['']
    });

    if (this.isEdit) {
      this.svc.getAccount(this.accountId).subscribe({
        next: a => this.form.patchValue(a),
        error: () => { this.serverError = { type: '', title: 'Account not found.', status: 404 }; }
      });
    }
  }

  get f() { return this.form.controls; }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.serverError = null;
    const val = this.form.getRawValue();

    const obs = this.isEdit
      ? this.svc.updateAccount(this.accountId, {
          description: val.description, category: val.category,
          status: val.status, balanceType: val.balanceType,
          normalBalance: val.normalBalance, currencyId: val.currencyId || undefined
        })
      : this.svc.createAccount({
          accountId: val.accountId, description: val.description,
          accountType: val.accountType, category: val.category,
          status: val.status, balanceType: val.balanceType,
          normalBalance: val.normalBalance, currencyId: val.currencyId || undefined
        });

    obs.subscribe({
      next: (a: AccountResponse) => this.router.navigate(['/gl/accounts', a.accountId]),
      error: (err: HttpErrorResponse) => {
        this.serverError = err.error as ProblemDetails;
        this.submitting = false;
      }
    });
  }

  cancel(): void {
    this.isEdit
      ? this.router.navigate(['/gl/accounts', this.accountId])
      : this.router.navigate(['/gl/accounts']);
  }
}
