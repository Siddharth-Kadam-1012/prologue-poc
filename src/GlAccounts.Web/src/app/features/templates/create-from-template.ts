import { ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, computed, effect, inject, input, output, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiError, toApiError } from '../../core/api-error';
import { Announcer } from '../../core/announcer';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import {
  ACCOUNT_STATUSES,
  Account,
  AccountStatus,
  AccountSummary,
  BALANCE_TYPES,
  BalanceType,
  CURRENCIES,
  Category,
  CreateAccountRequest,
  CurrencyId,
  FINANCIAL_CATEGORIES,
  NORMAL_BALANCES,
  NormalBalance,
} from '../../core/models';
import { ErrorBanner } from '../../shared/error-banner';

interface SummaryItem {
  /** Id of the element to focus, or null for a message that belongs to no field. */
  id: string | null;
  message: string;
}

const LABELS: Record<string, string> = {
  accountId: 'New Account ID',
  description: 'Description',
  category: 'Category',
  currencyId: 'Currency',
  status: 'Status',
  balanceType: 'Balance type',
  normalBalance: 'Normal balance',
};

const FIELD_ORDER = ['accountId', 'description', 'category', 'currencyId', 'status', 'balanceType', 'normalBalance'] as const;

/**
 * UX-GLA-006 (panel): create an account from a template (Flow 8, REQ-GLA-033). Stub data only.
 * The template's settings are loaded (API-GLA-002) and prefilled; every one of them is editable before saving.
 * Errors from the API (duplicate, invalid segments, source no longer a template) are shown as in Flow 1.
 */
