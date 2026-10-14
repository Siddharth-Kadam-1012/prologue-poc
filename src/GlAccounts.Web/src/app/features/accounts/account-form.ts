import { ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, computed, effect, inject, input, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiError, ConflictMeaning, toApiError } from '../../core/api-error';
import { Announcer } from '../../core/announcer';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import {
  ACCOUNT_STATUSES,
  ACCOUNT_TYPES,
  Account,
  AccountAttribute,
  AccountStatus,
  AccountType,
  BALANCE_TYPES,
  BalanceType,
  CURRENCIES,
  Category,
  CreateAccountRequest,
  CurrencyId,
  FINANCIAL_CATEGORIES,
  NORMAL_BALANCES,
  NormalBalance,
  UpdateAccountRequest,
} from '../../core/models';
import { DescriptionItem, DescriptionList } from '../../shared/description-list';
import { EmptyState } from '../../shared/empty-state';
import { ErrorBanner } from '../../shared/error-banner';
import { PageHeader } from '../../shared/page-header';
import { AttributePanel } from './attribute-panel';

interface SummaryItem {
  /** Id of the element to focus, or null for a message that belongs to no field. */
  id: string | null;
  message: string;
}

const LABELS: Record<string, string> = {
  accountId: 'Account ID',
  accountType: 'Account type',
  description: 'Description',
  category: 'Category',
  currencyId: 'Currency',
  status: 'Status',
  statusJustification: 'Justification',
  balanceType: 'Balance type',
  normalBalance: 'Normal balance',
};

const FIELD_ORDER = [
  'accountId',
  'accountType',
  'description',
  'category',
  'currencyId',
  'status',
  'statusJustification',
  'balanceType',
  'normalBalance',
] as const;

/**
 * UX-GLA-001: account create / edit form (Flows 1, 2 and 4). Stub data only.
 * Validation messages come from the API after submit (NFR-GLA-006, no as-you-type segment validation);
 * only required and length checks run in the browser.
 */
