import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MutashabihatGroupsCardComponent } from './mutashabihat-groups-card.component';
import {
  AyahMutashabihatDto,
  MutashabihatOccurrenceDto,
  MUTASHABIHAT_EMPTY_MESSAGE,
  ResourceLoadState,
} from '../../models/mushaf.models';

const IDLE: ResourceLoadState = { isLoading: false, isEmpty: false, errorMessage: null };

function buildOccurrence(
  verseKey: string,
  ayahNumber: number,
  overrides: Partial<MutashabihatOccurrenceDto> = {},
): MutashabihatOccurrenceDto {
  return {
    verseKey,
    surahNumber: 2,
    surahNameArabic: 'البقرة',
    ayahNumber,
    pageNumber: 5,
    wordFrom: 1,
    wordTo: 2,
    isSelectedAyah: false,
    isRepresentative: false,
    textUthmani: `نص-آية-${ayahNumber}`,
    phraseTextUthmani: `عبارة-${ayahNumber}`,
    ...overrides,
  };
}

const SAMPLE_MUTASHABIHAT: AyahMutashabihatDto = {
  verseKey: '2:25',
  groupCount: 2,
  groups: [
    {
      groupKey: 'mutashabihat:90001',
      sourceGroupId: 90001,
      representativeVerseKey: '2:25',
      representativeWordFrom: 1,
      representativeWordTo: 2,
      phraseTextUthmani: 'عبارة-مجموعة-أولى',
      occurrenceCount: 2,
      distinctAyahCount: 2,
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
        buildOccurrence('2:25', 25, {
          isSelectedAyah: true,
          isRepresentative: true,
          textUthmani: 'نص يحتوي عبارة-مجموعة-أولى في الآية',
          phraseTextUthmani: 'عبارة-مجموعة-أولى',
        }),
        buildOccurrence('2:26', 26, {
          textUthmani: 'نص يحتوي كلمة-شقيقة في الآية',
          phraseTextUthmani: 'كلمة-شقيقة',
        }),
      ],
    },
    {
      groupKey: 'mutashabihat:90002',
      sourceGroupId: 90002,
      representativeVerseKey: '2:25',
      representativeWordFrom: 3,
      representativeWordTo: 4,
      phraseTextUthmani: 'عبارة-مجموعة-ثانية',
      occurrenceCount: 1,
      distinctAyahCount: 1,
      distinctSurahCount: 1,
      selectedOccurrences: [
        {
          verseKey: '2:25',
          wordFrom: 3,
          wordTo: 4,
          isRepresentative: true,
          phraseTextUthmani: 'عبارة-مجموعة-ثانية',
        },
      ],
      occurrences: [
        buildOccurrence('2:25', 25, {
          isSelectedAyah: true,
          isRepresentative: true,
          textUthmani: 'نص يحتوي عبارة-مجموعة-ثانية في الآية',
          phraseTextUthmani: 'عبارة-مجموعة-ثانية',
        }),
      ],
    },
  ],
};

function buildLargeGroup(): AyahMutashabihatDto {
  const occurrences = Array.from({ length: 7 }, (_, index) =>
    buildOccurrence(`2:${30 + index}`, 30 + index, {
      isSelectedAyah: index === 0,
      pageNumber: 6 + index,
    }),
  );

  return {
    verseKey: '2:25',
    groupCount: 1,
    groups: [
      {
        groupKey: 'mutashabihat:90010',
        sourceGroupId: 90010,
        representativeVerseKey: '2:30',
        representativeWordFrom: 1,
        representativeWordTo: 2,
        phraseTextUthmani: 'عبارة-مجموعة-كبيرة',
        occurrenceCount: 7,
        distinctAyahCount: 7,
        distinctSurahCount: 1,
        selectedOccurrences: [
          {
            verseKey: '2:30',
            wordFrom: 1,
            wordTo: 2,
            isRepresentative: true,
            phraseTextUthmani: 'عبارة-مجموعة-كبيرة',
          },
        ],
        occurrences,
      },
    ],
  };
}

function buildLargeGroupWithLateSelectedAyah(): AyahMutashabihatDto {
  const occurrences = Array.from({ length: 7 }, (_, index) =>
    buildOccurrence(`2:${30 + index}`, 30 + index, {
      isSelectedAyah: index === 6,
      pageNumber: 6 + index,
    }),
  );

  return {
    verseKey: '2:25',
    groupCount: 1,
    groups: [
      {
        groupKey: 'mutashabihat:90011',
        sourceGroupId: 90011,
        representativeVerseKey: '2:30',
        representativeWordFrom: 1,
        representativeWordTo: 2,
        phraseTextUthmani: 'عبارة-مجموعة-متأخرة',
        occurrenceCount: 7,
        distinctAyahCount: 7,
        distinctSurahCount: 1,
        selectedOccurrences: [
          {
            verseKey: '2:36',
            wordFrom: 1,
            wordTo: 2,
            isRepresentative: false,
            phraseTextUthmani: 'عبارة-مجموعة-متأخرة',
          },
        ],
        occurrences,
      },
    ],
  };
};