@Component({
  selector: 'app-create-from-template',
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, ErrorBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel" aria-labelledby="tpl-heading" [attr.aria-busy]="loading()">
      <h2 id="tpl-heading" tabindex="-1" #heading>Create account from template {{ template().accountId }}</h2>

      @if (loading()) {
        <p role="status">Loading template settings…</p>
      } @else if (loadError(); as failure) {
        <app-error-banner [error]="failure" actionLabel="Try again" (action)="loadTemplate(template().accountId)" />
        <button mat-stroked-button type="button" (click)="closed.emit()">Cancel</button>
      } @else {
        @if (saveError(); as failure) {
          <app-error-banner [error]="failure" />
        }
        @if (summary().length > 0) {
          <div #summaryBox class="error-summary" role="alert" tabindex="-1">
            <h3>There is a problem</h3>
            <ul>
              @for (item of summary(); track $index) {
                <li>
                  @if (item.id) {
                    <a [href]="'#' + item.id" (click)="focusField($event, item.id)">{{ item.message }}</a>
                  } @else {
                    {{ item.message }}
                  }
                </li>
              }
            </ul>
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate aria-label="Create account from template">
          <div class="fields">
            <mat-form-field>
              <mat-label>New Account ID</mat-label>
              <input matInput id="tpl-field-accountId" formControlName="accountId" autocomplete="off" />
              <mat-hint>Format 9999-999-99 (4-3-2 digits), for example 1000-100-02</mat-hint>
              <mat-error>{{ message('accountId') }}</mat-error>
            </mat-form-field>
            @if (duplicateOf(); as existing) {
              <p class="duplicate"><a [routerLink]="['/gl/accounts', existing]">View existing account {{ existing }}</a></p>
            }

            <p class="copied">Copied settings (editable before saving). Account type {{ source()?.accountType }} is copied and permanent.</p>

            <mat-form-field>
              <mat-label>Description</mat-label>
              <input matInput id="tpl-field-description" formControlName="description" autocomplete="off" />
              <mat-error>{{ message('description') }}</mat-error>
            </mat-form-field>
            <mat-form-field>
              <mat-label>Category</mat-label>
              <mat-select id="tpl-field-category" formControlName="category">
                @for (category of categoryOptions(); track category) {
                  <mat-option [value]="category">{{ category }}</mat-option>
                }
              </mat-select>
              <mat-error>{{ message('category') }}</mat-error>
            </mat-form-field>
            <mat-form-field>
              <mat-label>Currency</mat-label>
              <mat-select id="tpl-field-currencyId" formControlName="currencyId">
                @for (currency of currencies; track currency) {
                  <mat-option [value]="currency">{{ currency }}</mat-option>
                }
              </mat-select>
              <mat-error>{{ message('currencyId') }}</mat-error>
            </mat-form-field>
            <mat-form-field>
              <mat-label>Status</mat-label>
              <mat-select id="tpl-field-status" formControlName="status">
                @for (status of statuses; track status) {
                  <mat-option [value]="status">{{ status }}</mat-option>
                }
              </mat-select>
              <mat-error>{{ message('status') }}</mat-error>
            </mat-form-field>
            @if (isFinancial()) {
              <mat-form-field>
                <mat-label>Balance type</mat-label>
                <mat-select id="tpl-field-balanceType" formControlName="balanceType">
                  @for (type of balanceTypes; track type) {
                    <mat-option [value]="type">{{ type }}</mat-option>
                  }
                </mat-select>
                <mat-error>{{ message('balanceType') }}</mat-error>
              </mat-form-field>
              <mat-form-field>
                <mat-label>Normal balance</mat-label>
                <mat-select id="tpl-field-normalBalance" formControlName="normalBalance">
                  @for (balance of normalBalances; track balance) {
                    <mat-option [value]="balance">{{ balance }}</mat-option>
                  }
                </mat-select>
                <mat-error>{{ message('normalBalance') }}</mat-error>
              </mat-form-field>
            }
          </div>
          <div class="actions">
            <button mat-flat-button type="submit" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button>
            <button mat-stroked-button type="button" (click)="closed.emit()">Cancel</button>
          </div>
        </form>
      }
    </section>
  `,
  styles: `
    .panel {
      margin-top: 24px;
      padding: 16px;
      border: 2px solid var(--mat-sys-outline);
      border-radius: 8px;
    }
    .fields {
      display: flex;
      flex-direction: column;
      max-width: 560px;
    }
    .actions {
      display: flex;
      gap: 8px;
      margin-top: 16px;
    }
    .copied {
      color: var(--mat-sys-on-surface-variant);
    }
    .error-summary {
      margin-bottom: 16px;
      padding: 12px 16px;
      border: 2px solid var(--mat-sys-error);
      border-radius: 8px;
    }
    .error-summary h3 {
      margin: 0 0 8px;
      font: var(--mat-sys-title-medium);
    }
    .duplicate {
      margin: 0 0 16px;
    }
  `,
})
export class CreateFromTemplate {
  readonly template = input.required<AccountSummary>();
  readonly closed = output<void>();

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly api = inject(GlAccountsApi);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly announcer = inject(Announcer);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly currencies = CURRENCIES;
  protected readonly statuses = ACCOUNT_STATUSES;
  protected readonly balanceTypes = BALANCE_TYPES;
  protected readonly normalBalances = NORMAL_BALANCES;

  protected readonly form = this.fb.group({
    accountId: ['', [Validators.required, Validators.maxLength(32), Validators.pattern(/\S/)]],
    description: ['', [Validators.required, Validators.maxLength(200), Validators.pattern(/\S/)]],
    category: ['' as Category | '', Validators.required],
    currencyId: ['USD' as CurrencyId, Validators.required],
    status: ['Active' as AccountStatus, Validators.required],
    balanceType: ['' as BalanceType | '', Validators.required],
    normalBalance: ['' as NormalBalance | '', Validators.required],
  });

  protected readonly loading = signal(true);
  protected readonly loadError = signal<ApiError | null>(null);
  protected readonly source = signal<Account | null>(null);
  protected readonly saving = signal(false);
  protected readonly saveError = signal<ApiError | null>(null);
  protected readonly summary = signal<SummaryItem[]>([]);
  protected readonly duplicateOf = signal<string | null>(null);

  private readonly heading = viewChild<ElementRef<HTMLElement>>('heading');
  private readonly summaryBox = viewChild<ElementRef<HTMLElement>>('summaryBox');

  protected readonly isFinancial = computed(() => this.source()?.accountType === 'Financial');
  protected readonly categoryOptions = computed<readonly Category[]>(() =>
    this.isFinancial() ? FINANCIAL_CATEGORIES : ['Statistical'],
  );

  constructor() {
    this.form.controls.accountId.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.duplicateOf.set(null));
    effect(() => {
      const id = this.template().accountId;
      untracked(() => this.loadTemplate(id));
    });
  }

  protected loadTemplate(templateId: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.api.getAccount(templateId).subscribe({
      next: ({ body }) => {
        this.source.set(body);
        this.form.reset(
          {
            accountId: '',
            description: body.description,
            category: body.category,
            currencyId: body.currencyId,
            status: 'Active',
            balanceType: body.balanceType ?? '',
            normalBalance: body.normalBalance ?? '',
          },
          { emitEvent: false },
        );
        this.summary.set([]);
        this.loading.set(false);
        afterNextRender(() => this.heading()?.nativeElement.focus(), { injector: this.injector });
      },
      error: (e: unknown) => {
        this.loadError.set(toApiError(e));
        this.loading.set(false);
      },
    });
  }

  protected message(name: string): string {
    const errors = this.form.get(name)?.errors;
    if (!errors) return '';
    if (errors['server']) return String(errors['server']);
    const label = LABELS[name] ?? name;
    if (errors['required'] || errors['pattern']) return `${label} is required.`;
    if (errors['maxlength']) return `${label} must be at most ${errors['maxlength'].requiredLength} characters.`;
    return `${label} is not valid.`;
  }

  protected submit(): void {
    if (this.saving()) return;
    this.saveError.set(null);
    if (!this.isFinancial()) {
      this.form.controls.balanceType.setErrors(null);
      this.form.controls.normalBalance.setErrors(null);
    }
    this.form.markAllAsTouched();
    if (this.invalidFields().length > 0) {
      this.showSummary();
      return;
    }
    this.summary.set([]);
    this.saving.set(true);

    const raw = this.form.getRawValue();
    const source = this.source()!;
    const request: CreateAccountRequest = {
      accountId: raw.accountId.trim(),
      templateAccountId: source.accountId,
      description: raw.description,
      accountType: source.accountType,
      category: raw.category as Category,
      currencyId: raw.currencyId,
      status: raw.status,
      ...(source.accountType === 'Financial'
        ? { balanceType: raw.balanceType as BalanceType, normalBalance: raw.normalBalance as NormalBalance }
        : {}),
    };
    this.api.createAccount(request).subscribe({
      next: ({ body }) => {
        this.saving.set(false);
        this.snackBar.open(`Account ${body.accountId} created from template ${source.accountId}.`, 'Dismiss', {
          duration: 6000,
          politeness: 'polite',
        });
        void this.router.navigate(['/gl/accounts', body.accountId]);
      },
      error: (e: unknown) => this.fail(e, request.accountId),
    });
  }

  private invalidFields(): (typeof FIELD_ORDER)[number][] {
    return FIELD_ORDER.filter((name) => {
      const control = this.form.controls[name];
      if (!this.isFinancial() && (name === 'balanceType' || name === 'normalBalance')) return false;
      return !!control.errors;
    });
  }

  private fail(e: unknown, accountId: string): void {
    const error = toApiError(e);
    this.saving.set(false);
    switch (error.kind) {
      case 'validation':
        this.applyFieldErrors(error);
        break;
      case 'duplicate':
        // AC-GLA-003 as in Flow 1: shown at the Account ID field, and the existing account is identifiable.
        this.form.controls.accountId.setErrors({ server: error.message });
        this.duplicateOf.set(accountId);
        this.showSummary();
        break;
      default:
        // 404 (the template was removed) and server errors keep the form values.
        this.saveError.set(error);
        this.announcer.alert(error.message);
    }
  }

  private applyFieldErrors(error: ApiError): void {
    const extras: SummaryItem[] = [];
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const control = this.form.get(field);
      if (control) {
        control.setErrors({ server: messages.join(' ') });
      } else {
        // For example `templateAccountId`: the source is no longer a template (AC-GLA-054).
        extras.push(...messages.map((message) => ({ id: null, message })));
      }
    }
    this.showSummary(extras);
  }

  private showSummary(extra: SummaryItem[] = []): void {
    const items: SummaryItem[] = this.invalidFields().map((name) => ({ id: `tpl-field-${name}`, message: this.message(name) }));
    const all = [...items, ...extra];
    this.summary.set(all);
    if (all.length > 0) {
      this.announcer.alert(`There ${all.length === 1 ? 'is 1 problem' : `are ${all.length} problems`} with this form.`);
      afterNextRender(() => this.summaryBox()?.nativeElement.focus(), { injector: this.injector });
    }
  }

  protected focusField(event: Event, id: string): void {
    event.preventDefault();
    this.host.nativeElement.querySelector<HTMLElement>(`#${id}`)?.focus();
  }
}
