import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export type QdChipElement = 'button' | 'a';

@Component({
  selector: 'qd-chip',
  standalone: true,
  templateUrl: './chip.component.html',
  styleUrl: './chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdChipComponent {
  readonly selected = input(false);
  readonly disabled = input(false);
  // Renamed internally: `as` is reserved in Angular template expressions and can't be called as `as()`.
  readonly elementType = input<QdChipElement>('button', { alias: 'as' });
  readonly count = input<number | null>(null);
  readonly href = input<string | null>(null);
  readonly ariaLabel = input<string | null>(null);

  readonly chipClick = output<void>();

  protected onClick(event: Event): void {
    if (this.disabled()) {
      event.preventDefault();
      return;
    }

    this.chipClick.emit();
  }
}