@Component({
  selector: 'app-account-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    PageHeader,
    DescriptionList,
    EmptyState,
    ErrorBanner,
    AttributePanel,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <p role="status" aria-busy="true">Loading account…</p>
    } @else if (loadError(); as failure) {
      @if (failure.kind === 'notFound') {
        <app-page-header title="Account not found" />
        <app-empty-state [message]="failure.message">
          <a mat-stroked-button routerLink="/gl/accounts">Search accounts</a>
        </app-empty-state>
      } @else {
        <app-error-banner [error]="failure" actionLabel="Try again" (action)="loadAccount(id()!)" />
      }
    } @else {
      <app-page-header [title]="isEdit() ? 'Edit account ' + id() : 'New account'" />

      @if (stale(); as conflict) {
        <app-error-banner [error]="conflict" actionLabel="Reload latest version" (action)="reload()" />
      } @else if (saveError(); as failure) {
        <app-error-banner [error]="failure" />
      }
      @if (notice(); as text) {
        <p class="notice" role="status">{{ text }}</p>
      }

      @if (summary().length > 0) {
        <div #summaryBox class="error-summary" role="alert" tabindex="-1">
          <h2>There is a problem</h2>
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

      <form [formGroup]="form" (ngSubmit)="submit()" (keydown.enter)="blockEnter($event)" novalidate aria-label="Account form">
        <div class="fields">
          @if (isEdit()) {
            <app-description-list [items]="readOnlyItems()" />
          } @else {
            <mat-form-field>
              <mat-label>Account ID</mat-label>
              <input matInput id="field-accountId" formControlName="accountId" autocomplete="off" />
              <mat-hint>Format 9999-999-99 (4-3-2 digits), for example 1000-100-02</mat-hint>
              <mat-error>{{ message('accountId') }}</mat-error>
            </mat-form-field>
            @if (duplicateOf(); as existing) {
              <p class="duplicate">
                <a [routerLink]="['/gl/accounts', existing]">View existing account {{ existing }}</a>
              </p>
            }
            <mat-form-field>
              <mat-label>Account type</mat-label>
              <mat-select id="field-accountType" formControlName="accountType">
                @for (type of accountTypes; track type) {
                  <mat-option [value]="type">{{ type }}</mat-option>
                }
              </mat-select>
              <mat-hint>Permanent: it cannot be changed after the account is created</mat-hint>
              <mat-error>{{ message('accountType') }}</mat-error>
            </mat-form-field>
          }

          <mat-form-field>
            <mat-label>Description</mat-label>
            <input matInput id="field-description" formControlName="description" autocomplete="off" />
            <mat-error>{{ message('description') }}</mat-error>
          </mat-form-field>

          <mat-form-field>
            <mat-label>Category</mat-label>
            <mat-select id="field-category" formControlName="category">
              @for (category of categoryOptions(); track category) {
                <mat-option [value]="category">{{ category }}</mat-option>
              }
            </mat-select>
            <mat-error>{{ message('category') }}</mat-error>
          </mat-form-field>

          <mat-form-field>
            <mat-label>Currency</mat-label>
            <mat-select id="field-currencyId" formControlName="currencyId">
              @for (currency of currencies; track currency) {
                <mat-option [value]="currency">{{ currency }}</mat-option>
              }
            </mat-select>
            <mat-error>{{ message('currencyId') }}</mat-error>
          </mat-form-field>

          <mat-form-field>
            <mat-label>Status</mat-label>
            <mat-select id="field-status" formControlName="status">
              @for (status of statusOptions(); track status) {
                <mat-option [value]="status">{{ status }}</mat-option>
              }
            </mat-select>
            <mat-error>{{ message('status') }}</mat-error>
          </mat-form-field>

          @if (justificationNeeded()) {
            <mat-form-field>
              <mat-label>Justification for the status change</mat-label>
              <textarea matInput id="field-statusJustification" formControlName="statusJustification" rows="3"></textarea>
              <mat-hint>Required, 1 to 500 characters</mat-hint>
              <mat-error>{{ message('statusJustification') }}</mat-error>
            </mat-form-field>
          }

          @if (!isEdit() && isFinancial()) {
            <mat-form-field>
              <mat-label>Balance type</mat-label>
              <mat-select id="field-balanceType" formControlName="balanceType">
                @for (type of balanceTypes; track type) {
                  <mat-option [value]="type">{{ type }}</mat-option>
                }
              </mat-select>
              <mat-error>{{ message('balanceType') }}</mat-error>
            </mat-form-field>
            <mat-form-field>
              <mat-label>Normal balance</mat-label>
              <mat-select id="field-normalBalance" formControlName="normalBalance">
                @for (balance of normalBalances; track balance) {
                  <mat-option [value]="balance">{{ balance }}</mat-option>
                }
              </mat-select>
              <mat-error>{{ message('normalBalance') }}</mat-error>
            </mat-form-field>
          }
        </div>

        @if (isEdit()) {
          <mat-checkbox
            class="immediate"
            [checked]="immediate()"
            [disabled]="panel()?.hasPending() ?? false"
            (change)="immediate.set($event.checked)"
          >
            Apply each attribute change immediately (otherwise attribute changes are saved with the account)
          </mat-checkbox>
        }

        <app-attribute-panel
          [stored]="attributesBase()"
          [accountId]="id() ?? null"
          [immediate]="isEdit() && immediate()"
          (etagChange)="etag = $event"
        />

        <div class="actions">
          <button mat-flat-button type="submit" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save' }}</button>
          <a mat-stroked-button [routerLink]="cancelLink()">Cancel</a>
        </div>
      </form>
    }
  `,
  styles: `
    .fields {
      display: flex;
      flex-direction: column;
      max-width: 560px;
    }
    .actions {
      display: flex;
      gap: 8px;
      margin-top: 24px;
    }
    .immediate {
      display: block;
      margin-top: 16px;
    }
    .error-summary {
      margin-bottom: 16px;
      padding: 12px 16px;
      border: 2px solid var(--mat-sys-error);
      border-radius: 8px;
    }
    .error-summary h2 {
      margin: 0 0 8px;
      font: var(--mat-sys-title-medium);
    }
    .notice {
      padding: 12px 16px;
      border: 1px solid var(--mat-sys-outline);
      border-radius: 8px;
    }
    .duplicate {
      margin: 0 0 16px;
    }
  `,
})
export class AccountForm {
  /** Route parameter `:id`; absent on `/gl/accounts/new`. */
  readonly id = input<string>();
  /** Query parameter `?status=`, sent by the delete dialog ("Set status to Inactive instead"). */
  readonly status = input<string>();

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly api = inject(GlAccountsApi);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly announcer = inject(Announcer);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly accountTypes = ACCOUNT_TYPES;
  protected readonly currencies = CURRENCIES;
  protected readonly balanceTypes = BALANCE_TYPES;
  protected readonly normalBalances = NORMAL_BALANCES;

  protected readonly form = this.fb.group({
    accountId: ['', [Validators.required, Validators.maxLength(32), Validators.pattern(/\S/)]],
    accountType: ['Financial' as AccountType, Validators.required],
    description: ['', [Validators.required, Validators.maxLength(200), Validators.pattern(/\S/)]],
    category: ['' as Category | '', Validators.required],
    currencyId: ['USD' as CurrencyId, Validators.required],
    status: ['Active' as AccountStatus, Validators.required],
    statusJustification: [''],
    balanceType: ['' as BalanceType | '', Validators.required],
    normalBalance: ['' as NormalBalance | '', Validators.required],
  });

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(true);
  protected readonly loadError = signal<ApiError | null>(null);
  protected readonly saving = signal(false);
  protected readonly saveError = signal<ApiError | null>(null);
  protected readonly stale = signal<ApiError | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly summary = signal<SummaryItem[]>([]);
  protected readonly duplicateOf = signal<string | null>(null);

  /** The stored account (edit only). Its status is the baseline for the justification rule. */
  protected readonly original = signal<Account | null>(null);
  protected readonly attributesBase = signal<AccountAttribute[]>([]);
  protected readonly immediate = signal(true);
  protected readonly accountType = signal<AccountType>('Financial');
  protected readonly justificationNeeded = signal(false);
  /** Version sent as `If-Match`. Refreshed by a reload and by immediate attribute calls. */
  protected etag: string | null = null;

  protected readonly panel = viewChild(AttributePanel);
  private readonly summaryBox = viewChild<ElementRef<HTMLElement>>('summaryBox');

  protected readonly isFinancial = computed(() => this.accountType() === 'Financial');
  protected readonly categoryOptions = computed<readonly Category[]>(() =>
    this.isFinancial() ? FINANCIAL_CATEGORIES : ['Statistical'],
  );
  protected readonly statusOptions = computed<readonly AccountStatus[]>(() => {
    if (!this.isEdit()) return ACCOUNT_STATUSES;
    // A change to or from Template is refused by the API (AC-GLA-023), so it is not offered.
    return this.original()?.status === 'Template' ? ['Template'] : ['Active', 'Hold', 'Inactive'];
  });
  protected readonly cancelLink = computed(() => (this.isEdit() ? ['/gl/accounts', this.id()!] : ['/gl/accounts']));

  /** Balance type and normal balance cannot be changed by an update (UpdateAccountRequest has no such fields). */
  protected readonly readOnlyItems = computed<DescriptionItem[]>(() => {
    const acct = this.original();
    if (!acct) return [];
    const items: DescriptionItem[] = [
      { label: 'Account ID', value: acct.accountId },
      { label: 'Account type', value: acct.accountType },
    ];
    if (acct.balanceType) items.push({ label: 'Balance type', value: acct.balanceType });
    if (acct.normalBalance) items.push({ label: 'Normal balance', value: acct.normalBalance });
    return items;
  });

  constructor() {
    this.form.controls.accountType.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.syncType());
    this.form.controls.status.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.syncJustification());
    this.form.controls.accountId.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.duplicateOf.set(null));

    effect(() => {
      const id = this.id();
      untracked(() => (id ? this.loadAccount(id) : this.initCreate()));
    });
  }

  protected loadAccount(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.api.getAccount(id).subscribe({
      next: ({ body, etag }) => {
        this.etag = etag;
        this.original.set(body);
        this.attributesBase.set(body.attributes);
        this.form.patchValue(
          {
            accountId: body.accountId,
            accountType: body.accountType,
            description: body.description,
            category: body.category,
            currencyId: body.currencyId,
            status: body.status,
            balanceType: body.balanceType ?? '',
            normalBalance: body.normalBalance ?? '',
          },
          { emitEvent: false },
        );
        for (const name of ['accountId', 'accountType', 'balanceType', 'normalBalance'] as const) {
          this.form.controls[name].disable({ emitEvent: false });
        }
        this.syncType();
        this.preselectStatus(body);
        this.syncJustification();
        this.loading.set(false);
      },
      error: (e: unknown) => {
        this.loadError.set(toApiError(e));
        this.loading.set(false);
      },
    });
  }

  private initCreate(): void {
    this.original.set(null);
    this.etag = null;
    this.attributesBase.set([]);
    this.form.reset(undefined, { emitEvent: false });
    this.form.controls.accountId.enable({ emitEvent: false });
    this.form.controls.accountType.enable({ emitEvent: false });
    this.syncType();
    this.syncJustification();
    this.loading.set(false);
  }

  /** From the delete dialog: the status is preselected, and the justification field appears with it. */
  private preselectStatus(account: Account): void {
    const wanted = this.status();
    if (wanted && wanted !== account.status && (this.statusOptions() as readonly string[]).includes(wanted)) {
      this.form.controls.status.setValue(wanted as AccountStatus, { emitEvent: false });
    }
  }

  private syncType(): void {
    const type = this.form.controls.accountType.value;
    this.accountType.set(type);
    const category = this.form.controls.category;
    if (type === 'Statistical') category.setValue('Statistical', { emitEvent: false });
    else if (category.value === 'Statistical') category.setValue('', { emitEvent: false });

    if (this.isEdit()) return;
    for (const name of ['balanceType', 'normalBalance'] as const) {
      const control = this.form.controls[name];
      if (type === 'Financial') control.enable({ emitEvent: false });
      else {
        control.setValue('', { emitEvent: false });
        control.disable({ emitEvent: false });
      }
    }
  }

  private syncJustification(): void {
    const stored = this.original();
    const needed = this.isEdit() && !!stored && this.form.controls.status.value !== stored.status;
    this.justificationNeeded.set(needed);
    const control = this.form.controls.statusJustification;
    if (needed) {
      control.setValidators([Validators.required, Validators.maxLength(500), Validators.pattern(/\S/)]);
    } else {
      control.clearValidators();
      control.setValue('', { emitEvent: false });
    }
    control.updateValueAndValidity({ emitEvent: false });
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
    this.stale.set(null);
    this.notice.set(null);
    this.form.markAllAsTouched();

    const attributesOk = this.panel()?.validate() ?? true;
    if (this.form.invalid || !attributesOk) {
      this.showSummary(attributesOk ? [] : [{ id: 'attributes-heading', message: 'Choose an attribute type and a value for every attribute row.' }]);
      return;
    }
    this.summary.set([]);
    this.saving.set(true);
    if (this.isEdit()) this.update();
    else this.create();
  }

  private create(): void {
    const raw = this.form.getRawValue();
    const attributes = this.panel()?.attributeInputs() ?? [];
    const request: CreateAccountRequest = {
      accountId: raw.accountId.trim(),
      description: raw.description,
      accountType: raw.accountType,
      category: raw.category as Category,
      currencyId: raw.currencyId,
      status: raw.status,
      ...(raw.accountType === 'Financial'
        ? { balanceType: raw.balanceType as BalanceType, normalBalance: raw.normalBalance as NormalBalance }
        : {}),
      ...(attributes.length > 0 ? { attributes } : {}),
    };
    this.api.createAccount(request).subscribe({
      next: ({ body }) => this.done(body.accountId, `Account ${body.accountId} created.`),
      error: (e: unknown) => this.fail(e, 'duplicate', request.accountId),
    });
  }

  private update(): void {
    const id = this.id()!;
    const raw = this.form.getRawValue();
    const changes = this.panel()?.changes() ?? [];
    const request: UpdateAccountRequest = {
      accountId: raw.accountId,
      accountType: raw.accountType,
      description: raw.description,
      category: raw.category as Category,
      currencyId: raw.currencyId,
      status: raw.status,
      ...(this.justificationNeeded() ? { statusJustification: raw.statusJustification.trim() } : {}),
      ...(changes.length > 0 ? { attributeChanges: changes } : {}),
    };
    this.api.updateAccount(id, request, this.etag ?? '').subscribe({
      next: () => this.done(id, `Account ${id} updated.`),
      error: (e: unknown) => this.fail(e, 'staleVersion', id),
    });
  }

  private done(accountId: string, text: string): void {
    this.saving.set(false);
    this.snackBar.open(text, 'Dismiss', { duration: 6000, politeness: 'polite' });
    void this.router.navigate(['/gl/accounts', accountId]);
  }

  private fail(e: unknown, conflict: ConflictMeaning, accountId: string): void {
    const error = toApiError(e, conflict);
    this.saving.set(false);
    switch (error.kind) {
      case 'validation':
        this.applyFieldErrors(error);
        break;
      case 'duplicate': {
        // AC-GLA-003: shown at the Account ID field, and the existing account is identifiable.
        this.form.controls.accountId.setErrors({ server: error.message });
        this.duplicateOf.set(accountId);
        this.showSummary();
        break;
      }
      case 'staleVersion':
        this.stale.set(error);
        this.announcer.alert(error.message);
        break;
      default:
        this.saveError.set(error);
        this.announcer.alert(error.message);
    }
  }

  private applyFieldErrors(error: ApiError): void {
    const extras: SummaryItem[] = [];
    const attributeMessages = this.panel()?.applyServerErrors(error.fieldErrors) ?? [];
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      if (/^(attributeChanges|attributes)\[\d+\]/.test(field)) continue;
      const control = this.form.get(field);
      if (control && control.enabled) control.setErrors({ server: messages.join(' ') });
      else extras.push(...messages.map((message) => ({ id: null, message })));
    }
    this.showSummary([...attributeMessages.map((message) => ({ id: 'attributes-heading', message })), ...extras]);
  }

  private showSummary(extra: SummaryItem[] = []): void {
    const items: SummaryItem[] = FIELD_ORDER.filter((name) => {
      const control = this.form.controls[name];
      return control.enabled && control.errors;
    }).map((name) => ({ id: `field-${name}`, message: this.message(name) }));
    const all = [...items, ...extra];
    this.summary.set(all);
    if (all.length > 0) {
      this.announcer.alert(`There ${all.length === 1 ? 'is 1 problem' : `are ${all.length} problems`} with this form.`);
      afterNextRender(() => this.summaryBox()?.nativeElement.focus(), { injector: this.injector });
    }
  }

  /** The user's input is kept: only the version and the stored-status baseline are refreshed (Flow 2, 409). */
  protected reload(): void {
    const id = this.id();
    if (!id) return;
    this.api.getAccount(id).subscribe({
      next: ({ body, etag }) => {
        this.etag = etag;
        this.original.set(body);
        this.stale.set(null);
        this.syncJustification();
        this.notice.set('The latest version was loaded. Your changes are kept. Review them and save again.');
        this.announcer.announce('The latest version was loaded. Your changes are kept.');
      },
      error: (e: unknown) => {
        this.stale.set(null);
        this.saveError.set(toApiError(e));
      },
    });
  }

  /** user-flows.md, Keyboard paths: Enter in a text field does not submit; only Save does. Selects, textareas and buttons keep Enter. */
  protected blockEnter(event: Event): void {
    if ((event.target as HTMLElement).tagName === 'INPUT') event.preventDefault();
  }

  protected focusField(event: Event, id: string): void {
    event.preventDefault();
    const target = this.host.nativeElement.querySelector<HTMLElement>(`#${id}`);
    if (!target) return;
    if (!target.matches('input, select, textarea, button, [tabindex]')) target.setAttribute('tabindex', '-1');
    target.focus();
  }
}
