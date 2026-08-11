export type LinkingSourceKind =
  | 'mushaf-word'
  | 'unique-word'
  | 'root'
  | 'lemma'
  | 'stem'
  | 'word-type';

export type LinkingUniqueWordMode = 'simple' | 'tashkeel';
export type LinkingWordTypeMainType = 'noun' | 'verb' | 'particle' | 'inl';
export type LinkingWordTypeCase = 'all' | 'nominative' | 'accusative' | 'genitive' | 'null';
export type LinkingWordTypeTense = 'all' | 'past' | 'present' | 'imperative';
export type LinkingWordTypeVoice = 'all' | 'active' | 'passive';

export interface LinkingWordTypeScope {
  type: LinkingWordTypeMainType;
  childCode: string | null;
  case: LinkingWordTypeCase;
  tense: LinkingWordTypeTense;
  voice: LinkingWordTypeVoice;
}

export type LinkingWordTypeSelection =
  | {
      kind: 'word';
      tashkeelWordId: number;
      contextCode: string;
      case: LinkingWordTypeCase;
      tense: LinkingWordTypeTense;
      voice: LinkingWordTypeVoice;
      scope: LinkingWordTypeScope;
    }
  | { kind: 'root'; rootId: number; scope: LinkingWordTypeScope }
  | { kind: 'stem'; stemId: number; scope: LinkingWordTypeScope }
  | { kind: 'lemma'; lemmaId: number; scope: LinkingWordTypeScope };

export type LinkingSourceDescriptor =
  | {
      kind: 'mushaf-word';
      quranWordId: number;
      wordLocation: string;
      verseKey: string;
      pageNumber: number;
      label: string;
    }
  | { kind: 'unique-word'; mode: LinkingUniqueWordMode; wordId: number; label: string }
  | { kind: 'root'; rootId: number; label: string }
  | { kind: 'lemma'; lemmaId: number; typeCode: string | null; label: string }
  | { kind: 'stem'; stemId: number; typeCode: string | null; label: string }
  | { kind: 'word-type'; selection: LinkingWordTypeSelection; label: string };

const wordTypeMainTypes: readonly LinkingWordTypeMainType[] = ['noun', 'verb', 'particle', 'inl'];
const wordTypeCases: readonly LinkingWordTypeCase[] = ['all', 'nominative', 'accusative', 'genitive', 'null'];
const wordTypeTenses: readonly LinkingWordTypeTense[] = ['all', 'past', 'present', 'imperative'];
const wordTypeVoices: readonly LinkingWordTypeVoice[] = ['all', 'active', 'passive'];

export function isLinkingSourceDescriptor(value: unknown): value is LinkingSourceDescriptor {
  if (!isRecord(value) || !isNonBlankString(value['label'])) {
    return false;
  }

  switch (value['kind']) {
    case 'mushaf-word':
      return (
        isPositiveSafeInteger(value['quranWordId']) &&
        isWordLocation(value['wordLocation']) &&
        isVerseKey(value['verseKey']) &&
        isPageNumber(value['pageNumber'])
      );
    case 'unique-word':
      return (
        (value['mode'] === 'simple' || value['mode'] === 'tashkeel') &&
        isPositiveSafeInteger(value['wordId'])
      );
    case 'root':
      return isPositiveSafeInteger(value['rootId']);
    case 'lemma':
      return isPositiveSafeInteger(value['lemmaId']) && isNullableTypeCode(value['typeCode']);
    case 'stem':
      return isPositiveSafeInteger(value['stemId']) && isNullableTypeCode(value['typeCode']);
    case 'word-type':
      return isLinkingWordTypeSelection(value['selection']);
    default:
      return false;
  }
}

export function isLinkingWordTypeSelection(value: unknown): value is LinkingWordTypeSelection {
  if (!isRecord(value) || !isLinkingWordTypeScope(value['scope'])) {
    return false;
  }

  switch (value['kind']) {
    case 'word':
      return (
        isPositiveSafeInteger(value['tashkeelWordId']) &&
        isNonBlankString(value['contextCode']) &&
        isWordTypeCase(value['case']) &&
        isWordTypeTense(value['tense']) &&
        isWordTypeVoice(value['voice'])
      );
    case 'root':
      return isPositiveSafeInteger(value['rootId']);
    case 'stem':
      return isPositiveSafeInteger(value['stemId']);
    case 'lemma':
      return isPositiveSafeInteger(value['lemmaId']);
    default:
      return false;
  }
}

export function isLinkingWordTypeScope(value: unknown): value is LinkingWordTypeScope {
  return (
    isRecord(value) &&
    isWordTypeMainType(value['type']) &&
    (value['childCode'] === null || isNonBlankString(value['childCode'])) &&
    isWordTypeCase(value['case']) &&
    isWordTypeTense(value['tense']) &&
    isWordTypeVoice(value['voice'])
  );
}

export function isVerseKey(value: unknown): value is string {
  if (typeof value !== 'string') {
    return false;
  }

  const match = /^(\d{1,3}):(\d{1,3})$/.exec(value);
  if (!match) {
    return false;
  }

  const surahNumber = Number(match[1]);
  const ayahNumber = Number(match[2]);
  return surahNumber >= 1 && surahNumber <= 114 && ayahNumber >= 1 && ayahNumber <= 286;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isPositiveSafeInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isPageNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 1 && value <= 604;
}

function isNonBlankString(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}

function isNullableTypeCode(value: unknown): value is string | null {
  return value === null || isNonBlankString(value);
}

function isWordLocation(value: unknown): value is string {
  return typeof value === 'string' && /^\d{1,3}:\d{1,3}:\d{1,2}$/.test(value);
}

function isWordTypeMainType(value: unknown): value is LinkingWordTypeMainType {
  return wordTypeMainTypes.includes(value as LinkingWordTypeMainType);
}

function isWordTypeCase(value: unknown): value is LinkingWordTypeCase {
  return wordTypeCases.includes(value as LinkingWordTypeCase);
}

function isWordTypeTense(value: unknown): value is LinkingWordTypeTense {
  return wordTypeTenses.includes(value as LinkingWordTypeTense);
}

function isWordTypeVoice(value: unknown): value is LinkingWordTypeVoice {
  return wordTypeVoices.includes(value as LinkingWordTypeVoice);
}
