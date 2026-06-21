import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MutashabihatGroupsCardComponent } from './mutashabihat-groups-card.component';
import { AyahMutashabihatDto, MUTASHABIHAT_EMPTY_MESSAGE, ResourceLoadState } from '../../models/mushaf.models';

const IDLE: ResourceLoadState = { isLoading: false, isEmpty: false, errorMessage: null };

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
          textUthmani: 'نص-آية-محددة',
          phraseTextUthmani: 'عبارة-مجموعة-أولى',
        },
        {
          verseKey: '2:26',
          surahNumber: 2,
          surahNameArabic: 'البقرة',
          ayahNumber: 26,
          pageNumber: 5,
          wordFrom: 1,
          wordTo: 1,
          isSelectedAyah: false,
          isRepresentative: false,
          textUthmani: 'نص-آية-شقيقة',
          phraseTextUthmani: 'كلمة-شقيقة',
        },
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
        {
          verseKey: '2:25',
          surahNumber: 2,
          surahNameArabic: 'البقرة',
          ayahNumber: 25,
          pageNumber: 5,
          wordFrom: 3,
          wordTo: 4,
          isSelectedAyah: true,
          isRepresentative: true,
          textUthmani: 'نص-آية-محددة',
          phraseTextUthmani: 'عبارة-مجموعة-ثانية',
        },
      ],
    },
  ],
};

function render(
  fixture: ComponentFixture<MutashabihatGroupsCardComponent>,
  options: {
    mutashabihat?: AyahMutashabihatDto | null;
    loadState?: ResourceLoadState;
  } = {},
): HTMLElement {
  fixture.componentRef.setInput('mutashabihat', options.mutashabihat ?? null);
  fixture.componentRef.setInput('loadState', options.loadState ?? IDLE);
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

  it('renders separate group sections with selected-occurrence labels and occurrence lists', () => {
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

    const selectedBadges = root.querySelectorAll('[data-testid="mutashabihat-selected-badge"]');
    expect(selectedBadges.length).toBeGreaterThan(0);
    expect(Array.from(selectedBadges).every((node) => node.textContent?.trim() === 'الآية المحددة')).toBe(true);

    const firstGroupOccurrences = groups[0].querySelectorAll('[data-testid="mutashabihat-occurrence"]');
    expect(firstGroupOccurrences).toHaveLength(2);

    const references = Array.from(
      groups[0].querySelectorAll('[data-testid="mutashabihat-occurrence-reference"]'),
    ).map((node) => node.textContent?.trim());
    expect(references).toEqual(['البقرة — 25', 'البقرة — 26']);

    const selectedOccurrenceLabel = groups[0].querySelector('[data-testid="mutashabihat-occurrence-selected-label"]');
    expect(selectedOccurrenceLabel?.textContent?.trim()).toBe('الآية المحددة');

    const pageContexts = Array.from(
      groups[0].querySelectorAll('[data-testid="mutashabihat-occurrence-page"]'),
    ).map((node) => node.textContent?.trim());
    expect(pageContexts).toEqual(['صفحة 5', 'صفحة 5']);

    const selectedRanges = Array.from(groups[0].querySelectorAll('.mutashabihat-groups-card__selected-range')).map(
      (node) => node.textContent?.trim(),
    );
    expect(selectedRanges).toEqual(['كلمات 1–2']);
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
              {
                verseKey: '2:25',
                surahNumber: 2,
                surahNameArabic: 'البقرة',
                ayahNumber: 25,
                pageNumber: 5,
                wordFrom: 3,
                wordTo: 5,
                isSelectedAyah: true,
                isRepresentative: true,
                textUthmani: 'نص-آية-محددة',
                phraseTextUthmani: null,
              },
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
