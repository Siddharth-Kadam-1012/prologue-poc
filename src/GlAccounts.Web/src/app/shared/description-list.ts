import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface DescriptionItem {
  label: string;
  value: string;
}

/** UX gap: DescriptionList. A real `<dl>` so label and value stay associated for assistive technology. */
@Component({
  selector: 'app-description-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dl class="description-list">
      @for (item of items(); track item.label) {
        <div class="row">
          <dt>{{ item.label }}</dt>
          <dd>{{ item.value }}</dd>
        </div>
      }
    </dl>
  `,
  styles: `
    .description-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
      gap: 12px 24px;
      margin: 0;
    }
    dt {
      font: var(--mat-sys-label-medium);
      color: var(--mat-sys-on-surface-variant);
    }
    dd {
      margin: 0;
      font: var(--mat-sys-body-large);
    }
  `,
})
export class DescriptionList {
  readonly items = input.required<DescriptionItem[]>();
}
