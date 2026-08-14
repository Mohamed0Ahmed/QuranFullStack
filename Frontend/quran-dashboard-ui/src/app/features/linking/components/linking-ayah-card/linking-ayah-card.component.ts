import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { toQuranWordDisplayText } from '../../../../shared/quran/quran-word-display-text';
import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';

type LinkingAyahWordHighlight = 'selected' | 'added' | 'existing' | 'removed' | null;

interface LinkingDoorWordImpact {
  added: readonly number[];
  existing: readonly number[];
  removed: readonly number[];
}

interface LinkingAyahCardWord {
  renderPosition: number;
  canonicalQuranWordId: number;
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
  readonly wordImpact = input<LinkingDoorWordImpact | null>(null);
  readonly statusLabel = input<string | null>(null);
  readonly wordSelectable = input(false);
  readonly wordToggled = output<number>();

  protected readonly labels = LINKING_LABELS;
  protected readonly displayWords = computed<readonly LinkingAyahCardWord[]>(() => {
    const wordImpact = this.wordImpact();
    const impactByWordId = wordImpact === null ? null : toImpactByWordId(wordImpact);
    return this.ayah().words
      .filter((word) => !word.isAyahMarker)
      .map((word) => ({
        renderPosition: word.renderPosition,
        canonicalQuranWordId: word.canonicalQuranWordId,
        textUthmani: toQuranWordDisplayText(word.textUthmani),
        highlight:
          impactByWordId === null
            ? this.highlightSourceWords() && word.isSourceMatch
              ? 'selected'
              : null
            : (impactByWordId.get(word.canonicalQuranWordId) ?? null),
      }));
  });

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