const LOADING: ResourceLoadState = { isLoading: true, isEmpty: false, errorMessage: null };

function render(
  fixture: ComponentFixture<MutashabihatGroupsCardComponent>,
  options: {
    mutashabihat?: AyahMutashabihatDto | null;
    loadState?: ResourceLoadState;
    expectedGroupCount?: number | null;
    expectedOccurrenceCount?: number | null;
  } = {},
): HTMLElement {
  fixture.componentRef.setInput('mutashabihat', options.mutashabihat ?? null);
  fixture.componentRef.setInput('loadState', options.loadState ?? IDLE);
  fixture.componentRef.setInput('expectedGroupCount', options.expectedGroupCount ?? null);
  fixture.componentRef.setInput('expectedOccurrenceCount', options.expectedOccurrenceCount ?? null);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('MutashabihatGroupsCardComponent (US3)', () => {
  it('shows the Arabic loading state while mutashabihat are loading', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const root = render(fixture, {
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
    });

    expect(root.querySelector('[data-testid="mutashabihat-loading"]')?.textContent?.trim()).toContain(
      'جارٍ تحميل المتشابهات اللفظية',
    );
    expect(root.querySelector('[data-testid="mutashabihat-groups-list"]')).toBeNull();
  });

  it('shows the Arabic empty state when there are zero groups', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const root = render(fixture, {
      mutashabihat: { verseKey: '1:1', groupCount: 0, groups: [] },
      loadState: IDLE,
    });

    expect(root.querySelector('[data-testid="mutashabihat-empty"]')?.textContent?.trim()).toBe(
      MUTASHABIHAT_EMPTY_MESSAGE,
    );
  });

  it('renders separate group sections with selected-occurrence labels and numbered occurrence lists', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const root = render(fixture, {
      mutashabihat: SAMPLE_MUTASHABIHAT,
      loadState: IDLE,
    });

    const groups = root.querySelectorAll('[data-testid="mutashabihat-group"]');
    expect(groups).toHaveLength(2);

    const phrases = Array.from(root.querySelectorAll('[data-testid="mutashabihat-group-title"] .mutashabihat-groups-card__phrase')).map(
      (node) => node.textContent?.trim(),
    );
    expect(phrases).toEqual(['عبارة-مجموعة-أولى', 'عبارة-مجموعة-ثانية']);

    const firstGroupOccurrences = groups[0].querySelectorAll('[data-testid="mutashabihat-occurrence"]');
    expect(firstGroupOccurrences).toHaveLength(2);

    const indexes = Array.from(
      groups[0].querySelectorAll('[data-testid="mutashabihat-occurrence-index"]'),
    ).map((node) => node.textContent?.trim());
    expect(indexes).toEqual(['1', '2']);

    const references = Array.from(
      groups[0].querySelectorAll('[data-testid="mutashabihat-occurrence-reference"]'),
    ).map((node) => node.textContent?.trim());
    expect(references).toEqual(['البقرة — 25', 'البقرة — 26']);

    const selectedOccurrenceLabel = groups[0].querySelector('[data-testid="mutashabihat-occurrence-selected-label"]');
    expect(selectedOccurrenceLabel?.textContent?.trim()).toBe('الآية المحددة');

    expect(groups[0].querySelector('[data-testid="mutashabihat-occurrence-phrase"]')).toBeNull();
  });

  it('shows only the first five occurrences with an expand toggle for larger groups', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const root = render(fixture, {
      mutashabihat: buildLargeGroup(),
      loadState: IDLE,
    });

    const group = root.querySelector('[data-testid="mutashabihat-group"]') as HTMLElement;
    expect(group.querySelectorAll('[data-testid="mutashabihat-occurrence"]')).toHaveLength(5);

    const expandToggle = group.querySelector('[data-testid="mutashabihat-expand-toggle"]') as HTMLButtonElement;
    expect(expandToggle.getAttribute('aria-expanded')).toBe('false');
    expect(expandToggle.textContent?.trim()).toBe('عرض الكل (2 آية أخرى)');

    expandToggle.click();
    fixture.detectChanges();

    expect(expandToggle.getAttribute('aria-expanded')).toBe('true');
    expect(group.querySelectorAll('[data-testid="mutashabihat-occurrence"]')).toHaveLength(7);
    expect(expandToggle.textContent?.trim()).toBe('عرض أقل');

    expandToggle.click();
    fixture.detectChanges();

    expect(expandToggle.getAttribute('aria-expanded')).toBe('false');
    expect(group.querySelectorAll('[data-testid="mutashabihat-occurrence"]')).toHaveLength(5);
  });

  it('keeps the selected ayah occurrence visible in the collapsed preview when it is beyond the first five', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const root = render(fixture, {
      mutashabihat: buildLargeGroupWithLateSelectedAyah(),
      loadState: IDLE,
    });

    const group = root.querySelector('[data-testid="mutashabihat-group"]') as HTMLElement;
    expect(group.querySelectorAll('[data-testid="mutashabihat-occurrence"]')).toHaveLength(6);

    const expandToggle = group.querySelector('[data-testid="mutashabihat-expand-toggle"]') as HTMLButtonElement;
    expect(expandToggle.textContent?.trim()).toBe('عرض الكل (1 آية أخرى)');
    expect(group.querySelector('[data-testid="mutashabihat-occurrence-selected-label"]')).toBeTruthy();
  });

  it('emits ayahNavigate when an occurrence text button is clicked', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const ayahNavigate = vi.fn();
    fixture.componentInstance.ayahNavigate.subscribe(ayahNavigate);

    const root = render(fixture, {
      mutashabihat: SAMPLE_MUTASHABIHAT,
      loadState: IDLE,
    });

    const textButton = root.querySelector('[data-testid="mutashabihat-occurrence-text"]') as HTMLButtonElement;
    textButton.click();

    expect(ayahNavigate).toHaveBeenCalledWith({
      verseKey: '2:25',
      pageNumber: 5,
    });
  });

  it('falls back to word-range labels when phrase text is unavailable', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const root = render(fixture, {
      mutashabihat: {
        verseKey: '2:25',
        groupCount: 1,
        groups: [
          {
            groupKey: 'mutashabihat:90003',
            sourceGroupId: 90003,
            representativeVerseKey: '2:25',
            representativeWordFrom: 3,
            representativeWordTo: 5,
            phraseTextUthmani: null,
            occurrenceCount: 1,
            distinctAyahCount: 1,
            distinctSurahCount: 1,
            selectedOccurrences: [
              {
                verseKey: '2:25',
                wordFrom: 3,
                wordTo: 5,
                isRepresentative: true,
                phraseTextUthmani: null,
              },
            ],
            occurrences: [
              buildOccurrence('2:25', 25, {
                wordFrom: 3,
                wordTo: 5,
                isSelectedAyah: true,
                isRepresentative: true,
                textUthmani: 'نص-آية-محددة',
                phraseTextUthmani: null,
              }),
            ],
          },
        ],
      },
      loadState: IDLE,
    });

    const groupTitle = root.querySelector('[data-testid="mutashabihat-group-title"]');
    expect(groupTitle?.textContent).toContain('كلمات 3–5');
    expect(groupTitle?.textContent).toContain('2:25');
    expect(root.querySelector('.mutashabihat-groups-card__phrase')).toBeNull();
  });

  describe('loading placeholders mirror the loaded groups (N3 row 12)', () => {
    it('renders one group-shaped placeholder per group the summary says is coming', () => {
      const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const root = render(fixture, {
        loadState: LOADING,
        expectedGroupCount: 3,
        expectedOccurrenceCount: 6,
      });

      expect(
        root.querySelectorAll('[data-testid="mutashabihat-skeleton"] .mutashabihat-groups-card__group'),
      ).toHaveLength(3);
    });

    it('sizes each placeholder group by the summary\'s own occurrences-per-group average', () => {
      const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const root = render(fixture, {
        loadState: LOADING,
        expectedGroupCount: 2,
        expectedOccurrenceCount: 6,
      });

      const groups = root.querySelectorAll('[data-testid="mutashabihat-skeleton"] .mutashabihat-groups-card__group');
      expect(groups).toHaveLength(2);
      for (const group of groups) {
        expect(group.querySelectorAll('.qd-ayah-card')).toHaveLength(3);
      }
    });

    it('clamps placeholder occurrences to the preview count a loaded group collapses to', () => {
      const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const root = render(fixture, {
        loadState: LOADING,
        expectedGroupCount: 1,
        expectedOccurrenceCount: 40,
      });

      expect(
        root.querySelectorAll('[data-testid="mutashabihat-skeleton"] .qd-ayah-card'),
      ).toHaveLength(5);
    });

    it('builds each placeholder from the same frames and line boxes as a loaded group', () => {
      const loadingFixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const loadingRoot = render(loadingFixture, {
        loadState: LOADING,
        expectedGroupCount: 1,
        expectedOccurrenceCount: 2,
      });

      const loadedFixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const loadedRoot = render(loadedFixture, { mutashabihat: SAMPLE_MUTASHABIHAT, loadState: IDLE });

      expect(loadingRoot.querySelector('[data-testid="mutashabihat-skeleton"]')?.classList).toContain(
        'mutashabihat-groups-card__groups',
      );

      const placeholderGroup = loadingRoot.querySelector('.mutashabihat-groups-card__group');
      const loadedGroup = loadedRoot.querySelector('[data-testid="mutashabihat-group"]');
      expect(loadedGroup?.classList).toContain('qd-card');
      expect(placeholderGroup?.classList).toContain('qd-card');

      expect(placeholderGroup?.querySelector('.mutashabihat-groups-card__group-header')).toBeTruthy();
      expect(placeholderGroup?.querySelector('.mutashabihat-groups-card__group-title')).toBeTruthy();
      expect(placeholderGroup?.querySelector('.mutashabihat-groups-card__group-meta')).toBeTruthy();
      expect(placeholderGroup?.querySelector('.mutashabihat-groups-card__occurrences')).toBeTruthy();

      const placeholderOccurrence = placeholderGroup?.querySelector('.qd-ayah-card');
      expect(placeholderOccurrence?.classList).toContain('mutashabihat-groups-card__occurrence');
      expect(placeholderOccurrence?.querySelector('.mutashabihat-groups-card__occurrence-meta')).toBeTruthy();
      expect(placeholderOccurrence?.querySelector('.mutashabihat-groups-card__text')).toBeTruthy();
    });

    it('falls back to fixed placeholder counts when no summary counts are known yet', () => {
      const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const root = render(fixture, {
        loadState: LOADING,
        expectedGroupCount: null,
        expectedOccurrenceCount: null,
      });

      const groups = root.querySelectorAll('[data-testid="mutashabihat-skeleton"] .mutashabihat-groups-card__group');
      expect(groups).toHaveLength(2);
      for (const group of groups) {
        expect(group.querySelectorAll('.qd-ayah-card')).toHaveLength(2);
      }
    });

    it('reserves nothing when the summary already says there are no groups', () => {
      const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const root = render(fixture, {
        loadState: LOADING,
        expectedGroupCount: 0,
        expectedOccurrenceCount: 0,
      });

      expect(
        root.querySelectorAll('[data-testid="mutashabihat-skeleton"] .mutashabihat-groups-card__group'),
      ).toHaveLength(0);
      expect(root.querySelectorAll('[data-testid="mutashabihat-skeleton"] .qd-ayah-card')).toHaveLength(0);
    });

    it('caps the placeholder run so a very long group list cannot reserve screens of shimmer', () => {
      const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const root = render(fixture, {
        loadState: LOADING,
        expectedGroupCount: 30,
        expectedOccurrenceCount: 60,
      });

      expect(
        root.querySelectorAll('[data-testid="mutashabihat-skeleton"] .mutashabihat-groups-card__group'),
      ).toHaveLength(4);
    });

    it('keeps the sr-only status announcement and hides the placeholder run from a11y', () => {
      const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
      const root = render(fixture, { loadState: LOADING, expectedGroupCount: 2, expectedOccurrenceCount: 4 });

      const status = root.querySelector('[data-testid="mutashabihat-loading"]');
      expect(status?.getAttribute('role')).toBe('status');

      const skeleton = root.querySelector('[data-testid="mutashabihat-skeleton"]');
      expect(skeleton?.getAttribute('aria-hidden')).toBe('true');
      expect(skeleton?.hasAttribute('aria-busy')).toBe(false);
    });
  });

  it('shows the scoped error state when mutashabihat loading fails', () => {
    const fixture = TestBed.createComponent(MutashabihatGroupsCardComponent);
    const root = render(fixture, {
      loadState: {
        isLoading: false,
        isEmpty: true,
        errorMessage: 'تعذّر تحميل المتشابهات اللفظية.',
      },
    });

    expect(root.querySelector('[data-testid="mutashabihat-error"]')?.textContent?.trim()).toBe(
      'تعذّر تحميل المتشابهات اللفظية.',
    );
  });
});
