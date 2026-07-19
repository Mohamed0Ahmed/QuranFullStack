import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

// Always an active router link — the pre-031 disabled / coming-soon branch is gone. Its text comes
// from the shared WordsExplainerContent, so a card and its page's hero can't drift (Feature 031).
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
