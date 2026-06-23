export interface SurahOnPageDto {
  surahNumber: number;
  nameArabic: string;
  firstAyahOnPage: number;
  lastAyahOnPage: number;
}

export interface AyahRangeDto {
  firstVerseKey: string;
  lastVerseKey: string;
}

export interface PageNavigationSummaryDto {
  juzNumbers: number[];
  hizbNumbers: number[];
  rubNumbers: number[];
}

export interface MushafWordDto {
  wordLocation: string;
  verseKey: string;
  wordNumber: number;
  lineWordOrder: number;
  textUthmani: string;
  isAyahMarker: boolean;
}

export interface MushafLineDto {
  lineNumber: number;
  lineType: 'ayah' | 'surah_name' | 'basmallah';
  isCentered: boolean;
  surahNumber: number | null;
  words: MushafWordDto[];
}

export interface PageMarkerDto {
  markerType: 'juz' | 'hizb' | 'rub' | 'sajda';
  markerNumber: number;
  verseKey: string;
  lineNumber: number;
  wordLocation: string;
  sajdahType: string | null;
}

export interface MushafPageDto {
  pageNumber: number;
  previousPageNumber: number | null;
  nextPageNumber: number | null;
  surahs: SurahOnPageDto[];
  ayahRange: AyahRangeDto;
  navigation: PageNavigationSummaryDto;
  lines: MushafLineDto[];
  markers: PageMarkerDto[];
}

export interface MushafSurahCatalogItemDto {
  surahNumber: number;
  nameArabic: string;
  startPageNumber: number;
}

export interface MushafSurahJuzGroupDto {
  juzNumber: number;
  surahs: MushafSurahCatalogItemDto[];
}

export interface StudySourceCatalogItemDto {
  sourceKey: string;
  displayNameAr: string;
  displayNameEn: string | null;
  languageCode: string;
  languageNameAr: string | null;
  direction: string;
  tafsirKind: string | null;
  translationType: string | null;
}

export interface MushafStudySourceCatalogDto {
  tafsirSources: StudySourceCatalogItemDto[];
  translationSources: StudySourceCatalogItemDto[];
  fullI3rabSources: StudySourceCatalogItemDto[];
}

export interface SajdaDto {
  sajdahNumber: number;
  verseKey: string;
  sajdahType: string;
}

export interface AyahCoreDto {
  verseKey: string;
  surahNumber: number;
  surahNameArabic: string;
  ayahNumber: number;
  textUthmani: string;
  wordsCount: number;
  pageFrom: number;
  pageTo: number;
  juzNumber: number;
  hizbNumber: number;
  rubNumber: number;
  sajda: SajdaDto | null;
}

export interface SelectedSourcesDto {
  tafsirSource: string | null;
  translationSource: string | null;
  fullI3rabSource: string | null;
}

export interface TafsirEntryDto {
  sourceKey: string;
  displayNameAr: string;
  shortNameAr: string | null;
  languageCode: string;
  direction: 'rtl' | 'ltr';
  tafsirKind: string;
  sourceValueKind: 'leader' | 'member_pointer' | 'flat';
  sourceLeaderVerseKey: string | null;
  isGroupLeader: boolean;
  coveredAyahCount: number;
  coveredAyahKeys: string[];
  text: string;
}

export interface TranslationEntryDto {
  sourceKey: string;
  displayNameAr: string | null;
  displayNameEn: string | null;
  languageCode: string;
  direction: 'rtl' | 'ltr';
  translationType: string;
  containsHtmlMarkup: boolean;
  text: string;
}

export interface FullI3rabEntryDto {
  sourceKey: string;
  displayNameAr: string;
  shortNameAr: string | null;
  markupFormat: 'html';
  sourceValueKind: 'leader' | 'member_pointer' | 'flat';
  sourceLeaderVerseKey: string | null;
  isGroupLeader: boolean;
  coveredAyahCount: number;
  coveredAyahKeys: string[];
  html: string;
}

export interface AyahSimilaritySummaryDto {
  similarAyahCount: number;
  mutashabihatGroupCount: number;
  mutashabihatOccurrenceCount: number;
}

export type SimilarAyahRelationshipDirection = 'outgoing' | 'incoming' | 'bidirectional';

export interface SimilarAyahItemDto {
  targetVerseKey: string;
  surahNumber: number;
  surahNameArabic: string;
  ayahNumber: number;
  pageNumber: number;
  juzNumber: number;
  hizbNumber: number;
  rubNumber: number;
  textUthmani: string;
  score: number;
  coverage: number;
  matchedWordsCount: number;
  relationshipDirection: SimilarAyahRelationshipDirection;
  hasReverseLink: boolean;
}

