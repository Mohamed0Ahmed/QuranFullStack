import { describe, expect, it, vi } from 'vitest';
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

const LOADING: ResourceLoadState = { isLoading: true, isEmpty: false, errorMessage: null };

function render(
  fixture: ComponentFixture<SimilarAyahsCardComponent>,
  options: {
    similarAyahs?: typeof SAMPLE_SIMILAR_AYAHS | null;
    loadState?: ResourceLoadState;
    expectedItemCount?: number | null;
  } = {},
): HTMLElement {
  fixture.componentRef.setInput('similarAyahs', options.similarAyahs ?? null);
  fixture.componentRef.setInput(
    'loadState',
    options.loadState ?? IDLE,
  );
  fixture.componentRef.setInput('expectedItemCount', options.expectedItemCount ?? null);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

function skeletonRows(root: HTMLElement): NodeListOf<Element> {
  return root.querySelectorAll('[data-testid="similar-ayahs-skeleton"] li');
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

    expect(root.querySelector('.similar-ayahs-card__verse-key')).toBeNull();

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

  it('emits ayahNavigate when a similar ayah text is clicked', () => {
    const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
    const ayahNavigate = vi.fn();
    fixture.componentInstance.ayahNavigate.subscribe(ayahNavigate);

    const root = render(fixture, {
      similarAyahs: SAMPLE_SIMILAR_AYAHS,
      loadState: IDLE,
    });

    const firstTextButton = root.querySelector('[data-testid="similar-ayah-text"]') as HTMLButtonElement;
    firstTextButton.click();

    expect(ayahNavigate).toHaveBeenCalledWith({
      verseKey: '2:26',
      pageNumber: 5,
    });
  });

  describe('loading placeholders mirror the loaded list (N3 row 11)', () => {
    it('reserves one placeholder row per item the summary says is coming', () => {
      const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
      const root = render(fixture, { loadState: LOADING, expectedItemCount: 5 });

      expect(skeletonRows(root)).toHaveLength(5);
    });

    it('reserves one shimmering row per incoming item, matching the loaded list length', () => {
      const loadingFixture = TestBed.createComponent(SimilarAyahsCardComponent);
      const loadingRoot = render(loadingFixture, {
        loadState: LOADING,
        expectedItemCount: SAMPLE_SIMILAR_AYAHS.items.length,
      });

      const loadedFixture = TestBed.createComponent(SimilarAyahsCardComponent);
      const loadedRoot = render(loadedFixture, { similarAyahs: SAMPLE_SIMILAR_AYAHS, loadState: IDLE });

      const placeholders = skeletonRows(loadingRoot);
      const loadedItems = loadedRoot.querySelectorAll('[data-testid="similar-ayah-item"]');

      expect(placeholders).toHaveLength(loadedItems.length);
      for (const placeholder of Array.from(placeholders)) {
        expect(placeholder.querySelector('.qd-skeleton')).not.toBeNull();
      }
    });

    it('falls back to a fixed placeholder count when no summary count is known yet', () => {
      const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
      const root = render(fixture, { loadState: LOADING, expectedItemCount: null });

      expect(skeletonRows(root)).toHaveLength(3);
    });

    it('reserves nothing when the summary already says the list is empty', () => {
      const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
      const root = render(fixture, { loadState: LOADING, expectedItemCount: 0 });

      expect(skeletonRows(root)).toHaveLength(0);
    });

    it('caps the placeholder run so a very long list cannot reserve screens of shimmer', () => {
      const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
      const root = render(fixture, { loadState: LOADING, expectedItemCount: 40 });

      expect(skeletonRows(root)).toHaveLength(8);
    });

    it('keeps the sr-only status announcement and hides the placeholder run from a11y', () => {
      const fixture = TestBed.createComponent(SimilarAyahsCardComponent);
      const root = render(fixture, { loadState: LOADING, expectedItemCount: 2 });

      const status = root.querySelector('[data-testid="similar-ayahs-loading"]');
      expect(status?.getAttribute('role')).toBe('status');

      const skeleton = root.querySelector('[data-testid="similar-ayahs-skeleton"]');
      expect(skeleton?.getAttribute('aria-hidden')).toBe('true');
      expect(skeleton?.hasAttribute('aria-busy')).toBe(false);
    });
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
