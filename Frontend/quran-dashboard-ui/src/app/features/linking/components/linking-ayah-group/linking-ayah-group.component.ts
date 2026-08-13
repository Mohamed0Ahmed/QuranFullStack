import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { toQuranWordDisplayText } from '../../../../shared/quran/quran-word-display-text';
import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';

export interface LinkingGroupWordToggle {
  verseKey: string;
  quranWordId: number;
}

interface LinkingGroupWord {
  key: string;
  verseKey: string;
  canonicalQuranWordId: number;
  textUthmani: string;
  selected: boolean;
}

interface LinkingGroupSegment {
  verseKey: string;
  ayahNumber: number;
  surahNameArabic: string;
  words: readonly LinkingGroupWord[];
}

@Component({
  selector: 'qd-linking-ayah-group',
  standalone: true,
  imports: [AyahCardComponent],
  templateUrl: './linking-ayah-group.component.html',
  styleUrl: './linking-ayah-group.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahGroupComponent {
  readonly ayahs = input.required<readonly LinkingAyah[]>();
  readonly wordSelectable = input(false);

  readonly wordToggled = output<LinkingGroupWordToggle>();
  readonly ayahRemoved = output<string>();

  protected readonly labels = LINKING_LABELS;
  protected readonly segments = computed<readonly LinkingGroupSegment[]>(() =>
    this.ayahs().map((ayah) => ({
      verseKey: ayah.verseKey,
      ayahNumber: ayah.ayahNumber,
      surahNameArabic: ayah.surahNameArabic,
      words: ayah.words
        .filter((word) => !word.isAyahMarker)
        .map((word) => ({
          key: `${ayah.verseKey}-${word.renderPosition}`,
          verseKey: ayah.verseKey,
          canonicalQuranWordId: word.canonicalQuranWordId,
          textUthmani: toQuranWordDisplayText(word.textUthmani),
          selected: word.isSourceMatch,
        })),
    })),
  );
  protected readonly rangeLabel = computed(() => {
    const segments = this.segments();
    const first = segments.at(0);
    const last = segments.at(-1);
    return first === undefined || last === undefined
      ? ''
      : `${first.surahNameArabic} ${first.ayahNumber} — ${last.surahNameArabic} ${last.ayahNumber}`;
  });
}
