import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/**
 * A count chip with real button semantics and an accessible label
 * (`"${label}: ${count}"`). Drill-down clicks are wired in US3; in US2 chips
 * render disabled where a drill-down is not yet available, but the button
 * element keeps stable, accessible affordance.
 */
@Component({
  selector: 'qd-word-count-chip',
  standalone: true,
  templateUrl: './word-count-chip.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordCountChipComponent {
  readonly label = input.required<string>();
  readonly count = input.required<number>();
  /** When true the chip is non-interactive (no drill-down yet in US2). */
  readonly disabled = input<boolean>(false);

  readonly chipClick = output<void>();

  protected readonly ariaLabel = computed(() => `${this.label()}: ${this.count()}`);

  onClick(): void {
    if (!this.disabled()) {
      this.chipClick.emit();
    }
  }
}
