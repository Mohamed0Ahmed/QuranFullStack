import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectedAyahSectionComponent } from './selected-ayah-section.component';
import {
  AyahStudyTab,
  AyahStudyViewModel,
  ResourceLoadState,
  SourceOption,
} from '../../models/mushaf.models';

/*
 * Source-safe synthetic placeholders — not Quranic text. Mirrors the
 * placeholders used in mushaf-reader.facade.ayah-study.spec.ts.
 */
const AYAH_TEXT_PLACEHOLDER = 'نص تجريبي للآية';

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
  };
}

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

    // Static structure mounted: source slot, tabs, content region.
    expect(root.querySelector('.selected-ayah-section__source')).toBeTruthy();
    expect(root.querySelector('.selected-ayah-section__tabs')).toBeTruthy();
    expect(root.querySelectorAll('.selected-ayah-section__tab')).toHaveLength(3);
    expect(root.querySelector('.selected-ayah-section__content')).toBeTruthy();

    // Loading content is several stacked shimmer lines, not one giant block.
    const contentSkeleton = root.querySelector('[data-testid="ayah-content-skeleton"]');
    expect(contentSkeleton).toBeTruthy();
    expect(contentSkeleton?.querySelectorAll('.qd-skeleton--text').length).toBeGreaterThanOrEqual(3);
    expect(root.querySelector('.qd-loading-overlay')).toBeNull();

    // No real study content while loading.
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

    // No single covering overlay block; structured stacked lines instead.
    expect(root.querySelector('.qd-loading-overlay')).toBeNull();
    expect(root.querySelector('[data-testid="ayah-content-skeleton"]')).toBeTruthy();

    // The real study content is not mounted during loading.
    expect(root.querySelector('qd-tafsir-card')).toBeNull();
    expect(root.querySelector('[data-testid="selected-ayah-section-ayah"]')).toBeNull();

    // Tabs stay mounted and disabled while loading.
    const tabs = Array.from(root.querySelectorAll<HTMLButtonElement>('.selected-ayah-section__tab'));
    expect(tabs).toHaveLength(3);
    expect(tabs.every((tab) => tab.disabled)).toBe(true);

    // Source slot shows a skeleton placeholder (not the live selector).
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

    // Static label stays (it does not depend on the loaded study).
    const label = root.querySelector('.selected-ayah-section__source-loading-label');
    expect(label?.textContent?.trim()).toBe('مصدر التفسير');

    // Only the value shimmers; the live selector is not mounted.
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
    // Actions may be disabled while loading but must not disappear.
    expect(tabs).toHaveLength(3);
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
    // Tabs are interactive again once loaded.
    const tabs = Array.from(root.querySelectorAll<HTMLButtonElement>('.selected-ayah-section__tab'));
    expect(tabs.every((tab) => !tab.disabled)).toBe(true);
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
