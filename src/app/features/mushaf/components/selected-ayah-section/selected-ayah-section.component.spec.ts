import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectedAyahSectionComponent } from './selected-ayah-section.component';
import {
  AyahStudyTab,
  AyahStudyViewModel,
  AyahMutashabihatDto,
  ResourceLoadState,
  SourceOption,
} from '../../models/mushaf.models';

const AYAH_TEXT_PLACEHOLDER = 'نص تجريبي للآية';

const ZERO_SIMILARITY_SUMMARY = {
  similarAyahCount: 0,
  mutashabihatGroupCount: 0,
  mutashabihatOccurrenceCount: 0,
};

const SAMPLE_SIMILARITY_SUMMARY = {
  similarAyahCount: 2,
  mutashabihatGroupCount: 2,
  mutashabihatOccurrenceCount: 3,
};

const IDLE: ResourceLoadState = { isLoading: false, isEmpty: false, errorMessage: null };

const tafsirOptions: SourceOption[] = [
  { key: 'ar-muyassar', label: 'التفسير الميسر', languageCode: 'ar', languageNameAr: 'العربية' },
];
const translationOptions: SourceOption[] = [
  { key: 'en-sahih-international', label: 'Sahih International', languageCode: 'en', languageNameAr: 'الإنجليزية' },
];
const fullI3rabOptions: SourceOption[] = [
  { key: 'muyassar', label: 'الإعراب الميسر', languageCode: 'ar', languageNameAr: 'العربية' },
];

function buildAyahStudyViewModel(verseKey = '2:25'): AyahStudyViewModel {
  return {
    ayah: {
      verseKey,
      surahNumber: 2,
      surahNameArabic: 'البقرة',
      ayahNumber: 25,
      textUthmani: AYAH_TEXT_PLACEHOLDER,
      wordsCount: 5,
      pageFrom: 5,
      pageTo: 5,
      juzNumber: 1,
      hizbNumber: 1,
      rubNumber: 1,
      sajda: null,
    },
    selectedSources: {
      tafsirSource: 'ar-muyassar',
      translationSource: 'en-sahih-international',
      fullI3rabSource: 'muyassar',
    },
    tafsir: {
      sourceKey: 'ar-muyassar',
      displayNameAr: 'التفسير الميسر',
      shortNameAr: null,
      languageCode: 'ar',
      direction: 'rtl',
      tafsirKind: 'brief',
      sourceValueKind: 'leader',
      sourceLeaderVerseKey: verseKey,
      isGroupLeader: true,
      coveredAyahCount: 2,
      coveredAyahKeys: [verseKey, '2:26'],
      text: '<p>تفسير تجريبي</p>',
    },
    translation: {
      sourceKey: 'en-sahih-international',
      displayNameAr: null,
      displayNameEn: 'Sahih International',
      languageCode: 'en',
      direction: 'ltr',
      translationType: 'simple',
      containsHtmlMarkup: false,
      text: 'Sample translation text',
    },
    fullI3rab: {
      sourceKey: 'muyassar',
      displayNameAr: 'الإعراب الميسر',
      shortNameAr: null,
      markupFormat: 'html',
      sourceValueKind: 'flat',
      sourceLeaderVerseKey: verseKey,
      isGroupLeader: true,
      coveredAyahCount: 1,
      coveredAyahKeys: [verseKey],
      html: '<p>إعراب تجريبي</p>',
    },
    similaritySummary: SAMPLE_SIMILARITY_SUMMARY,
  };
}

