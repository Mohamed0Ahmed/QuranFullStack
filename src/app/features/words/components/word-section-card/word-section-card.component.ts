import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * A single Words hub section card. Active cards navigate to their route;
 * disabled (coming-soon) cards render as non-navigable, keyboard-safe surfaces
 * that clearly communicate `قريبًا` and never change the URL.
 */
@Component({
  selector: 'qd-word-section-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './word-section-card.component.html',
  styleUrls: ['./word-section-card.component.scss'],
})
export class WordSectionCardComponent {
  /** Arabic label shown as the card title. */
  readonly labelAr = input.required<string>();

  /** Short Arabic description shown under the label. */
  readonly descriptionAr = input.required<string>();

  /**
   * Target route for an active card. Ignored when the card is disabled.
   * Optional so coming-soon cards do not need to supply a route.
   */
  readonly route = input<string | null>(null);

  /** When true, the card is a non-navigable coming-soon section. */
  readonly disabled = input<boolean>(false);

  /** Coming-soon badge text; defaults to the shared label. */
  readonly comingSoonLabel = input<string>('قريبًا');

  readonly isActive = computed(() => !this.disabled());
  readonly cardClass = computed(() => (this.disabled() ? 'qd-card--quiet' : 'qd-card--hover'));
}
