import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { MergedLinkingWordSelection } from '../../models/linking-merge.models';
import { LINKING_LABELS } from '../../models/linking.labels';

interface LinkingAyahCardWord {
  renderPosition: number;
  textUthmani: string;
  isMatched: boolean;
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

  protected readonly labels = LINKING_LABELS;
  protected readonly displayWords = computed<readonly LinkingAyahCardWord[]>(() => {
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
      textUthmani: word.textUthmani,
      isMatched:
        mergedMatches === null ? word.isSourceMatch : mergedMatches.has(word.canonicalQuranWordId),
    }));
  });

  protected isMatched(isMatched: boolean): boolean {
    return this.highlightSourceWords() && isMatched;
  }
}
