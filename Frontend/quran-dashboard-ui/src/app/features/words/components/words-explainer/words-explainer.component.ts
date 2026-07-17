import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { WordsExplainerContent } from '../../models/words-explainer.content';

/**
 * Presentation-only Words explainer hero (Feature 031). Renders the invariant frame from
 * `content` (ordinal, eyebrow, tagline, body, benefit callout, collapse toggle, a11y wiring) and
 * projects each page's own example region through `<ng-content>`.
 *
 * It does NOT re-render the title: the explorer page's existing `<h1>` owns the visible title, and
 * the section is named by it via `aria-label` — so no heading is duplicated. It also does NOT own
 * the persisted collapse state: it renders `expanded` and emits `toggled`; the page wires storage
 * (kept out of a presentational). No `inject()`, no Router, no Quran-data logic.
 */
@Component({
  selector: 'qd-words-explainer',
  standalone: true,
  templateUrl: './words-explainer.component.html',
  styleUrl: './words-explainer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordsExplainerComponent {
  readonly content = input.required<WordsExplainerContent>();
  readonly expanded = input<boolean>(true);
  readonly toggled = output<boolean>();

  protected readonly benefitLabel = 'الفائدة';
  private readonly collapseLabel = 'طيّ الشرح';
  private readonly expandLabel = 'عرض الشرح';

  protected readonly toggleLabel = computed(() => (this.expanded() ? this.collapseLabel : this.expandLabel));
  protected readonly bodyId = computed(() => `words-explainer-body--${this.content().key}`);

  protected onToggle(): void {
    this.toggled.emit(!this.expanded());
  }
}
