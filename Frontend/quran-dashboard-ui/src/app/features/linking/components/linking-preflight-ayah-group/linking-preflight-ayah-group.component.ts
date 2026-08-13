import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { toQuranWordDisplayText } from '../../../../shared/quran/quran-word-display-text';
import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingAyahPreflight, LinkingDoorWordImpact } from '../../models/linking-preflight.models';

type LinkingPreflightGroupWordHighlight = 'added' | 'existing' | 'removed' | null;

export interface LinkingPreflightGroupedAyahView {
  ayah: LinkingAyah;
  preflight: LinkingAyahPreflight;
}

interface LinkingPreflightGroupWord {
  key: string;
  textUthmani: string;
  highlight: LinkingPreflightGroupWordHighlight;
}

interface LinkingPreflightGroupSegment {
  verseKey: string;
  ayahNumber: number;
  surahNameArabic: string;
  words: readonly LinkingPreflightGroupWord[];
}

@Component({
  selector: 'qd-linking-preflight-ayah-group',
  standalone: true,
  imports: [AyahCardComponent],
  templateUrl: './linking-preflight-ayah-group.component.html',
  styleUrl: './linking-preflight-ayah-group.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightAyahGroupComponent {
  readonly items = input.required<readonly LinkingPreflightGroupedAyahView[]>();
  readonly statusLabel = input.required<string>();

  protected readonly labels = LINKING_LABELS;
  protected readonly segments = computed<readonly LinkingPreflightGroupSegment[]>(() =>
    this.items().map(({ ayah, preflight }) => ({
      verseKey: ayah.verseKey,
      ayahNumber: ayah.ayahNumber,
      surahNameArabic: ayah.surahNameArabic,
      words: ayah.words
        .filter((word) => !word.isAyahMarker)
        .map((word) => ({
          key: `${ayah.verseKey}-${word.renderPosition}`,
          textUthmani: toQuranWordDisplayText(word.textUthmani),
          highlight: highlightFor(word.canonicalQuranWordId, preflight.doorWordImpact),
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

function highlightFor(
  quranWordId: number,
  impact: LinkingDoorWordImpact,
): LinkingPreflightGroupWordHighlight {
  if (impact.removed.includes(quranWordId)) {
    return 'removed';
  }
  if (impact.added.includes(quranWordId)) {
    return 'added';
  }
  return impact.existing.includes(quranWordId) ? 'existing' : null;
}
