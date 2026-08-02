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
  // Aliased to `as` per the qd-chip contract; renamed internally because `as` is a
  // reserved word in Angular template expressions and can't be called as `as()`.
  readonly elementType = input<QdChipElement>('button', { alias: 'as' });
  readonly count = input<number | null>(null);
  readonly href = input<string | null>(null);
  readonly ariaLabel = input<string | null>(null);

  readonly chipClick = output<void>();

  // The remove affordance (plan-slice-b.md T412 — Abwab alias chips). Orthogonal to
  // `selected`/`as`: a removable chip is informational, not a selectable action, so it
  // renders a static `<span>` wrapper instead of `elementType()`'s button/anchor —
  // nesting an interactive remove `<button>` inside another `<button>` is invalid HTML.
  readonly removable = input(false);
  readonly removeAriaLabel = input<string | null>(null);
  readonly remove = output<void>();

  /**
   * Opt-in: renders the label as its own nested `<button>` so a removable chip can carry two
   * independent controls — a name that acts and an `×` that removes. Default off, so the
   * chip's other consumers render byte-identically.
   *
   * Only honored in the removable branch, and that guard is load-bearing rather than
   * conservative: the other two branches ARE a `<button>`/`<a>`, and nesting an interactive
   * control inside one is invalid HTML. The removable branch's wrapper is a static `<span>`,
   * which is exactly what makes the nesting legal there.
   */
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