const SAMPLE_MUTASHABIHAT: AyahMutashabihatDto = {
  verseKey: '2:25',
  groupCount: 1,
  groups: [
    {
      groupKey: 'mutashabihat:90001',
      sourceGroupId: 90001,
      representativeVerseKey: '2:25',
      representativeWordFrom: 1,
      representativeWordTo: 2,
      phraseTextUthmani: 'عبارة-مجموعة-أولى',
      occurrenceCount: 1,
      distinctAyahCount: 1,
      distinctSurahCount: 1,
      selectedOccurrences: [
        {
          verseKey: '2:25',
          wordFrom: 1,
          wordTo: 2,
          isRepresentative: true,
          phraseTextUthmani: 'عبارة-مجموعة-أولى',
        },
      ],
      occurrences: [
        {
          verseKey: '2:25',
          surahNumber: 2,
          surahNameArabic: 'البقرة',
          ayahNumber: 25,
          pageNumber: 5,
          wordFrom: 1,
          wordTo: 2,
          isSelectedAyah: true,
          isRepresentative: true,
          textUthmani: 'نص-آية-25',
          phraseTextUthmani: 'عبارة-مجموعة-أولى',
        },
      ],
    },
  ],
};

function setInputs(
  fixture: ComponentFixture<SelectedAyahSectionComponent>,
  inputs: {
    study?: AyahStudyViewModel | null;
    loadState: ResourceLoadState;
    selectedVerseKey?: string | null;
    activeTab?: AyahStudyTab;
    tafsirOptions?: SourceOption[];
    translationOptions?: SourceOption[];
    fullI3rabOptions?: SourceOption[];
    embedded?: boolean;
    mutashabihat?: AyahMutashabihatDto | null;
    mutashabihatLoadState?: ResourceLoadState;
  },
): void {
  fixture.componentRef.setInput('study', inputs.study ?? null);
  fixture.componentRef.setInput('loadState', inputs.loadState);
  fixture.componentRef.setInput('selectedVerseKey', inputs.selectedVerseKey ?? null);
  fixture.componentRef.setInput('activeTab', inputs.activeTab ?? 'tafsir');
  fixture.componentRef.setInput('tafsirOptions', inputs.tafsirOptions ?? tafsirOptions);
  fixture.componentRef.setInput('translationOptions', inputs.translationOptions ?? translationOptions);
  fixture.componentRef.setInput('fullI3rabOptions', inputs.fullI3rabOptions ?? fullI3rabOptions);
  fixture.componentRef.setInput('embedded', inputs.embedded ?? false);
  fixture.componentRef.setInput('mutashabihat', inputs.mutashabihat ?? null);
  fixture.componentRef.setInput(
    'mutashabihatLoadState',
    inputs.mutashabihatLoadState ?? { isLoading: false, isEmpty: false, errorMessage: null },
  );
  fixture.detectChanges();
}

