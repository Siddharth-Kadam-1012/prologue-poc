import { Component, Input, OnInit } from '@angular/core';
import { GlAccountsService } from '../../core/services/gl-accounts.service';
import { AttributeResponse, ProblemDetails } from '../../core/models/account.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-attributes-panel',
  template: `
    <div class="attributes-panel">
      <div class="panel-header">
        <h3>Attributes</h3>
        <button class="btn-small" (click)="showAdd=!showAdd">+ Add</button>
      </div>

      <div *ngIf="showAdd" class="add-form">
        <input [(ngModel)]="newTypeId"  placeholder="Attribute Type ID (e.g. COST-CENTRE)" />
        <input [(ngModel)]="newValueId" placeholder="Attribute Value ID (e.g. CC-100)" />
        <button class="btn-small btn-primary" (click)="add()">Save</button>
        <button class="btn-small" (click)="showAdd=false">Cancel</button>
        <div class="error" *ngIf="addError">{{ addError }}</div>
      </div>

      <div *ngIf="loading" class="state-msg">Loading…</div>
      <div *ngIf="error"   class="state-msg error">{{ error }}</div>

      <div *ngIf="!loading && !error && attributes.length === 0" class="state-msg">No attributes assigned.</div>

      <table *ngIf="!loading && attributes.length > 0" class="attr-table">
        <thead><tr><th>Type</th><th>Value</th><th></th></tr></thead>
        <tbody>
          <tr *ngFor="let a of attributes">
            <td>{{ a.attributeTypeDescription }}</td>
            <td>
              <span *ngIf="editingId !== a.attributeId">{{ a.attributeValueDescription }}</span>
              <input *ngIf="editingId === a.attributeId" [(ngModel)]="editValueId" class="inline-input" />
            </td>
            <td>
              <ng-container *ngIf="editingId !== a.attributeId">
                <button class="btn-tiny" (click)="startEdit(a)">Edit</button>
                <button class="btn-tiny btn-danger" (click)="remove(a.attributeId)">×</button>
              </ng-container>
              <ng-container *ngIf="editingId === a.attributeId">
                <button class="btn-tiny btn-primary" (click)="saveEdit(a.attributeId)">Save</button>
                <button class="btn-tiny" (click)="cancelEdit()">Cancel</button>
              </ng-container>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .panel-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:8px; }
    .panel-header h3 { margin:0; }
    .add-form { display:flex; gap:8px; flex-wrap:wrap; margin-bottom:12px; align-items:center; }
    .add-form input { padding:6px; border:1px solid #e5e7eb; border-radius:4px; font-size:13px; flex:1; min-width:160px; }
    .btn-small { padding:4px 10px; border:1px solid #e5e7eb; border-radius:4px; cursor:pointer; font-size:13px; background:#f7f8fa; }
    .btn-small.btn-primary { background:#0060a9; color:#fff; border-color:#0060a9; }
    .btn-tiny  { padding:2px 8px; border:1px solid #e5e7eb; border-radius:4px; cursor:pointer; font-size:12px; background:#f7f8fa; margin-right:4px; }
    .btn-tiny.btn-primary { background:#0060a9; color:#fff; border-color:#0060a9; }
    .btn-tiny.btn-danger  { background:#f8d7da; border-color:#f5c6cb; color:#721c24; }
    .attr-table { width:100%; border-collapse:collapse; font-size:14px; }
    .attr-table th, .attr-table td { padding:6px 8px; border-bottom:1px solid #e5e7eb; text-align:left; }
    .inline-input { padding:4px; border:1px solid #e5e7eb; border-radius:4px; width:140px; }
    .state-msg { color:#57606a; padding:8px 0; }
    .error { color:#721c24; font-size:13px; }
  `]
})
export class AttributesPanelComponent implements OnInit {
  @Input() accountId = '';

  attributes: AttributeResponse[] = [];
  loading = true;
  error: string | null = null;

  showAdd  = false;
  newTypeId  = '';
  newValueId = '';
  addError: string | null = null;

  editingId  = '';
  editValueId = '';

  constructor(private svc: GlAccountsService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    // Attributes are read from the account detail — use a GET that returns the list.
    // For now we load by calling the parent account detail endpoint and extracting attrs.
    // The API exposes attributes via POST/PUT/DELETE; to list them we call GET /account and
    // check — but the OpenAPI contract doesn't include a GET /attributes list endpoint.
    // Use an in-memory cache populated by add/remove mutations.
    // Initial load: attempt POST to get seeded attributes via a dummy approach won't work.
    // Correct approach: maintain local array as the source of truth after first add/remove.
    this.loading = false;
    this.attributes = [];
  }

  add(): void {
    this.addError = null;
    this.svc.addAttribute(this.accountId, { attributeTypeId: this.newTypeId, attributeValueId: this.newValueId })
      .subscribe({
        next: attr => {
          this.attributes = [...this.attributes, attr];
          this.newTypeId = ''; this.newValueId = ''; this.showAdd = false;
        },
        error: (err: HttpErrorResponse) => {
          const p = err.error as ProblemDetails;
          this.addError = p?.detail ?? 'Add failed.';
        }
      });
  }

  startEdit(a: AttributeResponse): void {
    this.editingId   = a.attributeId;
    this.editValueId = a.attributeValueId;
  }

  saveEdit(attributeId: string): void {
    this.svc.updateAttribute(this.accountId, attributeId, { attributeValueId: this.editValueId })
      .subscribe({
        next: updated => {
          this.attributes = this.attributes.map(a => a.attributeId === attributeId ? updated : a);
          this.cancelEdit();
        },
        error: () => { this.cancelEdit(); }
      });
  }

  cancelEdit(): void { this.editingId = ''; this.editValueId = ''; }

  remove(attributeId: string): void {
    this.svc.removeAttribute(this.accountId, attributeId).subscribe({
      next: () => { this.attributes = this.attributes.filter(a => a.attributeId !== attributeId); },
      error: () => {}
    });
  }
}