export interface SimilarAyahsDto {
  verseKey: string;
  count: number;
  items: SimilarAyahItemDto[];
}

export interface AyahNavigationTarget {
  verseKey: string;
  pageNumber: number;
}

export const SIMILAR_AYAHS_EMPTY_MESSAGE =
  'لا توجد آيات قريبة في المعنى لهذه الآية في البيانات الحالية.';

export const SIMILAR_AYAHS_LOADING_MESSAGE = 'جارٍ تحميل الآيات القريبة...';

export interface MutashabihatSelectedOccurrenceDto {
  verseKey: string;
  wordFrom: number;
  wordTo: number;
  isRepresentative: boolean;
  phraseTextUthmani: string | null;
}

export interface MutashabihatOccurrenceDto {
  verseKey: string;
  surahNumber: number;
  surahNameArabic: string;
  ayahNumber: number;
  pageNumber: number;
  wordFrom: number;
  wordTo: number;
  isSelectedAyah: boolean;
  isRepresentative: boolean;
  textUthmani: string;
  phraseTextUthmani: string | null;
}

export interface MutashabihatGroupDto {
  groupKey: string;
  sourceGroupId: number;
  representativeVerseKey: string;
  representativeWordFrom: number;
  representativeWordTo: number;
  phraseTextUthmani: string | null;
  occurrenceCount: number;
  distinctAyahCount: number;
  distinctSurahCount: number;
  selectedOccurrences: MutashabihatSelectedOccurrenceDto[];
  occurrences: MutashabihatOccurrenceDto[];
}

export interface AyahMutashabihatDto {
  verseKey: string;
  groupCount: number;
  groups: MutashabihatGroupDto[];
}

export const MUTASHABIHAT_EMPTY_MESSAGE =
  'لا توجد متشابهات لفظية مسجلة لهذه الآية في البيانات الحالية.';

export const MUTASHABIHAT_LOADING_MESSAGE = 'جارٍ تحميل المتشابهات اللفظية...';

export interface AyahStudyDto {
  ayah: AyahCoreDto;
  selectedSources: SelectedSourcesDto;
  tafsir: TafsirEntryDto | null;
  translation: TranslationEntryDto | null;
  fullI3rab: FullI3rabEntryDto | null;
  similaritySummary: AyahSimilaritySummaryDto;
}

export interface WordOccurrenceDto {
  quranWordId: number;
  wordLocation: string;
  verseKey: string;
  surahNumber: number;
  ayahNumber: number;
  wordNumber: number;
  pageNumber: number;
  lineNumber: number;
  lineWordOrder: number;
  textUthmani: string;
  textUthmaniSimple: string | null;
  textImlaeiSimple: string | null;
  qpcGlyph: string | null;
}

export interface WordCountSummary {
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
}

export interface UniqueWordCountSummary extends WordCountSummary {
  id: number;
}

export interface UniqueSimpleWordCountSummary extends UniqueWordCountSummary {
  wordKeyImlaeiSimple: string;
}

export interface WordIdentityDto {
  orderedTashkeel: WordCountSummary;
  orderedSimple: WordCountSummary;
  uniqueTashkeel: UniqueWordCountSummary;
  uniqueSimple: UniqueSimpleWordCountSummary;
}

export interface LocalizedLabel {
  ar: string;
  en: string;
}

export interface WordMorphologyDto {
  headPos: string;
  headPosLabel: LocalizedLabel;
  root: { text: string | null; buckwalter: string | null } | null;
  lemma: { text: string | null; buckwalter: string | null } | null;
  stem: { text: string | null } | null;
  isVerb: boolean;
  verbTense: string | null;
  verbVoice: string | null;
  caseFeature: string | null;
}

export type SegmentDisplayTextStatus = 'available' | 'missing';

export interface RenderedSegmentDto {
  segmentLocation: string;
  segmentNumber: number;
  segmentColorSlot: number;
  segmentKind: string | null;
  segmentDisplayText: string | null;
  displayTextStatus: SegmentDisplayTextStatus;
  segmentPos: string | null;
  segmentPosLabel: LocalizedLabel | null;
  segmentI3rabArabic: string | null;
  i3rabRuleId: number | null;
  i3rabRuleSignature: string | null;
  i3rabRuleFamily: string | null;
  i3rabStatus: string | null;
  segmentFeatures: { raw: string | null; json: object[] } | null;
}