describe('SelectedAyahSectionComponent — stable loading (UI-001)', () => {
  it('keeps the source slot + tabs mounted and shows stacked shimmer lines while loading', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.qd-loading-state')).toBeNull();
    expect(root.querySelector('[data-testid="ayah-study-loading"]')).toBeTruthy();

    expect(root.querySelector('.selected-ayah-section__source')).toBeTruthy();
    expect(root.querySelector('.selected-ayah-section__tabs')).toBeTruthy();
    expect(root.querySelectorAll('.selected-ayah-section__tab')).toHaveLength(5);
    expect(root.querySelector('.selected-ayah-section__content')).toBeTruthy();

    const contentSkeleton = root.querySelector('[data-testid="ayah-content-skeleton"]');
    expect(contentSkeleton).toBeTruthy();
    expect(contentSkeleton?.querySelectorAll('.qd-skeleton--text').length).toBeGreaterThanOrEqual(3);
    expect(root.querySelector('.qd-loading-overlay')).toBeNull();

    expect(root.querySelector('qd-tafsir-card')).toBeNull();
    expect(root.querySelector('[data-testid="selected-ayah-section-ayah"]')).toBeNull();
  });

  it('shows stacked shimmer lines (not the real study) while loading even if a previous study is provided', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: buildAyahStudyViewModel(),
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.qd-loading-overlay')).toBeNull();
    expect(root.querySelector('[data-testid="ayah-content-skeleton"]')).toBeTruthy();

    expect(root.querySelector('qd-tafsir-card')).toBeNull();
    expect(root.querySelector('[data-testid="selected-ayah-section-ayah"]')).toBeNull();

    const tabs = Array.from(root.querySelectorAll<HTMLButtonElement>('.selected-ayah-section__tab'));
    expect(tabs).toHaveLength(5);
    expect(tabs.every((tab) => tab.disabled)).toBe(true);

    expect(root.querySelector('.selected-ayah-section__source-skeleton')).toBeTruthy();
    expect(root.querySelector('qd-source-selector')).toBeNull();
  });

  it('keeps the static source label visible and shimmers only the value while loading', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedVerseKey: '2:25',
      activeTab: 'tafsir',
    });

    const root = fixture.nativeElement as HTMLElement;

    const label = root.querySelector('.selected-ayah-section__source-loading-label');
    expect(label?.textContent?.trim()).toBe('مصدر التفسير');

    expect(root.querySelector('.selected-ayah-section__source-skeleton')).toBeTruthy();
    expect(root.querySelector('qd-source-selector')).toBeNull();
  });

  it.each([
    { activeTab: 'tafsir' as AyahStudyTab, label: 'مصدر التفسير' },
    { activeTab: 'translation' as AyahStudyTab, label: 'مصدر الترجمة' },
    { activeTab: 'full-i3rab' as AyahStudyTab, label: 'مصدر الإعراب' },
  ])('shows the "$label" source label for the $activeTab tab while loading', ({ activeTab, label }) => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedVerseKey: '2:25',
      activeTab,
    });

    const root = fixture.nativeElement as HTMLElement;
    const loadingLabel = root.querySelector('.selected-ayah-section__source-loading-label');
    expect(loadingLabel?.textContent?.trim()).toBe(label);
  });

  it('keeps the tab buttons mounted but disabled while loading', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;
    const tabs = Array.from(root.querySelectorAll<HTMLButtonElement>('.selected-ayah-section__tab'));

    expect(tabs).toHaveLength(5);
    expect(tabs.every((tab) => tab.disabled)).toBe(true);
  });

  it('renders real study data and no skeletons when loaded', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: buildAyahStudyViewModel(),
      loadState: IDLE,
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="ayah-study-loading"]')).toBeNull();
    expect(root.querySelector('qd-source-selector')).toBeTruthy();
    expect(root.querySelector('[data-testid="selected-ayah-section-ayah"]')).toBeTruthy();
    expect(root.querySelector('qd-tafsir-card')).toBeTruthy();
    expect(root.querySelectorAll('.qd-skeleton').length).toBe(0);

    const tabs = Array.from(root.querySelectorAll<HTMLButtonElement>('.selected-ayah-section__tab'));
    expect(tabs.every((tab) => !tab.disabled)).toBe(true);
  });

  it('renders tabs above the source selector for source-backed tabs', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: buildAyahStudyViewModel(),
      loadState: IDLE,
      selectedVerseKey: '2:25',
      activeTab: 'full-i3rab',
    });

    const root = fixture.nativeElement as HTMLElement;
    const tabs = root.querySelector('.selected-ayah-section__tabs');
    const source = root.querySelector('.selected-ayah-section__source');

    expect(tabs).toBeTruthy();
    expect(source).toBeTruthy();
    expect(tabs!.compareDocumentPosition(source!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('renders the empty "select an ayah" state when no verse is selected', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: null,
      loadState: IDLE,
      selectedVerseKey: null,
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.qd-empty-state')).toBeTruthy();
    expect(root.querySelector('.qd-skeleton')).toBeNull();
    expect(root.querySelector('[data-testid="ayah-study-loading"]')).toBeNull();
  });

  it('renders the error state and does not hide it behind a skeleton', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: null,
      loadState: { isLoading: false, isEmpty: false, errorMessage: 'تعذّر الاتصال' },
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;
    const error = root.querySelector('[data-testid="ayah-study-error"]');
    expect(error).toBeTruthy();
    expect(error?.textContent).toContain('تعذّر الاتصال');
    expect(root.querySelector('.qd-skeleton')).toBeNull();
    expect(root.querySelector('.selected-ayah-section__tabs')).toBeNull();
  });

  it('renders the failed-to-load empty state without a skeleton', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: null,
      loadState: { isLoading: false, isEmpty: true, errorMessage: null },
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.qd-empty-state')).toBeTruthy();
    expect(root.querySelector('.qd-skeleton')).toBeNull();
    expect(root.querySelector('[data-testid="ayah-study-loading"]')).toBeNull();
  });
});

