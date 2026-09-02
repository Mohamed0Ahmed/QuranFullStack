import type {
  AyahCoreDto,
  AyahMutashabihatResponse as AyahMutashabihatDto,
  AyahStudyResponse as AyahStudyWireDto,
  FullI3RabEntryDto as FullI3rabEntryWireDto,
  LocalizedLabel,
  MushafLineDto as MushafLineWireDto,
  MushafPageResponse as MushafPageWireDto,
  MushafStudySourceCatalogResponse as MushafStudySourceCatalogDto,
  MushafSurahCatalogItem as MushafSurahCatalogItemDto,
  MushafWordDto as MushafWordWireDto,
  MutashabihatGroupDto,
  MutashabihatOccurrenceDto,
  MutashabihatSelectedOccurrenceDto,
  PageMarkerDto as PageMarkerWireDto,
  PageNavigationSummary as PageNavigationSummaryDto,
  RenderedSegmentDto as RenderedSegmentWireDto,
  SajdaDto,
  SelectedSourcesDto,
  SimilarAyahItemDto as SimilarAyahItemWireDto,
  SimilarAyahsResponse as SimilarAyahsWireDto,
  SimilaritySummaryDto as AyahSimilaritySummaryDto,
  StudySourceCatalogItem as StudySourceCatalogItemDto,
  SurahOnPage as SurahOnPageDto,
  TafsirEntryDto as TafsirEntryWireDto,
  TranslationEntryDto as TranslationEntryWireDto,
  UniqueSimpleWordCountSummary,
  UniqueWordCountSummary,
  WordAnalysisResponse as WordAnalysisWireDto,
  WordCountSummary,
  WordIdentityDto,
  WordMorphologyDto,
  WordOccurrenceDto,
  AyahRange as AyahRangeDto,
} from '../../../core/api/generated/models';
import type { QuranVerseKey, QuranWordLocation } from '../../../shared/quran/quran-location';

export type {
  AyahCoreDto,
  AyahMutashabihatDto,
  AyahRangeDto,
  AyahSimilaritySummaryDto,
  LocalizedLabel,
  MushafStudySourceCatalogDto,
  MushafSurahCatalogItemDto,
  MutashabihatGroupDto,
  MutashabihatOccurrenceDto,
  MutashabihatSelectedOccurrenceDto,
  PageNavigationSummaryDto,
  SajdaDto,
  SelectedSourcesDto,
  StudySourceCatalogItemDto,
  SurahOnPageDto,
  UniqueSimpleWordCountSummary,
  UniqueWordCountSummary,
  WordCountSummary,
  WordIdentityDto,
  WordMorphologyDto,
  WordOccurrenceDto,
};

export interface MushafWordDto extends Omit<MushafWordWireDto, 'verseKey' | 'wordLocation'> {
  verseKey: QuranVerseKey;
  wordLocation: QuranWordLocation;
}

export interface MushafLineDto extends Omit<MushafLineWireDto, 'lineType' | 'words'> {
  lineType: 'ayah' | 'surah_name' | 'basmallah';
  words: MushafWordDto[];
}

export interface PageMarkerDto extends Omit<PageMarkerWireDto, 'markerType' | 'verseKey' | 'wordLocation'> {
  markerType: 'juz' | 'hizb' | 'rub' | 'sajda';
  verseKey: QuranVerseKey;
  wordLocation: QuranWordLocation;
}

export interface MushafPageDto extends Omit<MushafPageWireDto, 'lines' | 'markers'> {
  lines: MushafLineDto[];
  markers: PageMarkerDto[];
}

export interface MushafSurahJuzGroupDto {
  juzNumber: number;
  surahs: MushafSurahCatalogItemDto[];
}

export interface TafsirEntryDto extends Omit<TafsirEntryWireDto, 'direction' | 'sourceValueKind'> {
  direction: 'rtl' | 'ltr';
  sourceValueKind: 'leader' | 'member_pointer' | 'flat';
}

export interface TranslationEntryDto extends Omit<TranslationEntryWireDto, 'direction'> {
  direction: 'rtl' | 'ltr';
}

export interface FullI3rabEntryDto extends Omit<FullI3rabEntryWireDto, 'markupFormat' | 'sourceValueKind'> {
  markupFormat: 'html';
  sourceValueKind: 'leader' | 'member_pointer' | 'flat';
}

export type SimilarAyahRelationshipDirection = 'outgoing' | 'incoming' | 'bidirectional';

export interface SimilarAyahItemDto extends Omit<SimilarAyahItemWireDto, 'relationshipDirection'> {
  relationshipDirection: SimilarAyahRelationshipDirection;
}

export interface SimilarAyahsDto extends Omit<SimilarAyahsWireDto, 'items'> {
  items: SimilarAyahItemDto[];
}

export interface AyahNavigationTarget {
  verseKey: QuranVerseKey;
  pageNumber: number;
}

export const SIMILAR_AYAHS_EMPTY_MESSAGE =
  'لا توجد آيات قريبة في المعنى لهذه الآية في البيانات الحالية.';

export const SIMILAR_AYAHS_LOADING_MESSAGE = 'جارٍ تحميل الآيات القريبة...';

export const MUTASHABIHAT_EMPTY_MESSAGE =
  'لا توجد متشابهات لفظية مسجلة لهذه الآية في البيانات الحالية.';

export const MUTASHABIHAT_LOADING_MESSAGE = 'جارٍ تحميل المتشابهات اللفظية...';

export interface AyahStudyDto extends Omit<AyahStudyWireDto, 'tafsir' | 'translation' | 'fullI3rab'> {
  tafsir: TafsirEntryDto | null;
  translation: TranslationEntryDto | null;
  fullI3rab: FullI3rabEntryDto | null;
}

export type SegmentDisplayTextStatus = 'available' | 'missing';

export interface RenderedSegmentDto extends Omit<RenderedSegmentWireDto, 'displayTextStatus'> {
  displayTextStatus: SegmentDisplayTextStatus;
}

export interface WordAnalysisDto extends Omit<WordAnalysisWireDto, 'renderedWordSegments'> {
  renderedWordSegments: RenderedSegmentDto[];
}

export type PanelMode = 'ayah' | 'word' | 'doors' | 'none';
export type AyahStudyTab = 'tafsir' | 'translation' | 'full-i3rab' | 'similar-ayahs' | 'mutashabihat';
export type AyahStudyGroup = 'sources' | 'similarity';
export type WordAnalysisTab = 'morphology' | 'segments' | 'i3rab' | 'identity';

export const AYAH_STUDY_TABS_BY_GROUP = {
  sources: ['tafsir', 'translation', 'full-i3rab'],
  similarity: ['mutashabihat', 'similar-ayahs'],
} as const satisfies Record<AyahStudyGroup, readonly AyahStudyTab[]>;

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
  selectedAyahKey: QuranVerseKey | null;
  selectedWordLocation: QuranWordLocation | null;
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