export interface WordAnalysisDto {
  word: WordOccurrenceDto;
  identity: WordIdentityDto;
  morphology: WordMorphologyDto;
  renderedWordSegments: RenderedSegmentDto[];
}

export type PanelMode = 'ayah' | 'word' | 'none';
export type AyahStudyTab = 'tafsir' | 'translation' | 'full-i3rab' | 'similar-ayahs' | 'mutashabihat';
export type WordAnalysisTab = 'morphology' | 'segments' | 'i3rab' | 'identity';

export const AYAH_STUDY_TAB_LABELS: Record<AyahStudyTab, { full: string; short: string }> = {
  tafsir: { full: 'التفسير', short: 'التفسير' },
  translation: { full: 'الترجمة', short: 'الترجمة' },
  'full-i3rab': { full: 'الإعراب الكامل', short: 'الإعراب' },
  'similar-ayahs': { full: 'آيات قريبة في المعنى', short: 'آيات قريبة' },
  mutashabihat: { full: 'المتشابهات اللفظية للحفظ', short: 'المتشابهات' },
};

export interface SourceOption {
  key: string;
  label: string;
  languageCode?: string | null;
  languageNameAr?: string | null;
}

export interface RenderedSegmentViewModel {
  segmentLocation: string;
  segmentNumber: number;
  segmentColorSlot: number;
  color: string;
  segmentKind: string | null;
  segmentDisplayText: string | null;
  isMissing: boolean;
  segmentPos: string | null;
  segmentPosLabel: LocalizedLabel | null;
  segmentI3rabArabic: string | null;
  i3rabStatus: string | null;
}

export interface WordAnalysisViewModel {
  word: WordOccurrenceDto;
  identity: WordIdentityDto;
  morphology: WordMorphologyDto;
  segments: RenderedSegmentViewModel[];
}

export interface AyahStudyViewModel {
  ayah: AyahCoreDto;
  selectedSources: SelectedSourcesDto;
  tafsir: TafsirEntryDto | null;
  translation: TranslationEntryDto | null;
  fullI3rab: FullI3rabEntryDto | null;
  similaritySummary: AyahSimilaritySummaryDto;
}

export interface MushafPageViewModel {
  pageNumber: number;
  previousPageNumber: number | null;
  nextPageNumber: number | null;
  surahs: SurahOnPageDto[];
  ayahRange: AyahRangeDto;
  navigation: PageNavigationSummaryDto;
  lines: MushafLineDto[];
  markers: PageMarkerDto[];
}

export interface ResourceLoadState {
  isLoading: boolean;
  isEmpty: boolean;
  errorMessage: string | null;
}

export interface MushafReaderSources {
  tafsirSource: string | null;
  translationSource: string | null;
  fullI3rabSource: string | null;
}

export interface MushafReaderState {
  pageNumber: number;
  selectedAyahKey: string | null;
  selectedWordLocation: string | null;
  selectedSegmentLocation: string | null;
  panel: PanelMode;
  ayahTab: AyahStudyTab;
  wordTab: WordAnalysisTab;
  sources: MushafReaderSources;
  page: ResourceLoadState;
  ayahStudy: ResourceLoadState;
  wordAnalysis: ResourceLoadState;
  similarAyahs: ResourceLoadState;
  mutashabihat: ResourceLoadState;
}

export const MUSHAF_URL_KEYS = {
  page: 'page',
  ayah: 'ayah',
  focusAyah: 'focusAyah',
  word: 'word',
  segment: 'segment',
  panel: 'panel',
  ayahTab: 'ayahTab',
  wordTab: 'wordTab',
  tafsirSource: 'tafsirSource',
  translationSource: 'translationSource',
  fullI3rabSource: 'fullI3rabSource',
} as const;

export const DEFAULT_MUSHAF_READER_STATE: MushafReaderState = {
  pageNumber: 1,
  selectedAyahKey: null,
  selectedWordLocation: null,
  selectedSegmentLocation: null,
  panel: 'none',
  ayahTab: 'tafsir',
  wordTab: 'segments',
  sources: {
    tafsirSource: null,
    translationSource: null,
    fullI3rabSource: null,
  },
  page: { isLoading: false, isEmpty: false, errorMessage: null },
  ayahStudy: { isLoading: false, isEmpty: false, errorMessage: null },
  wordAnalysis: { isLoading: false, isEmpty: false, errorMessage: null },
  similarAyahs: { isLoading: false, isEmpty: false, errorMessage: null },
  mutashabihat: { isLoading: false, isEmpty: false, errorMessage: null },
};
