import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SimilarAyahsCardComponent } from './similar-ayahs-card.component';
import { ResourceLoadState, SIMILAR_AYAHS_EMPTY_MESSAGE } from '../../models/mushaf.models';

const IDLE: ResourceLoadState = { isLoading: false, isEmpty: false, errorMessage: null };

const SAMPLE_SIMILAR_AYAHS = {
  verseKey: '2:25',
  count: 2,
  items: [
    {
      targetVerseKey: '2:26',
      surahNumber: 2,
      surahNameArabic: 'البقرة',
      ayahNumber: 26,
      pageNumber: 5,
      juzNumber: 1,
      hizbNumber: 1,
      rubNumber: 2,
      textUthmani: 'نص مرتبط أول',
      score: 80,
      coverage: 90,
      matchedWordsCount: 3,
      relationshipDirection: 'bidirectional' as const,
      hasReverseLink: true,
    },
    {
      targetVerseKey: '1:2',
      surahNumber: 1,
      surahNameArabic: 'الفاتحة',
      ayahNumber: 2,
      pageNumber: 1,
      juzNumber: 1,
      hizbNumber: 1,
      rubNumber: 1,
      textUthmani: 'نص مرتبط ثان',
      score: 70,
      coverage: 85,
      matchedWordsCount: 2,
      relationshipDirection: 'incoming' as const,
      hasReverseLink: false,
    },
  ],
};

function render(
  fixture: ComponentFixture<SimilarAyahsCardComponent>,
  options: {
    similarAyahs?: typeof SAMPLE_SIMILAR_AYAHS | null;
    loadState?: ResourceLoadState;
  } = {},
): HTMLElement {
  fixture.componentRef.setInput('similarAyahs', options.similarAyahs ?? null);
  fixture.componentRef.setInput(
    'loadState',
    options.loadState ?? IDLE,
  );
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('SimilarAyahsCardComponent (US2)', () => {
  it('shows the Arabic loading state while similar ayahs are loading', () => {
    const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
    const root = render(fixture, {
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
    });

    expect(root.querySelector('[data-testid="similar-ayahs-loading"]')?.textContent?.trim()).toContain(
      'جارٍ تحميل الآيات القريبة',
    );
    expect(root.querySelector('[data-testid="similar-ayahs-list"]')).toBeNull();
  });

  it('shows the Arabic empty state when the flat list has zero items', () => {
    const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
    const root = render(fixture, {
      similarAyahs: { verseKey: '1:1', count: 0, items: [] },
      loadState: IDLE,
    });

    expect(root.querySelector('[data-testid="similar-ayahs-empty"]')?.textContent?.trim()).toBe(
      SIMILAR_AYAHS_EMPTY_MESSAGE,
    );
  });

  it('renders a flat deduplicated list with ayah reference, page context, and text', () => {
    const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
    const root = render(fixture, {
      similarAyahs: SAMPLE_SIMILAR_AYAHS,
      loadState: IDLE,
    });

    const items = root.querySelectorAll('[data-testid="similar-ayah-item"]');
    expect(items).toHaveLength(2);

    const references = Array.from(root.querySelectorAll('[data-testid="similar-ayah-reference"]')).map(
      (node) => node.textContent?.trim(),
    );
    expect(references).toEqual(['البقرة — 26', 'الفاتحة — 2']);

    const pageContexts = Array.from(
      root.querySelectorAll('[data-testid="similar-ayah-page-context"]'),
    ).map((node) => node.textContent?.trim());
    expect(pageContexts).toEqual(['صفحة 5', 'صفحة 1']);

    const texts = Array.from(root.querySelectorAll('[data-testid="similar-ayah-text"]')).map((node) =>
      node.textContent?.trim(),
    );
    expect(texts).toEqual(['نص مرتبط أول', 'نص مرتبط ثان']);
  });

  it('shows the scoped error state when similar ayahs loading fails', () => {
    const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
    const root = render(fixture, {
      loadState: {
        isLoading: false,
        isEmpty: true,
        errorMessage: 'تعذّر تحميل الآيات القريبة في المعنى.',
      },
    });

    expect(root.querySelector('[data-testid="similar-ayahs-error"]')?.textContent?.trim()).toBe(
      'تعذّر تحميل الآيات القريبة في المعنى.',
    );
  });
});