describe('SelectedAyahSectionComponent — similarity actions (US1)', () => {
  it('renders the two new similarity tabs with count badges when study is loaded', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: buildAyahStudyViewModel(),
      loadState: IDLE,
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="ayah-tab-similar-ayahs"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="ayah-tab-mutashabihat"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="similar-ayah-count"]')?.textContent?.trim()).toBe('2');
    expect(root.querySelector('[data-testid="mutashabihat-group-count"]')?.textContent?.trim()).toBe('2');
  });

  it('does not show similarity count badges while loading', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    setInputs(fixture, {
      study: buildAyahStudyViewModel(),
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedVerseKey: '2:25',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="similar-ayah-count"]')).toBeNull();
    expect(root.querySelector('[data-testid="mutashabihat-group-count"]')).toBeNull();
  });

  it('emits tabChange for the similarity tabs and renders the similar ayahs card only for similar-ayahs', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    const tabChange = vi.fn();
    fixture.componentInstance.tabChange.subscribe(tabChange);

    setInputs(fixture, {
      study: buildAyahStudyViewModel(),
      loadState: IDLE,
      selectedVerseKey: '2:25',
      activeTab: 'similar-ayahs',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('qd-similar-ayahs-card')).toBeTruthy();
    expect(root.querySelector('qd-mutashabihat-groups-card')).toBeNull();

    root.querySelector<HTMLButtonElement>('[data-testid="ayah-tab-mutashabihat"]')?.click();
    expect(tabChange).toHaveBeenCalledWith('mutashabihat');
  });

  it('renders the mutashabihat groups card only for the mutashabihat tab', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);

    setInputs(fixture, {
      study: buildAyahStudyViewModel(),
      loadState: IDLE,
      selectedVerseKey: '2:25',
      activeTab: 'mutashabihat',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('qd-mutashabihat-groups-card')).toBeTruthy();
    expect(root.querySelector('qd-similar-ayahs-card')).toBeNull();
  });

  it('shows zero counts for an ayah without similarity data', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    const study = buildAyahStudyViewModel('1:1');
    study.similaritySummary = ZERO_SIMILARITY_SUMMARY;

    setInputs(fixture, {
      study,
      loadState: IDLE,
      selectedVerseKey: '1:1',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="similar-ayah-count"]')?.textContent?.trim()).toBe('0');
    expect(root.querySelector('[data-testid="mutashabihat-group-count"]')?.textContent?.trim()).toBe('0');
  });
});

describe('SelectedAyahSectionComponent — ayah navigation', () => {
  it('emits ayahNavigate when the selected ayah header is clicked', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    const ayahNavigate = vi.fn();
    fixture.componentInstance.ayahNavigate.subscribe(ayahNavigate);

    setInputs(fixture, {
      study: buildAyahStudyViewModel('2:25'),
      loadState: IDLE,
      selectedVerseKey: '2:25',
    });

    const ayahButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="selected-ayah-section-ayah"]',
    );
    ayahButton?.click();

    expect(ayahNavigate).toHaveBeenCalledWith({
      verseKey: '2:25',
      pageNumber: 5,
    });
  });

  it('emits ayahNavigate when a mutashabihat occurrence text is clicked', () => {
    const fixture = TestBed.createComponent(SelectedAyahSectionComponent);
    const ayahNavigate = vi.fn();
    fixture.componentInstance.ayahNavigate.subscribe(ayahNavigate);

    setInputs(fixture, {
      study: buildAyahStudyViewModel('2:25'),
      loadState: IDLE,
      selectedVerseKey: '2:25',
      activeTab: 'mutashabihat',
      mutashabihat: SAMPLE_MUTASHABIHAT,
      mutashabihatLoadState: IDLE,
    });

    const textButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[data-testid="mutashabihat-occurrence-text"]',
    );
    textButton?.click();

    expect(ayahNavigate).toHaveBeenCalledWith({
      verseKey: '2:25',
      pageNumber: 5,
    });
  });
});
