import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Presentation-only ayah-card frame (Feature 029, Change A): the one flat card
 * treatment for ayah-shaped list items across Words ayah matches, Mushaf Similar
 * Ayahs, and Mutashabihat occurrences.
 *
 * It owns surface background, hairline border, control radius, and compact
 * logical padding/gap — nothing else. It accepts no Quran/domain model, text,
 * formatter, route, or output; callers keep their own semantic wrapper element
 * (article/li), Quran renderer, and navigation behavior.
 */
@Component({
  selector: '[qdAyahCard]',
  standalone: true,
  templateUrl: './ayah-card.component.html',
  styleUrl: './ayah-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'qd-ayah-card' },
})
export class AyahCardComponent {}
