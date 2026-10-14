import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { flattenFieldErrors, toApiError } from '../../core/api-error';
import { GlAccountsApi } from '../../core/gl-accounts-api';
import { AccountAttribute, AttributeChange, AttributeInput } from '../../core/models';
import { EmptyState } from '../../shared/empty-state';
import { ATTRIBUTE_CATALOG, typeLabel, valueLabel, valuesFor } from './attribute-catalog';

interface Row {
  key: number;
  /** Null for a row that is not stored yet. */
  attributeId: string | null;
  typeId: string;
  valueId: string;
  origTypeId: string;
  origValueId: string;
  editing: boolean;
  deleted: boolean;
  error: string | null;
  busy: boolean;
}

/**
 * UX-GLA-003: attribute panel. Stub data only.
 * Staged mode (default): changes wait for the parent Save and are sent as `attributeChanges` (or `attributes` on create),
 * applied all or nothing by the API. Immediate mode (saved account): each row action calls the single-attribute operation.
 */
@Component({
  selector: 'app-attribute-panel',
  imports: [MatButtonModule, EmptyState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section aria-labelledby="attributes-heading">
      <h2 id="attributes-heading">Attributes</h2>
      @if (rows().length === 0) {
        <app-empty-state message="No attributes yet.">
          <button mat-stroked-button type="button" (click)="add()">Add attribute</button>
        </app-empty-state>
      } @else {
        <table aria-label="Attributes">
          <thead>
            <tr>
              <th scope="col">Attribute type</th>
              <th scope="col">Value</th>
              <th scope="col">State</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (row of rows(); track row.key) {
              <tr [class.deleted]="row.deleted">
                <td>
                  @if (isEditable(row)) {
                    <select
                      [id]="'attr-type-' + row.key"
                      aria-label="Attribute type"
                      (change)="setType(row, $any($event.target).value)"
                    >
                      <option value="" [selected]="row.typeId === ''">Choose a type</option>
                      @for (type of catalog; track type.id) {
                        <option [value]="type.id" [selected]="type.id === row.typeId">{{ type.label }}</option>
                      }
                    </select>
                  } @else {
                    {{ typeText(row.typeId) }}
                  }
                </td>
                <td>
                  @if (isEditable(row)) {
                    <select
                      [id]="'attr-value-' + row.key"
                      aria-label="Attribute value"
                      [attr.aria-describedby]="row.error ? 'attr-error-' + row.key : null"
                      [attr.aria-invalid]="row.error ? 'true' : null"
                      (change)="setValue(row, $any($event.target).value)"
                    >
                      <option value="" [selected]="row.valueId === ''">Choose a value</option>
                      @for (value of valuesOf(row.typeId); track value.id) {
                        <option [value]="value.id" [selected]="value.id === row.valueId">{{ value.label }}</option>
                      }
                    </select>
                  } @else {
                    {{ valueText(row.typeId, row.valueId) }}
                  }
                  @if (row.error) {
                    <p class="row-error" [id]="'attr-error-' + row.key">{{ row.error }}</p>
                  }
                </td>
                <td>{{ stateText(row) }}</td>
                <td class="actions">
                  @if (row.deleted) {
                    <button mat-stroked-button type="button" (click)="undo(row)" [attr.aria-label]="'Undo delete of ' + typeText(row.typeId)">
                      Undo
                    </button>
                  } @else if (row.attributeId === null) {
                    @if (immediate()) {
                      <button mat-flat-button type="button" [disabled]="row.busy" (click)="saveRow(row)">Save row</button>
                    }
                    <button mat-stroked-button type="button" [disabled]="row.busy" (click)="removeRow(row)">Remove row</button>
                  } @else if (row.editing) {
                    @if (immediate()) {
                      <button mat-flat-button type="button" [disabled]="row.busy" (click)="saveRow(row)">Save row</button>
                    }
                    <button mat-stroked-button type="button" [disabled]="row.busy" (click)="cancelEdit(row)">Cancel edit</button>
                  } @else {
                    <button
                      mat-stroked-button
                      type="button"
                      [disabled]="row.busy"
                      (click)="startEdit(row)"
                      [attr.aria-label]="'Edit ' + typeText(row.typeId) + ' attribute'"
                    >
                      Edit
                    </button>
                    @if (immediate()) {
                      <button
                        mat-stroked-button
                        type="button"
                        [disabled]="row.busy"
                        (click)="deleteRow(row)"
                        [attr.aria-label]="'Delete ' + typeText(row.typeId) + ' attribute'"
                      >
                        Delete
                      </button>
                    } @else {
                      <button
                        mat-stroked-button
                        type="button"
                        (click)="markDelete(row)"
                        [attr.aria-label]="'Mark ' + typeText(row.typeId) + ' attribute for delete'"
                      >
                        Mark for delete
                      </button>
                    }
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
        <p><button mat-stroked-button type="button" (click)="add()">Add attribute</button></p>
      }
    </section>
  `,
  styles: `
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th,
    td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
      vertical-align: top;
    }
    select {
      min-width: 160px;
      padding: 6px;
      font: inherit;
    }
    tr.deleted td {
      text-decoration: line-through;
      color: var(--mat-sys-on-surface-variant);
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }
    .row-error {
      margin: 4px 0 0;
      color: var(--mat-sys-error);
    }
  `,
})
export class AttributePanel {
  /** Stored attributes of a saved account; empty on create. */
  readonly stored = input<AccountAttribute[]>([]);
  /** Set for a saved account. Needed only in immediate mode. */
  readonly accountId = input<string | null>(null);
  readonly immediate = input(false);
  /** The account's new ETag after an immediate-mode call. */
  readonly etagChange = output<string>();

  private readonly api = inject(GlAccountsApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  private nextKey = 1;

  protected readonly catalog = ATTRIBUTE_CATALOG;
  protected readonly typeText = typeLabel;
  protected readonly valueText = valueLabel;
  protected readonly valuesOf = valuesFor;

  protected readonly rows = linkedSignal<AccountAttribute[], Row[]>({
    source: this.stored,
    computation: (list) =>
      list.map((a) => ({
        key: this.nextKey++,
        attributeId: a.attributeId,
        typeId: a.attributeTypeId,
        valueId: a.attributeValueId,
        origTypeId: a.attributeTypeId,
        origValueId: a.attributeValueId,
        editing: false,
        deleted: false,
        error: null,
        busy: false,
      })),
  });

  private readonly staged = computed(() =>
    this.rows().flatMap((row): { key: number; change: AttributeChange }[] => {
      if (row.attributeId === null) {
        return [{ key: row.key, change: { action: 'Add', attributeTypeId: row.typeId, attributeValueId: row.valueId } }];
      }
      if (row.deleted) return [{ key: row.key, change: { action: 'Remove', attributeId: row.attributeId } }];
      if (this.differs(row)) {
        return [
          {
            key: row.key,
            change: { action: 'Edit', attributeId: row.attributeId, attributeTypeId: row.typeId, attributeValueId: row.valueId },
          },
        ];
      }
      return [];
    }),
  );

  /** Staged changes for `attributeChanges` on update. */
  readonly changes = computed(() => this.staged().map((s) => s.change));
  /** New rows as `attributes` for create. */
  readonly attributeInputs = computed<AttributeInput[]>(() =>
    this.changes()
      .filter((c) => c.action === 'Add')
      .map((c) => ({ attributeTypeId: c.attributeTypeId!, attributeValueId: c.attributeValueId! })),
  );
  readonly hasPending = computed(() => this.changes().length > 0);

  /** AC-GLA-016: an empty type or value is refused before the call, with a message on the row. */
  validate(): boolean {
    let valid = true;
    this.rows.update((rows) =>
      rows.map((row) => {
        const needsValue = row.attributeId === null || (!row.deleted && this.differs(row));
        if (needsValue && (row.typeId === '' || row.valueId === '')) {
          valid = false;
          return { ...row, error: 'Choose an attribute type and a value.' };
        }
        return row;
      }),
    );
    return valid;
  }

  /** Puts the API's `attributeChanges[i]` / `attributes[i]` errors on the matching rows. Returns the messages used. */
  applyServerErrors(fieldErrors: Record<string, string[]>): string[] {
    const staged = this.staged();
    const used: string[] = [];
    const byKey = new Map<number, string>();
    for (const [field, messages] of Object.entries(fieldErrors)) {
      const match = /^(?:attributeChanges|attributes)\[(\d+)\]/.exec(field);
      if (!match) continue;
      const target = staged[Number(match[1])];
      if (!target) continue;
      byKey.set(target.key, messages.join(' '));
      used.push(...messages);
    }
    if (byKey.size > 0) {
      this.rows.update((rows) => rows.map((row) => (byKey.has(row.key) ? { ...row, error: byKey.get(row.key)! } : row)));
    }
    return used;
  }

  protected isEditable(row: Row): boolean {
    return !row.deleted && (row.attributeId === null || row.editing);
  }

  protected stateText(row: Row): string {
    if (row.deleted) return 'Marked for delete';
    if (row.attributeId === null) return this.immediate() ? 'New' : 'New (staged)';
    if (this.differs(row)) return this.immediate() ? 'Edited (not saved)' : 'Edited (staged)';
    return 'Stored';
  }

  protected add(): void {
    const key = this.nextKey++;
    this.rows.update((rows) => [
      ...rows,
      { key, attributeId: null, typeId: '', valueId: '', origTypeId: '', origValueId: '', editing: true, deleted: false, error: null, busy: false },
    ]);
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLSelectElement>(`#attr-type-${key}`)?.focus(), {
      injector: this.injector,
    });
  }

  protected setType(row: Row, typeId: string): void {
    // The value must belong to the type, so it is cleared when the type changes.
    this.patch(row.key, { typeId, valueId: '', error: null });
  }

  protected setValue(row: Row, valueId: string): void {
    this.patch(row.key, { valueId, error: null });
  }

  protected startEdit(row: Row): void {
    this.patch(row.key, { editing: true, error: null });
  }

  protected cancelEdit(row: Row): void {
    this.patch(row.key, { editing: false, typeId: row.origTypeId, valueId: row.origValueId, error: null });
  }

  protected markDelete(row: Row): void {
    this.patch(row.key, { deleted: true, editing: false, error: null });
  }

  protected undo(row: Row): void {
    this.patch(row.key, { deleted: false, error: null });
  }

  protected removeRow(row: Row): void {
    this.rows.update((rows) => rows.filter((r) => r.key !== row.key));
  }

  /** Immediate mode: add (API-GLA-005) or update (API-GLA-006) one attribute now. */
  protected saveRow(row: Row): void {
    const accountId = this.accountId();
    if (!accountId) return;
    if (row.typeId === '' || row.valueId === '') {
      this.patch(row.key, { error: 'Choose an attribute type and a value.' });
      return;
    }
    const input: AttributeInput = { attributeTypeId: row.typeId, attributeValueId: row.valueId };
    const call =
      row.attributeId === null
        ? this.api.addAccountAttribute(accountId, input)
        : this.api.updateAccountAttribute(accountId, row.attributeId, input);
    this.patch(row.key, { busy: true, error: null });
    call.subscribe({
      next: ({ body, etag }) => {
        this.patch(row.key, {
          busy: false,
          editing: false,
          attributeId: body.attributeId,
          typeId: body.attributeTypeId,
          valueId: body.attributeValueId,
          origTypeId: body.attributeTypeId,
          origValueId: body.attributeValueId,
        });
        if (etag) this.etagChange.emit(etag);
      },
      error: (e: unknown) => this.patch(row.key, { busy: false, error: this.messageOf(e) }),
    });
  }

  /** Immediate mode: remove one attribute now (API-GLA-007). */
  protected deleteRow(row: Row): void {
    const accountId = this.accountId();
    if (!accountId || row.attributeId === null) return;
    this.patch(row.key, { busy: true, error: null });
    this.api.removeAccountAttribute(accountId, row.attributeId).subscribe({
      next: (etag) => {
        this.rows.update((rows) => rows.filter((r) => r.key !== row.key));
        if (etag) this.etagChange.emit(etag);
      },
      error: (e: unknown) => this.patch(row.key, { busy: false, error: this.messageOf(e) }),
    });
  }

  private differs(row: Row): boolean {
    return row.typeId !== row.origTypeId || row.valueId !== row.origValueId;
  }

  private patch(key: number, changes: Partial<Row>): void {
    this.rows.update((rows) => rows.map((row) => (row.key === key ? { ...row, ...changes } : row)));
  }

  private messageOf(e: unknown): string {
    const error = toApiError(e);
    const fields = flattenFieldErrors(error);
    return fields.length > 0 ? fields.map((f) => f.message).join(' ') : error.message;
  }
}
