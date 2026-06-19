/**
 * Mushaf Reader study-context models.
 *
 * Mirrors the v1 backend contracts in
 * `specs/011-mushaf-reader-study-context/data-model.md` (sections B and C) and
 * the URL-key/enum scope locked there. DTOs match the JSON shapes returned by
 * the backend; view models are the page-ready forms the facade emits to
 * components.
 *
 * Locked v1 enum scope (do not widen):
 *   panel   in { 'ayah' | 'word' | 'none' }
 *   ayahTab in { 'tafsir' | 'translation' | 'full-i3rab' }
 *   wordTab in { 'morphology' | 'segments' | 'i3rab' | 'identity' }
 * `panel=sources` and `ayahTab=links` are out of scope for v1.
 */

/* ============================================================================
 * B1. Mushaf page DTO (lean: no tafsir / translation / i3rab / morphology)
 * ========================================================================== */

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

export interface MushafSurahCatalogDto {
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

/* ============================================================================
 * B2. Ayah study DTO (three sources together)
 * ========================================================================== */

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

export interface AyahStudyDto {
  ayah: AyahCoreDto;
  selectedSources: SelectedSourcesDto;
  tafsir: TafsirEntryDto | null;
  translation: TranslationEntryDto | null;
  fullI3rab: FullI3rabEntryDto | null;
}

/* ============================================================================
 * B3. Word analysis DTO
 * ========================================================================== */

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

/* ============================================================================
 * C. View models (UI-ready) + reader state + URL keys
 * ========================================================================== */

export type PanelMode = 'ayah' | 'word' | 'none';
export type AyahStudyTab = 'tafsir' | 'translation' | 'full-i3rab';
export type WordAnalysisTab = 'morphology' | 'segments' | 'i3rab' | 'identity';

/** A selectable source entry for the ayah-study source selectors. */
export interface SourceOption {
  key: string;
  label: string;
  languageNameAr?: string | null;
}

/**
 * A segment enriched for rendering: the backend `segmentColorSlot` mapped to a
 * concrete color, plus an `isMissing` flag derived from `displayTextStatus`.
 * Colors are visual-linking only (slot -> palette), never POS-semantic.
 */
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

/** Per-resource loading primitives consumed by `qd-` state components. */
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

/**
 * The full reader view state. URL <-> state synchronization is owned by the
 * facade; this shape is what the shell + child components render from.
 */
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
}

/** Stable, natural Quran keys used in the URL query params. */
export const MUSHAF_URL_KEYS = {
  page: 'page',
  ayah: 'ayah',
  word: 'word',
  segment: 'segment',
  panel: 'panel',
  ayahTab: 'ayahTab',
  wordTab: 'wordTab',
  tafsirSource: 'tafsirSource',
  translationSource: 'translationSource',
  fullI3rabSource: 'fullI3rabSource',
} as const;

/** Default reader state used to seed the facade before the URL is hydrated. */
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
};
