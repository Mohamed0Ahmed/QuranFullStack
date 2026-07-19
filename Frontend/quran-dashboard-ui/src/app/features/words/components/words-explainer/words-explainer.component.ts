import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { WordsExplainerContent } from '../../models/words-explainer.content';

// Presentational hero (Feature 031): renders no title of its own — the page's <h1> owns it and the
// section is named via aria-label, so no heading is duplicated. It also does not own the persisted
// collapse state: it renders `expanded` and emits `toggled`, leaving storage to the page.
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
