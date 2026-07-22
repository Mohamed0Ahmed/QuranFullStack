import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

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
