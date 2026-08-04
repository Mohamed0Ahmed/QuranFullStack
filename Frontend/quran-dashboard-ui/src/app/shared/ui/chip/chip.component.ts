import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

export type QdChipElement = 'button' | 'a';

@Component({
  selector: 'qd-chip',
  standalone: true,
  imports: [NgTemplateOutlet],
  templateUrl: './chip.component.html',
  styleUrl: './chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdChipComponent {
  readonly selected = input(false);
  readonly disabled = input(false);
  readonly elementType = input<QdChipElement>('button', { alias: 'as' });
  readonly count = input<number | null>(null);
  readonly href = input<string | null>(null);
  readonly ariaLabel = input<string | null>(null);

  readonly chipClick = output<void>();

  readonly removable = input(false);
  readonly removeAriaLabel = input<string | null>(null);
  readonly remove = output<void>();

  readonly labelClickable = input(false);
  readonly labelAriaLabel = input<string | null>(null);
  readonly labelClick = output<void>();

  protected readonly labelIsButton = computed(() => this.labelClickable() && this.removable());

  protected onClick(event: Event): void {
    if (this.disabled()) {
      event.preventDefault();
      return;
    }

    this.chipClick.emit();
  }

  protected onLabelClick(event: Event): void {
    event.stopPropagation();
    if (this.disabled()) {
      return;
    }
    this.labelClick.emit();
  }

  protected onRemoveClick(event: Event): void {
    event.stopPropagation();
    if (this.disabled()) {
      return;
    }
    this.remove.emit();
  }
}
