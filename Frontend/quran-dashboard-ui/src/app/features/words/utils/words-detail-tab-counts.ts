import { LemmaSummaryDto, LemmaView, LemmaWordView } from '../models/lemmas.models';
import { RootSummaryDto, RootView, RootWordView } from '../models/roots.models';
import { StemSummaryDto, StemView, StemWordView } from '../models/stems.models';
import { WordTypeDetailView } from '../models/word-types.models';

export type WordsDetailTabCounts<TKey extends string> = Partial<Record<TKey, number | null>>;

export function rootDetailTabCounts(
  summary: RootSummaryDto | null,
  wordView: RootWordView,
): WordsDetailTabCounts<RootView> {
  return {
    words: morphologyWordCount(summary, wordView),
    ayahs: summary?.ayahsCount ?? null,
    surahs: summary?.surahsCount ?? null,
    lemmas: summary?.lemmasCount ?? null,
    stems: summary?.stemsCount ?? null,
  };
}

export function lemmaDetailTabCounts(
  summary: LemmaSummaryDto | null,
  wordView: LemmaWordView,
): WordsDetailTabCounts<LemmaView> {
  return {
    words: morphologyWordCount(summary, wordView),
    ayahs: summary?.ayahsCount ?? null,
    surahs: summary?.surahsCount ?? null,
    stems: summary?.stemsCount ?? null,
  };
}

export function stemDetailTabCounts(
  summary: StemSummaryDto | null,
  wordView: StemWordView,
  lemmasCount: number | null,
): WordsDetailTabCounts<StemView> {
  return {
    words: morphologyWordCount(summary, wordView),
    ayahs: summary?.ayahsCount ?? null,
    surahs: summary?.surahsCount ?? null,
    lemmas: lemmasCount,
  };
}

export function wordTypeDetailTabCounts(
  wordsCount: number | null,
  ayahsCount: number | null,
  surahsCount: number | null,
): WordsDetailTabCounts<WordTypeDetailView> {
  return { words: wordsCount, ayahs: ayahsCount, surahs: surahsCount };
}

function morphologyWordCount(
  summary: { simpleWordsCount: number; tashkeelWordsCount: number } | null,
  wordView: RootWordView | LemmaWordView | StemWordView,
): number | null {
  if (summary === null) {
    return null;
  }
  return wordView === 'simple' ? summary.simpleWordsCount : summary.tashkeelWordsCount;
}
