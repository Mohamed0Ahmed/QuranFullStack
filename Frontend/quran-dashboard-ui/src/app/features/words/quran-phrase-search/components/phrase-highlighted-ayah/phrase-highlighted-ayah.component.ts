import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PhraseAyahWordDto } from '../../../../../core/api/generated/models/phrase-ayah-word-dto';
import { PhraseContextHighlightsDto } from '../../../../../core/api/generated/models/phrase-context-highlights-dto';
import { PhraseSimilarityHighlightsDto } from '../../../../../core/api/generated/models/phrase-similarity-highlights-dto';

type PhraseHighlightScheme = 'query' | 'context' | 'similarity';
type PhraseWordRole = 'query' | 'previous' | 'following' | 'matched' | 'differing' | null;

@Component({
  selector: 'qd-phrase-highlighted-ayah',
  standalone: true,
  templateUrl: './phrase-highlighted-ayah.component.html',
  styleUrl: './phrase-highlighted-ayah.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseHighlightedAyahComponent {
  readonly words = input.required<readonly PhraseAyahWordDto[]>();
  readonly queryQuranWordIds = input<readonly number[]>([]);
  readonly compact = input(false);
  readonly roleScheme = input<PhraseHighlightScheme>('query');
  readonly contextHighlights = input<PhraseContextHighlightsDto | null>(null);
  readonly similarityHighlights = input<PhraseSimilarityHighlightsDto | null>(null);

  private readonly queryWordIds = computed(() => new Set(this.queryQuranWordIds()));
  private readonly contextQueryIds = computed(
    () => new Set(this.contextHighlights()?.queryQuranWordIds ?? []),
  );
  private readonly previousIds = computed(
    () => new Set(this.contextHighlights()?.previousQuranWordIds ?? []),
  );
  private readonly followingIds = computed(
    () => new Set(this.contextHighlights()?.followingQuranWordIds ?? []),
  );
  private readonly matchedIds = computed(
    () => new Set(this.similarityHighlights()?.matchedQuranWordIds ?? []),
  );
  private readonly differingIds = computed(
    () => new Set(this.similarityHighlights()?.differingQuranWordIds ?? []),
  );

  protected wordRole(quranWordId: number): PhraseWordRole {
    if (this.roleScheme() === 'context') {
      if (this.contextQueryIds().has(quranWordId)) {
        return 'query';
      }
      if (this.previousIds().has(quranWordId)) {
        return 'previous';
      }
      return this.followingIds().has(quranWordId) ? 'following' : null;
    }
    if (this.roleScheme() === 'similarity') {
      if (this.differingIds().has(quranWordId)) {
        return 'differing';
      }
      return this.matchedIds().has(quranWordId) ? 'matched' : null;
    }
    return this.queryWordIds().has(quranWordId) ? 'query' : null;
  }

  protected wordAriaLabel(word: PhraseAyahWordDto): string {
    const role = this.wordRole(word.quranWordId);
    const labels: Record<Exclude<PhraseWordRole, null>, string> = {
      query: 'كلمة من عبارة البحث',
      previous: 'كلمة من السياق السابق',
      following: 'كلمة من السياق اللاحق',
      matched: 'موضع مطابق',
      differing: 'موضع مختلف',
    };
    return role ? `${labels[role]}: ${word.textUthmani}` : word.textUthmani;
  }
}
