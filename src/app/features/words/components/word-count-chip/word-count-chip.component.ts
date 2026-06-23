import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'qd-word-count-chip',
  standalone: true,
  templateUrl: './word-count-chip.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordCountChipComponent {
  readonly label = input.required<string>();
  readonly count = input.required<number>();
  readonly disabled = input<boolean>(false);
  readonly showLabel = input(true);

  readonly chipClick = output<void>();

  protected readonly ariaLabel = computed(() => `${this.label()}: ${this.count()}`);

  onClick(): void {
    if (!this.disabled()) {
      this.chipClick.emit();
    }
  }
}
