import { WordTypeDetailScope } from '../../models/word-types-detail.models';
import {
  DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  WordTypeDetailView,
  WordTypeTableRowDto,
} from '../../models/word-types.models';
import {
  buildWordTypesDetailScopeQuery,
  buildWordTypesQueryParams,
  canonicalWordTypesDetailPage,
  clearWordTypesSelection,
} from '../../state/word-types-url-sync';

export function defaultWordTypeTableDetailView(row: WordTypeTableRowDto): WordTypeDetailView {
  return row.kind === 'word' ? DEFAULT_WORD_TYPES_DETAIL_VIEW : DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW;
}

export function buildWordTypeTableDetailQuery(
  row: WordTypeTableRowDto,
  scope: WordTypeDetailScope,
  view: WordTypeDetailView,
  column: string,
): Record<string, string | null> {
  const selection = row.kind === 'word'
    ? { word: row.tashkeelWordId, contextCode: row.contextCode }
    : row.kind === 'root'
      ? { root: row.rootId }
      : row.kind === 'stem'
        ? { stem: row.stemId }
        : { lemma: row.lemmaId };

  return {
    ...clearWordTypesSelection(),
    ...buildWordTypesQueryParams({
      ...selection,
      ...buildWordTypesDetailScopeQuery({ scope }),
      view,
      detailPage: canonicalWordTypesDetailPage(view, DEFAULT_WORD_TYPES_DETAIL_PAGE),
      location: null,
      column,
    }),
  };
}
