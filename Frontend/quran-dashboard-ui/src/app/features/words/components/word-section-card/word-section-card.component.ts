import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'qd-word-section-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './word-section-card.component.html',
  styleUrls: ['./word-section-card.component.scss'],
})
export class WordSectionCardComponent {
  readonly labelAr = input.required<string>();
  readonly descriptionAr = input.required<string>();
  readonly route = input<string | null>(null);
  readonly disabled = input<boolean>(false);
  readonly comingSoonLabel = input<string>('قريبًا');

  readonly isActive = computed(() => !this.disabled());
  readonly cardClass = computed(() => (this.disabled() ? 'qd-card--quiet' : 'qd-card--hover'));
}
