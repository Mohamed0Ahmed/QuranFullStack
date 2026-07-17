import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * A single Words-hub navigation card (Feature 031). Every explorer has shipped, so this is always an
 * active router link — the pre-031 disabled / coming-soon branch is removed. Its text (ordinal,
 * eyebrow, title, description) comes from the shared `WordsExplainerContent`, so a card and its
 * page's hero can never drift. Presentation-only.
 */
@Component({
  selector: 'qd-word-section-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './word-section-card.component.html',
  styleUrls: ['./word-section-card.component.scss'],
})
export class WordSectionCardComponent {
  readonly ordinal = input.required<string>();
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly description = input.required<string>();
  readonly route = input.required<string>();
}
