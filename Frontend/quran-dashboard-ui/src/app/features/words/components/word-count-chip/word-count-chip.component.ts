import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

export const WORD_COUNT_DISABLED_REASON = 'لا كلمات مرتبطة بهذا النوع، لذا لا تفاصيل لعرضها.';

@Component({
  selector: 'qd-word-count-chip',
  standalone: true,
  templateUrl: './word-count-chip.component.html',
  styleUrl: './word-count-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordCountChipComponent {
  readonly label = input.required<string>();
  readonly count = input.required<number>();
  readonly disabled = input<boolean>(false);
  readonly disabledReasonId = input<string | null>(null);
  readonly selected = input(false);
  readonly showLabel = input(true);

  readonly chipClick = output<void>();

  protected readonly ariaLabel = computed(() => `${this.label()}: ${this.count()}`);
  protected readonly ariaPressed = computed(() =>
    !this.disabled() && this.selected() ? 'true' : 'false',
  );

  onClick(): void {
    if (!this.disabled()) {
      this.chipClick.emit();
    }
  }
}
