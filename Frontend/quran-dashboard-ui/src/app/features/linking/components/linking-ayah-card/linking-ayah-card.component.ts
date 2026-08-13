import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { toQuranWordDisplayText } from '../../../../shared/quran/quran-word-display-text';
import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { MergedLinkingWordSelection } from '../../models/linking-merge.models';
import { LinkingDoorWordImpact } from '../../models/linking-preflight.models';
import { LINKING_LABELS } from '../../models/linking.labels';

type LinkingAyahWordHighlight = 'selected' | 'added' | 'existing' | 'removed' | null;

interface LinkingAyahCardWord {
  renderPosition: number;
  textUthmani: string;
  highlight: LinkingAyahWordHighlight;
}

@Component({
  selector: 'qd-linking-ayah-card',
  standalone: true,
  imports: [AyahCardComponent],
  templateUrl: './linking-ayah-card.component.html',
  styleUrl: './linking-ayah-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahCardComponent {
  readonly ayah = input.required<LinkingAyah>();
  readonly highlightSourceWords = input(true);
  readonly mergedWords = input<readonly MergedLinkingWordSelection[] | null>(null);
  readonly sourceLabels = input<readonly string[]>([]);
  readonly wordImpact = input<LinkingDoorWordImpact | null>(null);

  protected readonly labels = LINKING_LABELS;
  protected readonly displayWords = computed<readonly LinkingAyahCardWord[]>(() => {
    const wordImpact = this.wordImpact();
    const impactByWordId = wordImpact === null ? null : toImpactByWordId(wordImpact);
    const merged = this.mergedWords();
    const mergedMatches =
      merged === null
        ? null
        : new Set(
            merged
              .filter((word) => word.sourceKeys.length > 0)
              .map((word) => word.canonicalQuranWordId),
          );
    return this.ayah().words.map((word) => ({
      renderPosition: word.renderPosition,
      textUthmani: toQuranWordDisplayText(word.textUthmani),
      highlight:
        impactByWordId === null
          ? this.isSelectedWord(word.isSourceMatch, mergedMatches, word.canonicalQuranWordId)
            ? 'selected'
            : null
          : (impactByWordId.get(word.canonicalQuranWordId) ?? null),
    }));
  });

  private isSelectedWord(
    isSourceMatch: boolean,
    mergedMatches: ReadonlySet<number> | null,
    quranWordId: number,
  ): boolean {
    return this.highlightSourceWords() &&
      (mergedMatches === null ? isSourceMatch : mergedMatches.has(quranWordId));
  }
}

function toImpactByWordId(
  impact: LinkingDoorWordImpact,
): ReadonlyMap<number, Exclude<LinkingAyahWordHighlight, 'selected' | null>> {
  return new Map([
    ...impact.added.map((wordId) => [wordId, 'added'] as const),
    ...impact.existing.map((wordId) => [wordId, 'existing'] as const),
    ...impact.removed.map((wordId) => [wordId, 'removed'] as const),
  ]);
}
