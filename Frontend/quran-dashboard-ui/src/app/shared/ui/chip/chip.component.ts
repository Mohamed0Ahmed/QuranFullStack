import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export type QdChipElement = 'button' | 'a';

/**
 * The one selectable/informational chip app-wide (UI_STYLE_SYSTEM.md §17 `qd-chip`).
 *
 * Renders as a `<button>` or an `<a>`. Selected state uses the tint background +
 * accent-text label + hairline border defined by §16.1 — never a solid gold fill.
 */
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
  /** Public input name is `as` (per the qd-chip contract); the field is
   * renamed internally because `as` is a reserved word in Angular template
   * expressions and cannot be called as `as()` from the template. */
  readonly elementType = input<QdChipElement>('button', { alias: 'as' });
  /** Optional trailing count badge; omitted from the DOM when `null`. */
  readonly count = input<number | null>(null);
  /** Href for the `as="a"` anchor variant; ignored for the button variant. */
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
