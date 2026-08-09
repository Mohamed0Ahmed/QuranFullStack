import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdSortableHeaderState } from './data-table.models';

@Component({
  selector: 'qd-sortable-header',
  standalone: true,
  templateUrl: './sortable-header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'columnheader',
    class: 'qd-sortable-header',
    '[class.qd-sortable-header--numeric]': 'numeric()',
    '[attr.aria-sort]': "sortState() === 'none' ? null : sortState()",
  },
})
export class QdSortableHeaderComponent {
  readonly label = input.required<string>();
  readonly currentSortLabel = input('');
  readonly nextSortLabel = input.required<string>();
  readonly actionAriaLabel = input<string | null>(null);
  readonly sortState = input<QdSortableHeaderState>('none');
  readonly glyph = input<string | null>(null);
  readonly numeric = input(false);
  readonly testId = input<string | null>(null);

  readonly activated = output<void>();

  protected get actionLabel(): string {
    if (this.actionAriaLabel()) {
      return this.actionAriaLabel() as string;
    }

    return this.currentSortLabel()
      ? `${this.label()}: ${this.currentSortLabel()}. ${this.nextSortLabel()}`
      : `${this.label()}: ${this.nextSortLabel()}`;
  }
}
